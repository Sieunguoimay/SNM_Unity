using System;

namespace GrabAndToss.Shared.Extensions
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