using System;
using Snm.PropertyAttributes;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    [Serializable]
    public class TrampleConfig
    {
    }

    [Serializable]
    public class GrassSystemConfig
    {
        public Mesh grassMesh;
        public WindConfig windConfig;
        public TrampleConfig trampleConfig;
        public GrassTrampleSystemConfig trampleSystemConfig;

        [RequireShader(GrassSystemInstaller.RequiredShader_InteractiveGrass)]
        public Material grassMaterial;
        public GrassField grassFieldPrefab;
    }
}