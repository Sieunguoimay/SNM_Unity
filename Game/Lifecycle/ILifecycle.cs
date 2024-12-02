namespace SNM.Lifecycle
{
    public interface ILifecycle
    {
        void Initialize();    // Handles internal initialization.
        void Setup();         // Configures with external dependencies.
        void Teardown();      // Cleans up or disconnects external dependencies.
        void Cleanup();       // Finalizes and cleans up internal state.
    }
}
