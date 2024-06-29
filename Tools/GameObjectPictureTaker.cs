#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Sieunguoimay.Tools
{
    public static class GameObjectPictureTaker
    {
        [MenuItem("GameObject/TakeAPicture/SceneView")]
        public static void TakeAPictureInSceneView(MenuCommand mc)
        {
            var sceneCamera = SceneView.lastActiveSceneView.camera;
            TakeAPictureCommand(mc, sceneCamera);
        }

        [MenuItem("GameObject/TakeAPicture/GameView")]
        public static void TakeAPictureInGameView(MenuCommand mc)
        {
            TakeAPictureCommand(mc, Camera.main);
        }

        private static void TakeAPictureCommand(MenuCommand mc, Camera camera)
        {
            var savePath = EditorUtility.SaveFilePanel("Save path", "Assets", "", "png");
            if (string.IsNullOrEmpty(savePath)) return;

            if (mc.context != null)
            {
                if (mc.context is not GameObject go) return;

                var renderers = go.GetComponentsInChildren<Renderer>().Select(r => new { r.gameObject, r.gameObject.layer }).ToArray();

                var layer = CreateTemporaryLayer("GameObjectPictureTaker");

                foreach (var m in renderers)
                {
                    m.gameObject.layer = layer;
                }

                TakeAPictureAtTransformWithLayer(camera, "GameObjectPictureTaker", savePath);

                foreach (var m in renderers)
                {
                    m.gameObject.layer = m.layer;
                }

                RemoveLayer("GameObjectPictureTaker");
            }
            else
            {
                TakeAPictureByCamera(camera, savePath);
            }
        }

        private static void TakeAPictureAtTransformWithLayer(Camera srcCamera, string layerName, string savePath)
        {

            var lights = UnityEngine.Object.FindObjectsOfType<Light>().Select(l => new { l, l.cullingMask }).ToArray();
            var cullingMask = LayerMask.GetMask(layerName);
            foreach (var l in lights)
            {
                l.l.cullingMask = cullingMask;
            }

            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(srcCamera.transform.position, srcCamera.transform.rotation);
            camera.fieldOfView = srcCamera.fieldOfView;
            camera.projectionMatrix = srcCamera.projectionMatrix;
            camera.aspect = srcCamera.aspect;

            var oldMask = camera.cullingMask;
            camera.cullingMask = cullingMask;
            camera.clearFlags = CameraClearFlags.Nothing;

            TakeAPictureByCamera(camera, savePath);

            foreach (var l in lights)
            {
                l.l.cullingMask = l.cullingMask;
            }
            camera.cullingMask = oldMask;

            UnityEngine.Object.DestroyImmediate(camera.gameObject);
        }

        private static void TakeAPictureByCamera(Camera camera, string savePath)
        {
            Debug.Log(camera.aspect);
            var rt = PrepareTheRenderTexture(camera.aspect);

            var oldRT = camera.targetTexture;
            camera.targetTexture = rt;
            camera.Render();
            camera.targetTexture = oldRT;

            SaveRenderTextureToImage(rt, savePath);
        }

        static int CreateTemporaryLayer(string layerName)
        {
            int newLayerIndex = -1;

            if (LayerMask.NameToLayer(layerName) == -1)
            {
                newLayerIndex = 0;
                while (newLayerIndex < 32 && !string.IsNullOrEmpty(LayerMask.LayerToName(newLayerIndex)))
                {
                    newLayerIndex++;
                }

                if (newLayerIndex < 32)
                {
                    var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                    var layers = tagManager.FindProperty("layers");
                    layers.GetArrayElementAtIndex(newLayerIndex).stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                }
                else
                {
                    Debug.LogError("No available space for a new layer.");
                }
            }
            else
            {
                Debug.LogWarning("Layer " + layerName + " already exists.");
            }

            return newLayerIndex;
        }

        static void RemoveLayer(string layerName)
        {
            int layerIndex = LayerMask.NameToLayer(layerName);
            if (layerIndex != -1)
            {
                var tagManager = new SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                var layers = tagManager.FindProperty("layers");
                layers.GetArrayElementAtIndex(layerIndex).stringValue = "";
                tagManager.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("Layer " + layerName + " not found.");
            }
        }

        public static RenderTexture PrepareTheRenderTexture(float aspect)
        {
            var renderTexture = new RenderTexture((int)(1920 * aspect), 1920, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 8 //1 2 4 8
            };
            renderTexture.Create();
            return renderTexture;
        }

        private static void SaveRenderTextureToImage(RenderTexture rt, string path)
        {
            var currentRenderTexture = RenderTexture.active;
            RenderTexture.active = rt;

            var image = new Texture2D(rt.width, rt.height);
            image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            image.Apply();
            RenderTexture.active = currentRenderTexture;

            var bytes = image.EncodeToPNG();

            if (Application.isEditor)
            {
                UnityEngine.Object.DestroyImmediate(image);
            }
            else
            {
                UnityEngine.Object.Destroy(image);
            }

            var title = "Picture_" + DateTime.Now.ToString("yyymmddhhmmss") + ".png";

            File.WriteAllBytes(path, bytes);
        }
    }
}
#endif