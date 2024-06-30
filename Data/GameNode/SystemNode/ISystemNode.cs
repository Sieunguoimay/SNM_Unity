namespace GameNode
{
    public interface ISystemNode : IGameNode
    {
        IKeyObjectCotainer Dependencies { get; }
    }
}