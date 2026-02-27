#if UNITY_EDITOR
namespace Snm.Tools.InspectorExtensions
{
    public interface IInspectorExtensionFilter
    {
        bool IsMatch(
            IInspectorExtension extension,
            InspectorExtensionContext context);
    }
}
#endif