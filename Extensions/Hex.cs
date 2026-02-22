using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Submodules.Utility.Extensions
{
    /// MISSING FUNCTIONALITY:
    // Intersecting ranges
    // Reflection across one of the axis
    // Rings around a center && spiral ring
    // Field of view
    // Map storage
    // Wraparound

    [Serializable]
    public struct Hex : IEquatable<Hex>, ISerializationCallbackReceiver
    {
        public enum HexDiagonal
        {
            DownRight = 0,  // qPlus 
            Up = 1,         // rPlus 
            DownLeft = 2,   // sPlus 
            UpLeft = 3,     // qMinus
            Down = 4,       // rMinus
            UpRight = 5,    // sMinus
        }

        public enum HexDirection
        {
            Right = 0,
            LeftUp = 1,
            LeftDown = 2,
            Left = 3,
            RightDown = 4,
            RightUp = 5,
            Zero = 6        // noDistance
        }

        public Hex(int q, int r)
        {
            name = Name(q, r);
            this.q = q;
            this.r = r;
        }
        
        public Hex(Vector2Int vector2) : this(vector2.x, vector2.y) { }
        public Hex(Vector3Int vector3) : this(vector3.x, vector3.y) { }


        [HideInInspector] public string name; // used for named collection elements
        [field: SerializeField] public int q { get; set; }
        [field: SerializeField] public int r { get; set; }
        public readonly int s => -q - r;

        public static readonly Hex zero = new(0, 0);
        public static readonly Hex right = new(1, 0);
        public static readonly Hex rightDown = new(1, -1);
        public static readonly Hex rightUp = new(0, 1);
        public static readonly Hex left = new(-1, 0);
        public static readonly Hex leftDown = new(0, -1);
        public static readonly Hex leftUp = new(-1, 1);

        public static readonly Hex Invalid = new(int.MinValue, int.MinValue);
        public static readonly Hex MinValue = new(int.MinValue + 1, int.MinValue + 1);
        public static readonly Hex MaxValue = new(int.MaxValue, int.MaxValue);
        
        public bool IsValid => !Equals(Invalid);
        
        #region Overrides and Operators
        public override int GetHashCode() => base.GetHashCode();

        public bool Equals(Hex other) => q == other.q && r == other.r;
        public override bool Equals(object other) => other is Hex hex && q == hex.q && r == hex.r;

        public override string ToString() => Name(this);
        public static string Name(Hex hex) => Name(hex.q, hex.r);
        public static string Name(Vector2Int grid) => Name(grid.x, grid.y);
        public static string Name(int q, int r) => $"Hex({q}, {r})";//, {-q - r})";

        public static bool operator ==(Hex a, Hex b) => a.q == b.q && a.r == b.r;
        public static bool operator !=(Hex a, Hex b) => !(a == b);
        public static Hex operator *(Hex hex, int scalar) => new (hex.q * scalar, hex.r * scalar);
        public static Hex operator *(Hex hex, Vector2Int scalar) => new (hex.q * scalar.x, hex.r * scalar.y);
        #endregion

        #region Coordinate arithmetic
        public readonly Hex Add(Hex vector) => new(q + vector.q, r + vector.r);
        public readonly Hex Subtract(Hex vector) => new(q - vector.q, r - vector.r);
        public readonly Hex Scale(int k) => new(q * k, r * k);
        #endregion

        //TODO: review rotations to support all 6 directions
        #region Rotation

        public readonly Hex Rotate( bool clockwise ) => clockwise ? new Hex( -r, -s ) : new Hex( -s, -q );
        
        public readonly Hex RotateAroundCenter( bool clockwise, Hex center)
        {
            var vector = Subtract(center);
            var rotatedVector = vector.Rotate( clockwise );
            return center.Add(rotatedVector);
        }
        #endregion

        private readonly int Length()
        {
            // Math.Abs(int.MinValue) is out of range 
            if (q == int.MinValue || r == int.MinValue || s == int.MinValue)
                return int.MaxValue;

            return (Math.Abs(q) + Math.Abs(r) + Math.Abs(s)) / 2;
        }

        public readonly int Distance(Hex to) => Subtract(to).Length();

        #region Conversion

        /// <summary>
        /// Converts a Hex position to cell position.
        /// </summary>
        public readonly Vector3Int ToCell() => new(q + (r - (r & 1)) / 2, -r, 0);

        public readonly Vector3 ToWorldPos( float hexWidth, float hexCircumradius )
        {
            var oddRowIndent = r % 2 == 0 ? 0 : hexWidth * .5f;
            var rowSquash = hexCircumradius * 1.5f;
            
            return new Vector3( (q + ( r - ( r & 1 ) ) / 2f) * hexWidth + oddRowIndent, 0, -r * rowSquash );
        }

        public readonly Vector3 ToPixel(Grid hexGrid) => hexGrid.CellToWorld(ToCell());
        public readonly Vector3 ToPixel(float hexSize, Vector2Int spacing, Vector2 origin)
        {
            var x = (hexSize + spacing.x) * (Mathf.Sqrt(3) * q + Mathf.Sqrt(3) / 2 * r);
            var y = (hexSize + spacing.y) * (3f / 2 * r);

            return new Vector3(x + origin.x, y + origin.y, 0);
        }
        public readonly Vector3 ToGUI(float hexSize, Vector2Int spacing, Vector2 origin) => ToPixel(hexSize, spacing, origin) * new Vector2(1, -1);

        #endregion

        #region Directions and Diagonals
        public static Hex Direction(HexDirection direction) => direction switch
        {
            HexDirection.Right => right,
            HexDirection.LeftUp => leftUp,
            HexDirection.LeftDown => leftDown,
            HexDirection.Left => left,
            HexDirection.RightDown => rightDown,
            HexDirection.RightUp => rightUp,
            HexDirection.Zero => zero,
            _ => zero
        };

        public readonly HexDirection GetDirection(Hex to)
        {
            var vector = Subtract(to);
            var length = vector.Length();
            if( length == 0 ) 
                return HexDirection.Zero;
                
            var normalized = new FloatHex(new Vector2(vector.q, vector.r) / length).Round();

            Debug.LogWarning($"direction: {normalized}");

            if (normalized == Hex.right)
                return HexDirection.Right;
            if (normalized == Hex.leftUp)
                return HexDirection.LeftUp;
            if (normalized == Hex.leftDown)
                return HexDirection.LeftDown;
            if (normalized == Hex.left)
                return HexDirection.Left;
            if (normalized == Hex.rightDown)
                return HexDirection.RightDown;
            if (normalized == Hex.rightUp)
                return HexDirection.RightUp;

            return HexDirection.Zero;
        }

        public static Hex Diagonal(HexDiagonal diagonal) => diagonal switch
        {
            HexDiagonal.DownRight => new Hex(+2, -1),
            HexDiagonal.Up => new Hex(-1, +2),
            HexDiagonal.DownLeft => new Hex(-1, -1),
            HexDiagonal.UpLeft => new Hex(-2, 1),
            HexDiagonal.Down => new Hex(1, -2),
            HexDiagonal.UpRight => new Hex(1, 1),
            _ => zero
        };
        #endregion

        public readonly Hex GetNeighbor(HexDirection direction) => Add(Direction(direction));
        public readonly List<Hex> Neighbors() => HexRange(1, true);

        public readonly Hex GetDiagonalNeighbor(HexDiagonal diagonal) => Add(Diagonal(diagonal));

        public readonly List<Hex> HexRange(int range, bool excludeCenter = false)
        {
            var inRange = new List<Hex>();

            for (var r = -range; r <= range; r++)
            {
                var max = Math.Max(-range, -r - range);
                var min = Math.Min(range, -r + range);

                for (var q = max; q <= min; q++)
                {
                    if (excludeCenter && q == 0 && r == 0)
                        continue;

                    inRange.Add(Add(new Hex(q, r)));
                }
            }

            return inRange;
        }

        //TODO: calculate terrain cost (heuristic)
        public readonly List<Hex> ReachableHexes(int range, List<Hex> invalidPositions)
        {
            var visited = new List<Hex> { this };

            var fringes = new List<List<Hex>> { new List<Hex>() };

            fringes[0].Add(this);

            for (var i = 1; i <= range; i++) //TODO: calculate terrain cost (heuristic)
            {
                fringes.Add(new List<Hex>());

                foreach (var hex in fringes[i - 1])
                {
                    var validNeighbors = hex.Neighbors().Except(invalidPositions.Concat(visited));

                    foreach (var neighbor in validNeighbors)
                    {
                        visited.Add(neighbor);

                        fringes[i].Add(neighbor);
                    }
                }
            }
            return visited;
        }

        public readonly Hex GetClosestValidNeighbor(List<Hex> invalidPositions, int maxRange = 10)
        {
            if (invalidPositions.Contains(this))
            {
                var neighbors = new List<Hex>();

                for (var i = 1; i <= maxRange; i++)
                {
                    neighbors = HexRange(i).Except(invalidPositions).ToList();

                    if( !neighbors.Any() ) 
                        continue;
                    
                    var from = this;
                    return neighbors.Randomize().OrderBy(x => x.Distance(from)).FirstOrDefault();
                }
            }
            return Invalid;
        }

        private readonly FloatHex Lerp(Hex to, float t) => new(Lerp(q, to.q, t), Lerp(r, to.r, t));
        private static float Lerp(int a, int b, float t)
        {
            t = Mathf.Clamp01(t);
            return a * (1 - t) + b * t;
        }

        // TODO return a pathnode instead of a linked list
        public readonly LinkedList<Hex> ExtendLine(HexDirection direction, Hex to) => ExtendLine(direction, Distance(to));
        public readonly LinkedList<Hex> ExtendLine(HexDirection direction, int distance)
        {
            var path = new LinkedList<Hex>();
            var dirVector = Direction(direction);
            path.AddFirst(this);

            for (var i = 0; i < distance; i++)
            {
                var next = path.Last.Value.Add(dirVector);
                path.AddLast(next);
            }

            return path;
        }
        
        public List<Hex> HexLine(Hex to)
        {
            var line = new List<Hex>();
            var distance = Distance(to);

            var step = 1f / Mathf.Max(distance, 1);

            for (var i = 0; i <= distance; i++)
                line.Add(Lerp(to, step * i).Round());

            return line;
        }

        public readonly void OnBeforeSerialize() { }
        public void OnAfterDeserialize() => name = ToString();
    }

    public readonly struct FloatHex
    {
        public FloatHex(float q, float r)
        {
            this.q = q;
            this.r = r;
            s = -q - r;
            if (q + r + s != 0)
                Debug.LogWarning($"Float inaccuracy: q + r + s should be 0 but was {q + r + s}");
        }
        public FloatHex(Hex hex)
        {
            q = hex.q;
            r = hex.r;
            s = -q - r;
            if (q + r + s != 0)
                Debug.LogWarning($"Float inaccuracy: q + r + s should be 0 but was {q + r + s}");
        }
        public FloatHex(Vector3 vector3)
        {
            q = vector3.x;
            r = vector3.y;
            s = -q - r;
            if (q + r + s != 0)
                throw new ArgumentException("q + r + s must be 0");
        }
        public FloatHex(Vector2 vector2)
        {
            q = vector2.x;
            r = vector2.y;
            s = -q - r;
            if (q + r + s != 0)
                Debug.LogWarning($"Float inaccuracy: q + r + s should be 0 but was {q + r + s}");
        }

        public readonly float q;
        public readonly float r;
        public readonly double s;

        public Hex Round()
        {
            var gridQ = (int)Math.Round(q);
            var gridR = (int)Math.Round(r);
            var remainderQ = q - gridQ;
            var remainderR = r - gridR;

            return Math.Abs(remainderQ) >= Math.Abs(remainderR)
                ? new Hex(gridQ + (int)Math.Round(remainderQ + 0.5 * remainderR), gridR)
                : new Hex(gridQ, gridR + (int)Math.Round(remainderR + 0.5 * remainderQ));
        }
    }
}
