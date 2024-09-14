using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ClickBoxManager : MonoBehaviour
{
    private static ClickBoxManager _instance;
    public static ClickBoxManager Instance
    {
        get
        {
            if (_isDestroyed) return null;

            if (_instance == null)
            {
                _instance = new GameObject($"[Singleton]{nameof(ClickBoxManager)}").AddComponent<ClickBoxManager>();
            }
            return _instance;
        }
    }

    private static bool _isDestroyed = false;
    private readonly List<ClickBox> clickBoxes = new();


    private void OnDestroy()
    {
        _isDestroyed = true;
    }

    public void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            var minDistance = float.MaxValue;
            ClickBox minDistanceClickBox = null;
            Vector3? minHit = null;

            foreach (var c in clickBoxes)
            {
                var worldToLocalMatrix = c.transform.worldToLocalMatrix;

                var localRay = new Ray(
                    worldToLocalMatrix.MultiplyPoint(ray.origin),
                    worldToLocalMatrix.MultiplyVector(ray.direction)
                );

                var bounds = new Bounds(Vector3.zero, c.BoxSize);
                if (bounds.IntersectRay(localRay, out var distance))
                {
                    if (minDistance > distance)
                    {
                        minDistance = distance;
                        minDistanceClickBox = c;
                        minHit = c.transform.TransformPoint(localRay.origin + localRay.direction * distance);
                    }
                }
            }
            
            if (minDistanceClickBox != null && minHit != null)
            {
                minDistanceClickBox.HandleClicked(minHit.Value);
            }
        }
    }

    public void RegisterClickBox(ClickBox clickBox)
    {
        clickBoxes.Add(clickBox);
    }

    public void UnregisterClickBox(ClickBox clickBox)
    {
        clickBoxes.Remove(clickBox);
    }
}