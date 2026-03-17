/*
------------------------------------------------------------
File: FlickAnalyzer.cs
Description:
    Converts raw sampled velocity into directional components.
    Applies screen normalization.

Flow:
    1. Receives raw velocity from PointerSampler
    2. Normalizes relative to screen height
    3. Extracts forward + horizontal components
    4. Passes data to FlickValidator
------------------------------------------------------------
*/

using UnityEngine;
using FlickThrowSystem.Core;

namespace FlickThrowSystem.Analysis
{
    public class FlickAnalyzer
    {
        public struct AnalyzedFlick
        {
            public Vector2 RawVelocity;
            public Vector2 NormalizedVelocity;
            public float ForwardComponent;
            public float HorizontalComponent;
            public float DragDistance;
            public float Duration;
            public float StartTime;    
            public float EndTime;    
        }

        public AnalyzedFlick Analyze(
            Vector2 rawVelocity,
            float dragDistance,
            float duration)
        {
            Vector2 normalized = rawVelocity / Screen.height;

            return new AnalyzedFlick
            {
                RawVelocity = rawVelocity,
                NormalizedVelocity = normalized,
                ForwardComponent = normalized.y,
                HorizontalComponent = normalized.x,
                DragDistance = dragDistance,
                Duration = duration
            };
        }
    }
}