#if UNITY_EDITOR && UNITY_EDITOR_WIN
using UnityEditor;

namespace Snm.Tools.HeaderAutoHide
{
    internal enum RevealState
    {
        Hidden,
        Shown,
        Pinned
    }

    internal sealed class RevealStateMachine
    {
        public RevealState State { get; private set; } = RevealState.Hidden;

        double _hideAtRealtime = -1;

        public bool IsPinned => State == RevealState.Pinned;

        public void TogglePin()
        {
            if (State == RevealState.Pinned)
            {
                State = RevealState.Shown;
                _hideAtRealtime = -1;
            }
            else
            {
                State = RevealState.Pinned;
                _hideAtRealtime = -1;
            }
        }

        public bool Tick(CursorSample sample, out bool shouldShow, out bool shouldHide)
        {
            shouldShow = false;
            shouldHide = false;

            if (State == RevealState.Pinned)
            {
                shouldShow = true;
                return false;
            }

            double now = EditorApplication.timeSinceStartup;
            int hideDelaySec = HeaderAutoHideSettings.HideDelayMs;
            double hideDelay = hideDelaySec / 1000.0;

            if (State == RevealState.Hidden)
            {
                if (sample.InRevealZone)
                {
                    State = RevealState.Shown;
                    shouldShow = true;
                    _hideAtRealtime = -1;
                    return true;
                }
                return false;
            }

            // State == Shown
            if (sample.InHeaderRegion)
            {
                _hideAtRealtime = -1;
                return false;
            }

            if (_hideAtRealtime < 0)
            {
                _hideAtRealtime = now + hideDelay;
                return false;
            }

            if (now >= _hideAtRealtime)
            {
                State = RevealState.Hidden;
                shouldHide = true;
                _hideAtRealtime = -1;
                return true;
            }

            return false;
        }

        public void ForceShown()
        {
            State = RevealState.Shown;
            _hideAtRealtime = -1;
        }

        public void ForceHidden()
        {
            if (State == RevealState.Pinned) return;
            State = RevealState.Hidden;
            _hideAtRealtime = -1;
        }
    }
}
#endif
