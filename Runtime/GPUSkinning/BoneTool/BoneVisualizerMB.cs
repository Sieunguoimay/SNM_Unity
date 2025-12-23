using System;
using UnityEditor;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    [ExecuteInEditMode]
    public class BoneVisualizerMB : MonoBehaviour
    {
        [SerializeField] private Transform[] transforms;

        private void OnDrawGizmos()
        {
            if (transforms == null || transforms.Length < 2) return;

            foreach (var tr in transforms)
            {
                if (Array.IndexOf(transforms, tr.parent) >= 0)
                {
                    var trFrom = tr;
                    var trTo = tr.parent;

                    if (trFrom == null || trTo == null) continue;

                    var from = trFrom.position;
                    var to = trTo.position;

                    Handles.DrawLine(from, to, 1);
                }
            }
        }

        [ContextMenu("Capture Child Transforms")]
        private void CaptureChildTransforms()
        {
            transforms = GetComponentsInChildren<Transform>();
            SetTransforms(transforms);
        }

        public void SetTransforms(Transform[] transforms)
        {
            this.transforms = transforms;
            EditorUtility.SetDirty(this);
        }
    }
}