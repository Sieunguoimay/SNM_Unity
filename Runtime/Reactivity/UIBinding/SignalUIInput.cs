using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Reactivity.Unity
{
    public static class SignalUIInput
    {
        public static (Effect effect, Signal<string> signal) BindTwoWayText(
            TextField textField,
            string initialValue = "")
        {
            var signal = new Signal<string>(initialValue);
            var effect = BindTwoWayText(textField, signal);
            return (effect, signal);
        }

        public static Effect BindTwoWayText(
            TextField textField,
            Signal<string> signal)
        {
            var effect = new Effect(() =>
            {
                if (textField.value != signal.Value)
                    textField.value = signal.Value;
            });

            textField.RegisterValueChangedCallback(evt =>
            {
                signal.Value = evt.newValue;
            });

            UIBindingUtil.AutoDispose(textField, effect);
            return effect;
        }

        public static (Effect effect, Signal<bool> signal) BindTwoWayToggle(
            Toggle toggle,
            bool initialValue = false)
        {
            var signal = new Signal<bool>(initialValue);
            var effect = BindTwoWayToggle(toggle, signal);
            return (effect, signal);
        }

        public static Effect BindTwoWayToggle(
            Toggle toggle,
            Signal<bool> signal)
        {
            var effect = new Effect(() =>
            {
                if (toggle.value != signal.Value)
                    toggle.value = signal.Value;
            });

            toggle.RegisterValueChangedCallback(evt =>
            {
                signal.Value = evt.newValue;
            });

            UIBindingUtil.AutoDispose(toggle, effect);
            return effect;
        }

        public static (Effect effect, Signal<float> signal) BindTwoWayFloat(
            FloatField floatField,
            float initialValue = 0f)
        {
            var signal = new Signal<float>(initialValue);
            var effect = BindTwoWayFloat(floatField, signal);
            return (effect, signal);
        }

        public static Effect BindTwoWayFloat(
            FloatField floatField,
            Signal<float> signal)
        {
            var effect = new Effect(() =>
            {
                if (Math.Abs(floatField.value - signal.Value) > float.Epsilon)
                    floatField.value = signal.Value;
            });

            floatField.RegisterValueChangedCallback(evt =>
            {
                signal.Value = evt.newValue;
            });

            UIBindingUtil.AutoDispose(floatField, effect);
            return effect;
        }

        public static Effect BindTwoWaySlider(
            Slider slider,
            Signal<float> signal)
        {
            bool updating = false;

            var effect = new Effect(() =>
            {
                if (updating) return;

                if (!Mathf.Approximately(slider.value, signal.Value))
                {
                    updating = true;
                    slider.value = signal.Value;
                    updating = false;
                }
            });

            slider.RegisterValueChangedCallback(evt =>
            {
                if (Mathf.Approximately(signal.Value, evt.newValue))
                    return;

                signal.Value = evt.newValue;
            });

            UIBindingUtil.AutoDispose(slider, effect);
            return effect;
        }
    }
}
