using System;

namespace SlimDX
{
    /// <summary>
    /// 4x4 行主序矩阵（兼容原 SlimDX.Matrix）。客户端只用到 2D 缩放/平移/剪切与单位阵，
    /// 后端从 M11/M21/M12/M22/M41/M42 提取二维仿射变换。
    /// </summary>
    public struct Matrix : IEquatable<Matrix>
    {
        public float M11, M12, M13, M14;
        public float M21, M22, M23, M24;
        public float M31, M32, M33, M34;
        public float M41, M42, M43, M44;

        public static Matrix Identity => new Matrix
        {
            M11 = 1F, M22 = 1F, M33 = 1F, M44 = 1F,
        };

        public static Matrix Scaling(float x, float y, float z)
        {
            Matrix m = Identity;
            m.M11 = x;
            m.M22 = y;
            m.M33 = z;
            return m;
        }

        public static Matrix Translation(float x, float y, float z)
        {
            Matrix m = Identity;
            m.M41 = x;
            m.M42 = y;
            m.M43 = z;
            return m;
        }

        public static Matrix operator *(Matrix a, Matrix b)
        {
            Matrix r;
            r.M11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41;
            r.M12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42;
            r.M13 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43;
            r.M14 = a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44;

            r.M21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41;
            r.M22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42;
            r.M23 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43;
            r.M24 = a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44;

            r.M31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41;
            r.M32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42;
            r.M33 = a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43;
            r.M34 = a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44;

            r.M41 = a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41;
            r.M42 = a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42;
            r.M43 = a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43;
            r.M44 = a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44;
            return r;
        }

        public bool Equals(Matrix other)
        {
            return M11 == other.M11 && M12 == other.M12 && M13 == other.M13 && M14 == other.M14
                && M21 == other.M21 && M22 == other.M22 && M23 == other.M23 && M24 == other.M24
                && M31 == other.M31 && M32 == other.M32 && M33 == other.M33 && M34 == other.M34
                && M41 == other.M41 && M42 == other.M42 && M43 == other.M43 && M44 == other.M44;
        }

        public override bool Equals(object obj) => obj is Matrix m && Equals(m);
        public override int GetHashCode() => HashCode.Combine(M11, M22, M33, M41, M42);
        public override string ToString() => $"[{M11},{M12},{M13},{M14}]";
    }
}
