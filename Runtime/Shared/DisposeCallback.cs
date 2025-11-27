using System;

namespace Snm.Runtime.Dispose
{
    public class DisposeCallback : IDisposable
    {
        private Action _disposeCallback;

        public DisposeCallback(Action disposeCallback = null)
        {
            _disposeCallback = disposeCallback;
        }

        public void SetDisposeCallback(Action disposeCallback)
        {
            _disposeCallback = disposeCallback;
        }

        public void Dispose()
        {
            _disposeCallback?.Invoke();
        }
    }
}