using System.Collections.Generic;
using UnityEngine;

namespace Snm.CameraRig
{
    public class CameraSequence
    {
        public struct Step
        {
            public Vector3 focusPoint;
            public float blendDuration;
            public float holdDuration;
            public bool isReturn;
        }

        private readonly List<Step> steps = new();
        public IReadOnlyList<Step> Steps => steps;

        public CameraSequence FocusOn(Vector3 point, float blendIn = 0.3f, float hold = 1f)
        {
            steps.Add(new Step
            {
                focusPoint = point,
                blendDuration = blendIn,
                holdDuration = hold,
            });
            return this;
        }

        public CameraSequence Return(float blendOut = 0.3f)
        {
            steps.Add(new Step
            {
                isReturn = true,
                blendDuration = blendOut,
            });
            return this;
        }
    }
}
