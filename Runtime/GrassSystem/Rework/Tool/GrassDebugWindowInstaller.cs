#if UNITY_EDITOR
using System;
using UnityEditor;

namespace Snm.Runtime.GrassSystem
{
    public class GrassDebugWindowInstaller
    {
        public GrassDebugToolManager Install(Func<GrassDebugTool> createToolCallback)
        {
            GrassDebugTool tool = null;
            EditorWindow toolWindow = null;

            return new GrassDebugToolManager(
                cleanupCallback: () =>
                {
                    toolWindow?.Close();
                    tool?.Dispose();
                },
                openCallback: () =>
                {
                    var window = EditorWindow.GetWindow<AnyVEWindow>();

                    toolWindow = window;
                    tool = createToolCallback();

                    window.SetVE(GrassDebugToolVECreator.Create(tool));
                    window.SetDisableCallback(() =>
                    {
                        tool.Dispose();
                    });
                });
        }
    }
}
#endif