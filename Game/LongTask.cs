using System;
using UnityEngine;

public abstract class LongTask
{
    public bool IsRunning { get; private set; }
    public bool IsSucceeded { get; private set; }

    private Action<LongTask> _taskEndCallback;

    public event Action<LongTask> RunningStatusChanged;

    public void TryBeginTask(Action<LongTask> endCallback)
    {
        if (IsRunning)
        {
            Debug.LogError("Failed to begin task. Task has already started");
        }
        else
        {
            BeginTask(endCallback);
        }
    }

    private void BeginTask(Action<LongTask> endCallback)
    {
        _taskEndCallback = endCallback;
        IsRunning = true;
        IsSucceeded = false;
        OnTaskBegin();
        RunningStatusChanged?.Invoke(this);
    }

    public void TryEndTask(bool success)
    {
        if (!IsRunning)
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
        IsSucceeded = success;
        IsRunning = false;
        _taskEndCallback?.Invoke(this);
        RunningStatusChanged?.Invoke(this);
    }

    protected abstract void OnTaskBegin();
}

