using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class AssetDependencyGraph
{
    private class ObjectVisitingPath : VisualElement
    {
        private readonly List<Segment> _segments = new();
        public Object Current => _segments.Count > 0 ? _segments[^1].target : null;
        public Object Root => _segments.Count > 0 ? _segments[0].target : null;
        public event System.Action<ObjectVisitingPath> OnCurrentChanged;
        public bool Navigatable => _segments.Count > 1;
        public ObjectVisitingPath()
        {
            style.flexDirection = FlexDirection.Row;
            OnCurrentChanged += HandleCurrentSegmentChanged;
        }

        private void HandleCurrentSegmentChanged(ObjectVisitingPath obj)
        {
            foreach (var s in _segments)
            {
                s.SetEnabled(true);
            }

            if (_segments.Count > 0)
            {
                _segments[^1].SetEnabled(false);
            }
        }

        private Segment CreateSegment(Object target)
        {
            var segment = new Segment
            {
                text = target.name,
                target = target,
                focusable = false
            };

            //if (_defaultBgColor == null)
            //{
            //    _defaultBgColor = segment.style.backgroundColor;
            //}

            segment.style.marginLeft = 0;
            segment.style.marginRight = 0;
            segment.clicked += () =>
            {
                if (segment.target != Current)
                {
                    JumpTo(segment);
                }
            };
            return segment;
        }

        private void ClearTrain()
        {
            Clear();
            _segments.Clear();
        }
        public void ClearStack()
        {
            ClearTrain();
        }
        public void Push(Object obj)
        {
            var segment = CreateSegment(obj);
            _segments.Add(segment);
            this.Add(segment);
            OnCurrentChanged?.Invoke(this);
        }
        public bool Back()
        {
            if (_segments.Count > 1)
            {
                var segment = _segments.Last();
                _segments.Remove(segment);
                this.Remove(segment);
                OnCurrentChanged?.Invoke(this);

                return true;
            }
            return false;
        }

        private void JumpTo(Segment sm)
        {
            var index = _segments.IndexOf(sm);
            if (index < _segments.Count - 1)
            {
                var n = _segments.Count - (index + 1);

                for (var i = 0; i < n; i++)
                {
                    var segment = _segments.Last();

                    _segments.Remove(segment);
                    this.Remove(segment);
                }
            }
            OnCurrentChanged?.Invoke(this);
        }
        private class Segment : Button
        {
            public Object target;
        }
    }
}