using System.Collections.Generic;
using UnityEngine;

namespace Snm.Graphics3D.GPUSkinning
{
    /// <summary>
    /// Standalone baked animation playback — no MonoBehaviour or Animator dependency.
    /// Reads AnimationInstancingData for clip metadata and tracks frame progression.
    /// </summary>
    public class BakedAnimationPlayer
    {
        private readonly List<AnimationInfo> _animInfoList;
        private readonly AnimationInfo.ComparerHash _comparer = new();

        private float _curFrame;
        private float _preAniFrame;
        private int _aniIndex = -1;
        private int _preAniIndex = -1;
        private float _playSpeed = 1f;
        private float _speedParameter = 1f;
        private float _cacheParameter = 1f;
        private WrapMode _wrapMode;

        // Transition
        private float _transitionDuration;
        private float _transitionTimer;
        private float _transitionProgress = 1f;
        private bool _isInTransition;

        public float FrameIndex => _aniIndex >= 0 && _aniIndex < (_animInfoList?.Count ?? 0)
            ? _animInfoList[_aniIndex].startFrameIndex + _curFrame : -1f;
        public float PreFrameIndex => _preAniIndex >= 0 && _preAniIndex < (_animInfoList?.Count ?? 0)
            ? _animInfoList[_preAniIndex].startFrameIndex + _preAniFrame : -1f;
        public float TransitionProgress => _transitionProgress;
        public int TextureIndex => _aniIndex >= 0 && _aniIndex < (_animInfoList?.Count ?? 0)
            ? _animInfoList[_aniIndex].textureIndex : 0;
        public bool IsPlaying => _aniIndex >= 0;
        public bool IsPaused => _speedParameter == 0f;

        /// <summary>Current local frame within the active clip (0 to totalFrame-1).</summary>
        public float CurrentFrame => _curFrame;

        /// <summary>Number of animations available.</summary>
        public int AnimationCount => _animInfoList?.Count ?? 0;

        public float PlaySpeed
        {
            get => _playSpeed;
            set => _playSpeed = value;
        }

        /// <summary>Get animation info by index.</summary>
        public AnimationInfo GetAnimationInfo(int index)
        {
            if (_animInfoList == null || index < 0 || index >= _animInfoList.Count) return null;
            return _animInfoList[index];
        }

        /// <summary>
        /// Set the current frame directly (for editor scrubbing). Clamps to valid range.
        /// </summary>
        public void SetFrame(float frame)
        {
            if (_aniIndex < 0 || _animInfoList == null) return;
            var info = _animInfoList[_aniIndex];
            _curFrame = Mathf.Clamp(frame, 0f, info.totalFrame - 1);
            _preAniFrame = _curFrame;
            _transitionProgress = 1f;
            _isInTransition = false;
        }

        public BakedAnimationPlayer(AnimationInstancingData data)
        {
            _animInfoList = data.animInfoList;
        }

        public void Play(string animName)
        {
            int hash = animName.GetHashCode();
            _comparer.CompareTarget.animationNameHash = hash;
            int index = _animInfoList.BinarySearch(_comparer.CompareTarget, _comparer);
            Play(index);
        }

        public void Play(int animationIndex)
        {
            if (_animInfoList == null) return;
            if (animationIndex == _aniIndex && !IsPaused) return;
            if (animationIndex < 0 || animationIndex >= _animInfoList.Count) return;

            _transitionDuration = 0f;
            _transitionProgress = 1f;
            _isInTransition = false;

            _preAniIndex = _aniIndex;
            _aniIndex = animationIndex;
            _preAniFrame = (int)(_curFrame + 0.5f);
            _curFrame = 0f;
            _wrapMode = _animInfoList[_aniIndex].wrapMode;
            _speedParameter = 1f;
        }

        public void CrossFade(string animName, float duration)
        {
            int hash = animName.GetHashCode();
            _comparer.CompareTarget.animationNameHash = hash;
            int index = _animInfoList.BinarySearch(_comparer.CompareTarget, _comparer);
            CrossFade(index, duration);
        }

        public void CrossFade(int animationIndex, float duration)
        {
            Play(animationIndex);
            if (duration > 0f)
            {
                _isInTransition = true;
                _transitionTimer = 0f;
                _transitionProgress = 0f;
            }
            else
            {
                _transitionProgress = 1f;
            }
            _transitionDuration = duration;
        }

        public void Pause()
        {
            _cacheParameter = _speedParameter;
            _speedParameter = 0f;
        }

        public void Resume()
        {
            _speedParameter = _cacheParameter;
        }

        public void Stop()
        {
            _aniIndex = -1;
            _preAniIndex = -1;
            _curFrame = 0f;
        }

        public void Update(float deltaTime)
        {
            if (_aniIndex < 0 || _animInfoList == null || IsPaused)
                return;

            UpdateTransition(deltaTime);
            UpdateCurrentFrame(deltaTime);
        }

        public AnimationInfo GetCurrentAnimationInfo()
        {
            if (_animInfoList != null && _aniIndex >= 0 && _aniIndex < _animInfoList.Count)
                return _animInfoList[_aniIndex];
            return null;
        }

        private void UpdateTransition(float deltaTime)
        {
            if (!_isInTransition) return;

            _transitionTimer += deltaTime;
            _transitionProgress = Mathf.Min(_transitionTimer / _transitionDuration, 1f);
            if (_transitionProgress >= 1f)
            {
                _isInTransition = false;
                _preAniIndex = -1;
                _preAniFrame = -1;
            }
        }

        private void UpdateCurrentFrame(float deltaTime)
        {
            var aniInfo = _animInfoList[_aniIndex];
            var totalFrame = aniInfo.totalFrame;
            _curFrame += _playSpeed * _speedParameter * deltaTime * aniInfo.fps;

            if (totalFrame <= 1)
            {
                _curFrame = 0f;
                return;
            }

            switch (_wrapMode)
            {
                case WrapMode.Loop:
                    float loopLen = totalFrame - 1;
                    _curFrame = ((_curFrame % loopLen) + loopLen) % loopLen;
                    break;
                case WrapMode.PingPong:
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
                default:
                    if (_curFrame < 0f || _curFrame > totalFrame - 1f)
                        Pause();
                    break;
            }

            _curFrame = Mathf.Clamp(_curFrame, 0f, totalFrame - 1);
        }
    }
}
