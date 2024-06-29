using System;

public class Transaction
{
    public bool IsStarted { get; private set; }
    public bool IsEnded { get; private set; }
    public bool IsSucceeded { get; private set; }

    public event Action<Transaction> TransactionEndedEvent;
    public event Action<Transaction> TransactionStartedEvent;

    private Action<Transaction> _transactionEndCallback;

    public virtual void Start(Action<Transaction> endCallback)
    {
        _transactionEndCallback = endCallback;
        IsStarted = true;
        IsEnded = false;
        IsSucceeded = false;
        TransactionStartedEvent?.Invoke(this);
    }

    public void EndSuccess()
    {
        if (IsStarted)
        {
            IsStarted = false;
            IsSucceeded = true;
            NotifyEnded();
        }
    }

    public void EndFailure()
    {
        if (IsStarted)
        {
            IsStarted = false;
            IsSucceeded = false;
            NotifyEnded();
        }
    }

    protected virtual void NotifyEnded()
    {
        IsEnded = true;
        _transactionEndCallback?.Invoke(this);
        TransactionEndedEvent?.Invoke(this);
    }
}

