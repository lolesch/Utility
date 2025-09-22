using UnityEngine;

namespace Submodules.Utility.Extensions
{
    public static class GizmosExtensions
    {
        /// <summary>
        ///     Draws a wire arc.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="dir">The direction from which the anglesRange is taken into account</param>
        /// <param name="anglesRange">The angle range, in degrees.</param>
        /// <param name="radius"></param>
        /// <param name="maxSteps">How many steps to use to draw the arc.</param>
        public static void DrawWireArc( Vector3 position, Vector3 dir, float anglesRange, float radius,
            float maxSteps = 20 )
        {
            var srcAngles = GetAnglesFromDir( position, dir );
            var initialPos = position;
            var posA = initialPos;
            var stepAngles = anglesRange / maxSteps;
            var angle = srcAngles - anglesRange / 2;
            for ( var i = 0; i <= maxSteps; i++ )
            {
                var rad = Mathf.Deg2Rad * angle;
                var posB = initialPos;
                posB += new Vector3( radius * Mathf.Cos( rad ), 0, radius * Mathf.Sin( rad ) );

                Gizmos.DrawLine( posA, posB );

                angle += stepAngles;
                posA = posB;
            }

            Gizmos.DrawLine( posA, initialPos );
        }

        private static float GetAnglesFromDir( Vector3 position, Vector3 dir )
        {
            var forwardLimitPos = position + dir;
            var srcAngles = Mathf.Rad2Deg *
                            Mathf.Atan2( forwardLimitPos.z - position.z, forwardLimitPos.x - position.x );

            return srcAngles;
        }

        public static void DrawArrow( Vector3 from, Vector3 direction, float arrowHeadLength = 0.25f,
            float arrowHeadAngle = 20.0f )
        {
            Gizmos.DrawRay( from, direction );
            DrawArrowEnd( from, direction, arrowHeadLength, arrowHeadAngle );
        }

        private static void DrawArrowEnd( Vector3 from, Vector3 length, float arrowHeadLength = 0.25f,
            float arrowHeadAngle = 20.0f )
        {
            var right = Quaternion.LookRotation( length ) * Quaternion.Euler( arrowHeadAngle, 0, 0 ) * Vector3.back;
            var left = Quaternion.LookRotation( length ) * Quaternion.Euler( -arrowHeadAngle, 0, 0 ) * Vector3.back;
            var up = Quaternion.LookRotation( length ) * Quaternion.Euler( 0, arrowHeadAngle, 0 ) * Vector3.back;
            var down = Quaternion.LookRotation( length ) * Quaternion.Euler( 0, -arrowHeadAngle, 0 ) * Vector3.back;

            Gizmos.DrawRay( from + length, right * arrowHeadLength );
            Gizmos.DrawRay( from + length, left * arrowHeadLength );
            Gizmos.DrawRay( from + length, up * arrowHeadLength );
            Gizmos.DrawRay( from + length, down * arrowHeadLength );
        }
    }
}