using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class Explosion_AreaTester : MonoBehaviour
{
    [Header("Explosion Setup")]
    [Tooltip("The ExplosionKnockback component to trigger after the delay.")]
    [SerializeField] private Explosion_KnockbackAbility _explosion;

    [Header("Trigger Settings")]
    [Tooltip("Only objects on these layers will start the countdown.")]
    [SerializeField] private LayerMask _triggerLayers = ~0;

    [Tooltip("Time in seconds to wait after an object enters before triggering the explosion.")]
    [SerializeField] private float _delayBeforeExplosion = 2f;

    [Tooltip("If true, once an explosion is triggered, the area will never trigger again.")]
    [SerializeField] private bool _oneShot = true;

    [Header("Debug State")]
    [SerializeField] private bool _isCountdownRunning;
    [SerializeField] private float _remainingTime;
    [SerializeField] private GameObject _lastTriggeringObject;

    private Coroutine _countdownCoroutine;
    private Collider _collider;
    private bool _hasExploded;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        if (_collider != null && !_collider.isTrigger)
        {
            Debug.LogWarning(
                $"[ExplosionAreaTester] Collider on '{name}' was not set as trigger; " +
                "forcing isTrigger = true for test behaviour."
            );
            _collider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_explosion == null)
        {
            Debug.LogWarning(
                $"[ExplosionAreaTester] No ExplosionKnockback reference assigned on '{name}'. " +
                "Cannot trigger explosion."
            );
            return;
        }

        if (_oneShot && _hasExploded)
        {
            // Already exploded once; ignore any further entries.
            return;
        }

        // Check layer mask
        if (((1 << other.gameObject.layer) & _triggerLayers.value) == 0)
            return;

        // If a countdown is already running, you can either:
        //  - ignore new entries, or
        //  - restart the timer.
        //
        // For a simple test, we'll **restart** the timer each time
        // something valid enters the area.
        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }

        _lastTriggeringObject = other.gameObject;
        _countdownCoroutine = StartCoroutine(CountdownAndExplode());
    }

    private IEnumerator CountdownAndExplode()
    {
        _isCountdownRunning = true;
        _remainingTime = Mathf.Max(0f, _delayBeforeExplosion);

        while (_remainingTime > 0f)
        {
            _remainingTime -= Time.deltaTime;
            yield return null;
        }

        _isCountdownRunning = false;
        _remainingTime = 0f;
        _countdownCoroutine = null;

        // Trigger the actual explosion.
        _explosion.TriggerExplosion();
        _hasExploded = true;
    }

    private void OnDisable()
    {
        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }

        _isCountdownRunning = false;
        _remainingTime = 0f;
    }

#if UNITY_EDITOR
    // Optional gizmo to show area position.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _isCountdownRunning ? Color.red : Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }
#endif
}