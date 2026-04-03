#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Snm.Graphics3D.Toolkit;

namespace Snm.Graphics3D.Modeling
{
    public class MeshPrimitivesWindow : EditorWindow
    {
        enum PrimitiveType { Plane, Box, Sphere, Icosphere, Cylinder, Cone, Tube, Torus, Capsule }

        [SerializeField] PrimitiveType type = PrimitiveType.Box;
        [SerializeField] Material defaultMaterial;

        // Plane
        [SerializeField] float planeWidth = 1f, planeHeight = 1f;
        [SerializeField] int planeSegsX = 1, planeSegsZ = 1;

        // Box
        [SerializeField] float boxWidth = 1f, boxHeight = 1f, boxDepth = 1f;
        [SerializeField] int boxSegsX = 1, boxSegsY = 1, boxSegsZ = 1;

        // Sphere
        [SerializeField] float sphereRadius = 0.5f;
        [SerializeField] int sphereLon = 24, sphereLat = 16;

        // Icosphere
        [SerializeField] float icoRadius = 0.5f;
        [SerializeField] int icoSubdivisions = 2;

        // Cylinder
        [SerializeField] float cylRadius = 0.5f, cylHeight = 1f;
        [SerializeField] int cylSides = 24, cylHeightSegs = 1;
        [SerializeField] bool cylCap = true;

        // Cone
        [SerializeField] float coneRadiusBottom = 0.5f, coneRadiusTop;
        [SerializeField] float coneHeight = 1f;
        [SerializeField] int coneSides = 24, coneHeightSegs = 1;

        // Tube
        [SerializeField] float tubeInner = 0.3f, tubeOuter = 0.5f, tubeHeight = 1f;
        [SerializeField] int tubeSides = 24, tubeHeightSegs = 1;

        // Torus
        [SerializeField] float torusMajor = 0.5f, torusMinor = 0.15f;
        [SerializeField] int torusMajorSegs = 24, torusMinorSegs = 12;

        // Capsule
        [SerializeField] float capsuleRadius = 0.25f, capsuleHeight = 1f;
        [SerializeField] int capsuleSegments = 16;

        Vector2 _scrollPos;

        [MenuItem("Tools/Snm/3D Toolkit/Modeling/Primitives Generator", priority = 2)]
        public static void Open()
        {
            var w = GetWindow<MeshPrimitivesWindow>("Primitives");
            w.minSize = new Vector2(300, 350);
        }

        void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawContent();
            EditorGUILayout.EndScrollView();
        }

        internal void DrawContent()
        {
            ToolkitGUI.Title("Primitives Generator");

            type = (PrimitiveType)EditorGUILayout.EnumPopup("Shape", type);
            EditorGUILayout.Space(ToolkitWindowStyles.ItemSpacing);

            defaultMaterial = (Material)EditorGUILayout.ObjectField(
                "Material", defaultMaterial, typeof(Material), false);

            ToolkitGUI.SectionHeader($"{type} Parameters");

            switch (type)
            {
                case PrimitiveType.Plane: DrawPlaneParams(); break;
                case PrimitiveType.Box: DrawBoxParams(); break;
                case PrimitiveType.Sphere: DrawSphereParams(); break;
                case PrimitiveType.Icosphere: DrawIcosphereParams(); break;
                case PrimitiveType.Cylinder: DrawCylinderParams(); break;
                case PrimitiveType.Cone: DrawConeParams(); break;
                case PrimitiveType.Tube: DrawTubeParams(); break;
                case PrimitiveType.Torus: DrawTorusParams(); break;
                case PrimitiveType.Capsule: DrawCapsuleParams(); break;
            }

            GUILayout.Space(ToolkitWindowStyles.SectionSpacing);

            if (ToolkitGUI.BigButton("Create"))
                CreatePrimitive();

            EditorGUILayout.Space(ToolkitWindowStyles.ItemSpacing);

            if (ToolkitGUI.ActionButton("Create & Select"))
            {
                var go = CreatePrimitive();
                if (go != null) Selection.activeGameObject = go;
            }

            GUILayout.Space(ToolkitWindowStyles.SectionSpacing);

            // Show save option for the selected object's mesh
            var selectedGo = Selection.activeGameObject;
            var selectedMf = selectedGo != null ? selectedGo.GetComponent<MeshFilter>() : null;
            var selectedMesh = selectedMf != null ? selectedMf.sharedMesh : null;
            if (selectedMesh != null && ToolkitGUI.GetMeshLocation(selectedMesh) == MeshLocation.Unsaved)
            {
                ToolkitGUI.SectionHeader("Selected Mesh");
                ToolkitGUI.MeshStatus(selectedMesh);
                if (ToolkitGUI.ActionButton("Save Mesh as Asset"))
                {
                    ToolkitGUI.SaveMeshCopy(selectedMesh, selectedMf, selectedMesh.name ?? type.ToString());
                }
            }
        }

