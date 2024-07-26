using System.Collections.Generic;
using UnityEngine;

namespace AnimationInstancing_v2
{
    [RequireComponent(typeof(AnimationInstancingRenderer))]
    public class AnimationInstancingAnimator : MonoBehaviour
    {
        [SerializeField] private float playSpeed = 1.0f;
        [SerializeField] private int startAnimation = 0;

        private float _speedParameter = 1.0f;
        private float _cacheParameter = 1.0f;
        private float _curFrame = 0;
        private float _preAniFrame = 0;
        private int _aniIndex = -1;
        private int _preAniIndex = -1;
        private int _aniTextureIndex = 0;
        private float _transitionProgress = 0.0f;

        private AnimationInstancingRenderer _renderer;
        private AnimationInstancingRenderer Renderer => _renderer ??= GetComponent<AnimationInstancingRenderer>();
        private IReadOnlyList<AnimationInfo> AnimInfoList => Renderer.InstancingData.animInfoList;

        public float FrameIndex => _aniIndex >= 0 ? AnimInfoList[_aniIndex].animationIndex + _curFrame : -1f;
        public float PreFrameIndex => _preAniIndex >= 0 ? AnimInfoList[_preAniIndex].animationIndex + _preAniFrame : -1f;
        public bool IsPlaying => _aniIndex >= 0;
        public float TransitionProgress => _transitionProgress;
        public int AniTextureIndex => _aniTextureIndex;

        private void Start()
        {
            _aniIndex = startAnimation;
        }

        public void UpdateCurrentFrame()
        {
            var aniInfo = AnimInfoList[_aniIndex];
            UpdateCurrentFrame(aniInfo.fps, aniInfo.totalFrame, aniInfo.wrapMode);
        }

        private void UpdateCurrentFrame(int fps, int totalFrame, WrapMode wrapMode)
        {
            var speed = playSpeed * _speedParameter;
            _curFrame += speed * Time.deltaTime * fps;
            switch (wrapMode)
            {
                case WrapMode.Loop:
                    {
                        if (_curFrame < 0f)
                            _curFrame += totalFrame - 1;
                        else if (_curFrame > totalFrame - 1)
                            _curFrame -= totalFrame - 1;
                        break;
                    }
                case WrapMode.PingPong:
                    {
                        if (_curFrame < 0f)
                        {
                            _speedParameter = Mathf.Abs(_speedParameter);
                            _curFrame = Mathf.Abs(_curFrame);
                        }
                        else if (_curFrame > totalFrame - 1)
                        {
                            _speedParameter = -Mathf.Abs(_speedParameter);
                            _curFrame = 2 * (totalFrame - 1) - _curFrame;
                        }
                        break;
                    }
                case WrapMode.Default:
                case WrapMode.Once:
                    {
                        if (_curFrame < 0f || _curFrame > totalFrame - 1.0f)
                        {
                            // Pause();
                        }
                        break;
                    }
            }

            _curFrame = Mathf.Clamp(_curFrame, 0f, totalFrame - 1);
        }

        [ContextMenu("Play Next")]

        private void TestPlayNext()
        {
            _aniIndex = (_aniIndex + 1) % AnimInfoList.Count;
        }
    }


    // public void UpdateAnimation()
    // {
    //     if (animationData.animInfoList == null)// || IsPause())
    //         return;

    // if (isInTransition)
    // {
    //     transitionTimer += Time.deltaTime;
    //     float weight = transitionTimer / transitionDuration;
    //     transitionProgress = Mathf.Min(weight, 1.0f);
    //     if (transitionProgress >= 1.0f)
    //     {
    //         isInTransition = false;
    //         preAniIndex = -1;
    //         preAniFrame = -1;
    //     }
    // }


    // UpdateCurrentFrame(playSpeed,
    //     animationData.animInfoList[aniIndex].fps,
    //     animationData.animInfoList[aniIndex].totalFrame,
    //     animationData.animInfoList[aniIndex].wrapMode);


    // for (int i = 0; i != listAttachment.Count; ++i)
    // {
    //     var attachment = listAttachment[i];
    //     attachment.transform.position = transform.position;
    //     attachment.transform.rotation = transform.rotation;
    // }
    // UpdateAnimationEvent();
    // }

}