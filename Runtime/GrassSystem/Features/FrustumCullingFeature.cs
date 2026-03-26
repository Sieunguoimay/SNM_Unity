using System.Collections.Generic;
using UnityEngine;

namespace Snm.GrassSystem
{
    public class FrustumCullingFeature : IGrassFeature
    {
        readonly List<GrassRenderer> _renderers;
        readonly float _margin;
        readonly Plane[] _frustumPlanes = new Plane[6];
        readonly Dictionary<GrassRenderer, Matrix4x4[]> _tempBuffers = new();

        public FrustumCullingFeature(List<GrassRenderer> renderers, float margin)
        {
            _renderers = renderers;
            _margin = margin;

            foreach (var r in renderers)
                _tempBuffers[r] = new Matrix4x4[r.AllMatrices.Length];
        }

        public void OnUpdate(float deltaTime)
        {
            var cam = Camera.main;
            if (cam == null) return;

            GeometryUtility.CalculateFrustumPlanes(cam, _frustumPlanes);

            foreach (var renderer in _renderers)
            {
                var allMatrices = renderer.AllMatrices;
                var temp = _tempBuffers[renderer];
                int visibleCount = 0;

                for (int i = 0; i < allMatrices.Length; i++)
                {
                    var pos = new Vector3(allMatrices[i].m03, allMatrices[i].m13, allMatrices[i].m23);
                    if (IsPointInFrustum(pos, _margin))
                        temp[visibleCount++] = allMatrices[i];
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
