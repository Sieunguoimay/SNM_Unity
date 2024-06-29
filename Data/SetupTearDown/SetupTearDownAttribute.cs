public class SetupTearDownAttribute : System.Attribute
{
    public bool Ignore { get; private set; }

    public SetupTearDownAttribute(bool ignore = false)
    {
        Ignore = ignore;
    }
}