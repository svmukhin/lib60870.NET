using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using lib60870.CS101;
using System.Threading.Tasks;

namespace lib60870.CS104
{
    public partial class Session : IMaster
    {
        public event EventHandler<MessageReceiveSendEventArgs> MessageReceived;
        public event EventHandler<MessageReceiveSendEventArgs> MessageSent;
        public event EventHandler<SessionConnetionChangedEventArgs> SessionConnetionChanged;
        public event EventHandler<AsduReceivedEventArgs> AsduReceived;

        private Session(
            string hostname,
            int tcpPort,
            APCIParameters apciParameters,
            ApplicationLayerParameters alParameters)
        {
            _hostname = hostname;
            _alParameters = alParameters;
            _apciParameters = apciParameters;
            _tcpPort = tcpPort;
            _connectTimeoutInMs = apciParameters.T0 * 1000;
        }

        public Session(string hostname, int tcpPort = 2404)
            : this(hostname, tcpPort, new APCIParameters(), new ApplicationLayerParameters()) { }

        public Session(string hostname, APCIParameters apciParameters, ApplicationLayerParameters alParameters)
            : this(hostname, 2404, apciParameters.Clone(), alParameters.Clone()) { }

        private int _sendSequenceNumber;
        private int _receiveSequenceNumber;

        private UInt64 _uMessageTimeout;

        private int _maxSentASDUs;
        private KBuffer _kBuffer;

        private UInt64 _nextT3Timeout;
        private int _outStandingTestFRConMessages = 0;

        private int _unconfirmedReceivedIMessages;
        private long _lastConfirmationTime;
        private bool _timeoutT2Triggered;

        private Socket _socket;

        private FileClient _fileClient;

        private string _hostname;
        private int _tcpPort;

        private APCIParameters _apciParameters;
        private ApplicationLayerParameters _alParameters;

        private int _connectTimeoutInMs = 1000;
        private int _receiveTimeoutInMs = 1000;

        private ReadState _readState;
        private int _currentReadPos;
        private int _currentReadMsgLength;
        private int _remainingReadLength;
        private long _currentReadTime;

        private CancellationTokenSource _cts;
        private Task _readingTask;

        public int SendSequenceNumber
        {
            get => _sendSequenceNumber;
            set => _sendSequenceNumber = value;
        }

        public int ReceiveSequenceNumber
        {
            get => _receiveSequenceNumber;
            set => _receiveSequenceNumber = value;
        }

        public ApplicationLayerParameters Parameters => _alParameters;

        public int ConnectTimeout
        {
            get => _connectTimeoutInMs;
            set => _connectTimeoutInMs = value;
        }
        public int ReceiveTimeout
        {
            get => _receiveTimeoutInMs;
            set => _receiveTimeoutInMs = value;
        }
        public bool Connected => _socket.Connected;

        private void ResetConnection()
        {
            _sendSequenceNumber = 0;
            _receiveSequenceNumber = 0;
            _unconfirmedReceivedIMessages = 0;
            _lastConfirmationTime = System.Int64.MaxValue;
            _timeoutT2Triggered = false;
            _outStandingTestFRConMessages = 0;

            _uMessageTimeout = 0;

            _maxSentASDUs = _apciParameters.K;
            _kBuffer = new KBuffer(_maxSentASDUs);
        }

        private void Send(byte[] buffer, int offset, int count)
        {
            if (!_socket.Connected || _socket == null)
                throw new ConnectionException("not connected", new SocketException(10057));

            try
            {
                _socket.Send(buffer, offset, count, SocketFlags.None);
            }
            catch (Exception ex)
            {
                throw new ConnectionException("Failed to write to socket", ex);
            }

            OnMessageSent(buffer, offset, count);
        }

        public void Send(ASDU asdu)
        {
            if (_kBuffer.IsFull)
                throw new ConnectionException("Flow control congestion. Try again later.");

            Send(new FormatIMessage(asdu, _sendSequenceNumber, _receiveSequenceNumber, _alParameters));

            _kBuffer.Add(_sendSequenceNumber);
            _sendSequenceNumber = (_sendSequenceNumber + 1) % 32768;
        }

