using System.Collections.Generic;
using UnityEngine;

namespace Snm.CameraRig
{
    public class CameraRigVisualizeData
    {
        public Bounds ndcBounds;
        public Camera camera;
        public IReadOnlyList<CameraTarget> targets;
    }
}
