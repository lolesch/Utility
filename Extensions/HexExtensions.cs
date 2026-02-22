using UnityEngine;

namespace Submodules.Utility.Extensions
{
    public static class HexExtensions
    {
        /// <summary>
        /// Converts a cell position to Hex position.
        /// </summary>
        //NOTE: inverted cell.y to match unity's grid with y pointing up
        public static Hex CellToHex(this Vector3Int cell) => new(cell.x - (-cell.y - (-cell.y & 1)) / 2, -cell.y);
        public static Hex PixelToHex(this Vector3 pixel, Grid hexGrid) => CellToHex(hexGrid.WorldToCell(pixel));
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