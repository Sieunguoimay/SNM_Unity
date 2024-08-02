using System;
using UnityEngine;
using UnityEngine.Serialization;

public class TransformImmediateMatch : MonoBehaviour
{
    [FormerlySerializedAs("from")]
    [SerializeField] private Transform from;
    [SerializeField] private Transform to;
    [SerializeField] private MatchType matchType = MatchType.All;

    private void Update()
    {
        if (matchType == MatchType.All)
        {
            to.SetPositionAndRotation(from.position, from.rotation);
        }
        if (matchType == MatchType.Position)
        {
            to.position = from.position;
        }
        if (matchType == MatchType.Rotation)
        {
            to.rotation = from.rotation;
        }
    }
    
    [Flags]
    private enum MatchType
    {
        All = 1,
        Position = 2,
        Rotation = 4
    }
}