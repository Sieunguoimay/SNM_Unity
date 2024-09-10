using System;

namespace StateMachines
{
    public class State
    {
        public event Action<State> OnStateEnter;
        public event Action<State> OnStateExit;
        public bool IsActive { get; private set; } = false;

        public StateMachine StateMachine { get; private set; }

        public void Enter(StateMachine stateMachine)
        {
            StateMachine = stateMachine;

            OnBeforeEnter();
            IsActive = true;
            OnStateEnter?.Invoke(this);
            OnAfterEnter();
        }

        public void Exit(StateMachine stateMachine)
        {
            OnBeforeExit();
            IsActive = false;
            OnStateExit?.Invoke(this);
            OnAfterExit();

            StateMachine = null;
        }

        protected virtual void OnBeforeEnter()
        {
        }

        protected virtual void OnAfterEnter()
        {
        }

        protected virtual void OnBeforeExit()
        {
        }

        protected virtual void OnAfterExit()
        {
        }

    }
}
