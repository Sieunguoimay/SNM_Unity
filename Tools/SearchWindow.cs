#if UNITY_EDITOR
using DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
namespace Tools
{

    public class SearchWindow : EditorWindow
    {
        [InjectField] private readonly Action<string> onResult;
        [InjectField] private readonly IEnumerable<string> options;

        private string _searchString = "";
        private string[] _searchResult;
        private int _displayCount = 20;
        private int _currentPage;
        private bool _firstTime = true;

        public static void Show(IEnumerable<string> options, Action<string> onResult)
        {
            var window = EditorWindow.GetWindow<SearchWindow>();
            DependencyInjector.Inject(window, new Dictionary<string, object>
            { { "onResult", onResult }, { "options", options } });
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
                    if (count > _displayCount) break;
                    if (GUILayout.Button($"{r}"))
                    {
                        onResult?.Invoke(r);
                        Close();
                    }
                    count++;
                }
            }
        }

        private void UpdateSearchResult()
        {
            var regex = new Regex(string.Join(".*", _searchString.Split(" ")), RegexOptions.IgnoreCase);
            _searchResult = options.Where(o => !string.IsNullOrEmpty(o)).Where(o => regex.IsMatch(o)).ToArray();
        }
    }
}
#endif