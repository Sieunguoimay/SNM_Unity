using System.Collections;
using UnityEngine;

namespace SNM.Camera
{
    public class CameraFOVConstraintByVisibleArea : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera cam;
        [SerializeField] private Vector3 worldTarget;
        [SerializeField] private Vector2 rectSize;

        private Vector2 _currentScreenSize;

        private void Start()
        {
            _currentScreenSize.x = Screen.width;
            _currentScreenSize.y = Screen.height;
            TryUpdateFOV();

            StartCoroutine(IntervalUpdate());
        }

        private IEnumerator IntervalUpdate()
        {
            while (true)
            {
                if (_currentScreenSize.x != Screen.width || _currentScreenSize.y != Screen.height)
                {
                    _currentScreenSize.x = Screen.width;
                    _currentScreenSize.y = Screen.height;
                    TryUpdateFOV();
                }
                yield return new WaitForSeconds(1f);
            }
        }

        [ContextMenu("TryUpdateFOV")]
        private void TryUpdateFOV()
        {
            var constraintAspect = rectSize.x / rectSize.y;
            var currentAspect = cam.aspect;

            var targetHeight = currentAspect < constraintAspect ? rectSize.x / currentAspect : rectSize.y;

            var distance = Vector3.Distance(cam.transform.position, worldTarget);
            var tan = targetHeight / (2f * distance);
            cam.fieldOfView = 2f * Mathf.Atan(tan) * Mathf.Rad2Deg;

            Debug.Log("TryUpdateFOV");
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(worldTarget, new Vector3(1, 1, 1));
            var size = Vector3.right * rectSize.x + Vector3.up * rectSize.y;
            size.z = 0;
            Gizmos.matrix = cam.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(cam.transform.InverseTransformPoint(worldTarget), size);
        }

        [ContextMenu("SnapPositionToCameraCenter")]
        private void SnapPositionToCameraCenter()
        {
            var plane = new Plane(cam.transform.forward, worldTarget);
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (plane.Raycast(ray, out var distance))
            {
                worldTarget = ray.origin + distance * ray.direction;
            }
        }

        [ContextMenu("MatchScreenSize")]
        private void MatchScreenSize()
        {
            var distance = Vector3.Distance(cam.transform.position, worldTarget);

            float verticalFOV = cam.fieldOfView * Mathf.Deg2Rad;
            Debug.Log(cam.fieldOfView);
            float verticalSize = 2f * distance * Mathf.Tan(verticalFOV / 2f);
            float widthOverHeight = cam.aspect;
            float horizontalSize = verticalSize * widthOverHeight;

            rectSize.x = horizontalSize;
            rectSize.y = verticalSize;

            Debug.Log(rectSize.x / rectSize.y);

        }
    }
}