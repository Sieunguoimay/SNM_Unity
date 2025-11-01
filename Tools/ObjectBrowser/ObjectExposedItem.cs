using System.Reflection;

namespace Snm.Tools.ObjectBrowser
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