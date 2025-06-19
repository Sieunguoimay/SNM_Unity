using UnityEngine;

namespace Snm.Camera
{
    public class CameraWidthConstraint : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera target;

        [SerializeField] private float targetWidth = 10.0f;

        private Vector2 _currentScreenSize;

        private void OnEnable()
        {
            _currentScreenSize.x = Screen.width;
            _currentScreenSize.y = Screen.height;

            UpdateOrthographicSize();
        }

        private void Update()
        {
            TryUpdateOrthographicSize();
        }

        private void TryUpdateOrthographicSize()
        {
            if (_currentScreenSize.x != Screen.width || _currentScreenSize.y != Screen.height)
            {
                UpdateOrthographicSize();
                _currentScreenSize.x = Screen.width;
                _currentScreenSize.y = Screen.height;
            }
        }

        private void UpdateOrthographicSize()
        {
            target.orthographicSize = targetWidth / Screen.width * Screen.height;
        }
    }
}