        public void Send(IMessage message)
        {
            var buffer = message.Encode();
            Send(buffer, 0, buffer.Length);

            _unconfirmedReceivedIMessages = 0;
            _timeoutT2Triggered = false;

            OnMessageSent(buffer, 0, buffer.Length);
        }

        public void SendCommand(CauseOfTransmission cot, int ca, InformationObject informationObject)
        {
            ASDU command = new ASDU(_alParameters, cot, false, false, (byte)_alParameters.OA, ca, false);
            command.AddInformationObject(informationObject);

            Send(command);
        }

        public async Task SendAsync(ASDU asdu)
        {
            if (_kBuffer.IsFull)
                throw new ConnectionException("Flow control congestion. Try again later.");

            await SendAsync(new FormatIMessage(asdu, _sendSequenceNumber, _receiveSequenceNumber, _alParameters));

            _kBuffer.Add(_sendSequenceNumber);
            _sendSequenceNumber = (_sendSequenceNumber + 1) % 32768;
        }

        private async Task SendAsync(byte[] buffer, int offset, int count)
        {
            if (!_socket.Connected || _socket == null)
                throw new ConnectionException("not connected", new SocketException(10057));

            try
            {
                await _socket.SendAsync(new ArraySegment<byte>(buffer, offset, count), SocketFlags.None);
            }
            catch (Exception ex)
            {
                throw new ConnectionException("Failed to write to socket", ex);
            }

            OnMessageSent(buffer, offset, count);
        }        

        public async Task SendAsync(IMessage message)
        {
            var buffer = message.Encode();
            await SendAsync(buffer, 0, buffer.Length);

            _unconfirmedReceivedIMessages = 0;
            _timeoutT2Triggered = false;

            OnMessageSent(buffer, 0, buffer.Length);
        }

        public async Task SendCommandAsync(CauseOfTransmission cot, int ca, InformationObject informationObject)
        {
            ASDU command = new ASDU(_alParameters, cot, false, false, (byte)_alParameters.OA, ca, false);
            command.AddInformationObject(informationObject);

            await SendAsync(command);
        }

        public ApplicationLayerParameters GetApplicationLayerParameters()
        {
            return _alParameters;
        }

        private void ResetT3Timeout()
        {
            _nextT3Timeout = (UInt64)SystemUtils.currentTimeMillis() + (UInt64)(_apciParameters.T3 * 1000);
        }

        public async Task ConnectAsync()
        {
            ResetConnection();
            ResetT3Timeout();

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await _socket.ConnectAsync(IPAddress.Parse(_hostname), _tcpPort);

            OnSessionConnectionChanged(ConnectionEvent.Opened);

            _cts = new CancellationTokenSource();
            _readingTask = StartMessageReadingAsync(_cts.Token);
        }        

        public async Task StartMessageReadingAsync(CancellationToken stoppingToken)
        {
            var buffer = new byte[300];
            int bytesReceived;
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (_readState != ReadState.Idle)
                    {
                        if (SystemUtils.currentTimeMillis() > _currentReadTime)
                            throw new Exception($"Reading timed out.");
                    }

                    switch (_readState)
                    {
                        case ReadState.Idle:
                            bytesReceived = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer, 0, 1), SocketFlags.None);

                            if (bytesReceived != 1)
                                throw new Exception("Error while read");

                            if (buffer[0] != 0x68)
                                throw new Exception("Missing SOF indicator!");

