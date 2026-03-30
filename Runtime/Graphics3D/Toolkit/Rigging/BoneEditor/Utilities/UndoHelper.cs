#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.Graphics3D.Rigging
{
    /// <summary>
    /// Static helper for recording undo operations on ScriptableObjects.
    /// Wraps Undo.RecordObject to keep call sites DRY.
    /// </summary>
    public static class UndoHelper
    {
        /// <summary>
        /// Records the current state of the document for undo.
        /// Must be called before any mutation.
        /// </summary>
        public static void Record(RigDocument doc, string operationName)
        {
            if (doc != null)
                Undo.RecordObject(doc, operationName);
        }
    }
}
#endif
