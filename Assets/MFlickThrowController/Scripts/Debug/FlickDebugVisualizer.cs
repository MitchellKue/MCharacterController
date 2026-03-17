/*
------------------------------------------------------------
File: FlickDebugVisualizer.cs
Description:
    Draws scene view debug visuals for flick system.

Visualizes:
    1. Final throw direction
    2. Yaw deviation
    3. Forward bias cone
    4. Spin vectors
    5. Power magnitude

Attach to same GameObject as BallThrowController.
------------------------------------------------------------
*/

using UnityEngine;
using FlickThrowSystem.Execution;
using FlickThrowSystem.Mapping;

namespace FlickThrowSystem.Debugging
{
    [ExecuteAlways]
    public class FlickDebugVisualizer : MonoBehaviour
    {
        [SerializeField] private BallThrowController controller;
        [SerializeField] private float directionLength = 2f;
        [SerializeField] private float spinScale = 0.2f;
        [SerializeField] private float coneLength = 2f;
        [SerializeField] private float coneAngle = 30f;

        private void OnDrawGizmos()
        {
            if (controller == null)
                return;

            var mapping = controller.GetMappingStrategy();
            if (mapping == null)
                return;

            ForwardBiasedMapping.DebugThrowData data =
                mapping.LastDebugData;

            Vector3 origin = data.Origin;

            // --------------------------------------------
            // 1. Final Throw Direction
            // --------------------------------------------

            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                origin,
                origin + data.FinalDirection * directionLength);

            // --------------------------------------------
            // 2. Yaw Arc Visualization
            // --------------------------------------------

            Gizmos.color = Color.yellow;
            Vector3 baseForward = controller.transform.forward;
            Gizmos.DrawLine(
                origin,
                origin + baseForward * directionLength);

            // --------------------------------------------
            // 3. Forward Bias Cone
            // --------------------------------------------

            Gizmos.color = new Color(0f, 0.5f, 1f, 0.4f);

            Vector3 left =
                Quaternion.AngleAxis(-coneAngle, Vector3.up) *
                baseForward;

            Vector3 right =
                Quaternion.AngleAxis(coneAngle, Vector3.up) *
                baseForward;

            Gizmos.DrawLine(origin, origin + left * coneLength);
            Gizmos.DrawLine(origin, origin + right * coneLength);

            // --------------------------------------------
            // 4. Roll Spin Vector
            // --------------------------------------------

            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                origin,
                origin + controller.transform.forward *
                -data.RollSpin * spinScale);

            // --------------------------------------------
            // 5. Curve Spin Vector
            // --------------------------------------------

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(
                origin,
                origin + Vector3.up *
                data.CurveSpin * spinScale);
        }
    }
}