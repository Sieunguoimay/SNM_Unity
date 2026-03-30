using UnityEngine.UIElements;

namespace Snm.Reactivity.Unity
{
    public static class UIBindingUtil
    {
        public static void AutoDispose(VisualElement element, Effect effect)
        {
            void OnDetach(DetachFromPanelEvent _)
            {
                effect.Dispose();
                element.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
            }

            element.RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }
    }
}
