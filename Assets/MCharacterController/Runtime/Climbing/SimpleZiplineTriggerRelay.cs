// File: Runtime/Environment/SimpleZiplineTriggerRelay.cs
// Namespace: Kojiko.MCharacterController.Environment

using UnityEngine;

namespace Kojiko.MCharacterController.Environment
{
    [RequireComponent(typeof(Collider))]
    public class SimpleZiplineTriggerRelay : MonoBehaviour
    {
        [SerializeField] private SimpleZiplinePair _ziplinePair;
        [SerializeField] private bool _isBottomTrigger = true;

        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_ziplinePair == null)
                return;

            if (_isBottomTrigger)
                _ziplinePair.BottomTriggerEnter(_collider, other);
            else
                _ziplinePair.TopTriggerEnter(_collider, other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (_ziplinePair == null)
                return;

            if (_isBottomTrigger)
                _ziplinePair.BottomTriggerExit(_collider, other);
            else
                _ziplinePair.TopTriggerExit(_collider, other);
        }
    }
}