using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.UIElements;

namespace Snm.Runtime.GrassSystem
{
    public class AnyVEWindow : EditorWindow
    {
        private VisualElement _ve;

        public static AnyVEWindow Open(VisualElement ve)
        {
            var window = GetWindow<AnyVEWindow>();
            window._ve = ve;
            window.Show();
            window.CreateGUI();
            return window;
        }

        private void CreateGUI()
        {
            rootVisualElement.Add(_ve);
        }
    }

}