using UnityEngine;

namespace Snm.GrassSystem
{
    public interface IGrassDisturber
    {
        Vector3 WorldPosition { get; }
        float GrassContactRadius { get; }
    }
}
