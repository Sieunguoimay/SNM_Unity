#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class SceneViewExtensionsEntryPoint
{
    static SceneViewExtensionsEntryPoint()
    {
        SceneGUIExtensionInstaller.Instance.Install();
    }
}

public class SceneGUIExtensionInstaller
{
    private static SceneGUIExtensionInstaller _instance;
    public static SceneGUIExtensionInstaller Instance => _instance ??= new SceneGUIExtensionInstaller();

    private BindingFlags BindingFlags => BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

    private readonly List<MethodInvoker> _methodInvokers = new();

    private EditorCoroutine _waitForSelectionCoroutine;

    public void Install()
    {
        Selection.selectionChanged -= OnSelectionChanged;
        Selection.selectionChanged += OnSelectionChanged;

        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;

        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;

        //Debug.Log($"Init");

        _methodInvokers.Clear();
        _waitForSelectionCoroutine = EditorCoroutineUtility.StartCoroutine(WaitForSelection(UpdateMethodInvokers), this);
    }

    private IEnumerator WaitForSelection(Action callback)
    {
        yield return new WaitUntil(() => Selection.activeObject != null);
        callback?.Invoke();
        _waitForSelectionCoroutine = null;
    }

    private void OnPlayModeChanged(PlayModeStateChange change)
    {
        //Debug.Log($"OnPlayModeChanged {change}");
        _methodInvokers.Clear();
        if (change == PlayModeStateChange.EnteredEditMode)
        {
            _waitForSelectionCoroutine = EditorCoroutineUtility.StartCoroutine(WaitForSelection(UpdateMethodInvokers), this);
        }
    }

    private void OnSceneGUI(SceneView obj)
    {
        foreach (var methodInvoker in _methodInvokers)
        {
            methodInvoker.methodInfo.Invoke(methodInvoker.target, new object[] { });
        }
    }

    private void OnSelectionChanged()
    {
        //Debug.Log($"OnSelectionChanged");
        if (_waitForSelectionCoroutine != null)
        {
            EditorCoroutineUtility.StopCoroutine(_waitForSelectionCoroutine);
            _waitForSelectionCoroutine = null;
        }
        else
        {
            UpdateMethodInvokers();
        }
    }

    private void UpdateMethodInvokers()
    {
        _methodInvokers.Clear();
        var activeObject = Selection.activeObject;
        if (activeObject is ScriptableObject so)
        {
            var methodInvokers = so.GetType().GetMethods(BindingFlags).Where(IsMethodValid).Select(mi => new MethodInvoker { target = so, methodInfo = mi });
            _methodInvokers.AddRange(methodInvokers);

        }
        else if (activeObject is GameObject go)
        {
            var mbs = go.GetComponents<MonoBehaviour>();
            if(mbs.Length>0){
                var methodInvokers = mbs.SelectMany(mb => mb.GetType().GetMethods(BindingFlags).Where(IsMethodValid).Select(mi => new MethodInvoker { target = mb, methodInfo = mi }));
                _methodInvokers.AddRange(methodInvokers);
            }
        }
    }
    private bool IsMethodValid(MethodInfo mi)
    {
        if (mi.GetParameters().Length > 0) return false;
        if (mi.GetCustomAttribute<SceneGUIMethodAttribute>() == null) return false;
        return true;
    }

    private class MethodInvoker
    {
        public MethodInfo methodInfo;
        public object target;
    }
}
#endif