using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    [ExecuteInEditMode]
    public class GrassTrampleUpdaterMB : MonoBehaviour
    {
        private GrassTrampleBrushDriver _driver;

        public void SetDriver(GrassTrampleBrushDriver driver)
        {
            _driver = driver;
        }

        private void Update()
        {
            _driver?.Update();
        }
    }
}