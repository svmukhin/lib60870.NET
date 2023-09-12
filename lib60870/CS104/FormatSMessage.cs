namespace lib60870.CS104
{
    public class FormatSMessage : IMessage
    {
        private int _receiveSequenceNumber;

        public FormatSMessage(int receiveSequenceNumber)
        {
            _receiveSequenceNumber = receiveSequenceNumber;
        }

        public byte[] Encode()
        {
            byte[] buffer = new byte[6];

            buffer[0] = 0x68;
            buffer[1] = 0x04;
            buffer[2] = 0x01;
            buffer[3] = 0;
            buffer[4] = (byte)((_receiveSequenceNumber % 128) * 2);
            buffer[5] = (byte)(_receiveSequenceNumber / 128);

            return buffer;
        }
    }
}