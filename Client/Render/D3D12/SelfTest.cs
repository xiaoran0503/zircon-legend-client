using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Client.Render;
using SlimDX;
using SlimDX.Direct3D9;

namespace Client.Render.D3D12
{
    /// <summary>
    /// 离屏渲染自检（无窗口）：清色背景 → 上传纯色精灵纹理 → 居中绘制 32x32 四边形 →
    /// 回读 PNG → 中心像素等于纹理色、角像素等于背景色。DX12 硬件与 WARP 各跑一遍。
    /// </summary>
    public static class SelfTest
    {
        private const int Size = 64;
        private const int SpriteSize = 32;

        public static int Run(string renderer, string pngPath)
        {
            IRenderBackend backend = null;
            try
            {
                backend = BackendFactory.Create(renderer, IntPtr.Zero, Size, Size, true, false);
                backend.InitOffscreen(Size, Size);
                backend.Clear(Color.FromArgb(255, 25, 25, 25));

                var tex = backend.CreateTexture(SpriteSize, SpriteSize, Format.A8R8G8B8, Usage.None, Pool.Managed);
                var rect = tex.LockRectangle(0, LockFlags.Discard);
                var staging = rect.Data.ToArray();
                for (int i = 0; i < SpriteSize * SpriteSize; i++)
                {
                    staging[i * 4 + 0] = 0;     // B
                    staging[i * 4 + 1] = 200;   // G
                    staging[i * 4 + 2] = 100;   // R
                    staging[i * 4 + 3] = 255;  // A
                }
                Marshal.Copy(staging, 0, rect.Data.DataPointer, staging.Length);
                tex.UnlockRectangle(0);
                tex.CommitUpload();

                backend.BeginScene();
                backend.Sprite.Begin(SpriteFlags.None);
                backend.Sprite.Draw(tex, null,
                    new SlimDX.Vector3(SpriteSize / 2F, SpriteSize / 2F, 0),
                    new SlimDX.Vector3(Size / 2F, Size / 2F, 0),
                    new Color4(1F, 1F, 1F, 1F));
                backend.Sprite.End();
                backend.EndScene();

                backend.ReadbackToPNG(pngPath);

                if (backend is D3D12Backend db) db.DumpInfoQueue();

                using var bmp = new Bitmap(pngPath);
                var center = bmp.GetPixel(Size / 2, Size / 2);
                var corner = bmp.GetPixel(0, 0);

                bool centerOk = Math.Abs(center.R - 100) <= 4 && Math.Abs(center.G - 200) <= 4
                    && center.B <= 4 && center.A == 255;
                bool cornerOk = Math.Abs(corner.R - 25) <= 4 && Math.Abs(corner.G - 25) <= 4
                    && Math.Abs(corner.B - 25) <= 4;

                Console.WriteLine($"[{renderer}] center=({center.R},{center.G},{center.B},{center.A}) corner=({corner.R},{corner.G},{corner.B},{corner.A})");
                Console.WriteLine(centerOk && cornerOk ? "SELFTEST PASS" : "SELFTEST FAIL");
                return centerOk && cornerOk ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{renderer}] SELFTEST EXCEPTION: {ex}");
                return 2;
            }
            finally
            {
                backend?.Dispose();
            }
        }
    }
}
