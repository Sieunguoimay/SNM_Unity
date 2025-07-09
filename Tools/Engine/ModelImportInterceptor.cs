#if UNITY_EDITOR
using UnityEditor;

namespace Snm.Tools
{
    public class ModelImportInterceptor : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (assetImporter is ModelImporter importer)
            {
                importer.bakeAxisConversion = true;
            }
        }
    }
}
#endif
