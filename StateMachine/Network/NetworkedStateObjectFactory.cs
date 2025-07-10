using System;
using System.Collections.Generic;
using UnityEngine;
using SOLA.Menu.Questionnaire;

namespace AEB.StateMachine
{
    /// <summary>
    /// Factory class for creating and managing networked state objects with collider settings.
    /// </summary>
    /// <typeparam name="EState">The enum type representing states.</typeparam>
    public class NetworkedStateObjectFactory<EState> where EState : Enum
    {
        NetworkedStateMachine<EState> _networkedStateMachine;
        Dictionary<GameObject, NetworkedStateObject<EState>> _objectToStateViewMap;
        Vector3 _colliderSize;
        Vector3 _colliderCenter;
        HashSet<GameObject> _pendingObjects;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkedStateObjectFactory{EState}"/> class.
        /// </summary>
        /// <param name="networkedStateMachine">The networked state machine.</param>
        /// <param name="objectToStateViewMap">The dictionary mapping game objects to their state views.</param>
        /// <param name="colliderSize">The default collider size.</param>
        public NetworkedStateObjectFactory(NetworkedStateMachine<EState> networkedStateMachine, Dictionary<GameObject, NetworkedStateObject<EState>> objectToStateViewMap, Vector3 colliderSize, Vector3 colliderCenter = default)
        {
            _networkedStateMachine = networkedStateMachine;
            _objectToStateViewMap = objectToStateViewMap;
            _colliderSize = colliderSize;
            _colliderCenter = colliderCenter;
            _pendingObjects = new HashSet<GameObject>();
        }

        #region Public

        /// <summary>
        /// Initializes the state view for a given game object.
        /// </summary>
        /// <param name="gameObject">The game object to initialize.</param>
        public void CreateStateObject(GameObject gameObject)
        {
            if (_objectToStateViewMap.ContainsKey(gameObject)) return;

            _networkedStateMachine.OnHookStateReady += HandleStateViewReady;
            _pendingObjects.Add(gameObject);

            int uniqueId = _networkedStateMachine.GetUniqueID(gameObject);
            _networkedStateMachine.DemandStateView(uniqueId);
        }

        /// <summary>
        /// Updates the default size of the collider.
        /// </summary>
        /// <param name="colliderSize">The new collider size.</param>
        public void UpdateCollider(Vector3 colliderSize, Vector3 colliderCenter)
        {
            _colliderSize = colliderSize;
            _colliderCenter = colliderCenter;
        }

        #endregion

        #region Private

        /// <summary>
        /// Handles the event when a state view is ready.
        /// </summary>
        /// <param name="uniqueId">The unique identifier of the state view.</param>
        /// <param name="stateView">The state view instance.</param>
        void HandleStateViewReady(int uniqueId, StateView<EState> stateView)
        {
            foreach (var obj in _pendingObjects)
                if (uniqueId == _networkedStateMachine.GetUniqueID(obj))
                {
                    InitializeObject(obj, stateView);
                    _pendingObjects.Remove(obj);
                    break;
                }

            if (_pendingObjects.Count == 0)
                _networkedStateMachine.OnHookStateReady -= HandleStateViewReady;
        }

        /// <summary>
        /// Initializes the game object with the given state view.
        /// </summary>
        /// <param name="obj">The game object to initialize.</param>
        /// <param name="stateView">The state view to associate with the game object.</param>
        void InitializeObject(GameObject obj, StateView<EState> stateView)
        {
            var stateObject = CreateStateObject(obj, stateView);

            if (!obj.TryGetComponent<BoxCollider>(out var collider))
                collider = obj.AddComponent<BoxCollider>();
            collider.size = _colliderSize;
            collider.center = _colliderCenter;
            collider.isTrigger = true;

            _objectToStateViewMap[obj] = stateObject;
        }

        /// <summary>
        /// Creates the state object for the given game object and state view.
        /// </summary>
        /// <param name="obj">The game object.</param>
        /// <param name="stateView">The state view.</param>
        /// <returns>The created networked state object.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the state object type is unsupported.</exception>
        NetworkedStateObject<EState> CreateStateObject(GameObject obj, StateView<EState> stateView)
        {
            Type stateObjectType = GetStateObjectType(stateView);

            if (stateObjectType != null)
            {
                var stateObject = (NetworkedStateObject<EState>)obj.GetComponent(stateObjectType) ??
                                  (NetworkedStateObject<EState>)obj.AddComponent(stateObjectType);
                return stateObject.Construct(stateView);
            }

            throw new InvalidOperationException("Failed to create state object");
        }

        /// <summary>
        /// Gets the type of the state object for the given state view.
        /// </summary>
        /// <param name="stateView">The state view.</param>
        /// <returns>The type of the state object.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the state type is unsupported.</exception>
        Type GetStateObjectType(StateView<EState> stateView)
        {
            switch (stateView)
            {
                case StateView<QuestionnaireStateManager.EQuestionnaireState> _:
                    return typeof(QuestionnaireStateView);

                // Add more cases here for other EState types and their corresponding subclasses
                default:
                    throw new InvalidOperationException($"Unsupported state type: {typeof(EState).Name}");
            }
        }

        #endregion
    }
}
