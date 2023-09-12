using System;

namespace lib60870.CS104
{
    public class KBuffer
    {
        private Deque<SentASDU> _sentASDUs;
        public KBuffer(int maxCount)
        {
            _sentASDUs = new Deque<SentASDU>(maxCount);
        }

        public bool IsEmpty => _sentASDUs.IsEmpty;
        public bool IsFull => _sentASDUs.IsFull;
        public long LastSentTime => _sentASDUs.Last.SentTime;

        public void Add(int sendSequenceNumber)
        {
            _sentASDUs.AddFirst(new SentASDU
            {
                SeqNo = sendSequenceNumber,
                SentTime = SystemUtils.currentTimeMillis()
            });
        }

        public void Update(int receivedSequenceNumber, int sendSequenceNumber)
        {
            if (_sentASDUs.IsEmpty)
            { 
                if(receivedSequenceNumber == sendSequenceNumber)
                    return;
                else
                    throw new Exception("Sequence number invalid");
            }

            if(_sentASDUs.Last.SeqNo <= _sentASDUs.First.SeqNo)
            {
                if(receivedSequenceNumber < _sentASDUs.Last.SeqNo || receivedSequenceNumber > _sentASDUs.First.SeqNo)
                    throw new Exception("Sequence number invalid");
            }
            else
            {
                if (receivedSequenceNumber < _sentASDUs.Last.SeqNo && receivedSequenceNumber > _sentASDUs.First.SeqNo)
                    throw new Exception("Sequence number invalid");
            }

            bool removeNext;
            do
            {
                var sentAsdu = _sentASDUs.RemoveLast();
                removeNext = sentAsdu.SeqNo != receivedSequenceNumber;
            } while (removeNext);
        }

        private struct SentASDU
        {
            public long SentTime;
            public int SeqNo;
        }

        private object _lock = new object();
    }
}