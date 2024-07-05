using System;
using UnityEngine;
using UnityEngine.Events;

public class FloatValueTransfer : MonoBehaviour
{
    [SerializeField] private UnityEventFloat onValue;

    [Serializable]
    private class UnityEventFloat : UnityEvent<float> { }

    public void Transfer(float value)
    {
        onValue?.Invoke(value);
    }
}
