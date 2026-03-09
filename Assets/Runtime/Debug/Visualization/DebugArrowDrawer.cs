// File: Runtime/Debug/Visualization/DebugArrowDrawer.cs
// Namespace: Kojiko.MCharacterController.Debug
//
// Summary:
// Provides a simple utility for drawing a 3D arrow (line + arrow head)
// using Unity's GL API. Intended for runtime debug visualization systems.

using UnityEngine;

namespace Kojiko.MCharacterController.Debug
{
    /// <summary>
    /// Static helper for drawing 3D arrows using GL lines.
    /// This class assumes that GL.Begin(GL.LINES) has already been called,
    /// and that an appropriate line material has been set with SetPass(0).
    /// </summary>
    public static class DebugArrowDrawer
    {
        /// <summary>
        /// Draws a 3D arrow from start to end using GL lines.
        /// Call only between GL.Begin(GL.LINES) and GL.End().
        /// </summary>
        /// <param name="start">World-space start position of the arrow.</param>
        /// <param name="end">World-space end position of the arrow.</param>
        /// <param name="color">Color to use for this arrow.</param>
        /// <param name="headSize">
        /// Approximate length of the arrow head in world units.
        /// The final head length is clamped to half of the total arrow length.
        /// </param>
        public static void DrawArrow(Vector3 start, Vector3 end, Color color, float headSize = 0.2f)
        {
            // Compute direction and total length.
            Vector3 direction = end - start;
            float length = direction.magnitude;

            // If the arrow is effectively zero-length, do nothing.
            if (length <= Mathf.Epsilon)
                return;

            direction /= length;

            // Draw the main line body.
            GL.Color(color);
            GL.Vertex(start);
            GL.Vertex(end);

            // We need a local coordinate system around the arrow's direction
            // to construct a simple arrow head shape (small "pyramid" or "X").
            Vector3 right = Vector3.Cross(direction, Vector3.up);

            // If direction is almost parallel to world up, choose another axis to cross with.
            if (right == Vector3.zero)
                right = Vector3.Cross(direction, Vector3.forward);

            right.Normalize();

            Vector3 up = Vector3.Cross(right, direction).normalized;

            // Compute the head length, clamped against total arrow length.
            float headLength = Mathf.Min(headSize, length * 0.5f);
            Vector3 headBase = end - direction * headLength;

            // Draw several small lines forming a simple arrow head.

            // Line 1
            GL.Vertex(end);
            GL.Vertex(headBase + right * headLength * 0.5f);

            // Line 2
            GL.Vertex(end);
            GL.Vertex(headBase - right * headLength * 0.5f);

            // Line 3
            GL.Vertex(end);
            GL.Vertex(headBase + up * headLength * 0.5f);

            // Line 4
            GL.Vertex(end);
            GL.Vertex(headBase - up * headLength * 0.5f);
        }
    }
}