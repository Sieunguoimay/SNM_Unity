namespace Identification
{
    public class IdentifiedObject
    {
        private readonly ID id;
        public string ID => id.Value;

        public IdentifiedObject(ID id)
        {
            id.SetIdentifiedObject(this);
            this.id = id;
        }
    }
}