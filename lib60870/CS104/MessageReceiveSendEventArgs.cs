using System;

namespace lib60870.CS104
{
    public class MessageReceiveSendEventArgs : EventArgs
    {
        public MessageReceiveSendEventArgs(byte[] message, int messageSize)
        {
            Message = message;
            MessageSize = messageSize;
        }

        public byte[] Message { get; set; }
        public int MessageSize { get; set; }
    }
}