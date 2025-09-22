using System;
using Submodules.Utility.AttributeRef.Attributes;
using UnityEngine;

namespace Submodules.Utility.Tools
{
    [Serializable]
    public sealed class Stopwatch
    {
        [SerializeField, ReadOnly] private float timeElapsed;

        public void Reset() => timeElapsed = 0;

        public void Tick( float tickInterval ) => timeElapsed += tickInterval;

        public static implicit operator float( Stopwatch stopwatch ) => stopwatch.timeElapsed;
    }
}