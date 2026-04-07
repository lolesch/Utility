using System;
using UnityEngine;

namespace Submodules.Utility.Tools.ShapeInspector.RectShape
{
    [Serializable]
    public class RectRow<T>
    {
        [SerializeField]
        private T[] row = new T[RectShape<T>.defaultSize];

        public T this[int i]
        {
            get => row[i];
            set => row[i] = value;
        }
    }

    [Serializable]
    public sealed class RectRowBool : RectRow<bool> {}
}
