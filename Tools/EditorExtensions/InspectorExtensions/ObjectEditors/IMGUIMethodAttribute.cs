namespace Snm.Tools.InspectorExtra
{
    public class IMGUIMethodAttribute : System.Attribute
    {
        public bool ShowTitle { get; private set; }

        public IMGUIMethodAttribute(bool showTitle = true)
        {
            ShowTitle = showTitle;
        }
    }
}