using lib60870.CS101;

namespace lib60870.CS104
{
    public class FormatIMessage : IMessage
    {
        private ASDU _asdu;
        private int _sendSequenceNumber;
        private int _receiveSequenceNumber;
        private ApplicationLayerParameters _alParameters;

        public FormatIMessage(ASDU asdu, int sendSequenceNumber, int receiveSequenceNumber, ApplicationLayerParameters alParameters)
        {
            _asdu = asdu;
            _sendSequenceNumber = sendSequenceNumber;
            _receiveSequenceNumber = receiveSequenceNumber;
            _alParameters = alParameters;
        }

        public byte[] Encode()
        {
            BufferFrame frame = new BufferFrame(new byte[260], 6);
            _asdu.Encode(frame, _alParameters);

            byte[] buffer = frame.GetBuffer();
            int msgSize = frame.GetMsgSize();

            buffer[0] = 0x68;
            buffer[1] = (byte)(msgSize - 2);
            buffer[2] = (byte)((_sendSequenceNumber % 128) * 2);
            buffer[3] = (byte)(_sendSequenceNumber / 128);
            buffer[4] = (byte)((_receiveSequenceNumber % 128) * 2);
            buffer[5] = (byte)(_receiveSequenceNumber / 128);

            return buffer;
        }
    }
}