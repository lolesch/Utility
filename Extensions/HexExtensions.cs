using UnityEngine;

namespace Submodules.Utility.Extensions
{
    public static class HexExtensions
    {
        /// <summary>
        /// Converts Unity's Grid cell position to Hex position.
        /// </summary>
        public static Hex CellToHex(this Vector3Int cell) => new(cell.x + ((cell.y + (cell.y & 1)) >> 1), -cell.y);
        
        public static Hex WorldToHex(this Vector3 worldPosition, float hexWidth, float hexCircumradius )
        {
            var rowSquash = hexCircumradius * 1.5f;
            var r = Mathf.RoundToInt(-worldPosition.z / rowSquash );
            
            var oddRowIndent = r % 2 == 0 ? 0 : hexWidth * .5f;
            var q = Mathf.RoundToInt((worldPosition.x - oddRowIndent) / hexWidth) - ( r - ( r & 1 ) ) / 2;
           
            return new Hex( q, r );
        }
        //public static Hex WorldToHex(this Vector3 worldPosition, float size) => new Vector2(worldPosition.x, worldPosition.z).PixelToHex(size);
        
        //public static Hex PixelToHex(this Vector3 pixel, Grid hexGrid) => CellToHex(hexGrid.WorldToCell(pixel));
        /*public static Hex PixelToHex(this Vector2 pixel, float hexSize, Vector2Int spacing, Vector2 origin)
        {
            pixel -= origin;
            var q = (Mathf.Sqrt(3) / 3 * pixel.x - 1f / 3 * pixel.y) / (hexSize + spacing.x);
            var r = 2f / 3 * pixel.y / (hexSize + spacing.y);

            return new FloatHex(q, r).Round();// * new Vector2Int(1, -1);
        }*/
        
        /*public static Hex PixelToHex(this Vector2 pixel, float size) 
        {
            var q = (Mathf.Sqrt(3) / 3 * pixel.x - 1f / 3 * pixel.y) / size;
            var r = 2f / 3 * pixel.y / size;
            return new FloatHex(q, r).Round();
        }*/

        //public static Hex GUIToHex(this Vector2 guiPos, float size) => new Vector2(guiPos.x, -guiPos.y).PixelToHex(size);
        //public static Hex GUIToHex(this Vector2 guiPos, float hexSize, Vector2Int spacing, Vector2 origin) => PixelToHex(new Vector2(guiPos.x, -guiPos.y), hexSize, spacing, new Vector2(origin.x, -origin.y));
    }
}