using System;

namespace lib60870.CS104
{
    public class SessionConnetionChangedEventArgs : EventArgs
    {
        public SessionConnetionChangedEventArgs(ConnectionEvent connectionEvent)
        {
            ConnectionEvent = connectionEvent;
        }

        public ConnectionEvent ConnectionEvent { get; set; }
    }
}