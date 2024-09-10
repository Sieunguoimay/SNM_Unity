using System;
using UnityEngine;

public abstract class LongTask
{
    public bool IsWaiting { get; private set; }

    private Action<LongTask, bool> _taskEndCallback;

    public event Action<LongTask> StatusChanged;

    public void TryBeginTask(Action<LongTask, bool> endCallback)
    {
        if (IsWaiting)
        {
            Debug.LogError("Failed to begin task. Task has already started");
        }
        else
        {
            BeginTask(endCallback);
        }
    }

    private void BeginTask(Action<LongTask, bool> endCallback)
    {
        _taskEndCallback = endCallback;
        IsWaiting = true;
        OnTaskBegin();
        StatusChanged?.Invoke(this);
    }

    public void TryEndTask(bool success)
    {
        if (!IsWaiting)
        {
            Debug.LogError("Failed to end task. Task is not started");
        }
        else
        {
            EndTask(success);
        }
    }

    private void EndTask(bool success)
    {
        IsWaiting = false;
        _taskEndCallback?.Invoke(this, success);
        StatusChanged?.Invoke(this);
    }

    protected abstract void OnTaskBegin();
}
