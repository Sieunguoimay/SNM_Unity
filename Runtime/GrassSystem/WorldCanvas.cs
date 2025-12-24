#if UNITY_EDITOR
#endif
using UnityEditor;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class WorldCanvas
    {
        public Vector2 worldMin;
        public Vector2 worldMax;
    }

    public class WorldCanvasChecker
    {
        private readonly WorldCanvas worldCanvas;

        public WorldCanvasChecker(WorldCanvas worldCanvas)
        {
            this.worldCanvas = worldCanvas;
        }

        public bool IsInWorldCanvas(Vector3 worldPos)
        {
            return
                worldPos.x > worldCanvas.worldMin.x && worldPos.x < worldCanvas.worldMax.x
                &&
                worldPos.y > worldCanvas.worldMin.y && worldPos.y < worldCanvas.worldMax.y;
        }
    }

    public class WorldCanvasVisualizer
    {
        private readonly WorldCanvas worldCanvas;

        public WorldCanvasVisualizer(WorldCanvas worldCanvas)
        {
            SceneView.duringSceneGui += SceneView_DuringSceneGui;
            this.worldCanvas = worldCanvas;
        }

        public void Cleanup()
        {
            SceneView.duringSceneGui -= SceneView_DuringSceneGui;
        }

        private void SceneView_DuringSceneGui(SceneView view)
        {
            var size = new Vector3(worldCanvas.worldMax.x - worldCanvas.worldMin.x, 0, worldCanvas.worldMax.y - worldCanvas.worldMin.y);

            var old = Handles.color;
            Handles.color = Color.red;
            Handles.DrawWireCube(Vector3.zero, size);
            Handles.color = old;
        }
    }
}