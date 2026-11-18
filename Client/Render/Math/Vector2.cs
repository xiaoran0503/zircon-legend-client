using System;

namespace SlimDX
{
    /// <summary>
    /// 二维向量（兼容原 SlimDX.Vector2，用于线段顶点与边框路径）。
    /// </summary>
    public struct Vector2 : IEquatable<Vector2>
    {
        public float X;
        public float Y;

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static readonly Vector2 Zero = new Vector2(0F, 0F);

        public static bool operator ==(Vector2 left, Vector2 right) => left.X == right.X && left.Y == right.Y;
        public static bool operator !=(Vector2 left, Vector2 right) => !(left == right);

        public bool Equals(Vector2 other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Vector2 v && Equals(v);
        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();
        public override string ToString() => $"({X}, {Y})";
    }
}
