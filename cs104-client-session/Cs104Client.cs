using lib60870.CS101;
using lib60870.CS104;
using System;
using System.Threading.Tasks;

namespace cs104_client_session
{
    public class Cs104Client
    {
        private Session? _session;
        private object _lock = new object();
        private SessionReconnectHandler? _reconnectHandler;
        private int _reconnectPeriod = 5000;

        public async Task ConnectAsync(string hostname, int tcpPort)
        {
            if(_session != null && _session.Connected)
            {
                return;
            }

            _session = new Session(hostname, tcpPort);
            _session.AsduReceived += OnAsduReceived;
            _session.SessionConnetionChanged += OnSessionConnetionChanged;

            try
            {
                await _session.ConnectAsync();
                await _session.SendAsync(new FormatUMessage(UMessageType.StartDtAct));
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
            }
            finally
            {
                BeginReconnect();
            }
        }

        private void OnSessionConnetionChanged(object? sender, SessionConnetionChangedEventArgs e)
        {
            switch (e.ConnectionEvent)
            {
                case ConnectionEvent.Opened:
                    Console.WriteLine("Connected");
                    break;
                case ConnectionEvent.Closed:
                    Console.WriteLine("Connection closed");

                    if (!ReferenceEquals(sender, _session))
                        return;

                    BeginReconnect();
                    break;
                case ConnectionEvent.StartDtConReceived:
                    Console.WriteLine("STARTDT CON received");
                    break;
                case ConnectionEvent.StopDtConReceived:
                    Console.WriteLine("STOPDT CON received");
                    break;
            }
        }

        private void BeginReconnect()
        {
            lock(_lock)
            {
                if(_reconnectHandler == null)
                {
                    _reconnectHandler = new SessionReconnectHandler();
                    _reconnectHandler.BeginReconnect(_session, _reconnectPeriod, OnReconnectCompleted);
                }
            }
        }

        private void OnReconnectCompleted(object? sender, EventArgs e)
        {
            if (!ReferenceEquals(sender, _reconnectHandler))
                return;

            lock(_lock)
            {
                if (_reconnectHandler?.Session != null)
                {
                    _session = _reconnectHandler.Session;
                }

                _reconnectHandler?.Dispose();
                _reconnectHandler = null;
            }
        }

        private void OnAsduReceived(object? sender, AsduReceivedEventArgs e)
        {
            var asdu = e.Asdu;

            Console.WriteLine(asdu.ToString());

            if (asdu.TypeId == TypeID.M_SP_NA_1)
            {

                for (int i = 0; i < asdu.NumberOfElements; i++)
                {

                    var val = (SinglePointInformation)asdu.GetElement(i);

                    Console.WriteLine("  IOA: " + val.ObjectAddress + " SP value: " + val.Value);
                    Console.WriteLine("   " + val.Quality.ToString());
                }
            }
            else if (asdu.TypeId == TypeID.M_ME_TE_1)
            {

                for (int i = 0; i < asdu.NumberOfElements; i++)
                {

                    var msv = (MeasuredValueScaledWithCP56Time2a)asdu.GetElement(i);

                    Console.WriteLine("  IOA: " + msv.ObjectAddress + " scaled value: " + msv.ScaledValue);
                    Console.WriteLine("   " + msv.Quality.ToString());
                    Console.WriteLine("   " + msv.Timestamp.ToString());
                }

            }
            else if (asdu.TypeId == TypeID.M_ME_TF_1)
            {

                for (int i = 0; i < asdu.NumberOfElements; i++)
                {
                    var mfv = (MeasuredValueShortWithCP56Time2a)asdu.GetElement(i);

                    Console.WriteLine("  IOA: " + mfv.ObjectAddress + " float value: " + mfv.Value);
                    Console.WriteLine("   " + mfv.Quality.ToString());
                    Console.WriteLine("   " + mfv.Timestamp.ToString());
                    Console.WriteLine("   " + mfv.Timestamp.GetDateTime().ToString());
                }
            }
            else if (asdu.TypeId == TypeID.M_SP_TB_1)
            {

                for (int i = 0; i < asdu.NumberOfElements; i++)
                {

                    var val = (SinglePointWithCP56Time2a)asdu.GetElement(i);

                    Console.WriteLine("  IOA: " + val.ObjectAddress + " SP value: " + val.Value);
                    Console.WriteLine("   " + val.Quality.ToString());
                    Console.WriteLine("   " + val.Timestamp.ToString());
                }
            }
            else if (asdu.TypeId == TypeID.M_ME_NC_1)
            {

                for (int i = 0; i < asdu.NumberOfElements; i++)
                {
                    var mfv = (MeasuredValueShort)asdu.GetElement(i);

                    Console.WriteLine("  IOA: " + mfv.ObjectAddress + " float value: " + mfv.Value);
                    Console.WriteLine("   " + mfv.Quality.ToString());
                }
            }
            else if (asdu.TypeId == TypeID.M_ME_NB_1)
            {

                for (int i = 0; i < asdu.NumberOfElements; i++)
                {

                    var msv = (MeasuredValueScaled)asdu.GetElement(i);

                    Console.WriteLine("  IOA: " + msv.ObjectAddress + " scaled value: " + msv.ScaledValue);
                    Console.WriteLine("   " + msv.Quality.ToString());
                }

            }
            else if (asdu.TypeId == TypeID.M_ME_ND_1)
            {

                for (int i = 0; i < asdu.NumberOfElements; i++)
                {

                    var msv = (MeasuredValueNormalizedWithoutQuality)asdu.GetElement(i);

                    Console.WriteLine("  IOA: " + msv.ObjectAddress + " scaled value: " + msv.NormalizedValue);
                }

            }
            else if (asdu.TypeId == TypeID.C_IC_NA_1)
            {
                if (asdu.Cot == CauseOfTransmission.ACTIVATION_CON)
                    Console.WriteLine((asdu.IsNegative ? "Negative" : "Positive") + "confirmation for interrogation command");
                else if (asdu.Cot == CauseOfTransmission.ACTIVATION_TERMINATION)
                    Console.WriteLine("Interrogation command terminated");
            }
            else if (asdu.TypeId == TypeID.F_DR_TA_1)
            {
                Console.WriteLine("Received file directory:\n------------------------");
                int ca = asdu.Ca;

                for (int i = 0; i < asdu.NumberOfElements; i++)
                {
                    FileDirectory fd = (FileDirectory)asdu.GetElement(i);

                    Console.Write(fd.FOR ? "DIR:  " : "FILE: ");

                    Console.WriteLine("CA: {0} IOA: {1} Type: {2}", ca, fd.ObjectAddress, fd.NOF.ToString());
                }

            }
            else
            {
                Console.WriteLine("Unknown message type!");
            }
        }
    }
}