using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public interface IGrassDisturber
    {
        Vector3 WorldPosition { get; }
        float GrassContactRadius { get; }
    }
}
