using System;
using System.Threading;
using System.Threading.Tasks;

namespace lib60870.CS104
{
    public class SessionReconnectHandler : IDisposable
    {
        public const int MinReconnectPeriod = 500;
        public const int MaxReconnectPeriod = 30000;

        public enum ReconnectState
        {
            Ready,
            Triggered,
            Reconnecting,
            Disposed
        }
        public Session Session => _session;

        public SessionReconnectHandler()
        {
            _reconnectTimer = new Timer(OnReconnect, this, Timeout.Infinite, Timeout.Infinite);
            _state = ReconnectState.Ready;
            _cancelReconnect = false;
        }

        public ReconnectState BeginReconnect(Session session, int reconnectPeriod, EventHandler callback)
        {
            lock (_lock)
            {
                if (_reconnectTimer == null)
                {
                    return ReconnectState.Disposed;
                }

                if (_state == ReconnectState.Ready)
                {
                    _session = session;
                    _cancelReconnect = false;
                    _callback = callback;
                    _reconnectPeriod = reconnectPeriod;
                    _reconnectTimer.Change(reconnectPeriod, Timeout.Infinite);
                    _state = ReconnectState.Triggered;
                    return _state;
                }

                return _state;
            }
        }

        public ReconnectState State
        {
            get
            {
                lock (_lock)
                {
                    if (_reconnectTimer == null)
                    {
                        return ReconnectState.Disposed;
                    }
                    return _state;
                }
            }
        }

        public void CancelReconnect()
        {
            lock (_lock)
            {
                if (_reconnectTimer == null)
                    return;

                if (_state == ReconnectState.Triggered)
                {
                    _session = null;
                    EnterReadyState();
                    return;
                }

                _cancelReconnect = true;
            }
        }

        private async void OnReconnect(object state)
        {
            DateTime reconnectStart = DateTime.UtcNow;
            try
            {
                lock (_lock)
                {
                    if (_reconnectTimer == null || _session == null)
                    {
                        return;
                    }
                    if (_state != ReconnectState.Triggered)
                    {
                        return;
                    }
                    _state = ReconnectState.Reconnecting;
                }

                if (await DoReconnect().ConfigureAwait(false))
                {
                    lock (_lock)
                    {
                        EnterReadyState();
                    }

                    if (_callback != null)
                        _callback(this, EventArgs.Empty);
                    return;
                }
            }
            catch (Exception)
            {
            }

            lock (_lock)
            {
                if (_state != ReconnectState.Disposed)
                {
                    if (_cancelReconnect)
                    {
                        EnterReadyState();
                    }
                    else
                    {
                        _reconnectTimer.Change(_reconnectPeriod, Timeout.Infinite);
                        _state = ReconnectState.Triggered;
                    }
                }
            }
        }

        private async Task<bool> DoReconnect()
        {
            if (_session != null)
            {
                try
                {
                    await _session.ConnectAsync();
                    await _session.SendAsync(new FormatUMessage(UMessageType.StartDtAct));
                    return true;
                }
                catch (Exception)
                {
                }
            }

            return false;
        }

        private void EnterReadyState()
        {
            _reconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _state = ReconnectState.Ready;
            _cancelReconnect = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_lock)
                {
                    if (_reconnectTimer != null)
                    {
                        _reconnectTimer.Dispose();
                    }
                    _state = ReconnectState.Disposed;
                }
            }
        }

        private Session _session;
        private EventHandler _callback;
        private ReconnectState _state;
        private Timer _reconnectTimer;
        private int _reconnectPeriod;
        private bool _cancelReconnect;
        private object _lock = new object();
    }
}