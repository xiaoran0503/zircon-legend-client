using System;
using System.IO;
using System.Windows.Forms;
using SlimDX.Direct3D9;

namespace Client.Render
{
    /// <summary>
    /// 后端工厂：按 Legend.ini [Graphics] Renderer=DX12|Vulkan 选择后端，
    /// 初始化失败时按 Vulkan → DX12 硬件 → DX12 WARP 顺序回退。
    /// </summary>
    public static class BackendFactory
    {
        public const string DefaultRenderer = "DX12";

        /// <summary>读取 Legend.ini 中的 Renderer 配置（缺失/非法时回退 DX12）。</summary>
        public static string ReadRequestedRenderer()
        {
            try
            {
                string ini = Path.Combine(Application.StartupPath, "Legend.ini");
                if (!File.Exists(ini)) ini = "Legend.ini";
                if (!File.Exists(ini)) return DefaultRenderer;

                bool inGraphics = false;
                foreach (string raw in File.ReadAllLines(ini))
                {
                    string line = raw.Trim();
                    if (line.StartsWith("["))
                    {
                        inGraphics = string.Equals(line, "[Graphics]", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }
                    if (!inGraphics || line.Length == 0 || line[0] == ';' || line[0] == '#') continue;

                    int eq = line.IndexOf('=');
                    if (eq < 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    if (string.Equals(key, "Renderer", StringComparison.OrdinalIgnoreCase))
                        return line.Substring(eq + 1).Trim();
                }
            }
            catch
            {
                // 配置读取失败不阻断启动，走默认后端。
            }
            return DefaultRenderer;
        }

        public static IRenderBackend Create(string renderer, IntPtr hwnd, int width, int height, bool windowed, bool vsync)
        {
            renderer = string.IsNullOrWhiteSpace(renderer) ? DefaultRenderer : renderer.Trim();

            // 1) 请求的后端
            if (string.Equals(renderer, "Vulkan", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return new Vulkan.VulkanBackend();
                }
                catch (Exception ex) when (ex is not Direct3D9Exception)
                {
                    System.Diagnostics.Debug.WriteLine($"Vulkan 初始化失败，回退 DX12：{ex.Message}");
                }
            }

            // 1b) 显式 WARP 软件后端（自检/无头回退）
            if (string.Equals(renderer, "WARP", StringComparison.OrdinalIgnoreCase))
            {
                return new D3D12.D3D12Backend(useWarp: true);
            }

            // 2) DX12 硬件设备
            try
            {
                return new D3D12.D3D12Backend(useWarp: false);
            }
            catch (Exception ex) when (ex is not Direct3D9Exception)
            {
                System.Diagnostics.Debug.WriteLine($"DX12 硬件设备初始化失败，回退 WARP：{ex.Message}");
            }

            // 3) DX12 WARP 软件回退
            return new D3D12.D3D12Backend(useWarp: true);
        }
    }
}
