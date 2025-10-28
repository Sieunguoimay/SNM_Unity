using System.Reflection;

namespace Sieunguoimay.Tools
{
    public class ObjectExposedItem
    {
        public string MemberName;
        public string DisplayMemberName;
        public string DisplayValue;
        public object Value;
        public bool IsPrimitive;
        public MemberInfo MemberInfo;
    }
}