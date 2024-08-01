namespace Identification
{
    public class IdentifiedObject
    {
        private readonly SUID suid;
        public string ID => suid.ID;

        public IdentifiedObject(SUID suid)
        {
            suid.SetIdentifiedObject(this);
            this.suid = suid;
        }
    }
}