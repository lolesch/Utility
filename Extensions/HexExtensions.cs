using UnityEngine;

namespace Submodules.Utility.Extensions
{
    public static class HexExtensions
    {
        public static Hex GridToHex(this Vector3Int grid) => new(grid.x - (grid.y - (grid.y & 1)) / 2, grid.y);
        public static Hex PixelToHex(this Vector3 pixel, Grid hexGrid) => GridToHex(hexGrid.WorldToCell(pixel));
        public static Hex PixelToHex(this Vector2 pixel, float hexSize, Vector2Int spacing, Vector2 origin)
        {
            pixel -= origin;
            var q = (Mathf.Sqrt(3) / 3 * pixel.x - 1f / 3 * pixel.y) / (hexSize + spacing.x);
            var r = 2f / 3 * pixel.y / (hexSize + spacing.y);

            return new FloatHex(q, r).Round();// * new Vector2Int(1, -1);
        }
        public static Hex GUIToHex(this Vector2 pixel, float hexSize, Vector2Int spacing, Vector2 origin) => PixelToHex(pixel * new Vector2(1, -1), hexSize, spacing, origin * new Vector2(1, -1));
        
        public static Hex WorldToHex(this Vector3 worldPosition, float hexWidth, float hexCircumradius )
        {
            var rowSquash = hexCircumradius * 1.5f;
            var r = Mathf.RoundToInt(-worldPosition.z / rowSquash );
            
            var oddRowIndent = r % 2 == 0 ? 0 : hexWidth * .5f;
            var q = Mathf.RoundToInt((worldPosition.x - oddRowIndent) / hexWidth) - ( r - ( r & 1 ) ) / 2;
           
            return new Hex( q, r );
        }
    }
}