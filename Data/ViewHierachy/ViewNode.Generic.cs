using InspectorExtensions;
using UnityEngine;

namespace Supports.ViewHierachy
{
    public class ViewNode<TData> : ViewNode where TData : class
    {
        [RevealNonSerialized]
        public bool IsSetup => Data != null;

        public TData Data { get; private set; }

        public override void Setup(object data)
        {
            AssertTypeCorrectness(data);
            Data = data as TData;
            base.Setup(data);
        }

        public override void TearDown()
        {
            base.TearDown();
            Data = null;
        }

        private void AssertTypeCorrectness(object data)
        {
            if (!typeof(TData).IsAssignableFrom(data.GetType()))
            {
                Debug.LogError($"Invalid data type! Given {data.GetType().FullName}, required {typeof(TData).FullName}");
            }
        }
    }
}

