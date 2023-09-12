namespace lib60870.CS104
{
    public class FormatUMessage : IMessage
    {
        private UMessageType _type;

        public FormatUMessage(UMessageType type)
        {
            _type = type;
        }

        public byte[] Encode()
        {
            byte[] buffer = new byte[6];

            buffer[0] = 0x68;
            buffer[1] = 0x04;
            buffer[2] = (byte)_type;
            buffer[3] = 0;
            buffer[4] = 0;
            buffer[5] = 0;

            return buffer;
        }
    }

    public enum UMessageType
    {
        StartDtAct = 0x07,
        StartDtCon = 0x0b,
        StopDtAct = 0x13,
        StopDtCon = 0x23,
        TestFrAct = 0x43,
        TestFrCon = 0x83
    }
}