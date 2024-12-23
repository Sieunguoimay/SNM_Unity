#if UNITY_EDITOR
using System;
using UnityEditor;

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
#endif
