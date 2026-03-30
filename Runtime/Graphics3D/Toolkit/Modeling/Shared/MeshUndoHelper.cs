#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.Graphics3D.Modeling
{
    public static class MeshUndoHelper
    {
        public static void RecordMesh(Mesh mesh, string operationName)
        {
            Undo.RegisterCompleteObjectUndo(mesh, operationName);
        }

        public static void RecordMeshFilter(MeshFilter filter, string operationName)
        {
            Undo.RegisterCompleteObjectUndo(filter, operationName);
        }

        public static void RecordTransform(Transform transform, string operationName)
        {
            Undo.RecordObject(transform, operationName);
        }

        public static void RecordMeshAndTransform(Mesh mesh, Transform transform, string operationName)
        {
            Undo.RegisterCompleteObjectUndo(mesh, operationName);
            Undo.RecordObject(transform, operationName);
        }

        public static void RegisterCreatedMesh(Mesh mesh, string operationName)
        {
            Undo.RegisterCreatedObjectUndo(mesh, operationName);
        }

        public static void RegisterCreatedGameObject(GameObject go, string operationName)
        {
            Undo.RegisterCreatedObjectUndo(go, operationName);
        }

        public static void BeginGroup(string name)
        {
            Undo.SetCurrentGroupName(name);
        }

        public static void CollapseGroup(int group)
        {
            Undo.CollapseUndoOperations(group);
        }

        public static int GetCurrentGroup()
        {
            return Undo.GetCurrentGroup();
        }
    }
}
#endif
