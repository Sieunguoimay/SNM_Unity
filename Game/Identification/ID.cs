namespace Identification
{
    /// <summary>
    /// System Unique IDentifier
    /// </summary>
    public class ID
    {
        private readonly string idValue;
        private IdentifiedObject _identifiedObject;

        public string Value => idValue;
        public IdentifiedObject IdentifiedObject => _identifiedObject;

        public ID(string idValue)
        {
            this.idValue = idValue;
        }

        public void SetIdentifiedObject(IdentifiedObject obj)
        {
            _identifiedObject = obj;
        }
    }
}