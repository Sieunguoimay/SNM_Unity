using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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

        private int _eventIndex = -1;

        private int _aniTextureIndex = 0;
        private int _preAniTextureIndex = 0;

        private WrapMode _wrapMode = WrapMode.Default;

        //Transition
        private float _transitionDuration = 0.0f;
        private bool _isInTransition = false;
        private float _transitionTimer = 0.0f;
        private float _transitionProgress = 0.0f;

        private AnimationInfo.ComparerHash _animationInfoComparer;
        private AnimationEvent _aniEvent = null;

        private AnimationInstancingRenderer _renderer;
        private AnimationInstancingRenderer Renderer => _renderer ??= GetComponent<AnimationInstancingRenderer>();
        private List<AnimationInfo> AnimInfoList => Renderer.InstancingData.animInfoList;

        public float FrameIndex => _aniIndex >= 0 ? AnimInfoList[_aniIndex].animationIndex + _curFrame : -1f;
        public float PreFrameIndex => _preAniIndex >= 0 ? AnimInfoList[_preAniIndex].animationIndex + _preAniFrame : -1f;
        public float TransitionProgress => _transitionProgress;
        public int AniTextureIndex => _aniTextureIndex;

        public bool IsPlaying => _aniIndex >= 0;
        public bool IsReady => AnimInfoList != null;
        public bool IsPause => _speedParameter == 0.0f;
        public bool IsLoop => _wrapMode == WrapMode.Loop;

        private void Start()
        {
            _aniIndex = startAnimation;
            _animationInfoComparer = new();
        }

        public void PlayAnimation(string name)
        {
            int hash = name.GetHashCode();
            int index = FindAnimationInfo(hash);
            PlayAnimation(index);
        }

        private int FindAnimationInfo(int hash)
        {
            if (AnimInfoList == null) return -1;
            _animationInfoComparer.CompareTarget.animationNameHash = hash;
            return AnimInfoList.BinarySearch(_animationInfoComparer.CompareTarget, _animationInfoComparer);
        }

        public void PlayAnimation(int animationIndex)
        {
            if (AnimInfoList == null)
            {
                return;
            }

            if (animationIndex == _aniIndex && !IsPause)
            {
                return;
            }

            _transitionDuration = 0.0f;
            _transitionProgress = 1.0f;
            _isInTransition = false;

            Debug.Assert(animationIndex < AnimInfoList.Count);

            if (0 <= animationIndex && animationIndex < AnimInfoList.Count)
            {
                _preAniIndex = _aniIndex;
                _aniIndex = animationIndex;
                _preAniFrame = (int)(_curFrame + 0.5f);
                _curFrame = 0.0f;
                _eventIndex = -1;
                _preAniTextureIndex = _aniTextureIndex;
                _aniTextureIndex = AnimInfoList[_aniIndex].textureIndex;
                _wrapMode = AnimInfoList[_aniIndex].wrapMode;
                _speedParameter = 1.0f;
            }
            else
            {
                Debug.LogWarning("The requested animation index is out of the count.");
                return;
            }
        }

        public void CrossFade(string animationName, float duration)
        {
            int hash = animationName.GetHashCode();
            int index = FindAnimationInfo(hash);
            CrossFade(index, duration);
        }

        public void CrossFade(int animationIndex, float duration)
        {
            PlayAnimation(animationIndex);
            if (duration > 0.0f)
            {
                _isInTransition = true;
                _transitionTimer = 0.0f;
                _transitionProgress = 0.0f;
            }
            else
            {
                _transitionProgress = 1.0f;
            }
            _transitionDuration = duration;
        }

        public void Pause()
        {
            _cacheParameter = _speedParameter;
            _speedParameter = 0.0f;
        }

        public void Resume()
        {
            _speedParameter = _cacheParameter;
        }

        public void Stop()
        {
            _aniIndex = -1;
            _preAniIndex = -1;
            _eventIndex = -1;
            _curFrame = 0.0f;
        }

        public AnimationInfo GetCurrentAnimationInfo()
        {
            if (AnimInfoList != null && 0 <= _aniIndex && _aniIndex < AnimInfoList.Count)
            {
                return AnimInfoList[_aniIndex];
            }
            return null;
        }

        public AnimationInfo GetPreAnimationInfo()
        {
            if (AnimInfoList != null && 0 <= _preAniIndex && _preAniIndex < AnimInfoList.Count)
            {
                return AnimInfoList[_preAniIndex];
            }
            return null;
        }

        public void UpdateAnimation()
        {
            if (AnimInfoList == null || IsPause)
                return;

            UpdateTransition();
            UpdateCurrentFrame();
        }

        private void UpdateTransition()
        {
            if (_isInTransition)
            {
                _transitionTimer += Time.deltaTime;
                float weight = _transitionTimer / _transitionDuration;
                _transitionProgress = Mathf.Min(weight, 1.0f);
                if (_transitionProgress >= 1.0f)
                {
                    _isInTransition = false;
                    _preAniIndex = -1;
                    _preAniFrame = -1;
                }
            }
        }

        private void UpdateCurrentFrame()
        {
            var aniInfo = AnimInfoList[_aniIndex];
            int fps = aniInfo.fps;
            int totalFrame = aniInfo.totalFrame;

            var speed = playSpeed * _speedParameter;

            _curFrame += speed * Time.deltaTime * fps;

            switch (_wrapMode)
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
                            Pause();
                        }
                        break;
                    }
            }

            _curFrame = Mathf.Clamp(_curFrame, 0f, totalFrame - 1);
            
            UpdateAnimationEvent();
        }

        private void UpdateAnimationEvent()
        {
            var info = GetCurrentAnimationInfo();
            if (info == null)
                return;
            if (info.eventList.Count == 0)
                return;

            if (_aniEvent == null)
            {
                float time = _curFrame / info.fps;
                for (int i = _eventIndex >= 0 ? _eventIndex : 0; i < info.eventList.Count; ++i)
                {
                    if (info.eventList[i].time > time)
                    {
                        _aniEvent = info.eventList[i];
                        _eventIndex = i;
                        break;
                    }
                }
            }

            if (_aniEvent != null)
            {
                var time = _curFrame / info.fps;
                if (_aniEvent.time <= time)
                {
                    gameObject.SendMessage(_aniEvent.function, _aniEvent);// use handler to handle this
                    _aniEvent = null;
                }
            }
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