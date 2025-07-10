using Photon.Pun;
using SOLA.Photon;
using SOLA.Utilities;
using System;
using UnityEngine;

namespace AEB.StateMachine
{
    public class StateViewTriggerHelper : MonoBehaviour
    {
        [TagSelector] [Tooltip("Enter the tag this Trigger should respond to.")] public string Tag = "";

        public event Action OnTrigger;

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(Tag)) return;

            OnTrigger?.Invoke();
        }
    }
}