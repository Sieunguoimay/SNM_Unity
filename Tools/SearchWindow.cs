#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools
{
    public class SearchWindow : EditorWindow
    {
        private readonly int displayCount = 20;

        private Action<string> _onResult;
        private IEnumerable<string> _options;

        private string _searchString = "";
        private string[] _searchResult;
        private bool _firstTime = true;

        public static void Show(IEnumerable<string> options, Action<string> onResult)
        {
            var window = GetWindow<SearchWindow>();
            window._options = options;
            window._onResult = onResult;
            window.ShowPopup();
        }

        private void OnGUI()
        {
            if (_firstTime)
            {
                _firstTime = false;
                UpdateSearchResult();
            }
            var str = EditorGUILayout.TextField(_searchString);
            if (_searchString != str)
            {
                _searchString = str;
                UpdateSearchResult();
            }
            if (_searchResult != null)
            {
                var count = 0;
                foreach (var r in _searchResult)
                {
                    if (count > displayCount) break;
                    if (GUILayout.Button($"{r}"))
                    {
                        _onResult?.Invoke(r);
                        Close();
                    }
                    count++;
                }
            }
        }

        private void UpdateSearchResult()
        {
            if (_options == null) return;
            var regex = new Regex(string.Join(".*", _searchString.Split(" ")), RegexOptions.IgnoreCase);
            _searchResult = _options.Where(o => !string.IsNullOrEmpty(o)).Where(o => regex.IsMatch(o)).ToArray();
        }
    }
}
#endif