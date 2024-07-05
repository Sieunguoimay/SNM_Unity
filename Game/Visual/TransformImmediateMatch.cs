using System;
using ObjectAccess;
using UnityEngine;
using UnityEngine.Serialization;

public class TransformImmediateMatch : MonoBehaviour
{
    [FormerlySerializedAs("from")]
    [SerializeField] private CommonObjectProvider from;
    [SerializeField] private Transform to;
    [SerializeField] private MatchType matchType = MatchType.All;

    private Transform _from;

    private void Start()
    {
        if (!from.TryGetObject<Transform>(out _from))
        {
            Debug.LogError("From is not assigned");
        }
    }

    private void Update()
    {
        if (matchType == MatchType.All)
        {
            to.SetPositionAndRotation(_from.position, _from.rotation);
        }
        if (matchType == MatchType.Position)
        {
            to.position = _from.position;
        }
        if (matchType == MatchType.Rotation)
        {
            to.rotation = _from.rotation;
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