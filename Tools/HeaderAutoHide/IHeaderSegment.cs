#if UNITY_EDITOR && UNITY_EDITOR_WIN
namespace Snm.Tools.HeaderAutoHide
{
    internal interface IHeaderSegment
    {
        string Name { get; }
        bool IsAvailable { get; }
        bool IsCurrentlyHidden { get; }

        void CaptureBaseline();
        void Hide();
        void Show();
        void ForceShowImmediate();

        // Called every editor tick. Default is no-op; segments that need to re-assert
        // their state against Unity reflow (toolbar) override this.
        void EnforceState();
    }
}
#endif
