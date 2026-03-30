using System;

namespace Snm.Runtime.Dispose
{
    public class DisposeCollection : IDisposable
    {
        private IDisposable[] _disposables;

        public DisposeCollection(params IDisposable[] disposables)
        {
            _disposables = disposables;
        }

        public void SetDisposeCollection(params IDisposable[] disposables)
        {
            _disposables = disposables;
        }

        public void Dispose()
        {
            foreach (var d in _disposables)
            {
                d.Dispose();
            }
        }
    }
}