                            _readState = ReadState.StartReceived;
                            _currentReadTime = SystemUtils.currentTimeMillis() + _receiveTimeoutInMs;
                            break;
                        case ReadState.StartReceived:
                            bytesReceived = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer, 1, 1), SocketFlags.None);

                            if (bytesReceived != 1)
                                break;

                            _currentReadMsgLength = buffer[1];
                            _remainingReadLength = _currentReadMsgLength;
                            _currentReadPos = 2;

                            _readState = ReadState.ReadingMessage;
                            break;
                        case ReadState.ReadingMessage:
                            bytesReceived = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer, _currentReadPos, _remainingReadLength), SocketFlags.None);

                            if (bytesReceived == _remainingReadLength)
                            {
                                _readState = ReadState.Idle;
                                OnMessageReceived(buffer, 2 + _currentReadMsgLength);
                                CheckMessage(buffer, 2 + _currentReadMsgLength);
                                break;
                            }

                            _currentReadPos += bytesReceived;
                            _remainingReadLength -= bytesReceived;
                            break;
                        default:
                            break;
                    }

                    await HandleTimeoutsAsync();

                    if (_fileClient != null)
                        _fileClient.HandleFileService();

                    if (_unconfirmedReceivedIMessages >= _apciParameters.W)
                    {
                        _lastConfirmationTime = SystemUtils.currentTimeMillis();

                        _unconfirmedReceivedIMessages = 0;
                        _timeoutT2Triggered = false;

                        await SendAsync(new FormatSMessage(_receiveSequenceNumber));
                    }

                    if(_readState == ReadState.Idle)
                        await Task.Delay(10);
                }
            }
            catch (Exception ex) 
            {
                await Console.Out.WriteLineAsync(ex.Message);
            }
            finally
            {
                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception) { }

                _socket.Close();
                _socket.Dispose();
                _socket = null;
                OnSessionConnectionChanged(ConnectionEvent.Closed);
            }
        }

        private void OnSessionConnectionChanged(ConnectionEvent connectionEvent)
        {
            SessionConnetionChanged?.Invoke(this, new SessionConnetionChangedEventArgs(connectionEvent));
        }

        private void OnMessageSent(byte[] buffer, int offset, int messageSize)
        {
            byte[] message = new byte[messageSize];
            for (int i = 0; i < messageSize; i++)
            {
                message[i] = buffer[i + offset];
            }
            MessageSent?.Invoke(this, new MessageReceiveSendEventArgs(message, messageSize));
        }

        private void OnMessageReceived(byte[] message, int messageSize)
        {
            MessageReceived?.Invoke(this, new MessageReceiveSendEventArgs(message, messageSize));
        }

        private void OnAsduReceived(ASDU asdu)
        {
            AsduReceived?.Invoke(this, new AsduReceivedEventArgs(asdu));
        }

        internal enum ReadState
        {
            Idle = 0,
            StartReceived = 1,
            ReadingMessage = 2
        }

        private void CheckMessage(byte[] buffer, int msgSize)
        {
            long currentTime = SystemUtils.currentTimeMillis();

            if ((buffer[2] & 1) == 0)
            { 
                if (_timeoutT2Triggered == false)
                {
                    _timeoutT2Triggered = true;
                    _lastConfirmationTime = currentTime;
                }

                if (msgSize < 7)
                    throw new Exception("I msg too small!");

                int frameSendSequenceNumber = ((buffer[3] * 0x100) + (buffer[2] & 0xfe)) / 2;
                int frameRecvSequenceNumber = ((buffer[5] * 0x100) + (buffer[4] & 0xfe)) / 2;

                if (frameSendSequenceNumber != _receiveSequenceNumber)
                    throw new Exception("Sequence error: Close connection!");

                _kBuffer.Update(frameRecvSequenceNumber, _sendSequenceNumber);

                _receiveSequenceNumber = (_receiveSequenceNumber + 1) % 32768;
                _unconfirmedReceivedIMessages++;

                try
                {
                    ASDU asdu = new ASDU(_alParameters, buffer, 6, msgSize);

                    bool messageHandled = false;

                    if (_fileClient != null)
                        messageHandled = _fileClient.HandleFileAsdu(asdu);

                    if (messageHandled == false)
                    {
                        OnAsduReceived(asdu);
                    }
                }
                catch (ASDUParsingException e)
                {
                    throw new Exception($"ASDU parsing failed: {e.Message}");
                }

            }
            else if ((buffer[2] & 0x03) == 0x01)
            {
                int seqNo = (buffer[4] + buffer[5] * 0x100) / 2;

                _kBuffer.Update(seqNo, _sendSequenceNumber);
            }
            else if ((buffer[2] & 0x03) == 0x03)
            {
                _uMessageTimeout = 0;

                switch (buffer[2])
                { 
                    case 0x43:
                        Send(new FormatUMessage(UMessageType.TestFrCon));
                        break;
                    case 0x83:
                        _outStandingTestFRConMessages = 0;
                        break;
                    case 0x07:
                        Send(new FormatUMessage(UMessageType.StartDtCon));
                        break;
                    case 0x0b:
                        OnSessionConnectionChanged(ConnectionEvent.StartDtConReceived);
                        break;
                    case 0x23:
                        OnSessionConnectionChanged(ConnectionEvent.StopDtConReceived);
                        break;
                }
            }
            else
            {
                throw new Exception("Unknown message type");
            }

            ResetT3Timeout();
        }

        private async Task HandleTimeoutsAsync()
        {
            UInt64 currentTime = (UInt64)SystemUtils.currentTimeMillis();

            if (currentTime > _nextT3Timeout)
            {

                if (_outStandingTestFRConMessages > 2)
                {
                    throw new TimeoutException("T3 timeout");
                }
                else
                {
                    await SendAsync(new FormatUMessage(UMessageType.TestFrAct));

                    _uMessageTimeout = (UInt64)currentTime + (UInt64)(_apciParameters.T1 * 1000);
                    _outStandingTestFRConMessages++;
                    ResetT3Timeout();
                }
            }

            if (_unconfirmedReceivedIMessages > 0)
            {
                if (((long)currentTime - _lastConfirmationTime) >= (_apciParameters.T2 * 1000))
                {
                    _lastConfirmationTime = (long)currentTime;

                    _unconfirmedReceivedIMessages = 0;
                    _timeoutT2Triggered = false;

                    await SendAsync(new FormatSMessage(_receiveSequenceNumber));
                }
            }

            if (_uMessageTimeout != 0)
            {
                if (currentTime > _uMessageTimeout)
                {
                    throw new SocketException(10060);
                }
            }

            if (!_kBuffer.IsEmpty && ((long)currentTime - _kBuffer.LastSentTime) >= (_apciParameters.T1 * 1000))
                throw new TimeoutException("T1 timeout");
        }        

        public async void Disconnect()
        {
            try
            {
                _cts?.Cancel();
                await Task.WhenAny(_readingTask, Task.Delay(5000));
            }
            catch (Exception) { }
            finally
            {
                if(_socket != null)
                {
                    _socket.Dispose();
                }
            }
        }

        public void GetFile(int ca, int ioa, NameOfFile nof, IFileReceiver receiver)
        {
            if (_fileClient == null)
                _fileClient = new FileClient(this, Console.WriteLine);

            _fileClient.RequestFile(ca, ioa, nof, receiver);
        }


        public void SendFile(int ca, int ioa, NameOfFile nof, IFileProvider fileProvider)
        {
            if (_fileClient == null)
                _fileClient = new FileClient(this, Console.WriteLine);

            _fileClient.SendFile(ca, ioa, nof, fileProvider);
        }

        public void GetDirectory(int ca)
        {
            ASDU getDirectoryAsdu = new ASDU(GetApplicationLayerParameters(), CauseOfTransmission.REQUEST, false, false, 0, ca, false);

            InformationObject io = new FileCallOrSelect(0, NameOfFile.DEFAULT, 0, SelectAndCallQualifier.DEFAULT);

            getDirectoryAsdu.AddInformationObject(io);

            Send(getDirectoryAsdu);
        }
    }
}