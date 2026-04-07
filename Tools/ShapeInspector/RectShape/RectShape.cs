using System;
using System.Collections.Generic;
using UnityEngine;

namespace Submodules.Utility.Tools.ShapeInspector.RectShape
{
    // based on: https://github.com/Eldoir/Array2DEditor
    [Serializable]
    public abstract class RectShape<T>
    {
        public const int defaultSize = 1;

        [SerializeField] protected Vector2Int shapeSize = Vector2Int.one * defaultSize;

#pragma warning disable 414
        /// <summary>
        /// NOTE: Only used to display the cells in the Editor. This won't affect the build.
        /// </summary>
        [SerializeField]
        private Vector2Int cellSize;
#pragma warning restore 414

        protected abstract RectRow<T> GetCellRow(int i);

        public T[,] GetCells()
        {
            var cells = new T[shapeSize.y, shapeSize.x];

            for (var y = 0; y < shapeSize.y; y++)
                for (var x = 0; x < shapeSize.x; x++)
                    cells[y, x] = GetCell(x, y);
            
            return cells;
        }

        public T GetCell(int x, int y) => GetCellRow(y)[x];

        public void SetCell(int x, int y, T value) => GetCellRow(y)[x] = value;
        
        public List<Vector2Int> GetVec2Ints()
        {
            var cells = GetCells();

            var map = new List<Vector2Int>();

            for (var x = 0; x < cells.GetLength(0); x++)
                for (var y = 0; y < cells.GetLength(1); y++)
                    if( IsValid( GetCell( y, x ) ) )
                        map.Add( new Vector2Int( y, x ) );

            return map;
        }
        public Vector2Int GetDimensions() => shapeSize;
        
        protected abstract bool IsValid(T target);
    }
    
    [Serializable]
    public class RectShapeBool : RectShape<bool>
    {
        public RectShapeBool(int shapeSize = defaultSize)
        {
            this.shapeSize = Vector2Int.one * shapeSize;
            rows = new RectRowBool[shapeSize];
        }
        
        [SerializeField]
        RectRowBool[] rows = new RectRowBool[defaultSize];

        protected override RectRow<bool> GetCellRow(int idx) => rows[idx];
        protected override bool IsValid(bool target) => target == true;
    }
}
