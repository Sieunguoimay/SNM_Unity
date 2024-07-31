using UnityEngine;

namespace EventSystem
{
    public partial class EventInfoContainer
    {
        private static EventInfoContainer _instance;
        public static EventInfoContainer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EventInfoContainer();
                }
                return _instance;
            }
        }
    }
}