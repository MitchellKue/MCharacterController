/*
------------------------------------------------------------
File: FlickTelemetryDebugHUD.cs

Displays drag telemetry:
    - Start / End Time
    - Duration
    - Total Distance
    - Screen Velocity
    - Power
    - Yaw
    - Spin
    - Valid State
------------------------------------------------------------
*/

using UnityEngine;
using FlickThrowSystem.Execution;
using FlickThrowSystem.Mapping;

namespace FlickThrowSystem.Debugging
{
    public class FlickTelemetryDebugHUD : MonoBehaviour
    {
        [SerializeField] private BallThrowController controller;
        [SerializeField] private bool show = true;

        [SerializeField] private Vector2 offset = new Vector2(20, 20);

        private GUIStyle style;

        private void Awake()
        {
            style = new GUIStyle();
            style.fontSize = 16;
            style.normal.textColor = Color.white;
        }

        private void OnGUI()
        {
            if (!show || controller == null)
                return;

            var flick = controller.LastFlickResult;
            var mapping = controller.GetMappingStrategy();

            if (mapping == null)
                return;

            var debug = mapping.LastDebugData;

            float y = offset.y;

            DrawLine($"Flick Valid: {flick.IsValid}", ref y);
            DrawLine($"Start Time: {flick.StartTime:F3}", ref y);
            DrawLine($"End Time: {flick.EndTime:F3}", ref y);
            DrawLine($"Duration: {flick.Duration:F3}", ref y);

            DrawLine($"Total Distance: {flick.TotalDistance:F2}", ref y);

            DrawLine($"Screen Velocity: {flick.ScreenVelocity:F2}", ref y);

            DrawLine("--------------------------", ref y);

            DrawLine($"Power: {debug.Power:F2}", ref y);
            DrawLine($"Yaw Angle: {debug.YawAngle:F2}", ref y);
            DrawLine($"Roll Spin: {debug.RollSpin:F2}", ref y);
            DrawLine($"Curve Spin: {debug.CurveSpin:F2}", ref y);
        }

        private void DrawLine(string text, ref float y)
        {
            GUI.Label(
                new Rect(offset.x, y, 600, 25),
                text,
                style);

            y += 22f;
        }
    }
}