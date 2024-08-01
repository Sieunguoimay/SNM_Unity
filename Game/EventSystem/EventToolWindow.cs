#if UNITY_EDITOR
using System.Linq;
using UnityEngine.UIElements;

namespace EventSystem
{
    public class EventToolWindow : UnityEditor.EditorWindow
    {
        [UnityEditor.MenuItem("Tools/Game/EventInfoWindow")]
        public static void OpenWindow()
        {
            GetWindow<EventToolWindow>().Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.Add(new EventInfoContainerVE(EventObjectContainer.Instance));
        }

        private class EventInfoContainerVE : VisualElement
        {
            private readonly EventObjectContainer container;

            private EventObjectVE[] _eventObjectVEs;
            private readonly VisualElement eventObjectVEsContainer;

            public EventInfoContainerVE(EventObjectContainer container)
            {
                this.container = container;
                var refreshBtn = new Button() { text = "Refresh" };
                refreshBtn.RegisterCallback<ClickEvent>(evt => Refresh());
                Add(refreshBtn);
                Add(eventObjectVEsContainer = new VisualElement());
                Refresh();
            }

            private void Refresh()
            {
                _eventObjectVEs = container.GetAllEventObjects().Select((i, j) => new EventObjectVE(i, j)).ToArray();

                eventObjectVEsContainer.Clear();

                foreach (var ve in _eventObjectVEs)
                {
                    eventObjectVEsContainer.Add(ve);
                }
            }
        }

        public class EventObjectVE : Foldout
        {
            private readonly EventObject data;
            private readonly int index;

            public EventObjectVE(EventObject data, int index)
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