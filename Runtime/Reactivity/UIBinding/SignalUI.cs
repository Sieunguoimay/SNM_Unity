using System;
using UnityEngine.UIElements;

namespace Snm.Reactivity.Unity
{
    public static class SignalUI
    {
        public static Effect BindText<T>(
            Label label,
            Signal<T> signal,
            Func<T, string> formatter = null)
        {
            formatter ??= v => v?.ToString() ?? "";

            var effect = new Effect(() =>
            {
                label.text = formatter(signal.Value);
            });

            UIBindingUtil.AutoDispose(label, effect);
            return effect;
        }

        public static Effect BindEnabled<T>(
            VisualElement element,
            Signal<T> signal,
            Func<T, bool> predicate)
        {
            var effect = new Effect(() =>
            {
                element.SetEnabled(predicate(signal.Value));
            });

            UIBindingUtil.AutoDispose(element, effect);
            return effect;
        }

        public static Effect BindVisible<T>(
            VisualElement element,
            Signal<T> signal,
            Func<T, bool> predicate)
        {
            var effect = new Effect(() =>
            {
                element.style.display = predicate(signal.Value)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            });

            UIBindingUtil.AutoDispose(element, effect);
            return effect;
        }

        public static Effect BindStyle<T>(
            VisualElement element,
            Signal<T> signal,
            Action<IStyle, T> applyStyle)
        {
            var effect = new Effect(() =>
            {
                applyStyle(element.style, signal.Value);
            });

            UIBindingUtil.AutoDispose(element, effect);
            return effect;
        }
    }
}
