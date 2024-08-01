namespace Identification
{
    /// <summary>
    /// System Unique IDentifier
    /// </summary>
    public class SUID
    {
        private readonly string id;
        private IdentifiedObject _identifiedObject;

        public string ID => id;
        public IdentifiedObject IdentifiedObject => _identifiedObject;

        public SUID(string iD)
        {
            id = iD;
        }

        public void SetIdentifiedObject(IdentifiedObject obj)
        {
            _identifiedObject = obj;
        }
    }
}