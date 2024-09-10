using System;
using UnityEngine;

namespace StateMachines
{
    public class StateMachine
    {
        public State OldState { get; private set; }
        public State CurrentState { get; private set; }
        public event Action<StateMachine> OnCurrentStateChanged;

        public void SetCurrentState(State state)
        {
            if (CurrentState == state)
            {
                Debug.LogError($"StateMachine: Setting to same state! {state}");
            }

            if (CurrentState != null)
            {
                CurrentState?.Exit(this);
            }

            OldState = CurrentState;
            CurrentState = state;
            OnCurrentStateChanged?.Invoke(this);

            if (CurrentState != null)
            {
                CurrentState?.Enter(this);
            }
        }

        public void BackToOldState()
        {
            if (OldState != null)
            {
                SetCurrentState(OldState);
            }
        }
    }
}
