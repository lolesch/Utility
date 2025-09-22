using UnityEngine;

namespace InventorySurvivor.Code.Utility.Extensions
{
    public static class ColorExtensions
    {
        public static Color Orange => new(1f, .5f, 0f);
        public static Color LightBlue => new(0f, .5f, 1f);
        public static Color Purple => new(.5f, 0f, 1f);
        public static Color LightGreen => new(.5f, 1f, 0f);
        public static Color LightGray => new(.75f, .75f, .75f);
        public static Color DarkGray => new(.25f, .25f, .25f);
        public static Color Prefab => new(.5f, .84f, 1f);
    }
}