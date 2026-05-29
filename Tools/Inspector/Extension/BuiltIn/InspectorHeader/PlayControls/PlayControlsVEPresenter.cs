#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    // Three-button play strip embedded in the Inspector header so play mode is
    // reachable when HeaderAutoHide hides the main toolbar.
    public class PlayControlsVEPresenter : IDisposable
    {
        private readonly Button playButton;
        private readonly Button pauseButton;
        private readonly Button stepButton;

        public VisualElement Root { get; }

        public PlayControlsVEPresenter()
        {
            Root = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, flexShrink = 0 }
            };

            playButton = MakeButton("▶", "Play / Stop", TogglePlay);
            pauseButton = MakeButton("⏸", "Pause", TogglePause);
            stepButton = MakeButton("▶▌", "Step one frame", Step);

            Root.Add(playButton);
            Root.Add(pauseButton);
            Root.Add(stepButton);

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.pauseStateChanged += OnPauseStateChanged;
            Refresh();
        }

        public void Dispose()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.pauseStateChanged -= OnPauseStateChanged;
        }

        private static Button MakeButton(string glyph, string tooltip, Action onClick)
        {
            return new Button(onClick)
            {
                text = glyph,
                tooltip = tooltip,
                style =
                {
                    width = 24,
                    marginLeft = 0, marginRight = 0,
                    paddingLeft = 0, paddingRight = 0,
                    fontSize = 12,
                    unityTextAlign = TextAnchor.MiddleCenter,
                }
            };
        }

        private void OnPlayModeStateChanged(PlayModeStateChange _) => Refresh();
        private void OnPauseStateChanged(PauseState _) => Refresh();

        private void TogglePlay() => EditorApplication.isPlaying = !EditorApplication.isPlaying;
        private void TogglePause() => EditorApplication.isPaused = !EditorApplication.isPaused;
        private void Step() => EditorApplication.Step();

        private void Refresh()
        {
            bool isPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
            bool isPaused = EditorApplication.isPaused;

            // Swap play glyph for stop while in play mode.
            playButton.text = isPlaying ? "■" : "▶";

            // Match Unity's toolbar: pause + step are only meaningful in play mode.
            pauseButton.SetEnabled(isPlaying);
            stepButton.SetEnabled(isPlaying);

            // Tint pause when paused so state is visible at a glance.
            pauseButton.style.color = isPaused
                ? new StyleColor(new Color(0.4f, 0.8f, 1f))
                : new StyleColor(StyleKeyword.Null);
        }
    }
}
#endif
