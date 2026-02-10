#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Snm.Tools.InspectorExtra;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{

    public class InspectorExtensionSystemInstaller
    {
        public InspectorExtensionSystemControl Install(IInspectorExtension[]extensions)
        {
            InspectorWindowControllerManager controllerManager = null;
            controllerManager = new InspectorWindowControllerManager(
                extensions,
                createHeaderVEFunc: w => new Label() { text = "OK" });//new InspectorExtensionHeaderVE(null, w, new RefreshHandler(() => controllerManager?.Refresh())));

            var destroyer = new InspectorExtensionSystemDestroyer(destroyCallback: () =>
            {
                controllerManager.Cleanup();
            });

            return new(destroyer);
        }

        private class RefreshHandler : IRefreshHandler
        {
            private readonly Action refreshCallback;

            public RefreshHandler(Action refreshCallback)
            {
                this.refreshCallback = refreshCallback;
            }

            public void Refresh()
            {
                refreshCallback();
            }
        }
    }
}
#endif