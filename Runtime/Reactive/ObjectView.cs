using System;
using Snm.Tools.InspectorExtra;
using Reflection;
using UnityEngine;

namespace Snm.Framework.Reactive
{
    public class ObjectView<TObject> : MonoBehaviour where TObject : class
    {
        private TObject _object;

        [PropertyChangeEvent(nameof(OnObjectChanged))]
        public TObject Object => _object;
        public event Action<ObjectView<TObject>> OnObjectChanged;

        [RevealNonSerialized]
        private bool HasObject => _object != null;

        public void SetObject(TObject obj)
        {
            if (_object != null)
            {
                OnTearDown();
            }

            _object = obj;

            if (_object != null)
            {
                OnSetup();
            }
            OnObjectChanged?.Invoke(this);
        }

        protected virtual void OnSetup() { }

        protected virtual void OnTearDown() { }
    }
}
