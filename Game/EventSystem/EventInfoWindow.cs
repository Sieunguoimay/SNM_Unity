#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace EventSystem
{
    public class EventInfoWindow : UnityEditor.EditorWindow
    {
        [UnityEditor.MenuItem("Tools/Game/EventInfoWindow")]
        public static void OpenWindow()
        {
            GetWindow<EventInfoWindow>().Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.Add(new EventInfoContainerVE(EventInfoContainer.Instance));
        }

        private class EventInfoContainerVE : VisualElement
        {
            private readonly EventInfoContainer container;

            private EventInfo[] _eventInfos;
            private EventInfoVE[] _eventInfoVEs;
            private VisualElement _eventInfoVEsContainer;

            public EventInfoContainerVE(EventInfoContainer container)
            {
                this.container = container;
                var refreshBtn = new Button() { text = "Refresh" };
                refreshBtn.RegisterCallback<ClickEvent>(evt => Refresh());
                Add(refreshBtn);
                Add(_eventInfoVEsContainer = new VisualElement());
                Refresh();
            }

            private void Refresh()
            {
                _eventInfos = ExtractEventInfos().ToArray();
                _eventInfoVEs = _eventInfos.Select((i, j) => new EventInfoVE(i, j)).ToArray();

                _eventInfoVEsContainer.Clear();

                foreach (var ve in _eventInfoVEs)
                {
                    _eventInfoVEsContainer.Add(ve);
                }
            }

            private IEnumerable<EventInfo> ExtractEventInfos()
            {
                return container.AllEventInfos;
                // var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                // return typeof(EventInfoContainer).GetProperties(flags)
                //     .Where(p => typeof(EventInfo).IsAssignableFrom(p.PropertyType))
                //     .Select(p => p.GetValue(container) as EventInfo);
            }
        }

        public class EventInfoVE : Foldout
        {
            private readonly EventInfo data;
            private readonly int index;

            public EventInfoVE(EventInfo data, int index)
            {
                this.data = data;
                this.index = index;
                var subscribers = EventDispatcher.Instance.GetEventReceivers(data).ToArray();
                text = $"{index}. {data.DisplayName} ({subscribers.Length} subscribers)";
                foreach (var s in subscribers)
                {
                    if (s is UnityEngine.Object o)
                    {
                        var btn = new Label() { text = $"{o.name}" };
                        btn.RegisterCallback<ClickEvent>(evt => UnityEditor.EditorGUIUtility.PingObject(o));
                        Add(btn);
                    }
                    else
                    {
                        Add(new Label($"{s}"));
                    }
                }
            }
        }
    }
}
#endif