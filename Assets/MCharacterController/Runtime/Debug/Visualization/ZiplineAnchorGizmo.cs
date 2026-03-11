// File: Runtime/Debug/ZiplineAnchorGizmo.cs
// Namespace: Kojiko.MCharacterController.Debug

using UnityEngine;

namespace Kojiko.MCharacterController.Debug
{
    /// <summary>
    /// Draws a small gizmo at the zipline anchor position
    /// so you can clearly see where the character attaches.
    /// Editor only (Scene view).
    /// </summary>
    [ExecuteAlways]
    public class ZiplineAnchorGizmo : MonoBehaviour
    {
        [Tooltip("Color of the anchor gizmo in the Scene view.")]
        public Color gizmoColor = Color.yellow;

        [Tooltip("Radius of the little sphere drawn at the anchor.")]
        public float radius = 0.07f;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, radius);

            // Optional cross
            float s = radius * 1.4f;
            Gizmos.DrawLine(transform.position - transform.right * s, transform.position + transform.right * s);
            Gizmos.DrawLine(transform.position - transform.up * s, transform.position + transform.up * s);
            Gizmos.DrawLine(transform.position - transform.forward * s, transform.position + transform.forward * s);
        }
#endif
    }
}