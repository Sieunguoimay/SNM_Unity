using System;
using Snm.PropertyAttributes;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    [Serializable]
    public class GrassSystemConfig
    {
        public WindConfig windConfig;
        public Mesh grassMesh;

        [RequireShader(GrassSystemInstaller.RequiredShader_InteractiveGrass)]
        public Material grassMaterial;
        public GrassField grassFieldPrefab;
    }
}