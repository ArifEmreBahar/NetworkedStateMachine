using Michsky.UI.Reach;
using System;
using UnityEngine;

namespace AEB.StateMachine
{
    /// <summary>
    /// Represents the base handler class for a state in a state machine.
    /// </summary>
    /// <typeparam name="EState">The Enum type representing the states.</typeparam>
    public abstract class BaseHandler<EState> : MonoBehaviour where EState : Enum
    {
        /// <summary>
        /// The StateMachine that this state is part of.
        /// </summary>
        protected StateMachine<EState> stateMachine;

        /// <summary>
        /// Gets the key representing this state.
        /// </summary>
        public EState StateKey { get; private set; }

        /// <summary>
        /// Initializes the BaseHandler class.
        /// </summary>
        /// <param name="stateMachine">The StateMachine to which this hadnler belongs.</param>
        /// <param name="key">The key representing this handler in the StateMachine.</param>
        public virtual BaseHandler<EState> Construct(StateMachine<EState> stateMachine, EState key)
        {
            this.stateMachine = stateMachine;
            StateKey = key;

            return this;
        }

        ///// <summary>
        ///// <br>Retrieves the state machine as the specified derived type.</br>
        ///// <br>You can also cast <see cref="stateMachine">stateMachine</see> and use it directly.</br>
        ///// </summary>
        ///// <typeparam name="T">The derived type of StateMachine to cast to.</typeparam>
        ///// <returns>The casted state machine or null.</returns>
        //protected T GetStateManager<T>() where T : StateMachine<EState>
        //{
        //    var castedStateMachine = stateMachine as T;
        //    if (castedStateMachine == null)
        //        throw new InvalidCastException($"stateMachine must be of type {typeof(T).Name}");

        //    return castedStateMachine;
        //}

        protected virtual void OnEnable()
        {
            // ---
        }

        protected virtual void OnDisable()
        {
            // ---
        }

        protected virtual void Awake()
        {
            // ---
        }

        protected virtual void Start()
        {
            // ---
        }

        protected virtual void Update()
        {
            // ---
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            // ---
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            // ---
        }

        // Consider the expand and add LateUpdate, FixedUpdate ... like methods can be inherited and placed here.
    }
}
