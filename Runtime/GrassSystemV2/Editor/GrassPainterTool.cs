using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Snm.GrassSystemV2.Editor
{
    /// <summary>
    /// Scene-view brush for painting grass, Terrain-style:
    ///
    ///   Select the GrassWorld → pick the Grass Painter in the toolbar →
    ///   drag over any collider to paint. Hold CTRL to erase.
    ///   [ and ] change the brush size. Settings live in a small overlay.
    ///
    /// Painting writes straight into GrassWorldData with full Undo support,
    /// and the world refreshes live — what you see is what ships.
    /// </summary>
    [EditorTool("Grass Painter", typeof(GrassWorld))]
    public class GrassPainterTool : EditorTool
    {
        // Session-persistent brush settings.
        static float _brushRadius = 3f;
        static float _density = 4f;           // blades per square meter
        static int _selectedType;
        static LayerMask _groundMask = ~0;
        static float _maxSlopeAngle = 45f;

        Vector3 _lastStampPosition;
        bool _hasLastStamp;
        System.Random _rng = new();

        static GUIContent _icon;
        public override GUIContent toolbarIcon =>
            _icon ??= new GUIContent(EditorGUIUtility.IconContent("TerrainInspector.TerrainToolPlants").image,
                "Grass Painter (V2)");

        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView) return;
            var world = target as GrassWorld;
            if (world == null || world.Data == null) return;

            var currentEvent = Event.current;

            // Block scene selection while painting.
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            // Brush resize shortcuts.
            if (currentEvent.type == EventType.KeyDown)
            {
                if (currentEvent.keyCode == KeyCode.LeftBracket) { _brushRadius = Mathf.Max(0.25f, _brushRadius * 0.8f); currentEvent.Use(); }
                if (currentEvent.keyCode == KeyCode.RightBracket) { _brushRadius = Mathf.Min(50f, _brushRadius * 1.25f); currentEvent.Use(); }
            }

            var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            bool hasHit = Physics.Raycast(ray, out var hit, 5000f, _groundMask);

            if (hasHit)
            {
                bool erasing = currentEvent.control;
                Handles.color = erasing ? new Color(1f, 0.3f, 0.2f, 0.9f) : new Color(0.3f, 1f, 0.4f, 0.9f);
                Handles.DrawWireDisc(hit.point, hit.normal, _brushRadius, 2f);
                Handles.DrawLine(hit.point, hit.point + hit.normal * 0.5f);

                bool paintEvent =
                    (currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag)
                    && currentEvent.button == 0 && !currentEvent.alt;

                if (paintEvent && ShouldStamp(hit.point, currentEvent.type == EventType.MouseDown))
                {
                    if (erasing) Erase(world, hit.point);
                    else Paint(world, hit.point);
                    currentEvent.Use();
                }
            }

            DrawSettingsOverlay(world);
            window.Repaint();
        }

        /// <summary>Stamp when the pointer traveled half a brush radius (smooth strokes, no flooding).</summary>
        bool ShouldStamp(Vector3 position, bool isMouseDown)
        {
            if (isMouseDown || !_hasLastStamp ||
                Vector3.Distance(position, _lastStampPosition) >= _brushRadius * 0.5f)
            {
                _lastStampPosition = position;
                _hasLastStamp = true;
                return true;
            }
            return false;
        }

        void Paint(GrassWorld world, Vector3 center)
        {
            // Half the disc area per stamp — strokes overlap, so this lands near the target density.
            int count = Mathf.Max(1, Mathf.RoundToInt(_density * Mathf.PI * _brushRadius * _brushRadius * 0.5f));

            var settings = new GrassPaintOps.ScatterSettings
            {
                TypeIndices = new[] { Mathf.Clamp(_selectedType, 0, world.Data.types.Length - 1) },
                GroundMask = _groundMask,
                MaxSlopeAngle = _maxSlopeAngle,
                RaycastOriginHeight = 2f,
                RaycastMaxDistance = 50f,
            };

            GrassPaintOps.WithUndo(world.Data, "Paint grass",
                () => GrassPaintOps.ScatterInDisc(world.Data, center, _brushRadius, count, settings, _rng));
        }

        void Erase(GrassWorld world, Vector3 center)
        {
            GrassPaintOps.WithUndo(world.Data, "Erase grass",
                () => world.Data.RemoveInRadius(center, _brushRadius));
        }

        void DrawSettingsOverlay(GrassWorld world)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10f, 10f, 250f, 190f), GUIContent.none, GUI.skin.window);
            GUILayout.Label("Grass Painter — CTRL = erase, [ ] = size", EditorStyles.miniBoldLabel);

            _brushRadius = EditorGUILayout.Slider("Radius", _brushRadius, 0.25f, 50f);
            _density = EditorGUILayout.Slider("Density /m²", _density, 0.1f, 50f);

            var typeNames = new string[world.Data.types.Length];
            for (int i = 0; i < typeNames.Length; i++)
            {
                typeNames[i] = world.Data.types[i] != null ? world.Data.types[i].name : $"<empty {i}>";
            }
            if (typeNames.Length > 0)
            {
                _selectedType = EditorGUILayout.Popup("Type", Mathf.Clamp(_selectedType, 0, typeNames.Length - 1), typeNames);
            }

            _groundMask = LayerMaskField("Ground", _groundMask);
            _maxSlopeAngle = EditorGUILayout.Slider("Max slope °", _maxSlopeAngle, 0f, 90f);

            GUILayout.Label($"Painted total: {world.Data.TotalInstanceCount:N0}", EditorStyles.miniLabel);
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        static LayerMask LayerMaskField(string label, LayerMask mask)
        {
            var concatenated = UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(mask);
            concatenated = EditorGUILayout.MaskField(label, concatenated, UnityEditorInternal.InternalEditorUtility.layers);
            return UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(concatenated);
        }
    }
}
