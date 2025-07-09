using UnityEngine;
using UnityEngine.Serialization;

namespace Snm.Framework.System
{
    public partial class SystemStructureAsset : ScriptableObject
    {
        [FormerlySerializedAs("elementDefinitionAssets")]
        [SerializeField] private StructureElementAsset[] elementAssets;

        public StructureElementAsset[] ElementAssets => elementAssets;
    }
}