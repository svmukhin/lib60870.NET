/*
 *  Connection.cs
 *
 *  Copyright 2016-2019 MZ Automation GmbH
 *
 *  This file is part of lib60870.NET
 *
 *  lib60870.NET is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  lib60870.NET is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with lib60870.NET.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  See COPYING file for the complete license text.
 */

namespace lib60870.CS104
{
    public class SessionStatistics
    {

        private int _sentMsgCounter;
        private int _rcvdMsgCounter;
        private int _rcvdTestFrActCounter;
        private int _rcvdTestFrConCounter;

        internal void Reset()
        {
            _sentMsgCounter = 0;
            _rcvdMsgCounter = 0;
            _rcvdTestFrActCounter = 0;
            _rcvdTestFrConCounter = 0;
        }

        public int SentMsgCounter
        {
            get
            {
                return _sentMsgCounter;
            }
            internal set
            {
                _sentMsgCounter = value;
            }
        }

        public int RcvdMsgCounter
        {
            get
            {
                return _rcvdMsgCounter;
            }
            internal set
            {
                _rcvdMsgCounter = value;
            }
        }

        public int RcvdTestFrActCounter
        {
            get
            {
                return _rcvdTestFrActCounter;
            }
            internal set
            {
                _rcvdTestFrActCounter = value;
            }
        }

        public int RcvdTestFrConCounter
        {
            get
            {
                return _rcvdTestFrConCounter;
            }
            internal set
            {
                _rcvdTestFrConCounter = value;
            }
        }
    }
}