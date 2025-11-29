using UnityEngine;

namespace Snm.Components.Camera
{
    public class CameraDragMover : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float dragSpeed = 2f;

        private Vector3 _beginMousePos;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                OnMouseButtonBeginPress();
            }

            if (Input.GetMouseButton(0))
            {
                OnMouseButtonPressing();
            }
        }

        private void OnMouseButtonBeginPress()
        {
            _beginMousePos = Input.mousePosition;
        }

        private void OnMouseButtonPressing()
        {
            var mousePos = Input.mousePosition;
            var diff = UnityEngine.Camera.main.ScreenToViewportPoint(mousePos - _beginMousePos);
            var move = new Vector3(-diff.x * dragSpeed, 0f, -diff.y * dragSpeed);

            cameraTransform.Translate(move, Space.World);

            _beginMousePos = mousePos;
        }
    }
}