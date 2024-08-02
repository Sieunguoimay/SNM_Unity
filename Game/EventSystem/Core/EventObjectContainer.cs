namespace EventSystem
{
    public partial class EventObjectContainer
    {
        private static EventObjectContainer _instance;
        public static EventObjectContainer Instance => _instance ??= new();
    }
}