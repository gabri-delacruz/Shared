using System.Collections.Generic;
using UnityEngine;

public class StateMachine<TStateID, TState>
    where TStateID : struct
    where TState : class, StateMachine<TStateID, TState>.IState
{
    public interface IState
    {
        bool Enabled { get; set; }

        void Register(StateMachine<TStateID, TState> stateMachine);

        bool NeedsTransition(out TStateID nextStateID);

        void Update(float deltaTime);

        void OnActiveChanged(bool active);
    }

    public class State : IState
    {
        public bool Enabled { get; set; } = true;

        public void Register(StateMachine<TStateID, TState> stateMachine)
        {
            Debug.Assert(this.stateMachine == null, "State was already registered");
            this.stateMachine = stateMachine;
        }

        public virtual bool NeedsTransition(out TStateID nextStateID)
        {
            nextStateID = default;
            return false;
        }

        public virtual void Update(float deltaTime)
        {
            Debug.Assert(stateMachine != null, "State has not been registered in a state machine yet");
        }

        public virtual void OnActiveChanged(bool active) { }

        protected StateMachine<TStateID, TState> stateMachine = null;
    }

    public StateMachine()
    {
    }

    public StateMachine(IEnumerable<KeyValuePair<TStateID, TState>> statePairs, TStateID initialState)
    {
        foreach (var statePair in statePairs)
        {
            AddState(statePair.Key, statePair.Value);

            if (EqualsStateID(statePair.Key, initialState))
            {
                PreviousStateID = CurrentStateID = statePair.Key;
                CurrentState = statePair.Value;
            }
        }

        Debug.Assert(CurrentState != null && CurrentState.Enabled);
    }

    public string Name { get; set; }

    public bool AllowSelfTransition { get; set; } = true;

    public bool LoggingEnabled { get; set; } = false;

    public TStateID? NextStateID { get; private set; }
    public TStateID PreviousStateID { get; private set; }
    public TStateID CurrentStateID { get; private set; }

    public TState CurrentState { get; private set; }

    public float StateMachineTimer { get; private set; } = 0.0f;
    public float StateTimer { get; private set; } = 0.0f;

    public bool HasChangedThisFrame { get; private set; }

    public delegate void StateMachineAction(StateMachine<TStateID, TState> stateMachine);

    public event StateMachineAction StateChanging = null;
    public event StateMachineAction StateChanged = null;

    public void ResetTimer()
    {
        StateMachineTimer = 0.0f;
        StateTimer = 0.0f;
    }

    public void ResetStateTimer()
    {
        StateTimer = 0.0f;
    }

    public void ClearStates()
    {
        states.Clear();
    }

    public TState GetState(in TStateID stateID)
    {
        TState state = null;
        states.TryGetValue(stateID, out state);
        return state;
    }

    public void AddState(in TStateID stateID, TState state)
    {
        Debug.Assert(state != null);
        states.Add(stateID, state);
        state.Register(this);
    }

    public bool RemoveState(in TStateID stateID) => states.Remove(stateID);

    public bool RequestTransition(in TStateID nextStateID, bool immediate = false)
    {
        bool found = false;
        if (CurrentState == null || AllowSelfTransition || !EqualsStateID(nextStateID, CurrentStateID))
        {
            if (immediate)
            {
                // Invalidates any pending transition
                NextStateID = null;

                found = ChangeState(nextStateID);
            }
            else
            {
                NextStateID = nextStateID;

                found = true;
            }
        }
        return found;
    }

    public void Update(float deltaTime)
    {
        HasChangedThisFrame = false;

        if (NextStateID != null)
        {
            ChangeState((TStateID)NextStateID);
            NextStateID = null;
        }

#if DEBUG
        HashSet<TStateID> visited = new HashSet<TStateID>();
#endif
        while (CurrentState.NeedsTransition(out TStateID nextStateID))
        {
            Debug.Assert(visited.Add(CurrentStateID)); // Checks for loops

            if (!ChangeState(nextStateID))
                break; // Break if can't find the next state
        }

        CurrentState.Update(deltaTime);

        StateTimer += deltaTime;
        StateMachineTimer += deltaTime;
    }

    private bool ChangeState(in TStateID nextStateID)
    {
        bool changed = false;

        TState nextState = null;
        if (states.TryGetValue(nextStateID, out nextState) && nextState != null && nextState.Enabled)
        {
            StateChanging?.Invoke(this);

            PreviousStateID = CurrentStateID;
            CurrentStateID = nextStateID;

            CurrentState = nextState;

            CurrentState?.OnActiveChanged(false);

            StateTimer = 0.0f;

            CurrentState?.OnActiveChanged(true);

            StateChanged?.Invoke(this);

            changed = true;

            Log("Changing from <" + PreviousStateID + "> to <" + CurrentStateID + ">");
        }
        else
        {
            Log("Skipping state <" + nextStateID + ">");
        }

        HasChangedThisFrame |= changed;

        return changed;
    }

    private static bool EqualsStateID(in TStateID stateID1, in TStateID stateID2) => EqualityComparer<TStateID>.Default.Equals(stateID1, stateID2);

    [System.Diagnostics.Conditional("DEBUG")]
    private void Log(string message)
    {
        if (LoggingEnabled)
        {
            Debug.Log("[" + Time.time + "] " + Name + ": " + message);
        }
    }

    protected Dictionary<TStateID, TState> states = new Dictionary<TStateID, TState>();
}

public class OwnedStateMachine<TStateID, TState, T> : StateMachine<TStateID, TState>
    where TStateID : struct
    where TState : OwnedStateMachine<TStateID, TState, T>.OwnedState
    where T : class
{
    public class OwnedState : State
    {
        public T Owner => (stateMachine as OwnedStateMachine<TStateID, TState, T>)?.Owner;
    }

    public OwnedStateMachine(T owner)
    {
        Owner = owner;
    }

    public OwnedStateMachine(T owner, IEnumerable<KeyValuePair<TStateID, TState>> statePairs, TStateID initialState) : base(statePairs, initialState)
    {
        Owner = owner;
    }

    public T Owner { get; private set; }
}
