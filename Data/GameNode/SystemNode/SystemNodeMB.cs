namespace GameNode
{
    public abstract class SystemNodeMB : NodeSO, ISystemNode
    {
        public IKeyObjectCotainer Dependencies { get; } = new KeyObjectCotainer();

        public override void Setup()
        {
            OnSetupOffSystemDependencies();
            base.Setup();
        }

        public override void TearDown()
        {
            base.TearDown();
            OnTearDownOffSystemDependencies();
        }

        public abstract void OnSetupOffSystemDependencies();
        public abstract void OnTearDownOffSystemDependencies();
    }
}