using UnityEngine;

public class LoggerMB : MonoBehaviour
{
    [SerializeField] private string message;

    public void Log()
    {
        Debug.Log($"{name}: " + message, this);
    }

    public void LogError()
    {
        Debug.LogError($"{name}: " + message, this);
    }
}