        #region Parameter UIs

        void DrawPlaneParams()
        {
            planeWidth = EditorGUILayout.FloatField("Width", planeWidth);
            planeHeight = EditorGUILayout.FloatField("Height", planeHeight);
            planeSegsX = EditorGUILayout.IntSlider("Segments X", planeSegsX, 1, 100);
            planeSegsZ = EditorGUILayout.IntSlider("Segments Z", planeSegsZ, 1, 100);
        }

        void DrawBoxParams()
        {
            boxWidth = EditorGUILayout.FloatField("Width", boxWidth);
            boxHeight = EditorGUILayout.FloatField("Height", boxHeight);
            boxDepth = EditorGUILayout.FloatField("Depth", boxDepth);
            boxSegsX = EditorGUILayout.IntSlider("Segments X", boxSegsX, 1, 20);
            boxSegsY = EditorGUILayout.IntSlider("Segments Y", boxSegsY, 1, 20);
            boxSegsZ = EditorGUILayout.IntSlider("Segments Z", boxSegsZ, 1, 20);
        }

        void DrawSphereParams()
        {
            sphereRadius = EditorGUILayout.FloatField("Radius", sphereRadius);
            sphereLon = EditorGUILayout.IntSlider("Longitude", sphereLon, 3, 64);
            sphereLat = EditorGUILayout.IntSlider("Latitude", sphereLat, 2, 64);
        }

        void DrawIcosphereParams()
        {
            icoRadius = EditorGUILayout.FloatField("Radius", icoRadius);
            icoSubdivisions = EditorGUILayout.IntSlider("Subdivisions", icoSubdivisions, 0, 5);

            int triCount = 20 * (int)Mathf.Pow(4, icoSubdivisions);
            EditorGUILayout.HelpBox($"~{triCount} triangles", MessageType.None);
        }

        void DrawCylinderParams()
        {
            cylRadius = EditorGUILayout.FloatField("Radius", cylRadius);
            cylHeight = EditorGUILayout.FloatField("Height", cylHeight);
            cylSides = EditorGUILayout.IntSlider("Sides", cylSides, 3, 64);
            cylHeightSegs = EditorGUILayout.IntSlider("Height Segments", cylHeightSegs, 1, 20);
            cylCap = EditorGUILayout.Toggle("Caps", cylCap);
        }

        void DrawConeParams()
        {
            coneRadiusBottom = EditorGUILayout.FloatField("Bottom Radius", coneRadiusBottom);
            coneRadiusTop = EditorGUILayout.FloatField("Top Radius", coneRadiusTop);
            coneHeight = EditorGUILayout.FloatField("Height", coneHeight);
            coneSides = EditorGUILayout.IntSlider("Sides", coneSides, 3, 64);
            coneHeightSegs = EditorGUILayout.IntSlider("Height Segments", coneHeightSegs, 1, 20);
        }

