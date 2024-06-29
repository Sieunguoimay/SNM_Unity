#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class AssetUsageHelper{
    
    public static void LogAssetUsages(UnityEngine.Object obj, IEnumerable<string> dependents)
    {
        UnityEngine.Debug.Log($"Usages of {AssetDatabase.GetAssetPath(obj)}: ", obj);

        foreach (var d in dependents)
        {
            UnityEngine.Debug.Log(d, AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(d));
        }
    }

    public static IEnumerable<string> GetAllDependents(UnityEngine.Object obj, IEnumerable<string> assetPaths)
    {
        if (obj != null)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long fileID);
            var dependentPaths = GetAllDependents(guid, fileID.ToString(), assetPaths);

            foreach (var d in dependentPaths)
            {
                yield return d;
            }
        }
    }

    private static IEnumerable<string> GetAllDependents(string guid, string fileID, IEnumerable<string> assetPaths)
    {
        foreach (var ap in assetPaths)
        {
            if (GetReferences(ap).Any(s => s.Item1 == guid && s.Item2 == fileID))
            {
                yield return ap;
            }
        }
    }

    public static IEnumerable<string> GetAllAssetPaths()
    {
        return AssetDatabase.FindAssets("", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath);
    }


    public static IEnumerable<(string, string)> GetReferences(string path)
    {
        var guid = AssetDatabase.AssetPathToGUID(path);
        var fullPath = Path.Combine(Application.dataPath, path["Assets/".Length..]);
        if (File.Exists(fullPath))
        {
            var assetText = File.ReadAllText(fullPath);

            foreach (var a in ParseReferences(assetText))
            {
                yield return (string.IsNullOrEmpty(a.Item1) ? guid : a.Item1, a.Item2);
            }

            foreach (var a in ParseAddressableReference(assetText))
            {
                yield return (string.IsNullOrEmpty(a.Item1) ? guid : a.Item1, a.Item2);
            }
        }
    }


    public static IEnumerable<(string, string)> ParseReferences(string text)
    {
        var referencePattern = @"\{fileID:\s(-?\d+),\s+guid:\s+([\w\d]+)(?:,\s+type:\s+(\d+))?\}";

        var matches = Regex.Matches(text, referencePattern);

        foreach (Match match in matches)
        {
            var guidMatch = match.Groups[2].Value;
            var localIdMatch = match.Groups[1].Value;
            yield return (guidMatch, localIdMatch);
        }

        var localIdPattern = @"\{fileID:\s(-?\d+)}";
        matches = Regex.Matches(text, localIdPattern);

        foreach (Match match in matches)
        {
            var localIdMatch = match.Groups[1].Value;
            yield return ("", localIdMatch);
        }
    }

    private static IEnumerable<(string, string)> ParseAddressableReference(string text)
    {
        var pattern = @"m_AssetGUID:\s*([a-fA-F0-9]{32})\s*,\s*m_AssetLocalId:\s*(-?\d+)";
        var matches = Regex.Matches(text, pattern);

        foreach (Match m in matches)
        {
            if (m.Success && m.Groups.Count >= 3)
            {
                var guidGroup = m.Groups[1].Value;
                var localidGroup = m.Groups[2].Value;
                yield return (guidGroup, localidGroup);
            }
        }
    }

}
#endif