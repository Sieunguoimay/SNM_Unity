namespace Snm.Identification
{
    public class IDHelper
    {
        public static string GenerateID()
        {
            return System.Guid.NewGuid().ToString()[..8];
        }
    }
}