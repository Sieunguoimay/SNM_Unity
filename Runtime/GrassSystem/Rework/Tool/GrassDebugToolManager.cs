using System;

namespace Snm.Runtime.GrassSystem
{
    public class GrassDebugToolManager
    {
        private readonly Action cleanupCallback;
        private readonly Action openCallback;

        public GrassDebugToolManager(Action cleanupCallback, Action openCallback)
        {
            this.cleanupCallback = cleanupCallback;
            this.openCallback = openCallback;
        }

        public void Cleanup()
        {
            cleanupCallback();
        }

        public void OpenWindow()
        {
            openCallback();
        }
    }
}