#if UNITY_EDITOR
#endif
using UnityEngine;
using PropertyAttribute = UnityEngine.PropertyAttribute;

namespace Snm.PropertyAttributes
{
    public class RequireShaderAttribute : PropertyAttribute
    {
        public RequireShaderAttribute(string shaderName)
        {

        }

        public static void CheckValid(Material material, string shaderName)
        {
            
        }
    }
}