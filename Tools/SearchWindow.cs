#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Snm.Tools
{
    // Thin wrapper over Unity's built-in AdvancedDropdown (the native searchable popup).
    // Keeps the old SearchWindow.Show(options, onResult) call sites unchanged while
    // delegating the search field / scrolling / filtering to the engine.
    public static class SearchWindow
    {
        public static void Show(IEnumerable<string> options, Action<string> onResult)
        {
            var dropdown = new StringSearchDropdown(new AdvancedDropdownState(), options, onResult);
            var mouse = Event.current != null ? Event.current.mousePosition : Vector2.zero;
            dropdown.Show(new Rect(mouse, Vector2.zero));
        }

        private class StringSearchDropdown : AdvancedDropdown
        {
            private readonly List<string> _options;
            private readonly Action<string> _onResult;

            public StringSearchDropdown(AdvancedDropdownState state, IEnumerable<string> options, Action<string> onResult)
                : base(state)
            {
                _options = new List<string>(options);
                _onResult = onResult;
                minimumSize = new Vector2(250, 350);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                var root = new AdvancedDropdownItem("Search");
                for (int i = 0; i < _options.Count; i++)
                {
                    if (string.IsNullOrEmpty(_options[i])) continue;
                    // id = index so selection maps back to the exact option string,
                    // robust against duplicate display names.
                    root.AddChild(new AdvancedDropdownItem(_options[i]) { id = i });
                }
                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item.id >= 0 && item.id < _options.Count)
                    _onResult?.Invoke(_options[item.id]);
            }
        }
    }
}
#endif
