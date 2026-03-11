// File: Runtime/Environment/SimpleClimbTriggerRelay.cs
// Namespace: Kojiko.MCharacterController.Environment

using UnityEngine;

namespace Kojiko.MCharacterController.Environment
{
    /// <summary>
    /// Relay script to forward trigger events from this collider to a SimpleClimbPair.
    /// Put this on each child with a trigger collider, assign the SimpleClimbPair and
    /// tell it whether this is the bottom or the top trigger.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SimpleClimbTriggerRelay : MonoBehaviour
    {
        [SerializeField] private SimpleClimbPair _climbPair;
        [SerializeField] private bool _isBottomTrigger = true;

        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_climbPair == null)
                return;

            // Expose a public method on SimpleClimbPair to handle this.
            if (_isBottomTrigger)
                _climbPair.BottomTriggerEnter(_collider, other);
            else
                _climbPair.TopTriggerEnter(_collider, other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (_climbPair == null)
                return;

            if (_isBottomTrigger)
                _climbPair.BottomTriggerExit(_collider, other);
            else
                _climbPair.TopTriggerExit(_collider, other);
        }
    }
}