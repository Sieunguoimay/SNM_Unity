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
        private int _pageIndex;

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
                for (int i = 0; i < displayCount; i++)
                {
                    var index = i + displayCount * _pageIndex;
                    if (index >= _searchResult.Length) break;

                    var r = _searchResult[index];

                    if (GUILayout.Button($"{r}"))
                    {
                        _onResult?.Invoke(r);
                        Close();
                    }
                }
            }

            var pageCount = Mathf.CeilToInt(_searchResult.Length / (float)displayCount);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("<")) { _pageIndex = Mathf.Max(0, _pageIndex - 1); }
            GUILayout.Label($"{_pageIndex + 1}/{pageCount}", GUILayout.ExpandWidth(false));
            if (GUILayout.Button(">")) { _pageIndex = Mathf.Min(pageCount - 1, _pageIndex + 1); }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
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