using System;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public sealed class SceneTextureDrawer : IDisposable
    {
        private readonly Mesh _quad;
        private readonly Material _material;

        public SceneTextureDrawer()
        {
            _quad = CreateUnitQuad();
            _quad.hideFlags = HideFlags.HideAndDontSave;

            _material = new Material(Shader.Find("Unlit/Transparent"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        public void Draw(
            Texture texture,
            Vector3 position,
            Quaternion rotation,
            Vector2 size,
            Vector2 textureScale)
        {
            if (texture == null)
                return;

            _material.mainTexture = texture;
            _material.mainTextureScale = new Vector2(1f / textureScale.x, 1f / textureScale.y);

            var matrix = Matrix4x4.TRS(
                position,
                rotation,
                new Vector3(size.x, size.y, 1f)
            );

            _material.SetPass(0);
            Graphics.DrawMeshNow(_quad, matrix);
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(_quad);
            UnityEngine.Object.DestroyImmediate(_material);
        }

        private static Mesh CreateUnitQuad()
        {
            var mesh = new Mesh
            {
                name = "SceneUnitQuad",
                vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0),
                    new Vector3(-0.5f,  0.5f, 0),
                    new Vector3( 0.5f,  0.5f, 0),
                    new Vector3( 0.5f, -0.5f, 0),
                },

                triangles = new[] { 0, 1, 2, 2, 3, 0 },

                uv = new[]
                    {
                    new Vector2(0,0),
                    new Vector2(0,1),
                    new Vector2(1,1),
                    new Vector2(1,0),
                }
            };

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}