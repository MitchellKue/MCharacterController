/*
------------------------------------------------------------
File: PointerSampler.cs
Description:
    Maintains rolling buffer of pointer samples.
    Provides windowed velocity calculation.

Flow:
    1. StartSampling() called on press
    2. AddSample() called every Update while held
    3. StopSampling() on release
    4. Analyzer requests windowed velocity
------------------------------------------------------------
*/

using System.Collections.Generic;
using UnityEngine;
using FlickThrowSystem.Core;

namespace FlickThrowSystem.Sampling
{
    public class PointerSampler
    {
        private readonly List<PointerSample> samples = new List<PointerSample>();

        private Vector2 pressPosition;
        private float pressTime;

        public void StartSampling(Vector2 position, float time)
        {
            samples.Clear();
            pressPosition = position;
            pressTime = time;

            samples.Add(new PointerSample(position, time));
        }

        public void AddSample(Vector2 position, float time)
        {
            samples.Add(new PointerSample(position, time));
        }

        public (Vector2 velocity, float dragDistance, float duration)
            CalculateWindowedVelocity(float window)
        {
            if (samples.Count < 2)
                return (Vector2.zero, 0f, 0f);

            PointerSample latest = samples[samples.Count - 1];
            float targetTime = latest.Time - window;

            PointerSample reference = samples[0];

            for (int i = samples.Count - 1; i >= 0; i--)
            {
                if (samples[i].Time <= targetTime)
                {
                    reference = samples[i];
                    break;
                }
            }

            Vector2 delta = latest.Position - reference.Position;
            float deltaTime = latest.Time - reference.Time;

            Vector2 velocity = deltaTime > 0f ? delta / deltaTime : Vector2.zero;

            float dragDistance = Vector2.Distance(pressPosition, latest.Position);
            float duration = latest.Time - pressTime;

            return (velocity, dragDistance, duration);
        }
    }
}