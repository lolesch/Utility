using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Submodules.Utility.Extensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        ///     Returns the items in a random order. Eager: the result is a shuffled copy, so
        ///     enumerating it twice yields the same order. Draws from <see cref="UnityEngine.Random"/>,
        ///     so <c>Random.InitState</c> makes it reproducible. Returns null for a null source.
        /// </summary>
        public static IEnumerable<T> Randomize<T>( this IEnumerable<T> source )
        {
            if ( source == null )
                return null;

            var copy = source.ToList();
            copy.Shuffle();
            return copy;
        }

        /// <summary>
        ///     Fisher–Yates shuffle in place, drawing from <see cref="UnityEngine.Random"/>.
        /// </summary>
        public static void Shuffle<T>( this IList<T> list )
        {
            list.Shuffle( UnityRandomIndex );
        }

        /// <summary> The default index source: [0, n) from <see cref="UnityEngine.Random"/>. </summary>
        internal static readonly Func<int, int> UnityRandomIndex = n => UnityEngine.Random.Range( 0, n );

        /// <summary>
        ///     Fisher–Yates shuffle in place. <paramref name="nextIndex"/> takes an exclusive upper
        ///     bound n and returns an index in [0, n) — e.g. <c>new System.Random(seed).Next</c>,
        ///     for a reproducible order in tests.
        /// </summary>
        public static void Shuffle<T>( this IList<T> list, Func<int, int> nextIndex )
        {
            if ( list == null )
                throw new ArgumentNullException( nameof(list) );
            if ( nextIndex == null )
                throw new ArgumentNullException( nameof(nextIndex) );

            for ( var i = list.Count; i-- > 1; )
            {
                var j = nextIndex( i + 1 );
                ( list[i], list[j] ) = ( list[j], list[i] );
            }
        }

        public static IEnumerable<T> GetScriptableObjectsOfType<T>() where T : ScriptableObject
        {
#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets( $"t:{typeof(T).Name}" );
            var paths = guids.Select( AssetDatabase.GUIDToAssetPath );

            return paths.Select( AssetDatabase.LoadAssetAtPath<T> );
#else
            Debug.LogWarning("'GetScriptableObjectsOfType' is an editor only call.");
            return null;
#endif
        }

        /// <summary>
        ///     Wraps this object instance into an IEnumerable&lt;T&gt;
        ///     consisting of a single item.
        /// </summary>
        /// <typeparam name="T"> Type of the object. </typeparam>
        /// <param name="item"> The instance that will be wrapped. </param>
        /// <returns> An IEnumerable&lt;T&gt; consisting of a single item. </returns>
        public static IEnumerable<T> Yield<T>( this T item )
        {
            yield return item;
        }
    }
}