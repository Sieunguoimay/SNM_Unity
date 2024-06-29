#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ObjectDependentsLogger
{
    [MenuItem("CONTEXT/Object/LogDependents")]
    private static void LogDependents(MenuCommand command)
    {
        var gameObject = command.context is GameObject go ? go : (command.context is Component c ? c.gameObject : null);
        if (gameObject != null)
        {
            var rootGO = gameObject.transform.root.gameObject;
            foreach (var d in IterateAllObjects(rootGO).Where(o => HasReferenceTo(o, command.context)))
            {
                Debug.Log(d.name + $" {d.GetType().Name}", d);
            }
        }
    }

    [MenuItem("GameObject/Tools/LogDependents")]
    private static void LogDependents_GameObject(MenuCommand command) => LogDependents(command);

    private static IEnumerable<Object> IterateAllObjects(GameObject root)
    {
        yield return root;
        foreach (var c in root.GetComponents<Component>())
        {
            yield return c;
        }
        foreach (Transform t in root.transform)
        {
            foreach (var o in IterateAllObjects(t.gameObject))
            {
                yield return o;
            }
        }
    }

    private static bool HasReferenceTo(Object obj, Object target)
    {
        var serializedObject = new SerializedObject(obj);
        serializedObject.Update();
        var it = serializedObject.GetIterator();
        while (it.Next(true))
        {
            if (it.propertyType == SerializedPropertyType.ObjectReference &&
            it.objectReferenceValue == target)
            {
                return true;
            }
        }
        return false;
    }
}
#endif