using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Snm.Reactivity.Unity
{
    public static class SignalUIList
    {
        public static Effect BindList<T>(
            ListView listView,
            Signal<IReadOnlyList<T>> signal,
            Func<VisualElement> makeItem,
            Action<VisualElement, int, T> bindItem)
        {
            listView.makeItem = makeItem;
            listView.bindItem = (element, index) =>
            {
                var items = signal.Value;
                if (index < items.Count)
                    bindItem(element, index, items[index]);
            };

            var effect = new Effect(() =>
            {
                var items = signal.Value;
                listView.itemsSource = items is List<T> list ? list : new List<T>(items);
                listView.RefreshItems();
            });

            UIBindingUtil.AutoDispose(listView, effect);
            return effect;
        }
    }
}
