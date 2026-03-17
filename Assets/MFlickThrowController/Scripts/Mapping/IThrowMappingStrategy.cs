/*
------------------------------------------------------------
File: IThrowMappingStrategy.cs
Description:
    Interface for mapping flick result into
    world velocity & spin torque.

------------------------------------------------------------
*/

using UnityEngine;
using FlickThrowSystem.Core;

namespace FlickThrowSystem.Mapping
{
    public interface IThrowMappingStrategy
    {
        void MapThrow(
            FlickResult flick,
            Transform cameraTransform,
            Rigidbody ballRigidbody);
    }
}