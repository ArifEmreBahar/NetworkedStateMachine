using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace AEB.StateMachine
{
    /// <summary>
    /// A generic state machine base.
    /// </summary>
    /// <typeparam name="EState">The Enum type representing the states.</typeparam>
    public abstract class StateMachine<EState> : MonoBehaviour where EState : Enum
    {
        #region Variables

        /// <summary>
        /// A dictionary mapping each state to its corresponding BaseState instance.
        /// </summary>
        protected Dictionary<EState, BaseState<EState>> states = new Dictionary<EState, BaseState<EState>>();

        /// <summary>
        /// A dictionary mapping each state to its corresponding BaseHandler instance.
        /// </summary>
        protected Dictionary<EState, BaseHandler<EState>> handlers = new Dictionary<EState, BaseHandler<EState>>();

        /// <summary>
        /// A queue of states to be processed by the state machine.
        /// </summary>
        protected Queue<EState> stateQueue = new Queue<EState>();

        /// <summary>
        /// Stores the last queued state in the state machine.
        /// </summary>
        protected EState lastQueue;


        /// <summary>
        /// The current active state of the state machine.
        /// </summary>
        protected BaseState<EState> currentState;

        /// <summary>
        /// The previous active state of the state machine.
        /// </summary>
        protected Stack<EState> previousStates = new Stack<EState>();

        protected bool isTransitioningState = false;
        protected bool isForcingState = false;

        #endregion 

        #region Events

        /// <summary>
        /// Occurs when the state machine transitions from one state to another with details.<br/>
        /// Parameters are the Previous state to the Next state.
        /// If the Previous state is null, It returns the default state.
        /// </summary>
        public event Action<EState, EState> OnStateChangedTo;

        /// <summary>
        /// Occurs when any state change happens in the state machine.
        /// </summary>
        public event Action OnStateChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Current State that active.
        /// </summary>
        public BaseState<EState> CurrentState => currentState;

        #endregion

        #region Unity

        protected virtual void Awake() { }

        protected virtual void Start() { }

        protected virtual void OnEnable() { }

        protected virtual void OnDisable() { }

        protected virtual void OnDestroy() { }

        protected virtual void Update()
        {
            if (isTransitioningState || isForcingState)
                return;

            if (stateQueue.Count > 0)
                TransitionToState();
            else
                CurrentState?.UpdateState();
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            OnTriggerEnter(other);
        }

        protected virtual void OnTriggerStay(Collider other)
        {
            OnTriggerStay(other);
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            OnTriggerExit(other);
        }

        #endregion

        #region Public

        /// <summary>
        /// Enqueues a new state to transition to.
        /// </summary>
        /// <param name="stateKey">The key of the state to enqueue.</param>
        public virtual void QueueNewState(EState stateKey, bool savePrevious = true)
        {
            if(lastQueue != null && savePrevious) 
                previousStates.Push(lastQueue);

            lastQueue = stateKey;
            stateQueue.Enqueue(stateKey);
        }

        /// <summary>
        /// Attempts to queue the previous state. If no previous state exists and useDefault is true, it queues the default state.
        /// </summary>
        /// <param name="useDefault">Indicates whether to queue the default state if no previous state exists.</param>
        /// <returns>True if a state was successfully queued, otherwise false.</returns>
        public bool QueuePreviousState(bool useDefault = false)
        {
            EState eState;
            if (!TryGetPreviousState(out eState))
                if (useDefault)
                {
                    QueueNewState(eState, false);
                    return true;
                }
                else
                    return false;

            QueueNewState(eState, false);
            return true;
        }

        /// <summary>
        /// Forces the state machine to transition immediately to the specified state.
        /// </summary>
        /// <param name="stateKey">The key of the state to force transition to.</param>
        public virtual async void ForceChangeState(EState stateKey)
        {
            isForcingState = true;

            while (isTransitioningState)
                await Task.Delay(100);

            stateQueue.Clear();
            stateQueue.Enqueue(stateKey);

            isForcingState = false;
        }

        /// <summary>
        /// Attempts to read the previous state without removing it from the stack.
        /// </summary>
        /// <param name="eState">The previous state if it exists.</param>
        /// <returns>True if the previous state was successfully read, otherwise false.</returns>
        public bool TryReadPreviousState(out EState eState)
        {
            return previousStates.TryPeek(out eState);      
        }

        /// <summary>
        /// Clears the history of previously saved states.
        /// </summary>
        public void ClearStateHistory()
        {
            previousStates.Clear();
        }

        #endregion

        #region Protected

        /// <summary>
        /// Transitions the state machine to the given state.
        /// </summary>
        /// <param name="stateKey">The key of the state to transition to.</param>
        protected virtual async void TransitionToState()
        {
            isTransitioningState = true;

            EState nextStateKey = stateQueue.Dequeue();
            if(nextStateKey == null)
            {
                isTransitioningState = false;
                return;
            }

            EState previousStateKey = currentState != null ? currentState.StateKey : default;

            await (currentState?.ExitState() ?? Task.CompletedTask);
            currentState = states[nextStateKey];
            await currentState.EnterState();

            isTransitioningState = false;

            OnStateChangedTo?.Invoke(previousStateKey, nextStateKey);
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Attempts to get the previous state by removing it from the stack.
        /// </summary>
        /// <param name="eState">The previous state if it exists.</param>
        /// <returns>True if the previous state was successfully retrieved, otherwise false.</returns>
        protected bool TryGetPreviousState(out EState eState)
        {
            return previousStates.TryPop(out eState);
        }

        #endregion
    }
}
