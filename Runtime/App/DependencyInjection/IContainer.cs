namespace Snm.Runtime.App.DependencyInjection
{
    public interface IBindingContext
    {
        BindingBuilder<T> Bind<T>(string id = null) where T : class;
    }

    public interface IContainer
    {
        RuntimeContainer Build();
    }
}
