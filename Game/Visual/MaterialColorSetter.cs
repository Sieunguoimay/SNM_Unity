using System.Collections.Generic;
using UnityEngine;

namespace Snm.Visual
{
    public class MaterialColorSetter : MonoBehaviour
    {
        [SerializeField] private Color defaultColor;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Mode mode = Mode.Material;

        [ContextMenu("SetDefaultColor")]
        public void SetDefaultColor()
        {
            SetColor(defaultColor);
        }

        public void SetColor(Color color)
        {
            foreach (var m in GetOneOrManyTargetMaterials()) m.color = color;
        }

        private IEnumerable<Material> GetOneOrManyTargetMaterials()
        {
            switch (mode)
            {
                case Mode.Material:
                    yield return targetRenderer.material;
                    break;
                case Mode.SharedMaterial:
                    yield return targetRenderer.sharedMaterial;
                    break;
                case Mode.Materials:
                    foreach (var m in targetRenderer.materials)
                    {
                        yield return m;
                    }
                    break;
                case Mode.SharedMaterials:
                    foreach (var m in targetRenderer.sharedMaterials)
                    {
                        yield return m;
                    }
                    break;
                default:
                    break;
            }
        }

        private enum Mode
        {
            Material,
            Materials,
            SharedMaterial,
            SharedMaterials,
        }
    }
}