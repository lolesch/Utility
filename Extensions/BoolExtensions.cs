using UnityEngine;

namespace InventorySurvivor.Code.Utility.Extensions
{
    public static class BoolExtensions
    {
        public static bool IsWithinRadius( Coordinate center, Coordinate point, float radius ) =>
            Coordinate.Distance( center, point ) <= radius;

        public static bool IsClockwise( Coordinate sectorArm, Coordinate point ) => 
            Vector2.Dot( point.ToVector2(), sectorArm.ToVector2().Normal() ) < 0;
    }
}
