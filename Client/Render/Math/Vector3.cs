using System;

namespace SlimDX
{
    /// <summary>
    /// 三维向量（兼容原 SlimDX.Vector3，精灵中心/位置用，Z 保留语义但 2D 渲染忽略）。
    /// </summary>
    public struct Vector3 : IEquatable<Vector3>
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static readonly Vector3 Zero = new Vector3(0F, 0F, 0F);

        public static bool operator ==(Vector3 left, Vector3 right) => left.X == right.X && left.Y == right.Y && left.Z == right.Z;
        public static bool operator !=(Vector3 left, Vector3 right) => !(left == right);

        public bool Equals(Vector3 other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is Vector3 v && Equals(v);
        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}
