#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.HeaderAutoHide
{
    internal static class HeaderAutoHideSettings
    {
        const string K_Prefix = "Snm.HeaderAutoHide.";

        const string K_KillSwitch       = K_Prefix + "KillSwitch";
        const string K_Enabled          = K_Prefix + "Enabled";
        const string K_StaticMode       = K_Prefix + "StaticMode";
        const string K_HideToolbar      = K_Prefix + "HideToolbar";
        const string K_HideMenuBar      = K_Prefix + "HideMenuBar";
        const string K_HideTitleBar     = K_Prefix + "HideTitleBar";
        const string K_RevealZonePx     = K_Prefix + "RevealZonePx";
        const string K_HideDelayMs      = K_Prefix + "HideDelayMs";
        const string K_AnimDurationMs   = K_Prefix + "AnimDurationMs";
        const string K_PollHz           = K_Prefix + "PollHz";
        const string K_HiddenAtShutdown = K_Prefix + "HiddenAtShutdown";

        public static bool  KillSwitch       { get => EditorPrefs.GetBool(K_KillSwitch, false);   set => EditorPrefs.SetBool(K_KillSwitch, value); }
        public static bool  Enabled          { get => EditorPrefs.GetBool(K_Enabled, false);      set => EditorPrefs.SetBool(K_Enabled, value); }
        public static bool  StaticMode       { get => EditorPrefs.GetBool(K_StaticMode, false);   set => EditorPrefs.SetBool(K_StaticMode, value); }
        public static bool  HideToolbar      { get => EditorPrefs.GetBool(K_HideToolbar, false);  set => EditorPrefs.SetBool(K_HideToolbar, value); }
        public static bool  HideMenuBar      { get => EditorPrefs.GetBool(K_HideMenuBar, false);  set => EditorPrefs.SetBool(K_HideMenuBar, value); }
        public static bool  HideTitleBar     { get => EditorPrefs.GetBool(K_HideTitleBar, false); set => EditorPrefs.SetBool(K_HideTitleBar, value); }
        public static int   RevealZonePx     { get => EditorPrefs.GetInt (K_RevealZonePx, 4);     set => EditorPrefs.SetInt (K_RevealZonePx, Mathf.Clamp(value, 1, 32)); }
        public static int   HideDelayMs      { get => EditorPrefs.GetInt (K_HideDelayMs, 400);    set => EditorPrefs.SetInt (K_HideDelayMs, Mathf.Clamp(value, 0, 5000)); }
        public static int   AnimDurationMs   { get => EditorPrefs.GetInt (K_AnimDurationMs, 120); set => EditorPrefs.SetInt (K_AnimDurationMs, Mathf.Clamp(value, 0, 1000)); }
        public static int   PollHz           { get => EditorPrefs.GetInt (K_PollHz, 20);          set => EditorPrefs.SetInt (K_PollHz, Mathf.Clamp(value, 5, 60)); }
        public static bool  HiddenAtShutdown { get => EditorPrefs.GetBool(K_HiddenAtShutdown, false); set => EditorPrefs.SetBool(K_HiddenAtShutdown, value); }

        public static event System.Action Changed;

        internal static void RaiseChanged() => Changed?.Invoke();

        public static void ResetDefaults()
        {
            Enabled = false;
            StaticMode = false;
            HideToolbar = false;
            HideMenuBar = false;
            HideTitleBar = false;
            RevealZonePx = 4;
            HideDelayMs = 400;
            AnimDurationMs = 120;
            PollHz = 20;
            RaiseChanged();
        }

#if UNITY_EDITOR_WIN
        [SettingsProvider]
        public static SettingsProvider Provide()
        {
            var p = new SettingsProvider("Preferences/Header Auto-Hide", SettingsScope.User)
            {
                label = "Header Auto-Hide",
                guiHandler = _ => DrawIMGUI(),
                keywords = new[] { "header", "hide", "toolbar", "menu", "title" }
            };
            return p;
        }

        static void DrawIMGUI()
        {
            EditorGUILayout.HelpBox(
                "Auto-hides the editor's title bar / menu / toolbar and reveals them when the mouse approaches the top of the Unity window. Windows-only.",
                MessageType.Info);

            if (KillSwitch)
            {
                EditorGUILayout.HelpBox("Kill switch is ON. Feature is fully disabled. Toggle below to re-enable.", MessageType.Warning);
            }

            EditorGUI.BeginChangeCheck();

            var killSwitch    = EditorGUILayout.Toggle(new GUIContent("Kill switch (panic disable)", "If ON, the entire feature is disabled regardless of other toggles. Use to recover if something breaks."), KillSwitch);
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(killSwitch))
            {
                var enabled = EditorGUILayout.Toggle(new GUIContent("Enabled", "Master toggle for the system."), Enabled);
                var staticMode = EditorGUILayout.Toggle(new GUIContent("Static mode (no auto-hide)",
                    "When ON: bars stay in their configured state — no mouse-driven reveal. " +
                    "Use to permanently hide some bars while keeping others always visible."), StaticMode);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Bars to hide", EditorStyles.boldLabel);
                var hideToolbar  = EditorGUILayout.Toggle(new GUIContent("Hide toolbar",   "Play/Pause/Step row."), HideToolbar);
                var hideMenuBar  = EditorGUILayout.Toggle(new GUIContent("Hide menu bar",  "File/Edit/Assets row. Note: Alt-mnemonics stop working when hidden."), HideMenuBar);
                var hideTitleBar = EditorGUILayout.Toggle(new GUIContent("Hide title bar", "Removes WS_CAPTION. WARNING: you lose drag-to-move and the close/min/max buttons. Use the drag hotkey instead."), HideTitleBar);

                using (new EditorGUI.DisabledScope(staticMode))
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Trigger (auto-hide mode only)", EditorStyles.boldLabel);
                    var revealZone   = EditorGUILayout.IntSlider(new GUIContent("Reveal zone (px)", "How close to the top edge before bars peek in."), RevealZonePx, 1, 32);
                    var hideDelay    = EditorGUILayout.IntSlider(new GUIContent("Hide delay (ms)",  "Debounce before bars hide after the cursor leaves them."), HideDelayMs, 0, 2000);
                    var pollHz       = EditorGUILayout.IntSlider(new GUIContent("Poll rate (Hz)",   "How often the cursor position is sampled."), PollHz, 5, 60);

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
                    var animMs       = EditorGUILayout.IntSlider(new GUIContent("Toolbar animation (ms)", "0 = snap. Menu/title bars cannot be animated and always snap."), AnimDurationMs, 0, 600);

                    if (EditorGUI.EndChangeCheck())
                    {
                        KillSwitch     = killSwitch;
                        Enabled        = enabled;
                        StaticMode     = staticMode;
                        HideToolbar    = hideToolbar;
                        HideMenuBar    = hideMenuBar;
                        HideTitleBar   = hideTitleBar;
                        RevealZonePx   = revealZone;
                        HideDelayMs    = hideDelay;
                        AnimDurationMs = animMs;
                        PollHz         = pollHz;
                        RaiseChanged();
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Compact (menu only)",
                    "Static mode: hide title bar + toolbar, keep menu bar. Taskbar reveal still works. No mouse-driven cycling."),
                    GUILayout.Width(180)))
                {
                    Enabled = true;
                    StaticMode = true;
                    HideTitleBar = true;
                    HideToolbar = true;
                    HideMenuBar = false;
                    RaiseChanged();
                }
                if (GUILayout.Button("Reset to defaults", GUILayout.Width(140))) ResetDefaults();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Force show all bars", GUILayout.Width(160))) HeaderAutoHideController.ForceShowAll();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hotkeys", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Pin / unpin (force visible):  Ctrl+Alt+H", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Drag (when title bar hidden):  Ctrl+Alt+drag-anywhere", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Minimize:                      Ctrl+Shift+M (Window menu)", EditorStyles.miniLabel);
        }
#endif
    }
}
#endif
