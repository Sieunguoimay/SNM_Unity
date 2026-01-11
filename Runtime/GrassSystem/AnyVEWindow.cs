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
        private Action _disableCallback;

        public void OnDisable()
        {
            _disableCallback?.Invoke();
        }

        public void SetDisableCallback(Action disableCallback)
        {
            _disableCallback = disableCallback;
        }

        public void SetVE(VisualElement ve)
        {
            _ve = ve;
            CreateGUI();
        }

        private void CreateGUI()
        {
            rootVisualElement.Add(_ve);
        }
    }

}