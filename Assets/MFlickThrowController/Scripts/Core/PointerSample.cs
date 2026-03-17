/*
------------------------------------------------------------
File: PointerSample.cs
Description:
    Immutable data container representing a single pointer
    sample in time. Used by PointerSampler.

Flow:
    1. Input source provides position + time
    2. Sampler stores PointerSample
    3. Analyzer consumes rolling buffer of samples
------------------------------------------------------------
*/

using UnityEngine;

namespace FlickThrowSystem.Core
{
    public struct PointerSample
    {
        public readonly Vector2 Position;
        public readonly float Time;

        public PointerSample(Vector2 position, float time)
        {
            Position = position;
            Time = time;
        }
    }
}