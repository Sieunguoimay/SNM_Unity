
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EventSystem
{
    [Serializable]
    public class EventObjectSelector
    {
        [StringSelector(nameof(EventIDs), false, false, true, nameof(MaskFunction))]
        [SerializeField] private string eventID;

        private EventObject _eventObject;

        private IEnumerable<string> EventIDs
            => EventObjectContainer.Instance.GetAllEventObjects().Select(e => e.ID);

        public EventObject EventObject
        {
            get
            {
                if (_eventObject == null)
                {
                    Cache();
                }
                return _eventObject;
            }
        }

        private string MaskFunction(string value)
        {
            return EventObjectContainer.Instance.GetAllEventObjects()
                .FirstOrDefault(e => e.ID == value)?.DisplayName ?? value;
        }

        public void Cache()
        {
            _eventObject = EventObjectContainer.Instance.GetAllEventObjects()
                .FirstOrDefault(i => i.ID == eventID);

            if (_eventObject == null)
            {
                Debug.LogError($"Event not found {eventID}");
            }
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(EventObjectSelector))]
    public class EventObjectSelectorDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            UnityEditor.EditorGUILayout.PropertyField(property.FindPropertyRelative("eventID"), label);
        }
    }
#endif
}