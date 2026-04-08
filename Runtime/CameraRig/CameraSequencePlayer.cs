using UnityEngine;

namespace Snm.CameraRig
{
    public class CameraSequencePlayer
    {
        private enum StepPhase { Blend, Hold }

        private CameraSequence sequence;
        private int currentStepIndex;
        private StepPhase phase;
        private float phaseTime;

        public bool IsPlaying => sequence != null;
        public Vector3 CurrentFocusPoint { get; private set; }

        /// <summary>
        /// 0 = fully tracking (natural camera), 1 = fully locked on focus point.
        /// </summary>
        public float BlendWeight { get; private set; }

        public void Play(CameraSequence seq)
        {
            sequence = seq;
            currentStepIndex = 0;
            phase = StepPhase.Blend;
            phaseTime = 0f;
            BlendWeight = 0f;

            if (seq.Steps.Count > 0 && !seq.Steps[0].isReturn)
                CurrentFocusPoint = seq.Steps[0].focusPoint;
        }

        public void Cancel()
        {
            sequence = null;
            BlendWeight = 0f;
        }

        public void Advance(float dt)
        {
            if (sequence == null) return;

            phaseTime += dt;
            var step = sequence.Steps[currentStepIndex];

            if (step.isReturn)
            {
                // Return step: blend weight goes from current toward 0
                BlendWeight = step.blendDuration > 0f
                    ? Mathf.Clamp01(1f - phaseTime / step.blendDuration)
                    : 0f;

                if (phaseTime >= step.blendDuration)
                {
                    sequence = null;
                    BlendWeight = 0f;
                }
                return;
            }

            switch (phase)
            {
                case StepPhase.Blend:
                {
                    // Blend toward this step's focus point
                    CurrentFocusPoint = step.focusPoint;

                    // If there's a previous focus step, blend between them
                    // Otherwise blend from tracking (weight 0 → 1)
                    BlendWeight = step.blendDuration > 0f
                        ? Mathf.Clamp01(phaseTime / step.blendDuration)
                        : 1f;

                    if (phaseTime >= step.blendDuration)
                    {
                        phaseTime -= step.blendDuration;
                        phase = StepPhase.Hold;
                        BlendWeight = 1f;
                    }
                    break;
                }
                case StepPhase.Hold:
                {
                    BlendWeight = 1f;

                    if (phaseTime >= step.holdDuration)
                    {
                        phaseTime -= step.holdDuration;
                        currentStepIndex++;

                        if (currentStepIndex >= sequence.Steps.Count)
                        {
                            // No more steps, done
                            sequence = null;
                            BlendWeight = 0f;
                            return;
                        }

                        phase = StepPhase.Blend;

                        var nextStep = sequence.Steps[currentStepIndex];
                        if (!nextStep.isReturn)
                            CurrentFocusPoint = nextStep.focusPoint;
                    }
                    break;
                }
            }
        }
    }
}
