// File: Runtime/Gameplay/ExplosionKnockback.cs
// Example usage of ExternalForcesAbility knockback.
//
// How to use:
// - Put this on a GameObject that represents an explosion (e.g., a sphere or empty).
// - Configure radius/strength in inspector.
// - Call TriggerExplosion() (from a script, animation event, etc.).
//
// It will:
// - Find all colliders in a radius.
// - For any collider with ExternalForcesAbility on it (or on a parent),
//   compute a knockback impulse and call AddImpulse().

using UnityEngine;
using Kojiko.MCharacterController.Abilities; // <-- make sure namespace matches your project

public class ExplosionKnockback : MonoBehaviour
{
    [Header("Explosion")]
    [Tooltip("Radius of the explosion effect in world units.")]
    [SerializeField] private float _radius = 8f;

    [Tooltip("Base knockback strength at the center (m/s).")]
    [SerializeField] private float _knockbackStrength = 15f;

    [Tooltip("Vertical boost added to the knockback (m/s).")]
    [SerializeField] private float _verticalBoost = 4f;

    [Tooltip("If true, characters at the center get full strength, then it falls off to 0 at radius.")]
    [SerializeField] private bool _useLinearFalloff = true;

    [Tooltip("Layer mask for what the explosion should affect.")]
    [SerializeField] private LayerMask _affectedLayers = ~0;

    [Header("Debug")]
    [Tooltip("Draws the explosion radius gizmo in the Scene view.")]
    [SerializeField] private bool _drawDebugRadius = true;

    /// <summary>
    /// Call this to apply knockback to nearby characters.
    /// You can hook this to an animation event, or call it directly from another script.
    /// </summary>
    [ContextMenu("Trigger Explosion")]
    public void TriggerExplosion()
    {
        Vector3 center = transform.position;

        // Find all colliders in the radius
        Collider[] hits = Physics.OverlapSphere(center, _radius, _affectedLayers, QueryTriggerInteraction.Ignore);

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            // Try to find ExternalForcesAbility on this collider or its parents.
            ExternalForcesAbility forces = hit.GetComponentInParent<ExternalForcesAbility>();
            if (forces == null)
                continue;

            // Direction from explosion center to the character.
            Vector3 toTarget = forces.transform.position - center;
            float distance = toTarget.magnitude;

            if (distance <= 0.0001f)
            {
                // Avoid NaN; just push in some arbitrary direction (e.g., up).
                toTarget = Vector3.up;
                distance = 0f;
            }

            Vector3 dir = toTarget.normalized;

            // Compute attenuation based on distance.
            float attenuation = 1f;
            if (_useLinearFalloff && _radius > 0f)
            {
                // 1.0 at distance 0, 0.0 at distance >= radius.
                float t = Mathf.Clamp01(distance / _radius);
                attenuation = 1f - t;
            }

            if (attenuation <= 0f)
                continue;

            // Base horizontal knockback.
            Vector3 horizontalDir = new Vector3(dir.x, 0f, dir.z);
            if (horizontalDir.sqrMagnitude < 0.0001f)
            {
                // If they are directly above/below, still give some push.
                horizontalDir = new Vector3(dir.x, 0f, dir.z + 0.0001f).normalized;
            }
            else
            {
                horizontalDir.Normalize();
            }

            float strength = _knockbackStrength * attenuation;

            Vector3 impulse = horizontalDir * strength;

            // Optional vertical kick.
            impulse.y += _verticalBoost * attenuation;

            // Apply to the character via ExternalForcesAbility.
            forces.AddImpulse(impulse, includeVertical: true);
        }
    }

    private void OnDrawGizmos()
    {
        if (!_drawDebugRadius)
            return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, _radius);

        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}