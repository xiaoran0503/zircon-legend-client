using System;
using System.Windows.Forms;

namespace SlimDX.Windows
{
    /// <summary>
    /// 兼容原 SlimDX.Windows.RenderForm：承载渲染后端的 WinForms 窗体基类。
    /// </summary>
    public class RenderForm : Form
    {
        public RenderForm()
        {
        }

        public RenderForm(string title)
        {
            Text = title;
        }
    }

    /// <summary>
    /// 兼容原 SlimDX.Windows.MessagePump：在消息队列空闲时反复执行渲染/更新回调。
    /// </summary>
    public static class MessagePump
    {
        public static void Run(Form form, Action idleCallback)
        {
            void OnIdle(object sender, EventArgs e) => idleCallback?.Invoke();
            Application.Idle += OnIdle;
            try
            {
                Application.Run(form);
            }
            finally
            {
                Application.Idle -= OnIdle;
            }
        }
    }
}
