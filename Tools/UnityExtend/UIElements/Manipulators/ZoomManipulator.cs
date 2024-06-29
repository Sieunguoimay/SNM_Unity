using UnityEngine;
using UnityEngine.UIElements;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace Common.UnityExtend.UIElements
{
    public class ZoomManipulator : IManipulator
    {
        public VisualElement _target;
        public VisualElement target
        {
            get => _target;
            set
            {
                _target = value;
                SetupEvents();
            }
        }

        private readonly VisualElement _zoomContent;
        private float _scale = 1f;
        private readonly float _scaleMin;
        private readonly float _scaleMax;

        public ZoomManipulator(VisualElement zoomContent, float zoomMin = .25f, float zoomMax = 4f)
        {
            _scaleMax = zoomMax;
            _scaleMin = zoomMin;
            _zoomContent = zoomContent;
        }

        public void ForceZoom(Vector2 contentCenter, float scale)
        {
            var parentCenter = new Vector2(target.contentRect.width / 2, target.contentRect.height / 2);

            var newScale = Mathf.Clamp(_scale * scale, _scaleMin, _scaleMax);

            var contentOrigin = new Vector2(_zoomContent.style.left.value.value, _zoomContent.style.top.value.value);
            var contentOriginOffset = contentOrigin - contentCenter;
            var scaledOriginOffset = contentOriginOffset * (newScale / _scale);
            var newOrigin = parentCenter + scaledOriginOffset;

            _zoomContent.style.left = newOrigin.x;
            _zoomContent.style.top = newOrigin.y;

            _scale = newScale;
            _zoomContent.transform.scale = Vector2.one * _scale;
            _zoomContent.MarkDirtyRepaint();
        }

        private void SetupEvents()
        {
            target.RegisterCallback<WheelEvent>(OnWheelEvent);
        }

        private void OnWheelEvent(WheelEvent wheelEvent)
        {
            var localMousePos = wheelEvent.localMousePosition;

            var delta = wheelEvent.delta.y * .1f;
            Zoom(localMousePos, -delta * _scale);

        }

        private void Zoom(Vector2 localPivot, float delta)
        {
            var valid = IsZoomValueValid(_scale + delta);
            var newDelta = valid - _scale;
            var changingFactor = newDelta / _scale;

            _scale = valid;
            _zoomContent.transform.scale = Vector2.one * _scale;

            var localOrigin = new Vector2(_zoomContent.style.left.value.value, _zoomContent.style.top.value.value);
            var offset = localPivot - localOrigin;
            var move = -offset * changingFactor;
            var newOrigin = localOrigin + move;

            _zoomContent.style.left = newOrigin.x;
            _zoomContent.style.top = newOrigin.y;

            target.MarkDirtyRepaint();
        }
        private float IsZoomValueValid(float newValue)
        {
            if (newValue >= _scaleMax)
            {
                return _scaleMax;
            }
            if (newValue <= _scaleMin)
            {
                return _scaleMin;
            }
            return newValue;
        }
    }

}