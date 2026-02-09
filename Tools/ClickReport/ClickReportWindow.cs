#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Snm.Tools
{
    public class ClickReportWindow : EditorWindow
    {
        [SerializeField] private List<GameObject> reportedObjects = new();

        private ClickReporter _reporter;

        [MenuItem("Tools/Snm/ClickReportWindow")]
        public static void Open()
        {
            GetWindow<ClickReportWindow>();
        }

        private void OnEnable() { Setup(); }
        private void OnDisable() { Teardown(); }

        private void Setup()
        {
            _reporter = FindObjectOfType<ClickReporter>();

            if (_reporter == null)
            {
                _reporter = new GameObject("ClickReporterMB")
                {
                    hideFlags = HideFlags.DontSave
                }.AddComponent<ClickReporter>();
            }

            _reporter.OnClickDetected += Reporter_OnClickDetected;
        }

        private void Teardown()
        {
            if (_reporter == null) return;

            _reporter.OnClickDetected -= Reporter_OnClickDetected;
            if (Application.isPlaying)
            {
                Destroy(_reporter.gameObject);
            }
            else
            {
                DestroyImmediate(_reporter.gameObject);
            }
        }

        private void Reporter_OnClickDetected(ClickReporter reporter, GameObject @object)
        {
            reportedObjects.Add(@object);
            Repaint();
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            var old = GUI.enabled;
            GUI.enabled = false;
            EditorGUILayout.ObjectField(_reporter, typeof(ClickReporter), true);
            GUI.enabled = old;
            if (GUILayout.Button("Refresh", GUILayout.Width(70)))
            {
                Teardown();
                Setup();
                reportedObjects.Clear();
            }
            if (GUILayout.Button($"Clear Reports ({reportedObjects.Count})"))
            {
                reportedObjects.Clear();
            }
            GUILayout.EndHorizontal();

            for (int i = 0; i < reportedObjects.Count; i++)
            {
                var ro = reportedObjects[i];

                if (GUILayout.Button($"{i}. {ro.name} - {GetHierarchyPathWithDepth(ro, 3)}", new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft }))
                {
                    EditorGUIUtility.PingObject(ro);
                }
            }
        }

        private static string GetHierarchyPathWithDepth(GameObject obj, int maxDepth)
        {
            if (obj == null)
                return "";

            List<string> path = new List<string>();
            Transform current = obj.transform;

            while (current != null)
            {
                path.Insert(0, current.name);
                current = current.parent;
            }

            if (path.Count <= maxDepth)
                return string.Join("/", path);

            // Return last segments if path is longer than maxDepth
            return string.Join("/", path.Skip(path.Count - maxDepth));
        }

    }
}
#endif