using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Submodules.Utility.Extensions
{
    [Serializable]
    public struct CellTile<T> : ISerializationCallbackReceiver where T : TileBase
    {
        [HideInInspector] public string name;
        [SerializeField, ReadOnly, AllowNesting] private Vector3Int cell;
        [SerializeField] private T tile;
            
        public  CellTile(Vector3Int cell, T tile)
        {
            this.cell = cell;
            this.tile = tile;
            name = string.Empty;
        }

        public void OnBeforeSerialize() => name = $"{cell}\t{tile.name}";
        public void OnAfterDeserialize() {}
    }
    
    public static class TilemapExtensions
    {
        private static bool TryGetTile<T>(this Tilemap tilemap, Vector3Int position, out T tile) where T : TileBase
        {
            tile = tilemap.GetTile(position) as T;
            return tile != null;
        }
        
        public static IEnumerable<CellTile<T>> GetAllCellTiles<T>(this Tilemap tilemap) where T : TileBase
        {
            tilemap.CompressBounds();
            foreach (var cell in tilemap.cellBounds.allPositionsWithin)
            {
                if (cell.z != 0) continue;
                if (tilemap.TryGetTile<T>(cell,  out var tile))
                    yield return new CellTile<T>( cell, tile);
            }
        }
        
        public static IEnumerable<T> GetAllTiles<T>(this Tilemap tilemap) where T : TileBase
        {
            tilemap.CompressBounds(); // Shrink bounds to actual tile area first
            foreach (var cell in tilemap.cellBounds.allPositionsWithin)
            {
                if (cell.z != 0) continue;
                if (tilemap.TryGetTile<T>(cell,  out var tile))
                    yield return tile;
            }
        }
        
        public static IEnumerable<Vector3Int> GetAllCells(this Tilemap tilemap)
        {
            tilemap.CompressBounds(); // Shrink bounds to actual tile area first
            foreach (var cell in tilemap.cellBounds.allPositionsWithin)
            {
                if (cell.z != 0) continue;
                if (tilemap.HasTile(cell))
                    yield return cell;
            }
        }
    }
}