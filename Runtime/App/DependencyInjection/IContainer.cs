namespace Snm.App.DependencyInjection
{
    public interface IContainer
    {
        BindingBuilder<T> Bind<T>(string id = null) where T : class;
        RuntimeContainer Build();
    }
}
