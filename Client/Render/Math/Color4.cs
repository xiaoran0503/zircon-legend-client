using System;
using System.Drawing;

namespace SlimDX
{
    /// <summary>
    /// 归一化 RGBA 颜色（兼容原 SlimDX.Color4）。与 System.Drawing.Color 互转，
    /// 通道取值 0..1；ToColor() 映射回 0..255。
    /// </summary>
    public struct Color4
    {
        public float Red;
        public float Green;
        public float Blue;
        public float Alpha;

        public Color4(float red, float green, float blue) : this(red, green, blue, 1F)
        {
        }

        public Color4(float red, float green, float blue, float alpha)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        public static readonly Color4 Black = new Color4(0F, 0F, 0F, 1F);
        public static readonly Color4 White = new Color4(1F, 1F, 1F, 1F);

        public Color ToColor()
        {
            byte r = (byte)Math.Clamp((int)(Red * 255F + 0.5F), 0, 255);
            byte g = (byte)Math.Clamp((int)(Green * 255F + 0.5F), 0, 255);
            byte b = (byte)Math.Clamp((int)(Blue * 255F + 0.5F), 0, 255);
            byte a = (byte)Math.Clamp((int)(Alpha * 255F + 0.5F), 0, 255);
            return Color.FromArgb(a, r, g, b);
        }

        public static implicit operator Color4(Color color)
        {
            return new Color4(color.R / 255F, color.G / 255F, color.B / 255F, color.A / 255F);
        }

        public static explicit operator Color(Color4 color) => color.ToColor();

        public override string ToString() => $"({Red}, {Green}, {Blue}, {Alpha})";
    }
}
