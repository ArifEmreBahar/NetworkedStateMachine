using System;
using System.Threading.Tasks;
using UnityEngine;

namespace AEB.StateMachine
{
    /// <summary>
    /// Represents the base state class for a state in a state machine.
    /// Each state encapsulates the behavior and transitions for a specific state of the state machine.
    /// </summary>
    /// <typeparam name="EState">The Enum type representing the states.</typeparam>
    public abstract class BaseState<EState> where EState : Enum
    {
        /// <summary>
        /// The StateMachine that this state is part of.
        /// </summary>
        protected StateMachine<EState> stateMachine;

        /// <summary>
        /// Initializes a new instance of the BaseState class.
        /// </summary>
        /// <param name="stateMachine">The StateMachine to which this state belongs.</param>
        /// <param name="key">The key representing this state in the StateMachine.</param>
        public BaseState(StateMachine<EState> stateMachine, EState key)
        {
            this.stateMachine = stateMachine;
            StateKey = key;
        }

        /// <summary>
        /// Gets the key representing this state.
        /// </summary>
        public EState StateKey { get; private set; }

        /// <summary>
        /// <br>Retrieves the state machine as the specified derived type.</br>
        /// <br>You can also cast <see cref="stateMachine">stateMachine</see> and use it directly.</br>
        /// </summary>
        /// <typeparam name="T">The derived type of StateMachine to cast to.</typeparam>
        /// <returns>The casted state machine or null.</returns>
        public T GetStateManager<T>() where T : StateMachine<EState>
        {
            var castedStateMachine = stateMachine as T;
            if (castedStateMachine == null)
                throw new InvalidCastException($"stateMachine must be of type {typeof(T).Name}");

            return castedStateMachine;
        }

        /// <summary>
        /// Method called when entering the state.
        /// </summary>
        public abstract Task EnterState();

        /// <summary>
        /// Method called when exiting the state.
        /// </summary>
        public abstract Task ExitState();

        /// <summary>
        /// Method called every frame when the state is active.
        /// </summary>
        public abstract void UpdateState();

        /// <summary>
        /// Called when a Collider enters a trigger associated with this state.
        /// </summary>
        /// <param name="other">The Collider that entered the trigger.</param>
        public abstract void OnTriggerEnter(Collider other);

        /// <summary>
        /// Called once per frame for every Collider that is touching the trigger associated with this state.
        /// </summary>
        /// <param name="other">The Collider that is touching the trigger.</param>
        public abstract void OnTriggerStay(Collider other);

        /// <summary>
        /// Called when a Collider stops touching the trigger associated with this state.
        /// </summary>
        /// <param name="other">The Collider that stopped touching the trigger.</param>
        public abstract void OnTriggerExit(Collider other);
    }
}
