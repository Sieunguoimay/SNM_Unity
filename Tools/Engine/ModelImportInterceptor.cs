#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

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