        void DrawTubeParams()
        {
            tubeInner = EditorGUILayout.FloatField("Inner Radius", tubeInner);
            tubeOuter = EditorGUILayout.FloatField("Outer Radius", tubeOuter);
            tubeHeight = EditorGUILayout.FloatField("Height", tubeHeight);
            tubeSides = EditorGUILayout.IntSlider("Sides", tubeSides, 3, 64);
            tubeHeightSegs = EditorGUILayout.IntSlider("Height Segments", tubeHeightSegs, 1, 20);
        }

        void DrawTorusParams()
        {
            torusMajor = EditorGUILayout.FloatField("Major Radius", torusMajor);
            torusMinor = EditorGUILayout.FloatField("Minor Radius", torusMinor);
            torusMajorSegs = EditorGUILayout.IntSlider("Major Segments", torusMajorSegs, 3, 64);
            torusMinorSegs = EditorGUILayout.IntSlider("Minor Segments", torusMinorSegs, 3, 64);
        }

        void DrawCapsuleParams()
        {
            capsuleRadius = EditorGUILayout.FloatField("Radius", capsuleRadius);
            capsuleHeight = EditorGUILayout.FloatField("Height", capsuleHeight);
            capsuleSegments = EditorGUILayout.IntSlider("Segments", capsuleSegments, 4, 64);
        }

        #endregion

        GameObject CreatePrimitive()
        {
            Mesh mesh = type switch
            {
                PrimitiveType.Plane => PrimitiveGenerators.CreatePlane(planeWidth, planeHeight, planeSegsX, planeSegsZ),
                PrimitiveType.Box => PrimitiveGenerators.CreateBox(boxWidth, boxHeight, boxDepth, boxSegsX, boxSegsY, boxSegsZ),
                PrimitiveType.Sphere => PrimitiveGenerators.CreateSphere(sphereRadius, sphereLon, sphereLat),
                PrimitiveType.Icosphere => PrimitiveGenerators.CreateIcosphere(icoRadius, icoSubdivisions),
                PrimitiveType.Cylinder => PrimitiveGenerators.CreateCylinder(cylRadius, cylHeight, cylSides, cylHeightSegs, cylCap),
                PrimitiveType.Cone => PrimitiveGenerators.CreateCone(coneRadiusBottom, coneRadiusTop, coneHeight, coneSides, coneHeightSegs),
                PrimitiveType.Tube => PrimitiveGenerators.CreateTube(tubeInner, tubeOuter, tubeHeight, tubeSides, tubeHeightSegs),
                PrimitiveType.Torus => PrimitiveGenerators.CreateTorus(torusMajor, torusMinor, torusMajorSegs, torusMinorSegs),
                PrimitiveType.Capsule => PrimitiveGenerators.CreateCapsule(capsuleRadius, capsuleHeight, capsuleSegments),
                _ => null
            };

            if (mesh == null) return null;

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            var selectedParent = Selection.activeTransform;

            var go = new GameObject(type.ToString());

            if (selectedParent != null)
            {
                go.transform.SetParent(selectedParent, false);
            }
            else if (prefabStage != null)
            {
                go.transform.SetParent(prefabStage.prefabContentsRoot.transform, false);
            }
            else
            {
                var sceneView = SceneView.lastActiveSceneView;
                if (sceneView != null)
                    go.transform.position = sceneView.pivot;
            }

            if (prefabStage != null)
            {
                mesh.name = type.ToString();
                AssetDatabase.AddObjectToAsset(mesh, prefabStage.assetPath);
            }

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = defaultMaterial != null ? defaultMaterial : GetDefaultMaterial();

            MeshUndoHelper.RegisterCreatedGameObject(go, $"Create {type}");

            if (prefabStage != null)
                EditorUtility.SetDirty(prefabStage.prefabContentsRoot);

            return go;
        }

        static Material _cachedDefaultMat;

        static Material GetDefaultMaterial()
        {
            if (_cachedDefaultMat == null)
            {
                // Try to find the default material
                var go = GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
                _cachedDefaultMat = go.GetComponent<MeshRenderer>().sharedMaterial;
                DestroyImmediate(go);
            }
            return _cachedDefaultMat;
        }
    }
}
#endif
