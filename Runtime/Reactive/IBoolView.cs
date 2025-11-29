using System;
using Reflection;
using UnityEngine;

namespace Snm.Framework.Reactive
{
    public class IBoolView : ObjectView<IBool>
    {
        [SerializeField] private bool defaultValue = false;

        [PropertyChangeEvent(nameof(OnObjectChanged))]
        [PropertyChangeEvent(nameof(OnValueChanged))]
        public bool Value => Object?.Value ?? defaultValue;
        public event Action<IBoolView> OnValueChanged;

        protected override void OnSetup()
        {
            base.OnSetup();
            Object.OnValueChanged -= IBool_OnValueChanged;
            Object.OnValueChanged += IBool_OnValueChanged;
        }

        protected override void OnTearDown()
        {
            Object.OnValueChanged -= IBool_OnValueChanged;
            base.OnTearDown();
        }

        private void IBool_OnValueChanged(IBool @bool)
        {
            OnValueChanged?.Invoke(this);
        }
    }
}
