using System.Collections.Generic;
using Snm.Runtime.Foundation;
using UnityEngine;

namespace Snm.GrassSystem
{
    public class FrustumCullingFeature : IGrassFeature
    {
        readonly List<GrassRenderer> _renderers;
        readonly float _margin;
        readonly Plane[] _frustumPlanes = new Plane[6];
        readonly Dictionary<GrassRenderer, Matrix4x4[]> _tempBuffers = new();
        readonly IMainCameraProvider _cameraProvider;

        public FrustumCullingFeature(List<GrassRenderer> renderers, float margin, IMainCameraProvider cameraProvider)
        {
            _renderers = renderers;
            _margin = margin;
            _cameraProvider = cameraProvider ?? MainCameraProvider.Default;

            foreach (var r in renderers)
                _tempBuffers[r] = new Matrix4x4[r.AllMatrices.Count];
        }

        public void OnUpdate(float deltaTime)
        {
            var camera = _cameraProvider.Current;
            if (camera == null) return;

            GeometryUtility.CalculateFrustumPlanes(camera, _frustumPlanes);

            foreach (var renderer in _renderers)
            {
                var allMatrices = renderer.AllMatrices;
                var temp = _tempBuffers[renderer];
                int visibleCount = 0;

                for (int i = 0; i < allMatrices.Count; i++)
                {
                    var m = allMatrices[i];
                    var pos = new Vector3(m.m03, m.m13, m.m23);
                    if (IsPointInFrustum(pos, _margin))
                        temp[visibleCount++] = m;
                }

                renderer.UpdateVisibleInstances(temp, visibleCount);
            }
        }

        bool IsPointInFrustum(Vector3 point, float margin)
        {
            for (int i = 0; i < 6; i++)
            {
                if (_frustumPlanes[i].GetDistanceToPoint(point) < -margin)
                    return false;
            }
            return true;
        }

        public void Dispose() { }
    }
}
