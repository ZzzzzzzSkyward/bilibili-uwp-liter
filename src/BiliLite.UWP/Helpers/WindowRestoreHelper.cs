using System;
using System.Runtime.InteropServices;
using Windows.UI.Core;

namespace BiliLite.Helpers
{
    /// <summary>
    /// 窗口还原：挂起后回到前台时 Window.Current.Activate() 对最小化窗口是空操作，
    /// 只能拿 CoreWindow 的 HWND 找到顶层框架窗口，直接走 Win32 把窗口叫回来。
    /// 被系统最小化的是包装应用的框架窗口（ApplicationFrameWindow），不是 CoreWindow 本身；
    /// 非桌面设备（拿不到 HWND）时所有方法都安静地返回，不抛异常。
    /// </summary>
    internal static class WindowRestoreHelper
    {
        /// <summary>
        /// CoreWindow 的未托管接口，只用来读 HWND 和标记消息已处理。
        /// 只在桌面版 Windows 上可用。
        /// </summary>
        [ComImport]
        [Guid("45D64A29-A63E-4CB6-B498-5781D298CB4F")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface ICoreWindowInterop
        {
            IntPtr WindowHandle { get; }
            void MessageHandled(bool value);
        }

        private const uint GA_ROOT = 2;

        /// <summary>取当前线程 CoreWindow 的窗口句柄；取不到（含非桌面设备）返回 false，句柄为 0。</summary>
        internal static bool TryGetHandle(out IntPtr handle)
        {
            handle = IntPtr.Zero;
            try
            {
                var coreWindow = CoreWindow.GetForCurrentThread();
                if (coreWindow == null) return false;
                // CoreWindow 是 sealed 且不声明这个接口，只能经 object 转成运行时 QueryInterface
                var interop = (ICoreWindowInterop)(object)coreWindow;
                handle = interop.WindowHandle;
                return handle != IntPtr.Zero;
            }
            catch (Exception)
            {
                // 非桌面设备上没有这个接口，取不到就当作没有句柄
                handle = IntPtr.Zero;
                return false;
            }
        }

        /// <summary>
        /// 取顶层框架窗口：CoreWindow 是子窗口，最小化/还原都作用在它的顶层祖先上。
        /// 解析失败时退回传进来的句柄；没有句柄返回 0。
        /// </summary>
        internal static IntPtr GetRootHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return IntPtr.Zero;
            try
            {
                var root = GetAncestor(handle, GA_ROOT);
                return root == IntPtr.Zero ? handle : root;
            }
            catch (Exception)
            {
                return handle;
            }
        }

        /// <summary>顶层窗口是否已最小化；没有句柄时返回 false。</summary>
        internal static bool IsMinimized(IntPtr handle)
        {
            var root = GetRootHandle(handle);
            return root != IntPtr.Zero && IsIconic(root);
        }

        /// <summary>顶层窗口是否已最大化；没有句柄时返回 false。</summary>
        internal static bool IsMaximized(IntPtr handle)
        {
            var root = GetRootHandle(handle);
            return root != IntPtr.Zero && IsZoomed(root);
        }

        /// <summary>
        /// 窗口被还原时是否应当最大化。
        /// 最小化的窗口 WS_MINIMIZE 置位、WS_MAXIMIZE 已被清掉，IsZoomed 在挂起时恒为 false，
        /// 只能读 WINDOWPLACEMENT：本仓库 BR-61 抓到的框架窗口就是 showCmd=2、flags=2，
        /// 即 WPF_RESTORETOMAXIMIZED「还原时回到最大化」。没有句柄或取不到时返回 false。
        /// </summary>
        internal static bool RestoresToMaximized(IntPtr handle)
        {
            var root = GetRootHandle(handle);
            if (root == IntPtr.Zero) return false;
            try
            {
                var placement = new WINDOWPLACEMENT();
                placement.length = (uint)Marshal.SizeOf(typeof(WINDOWPLACEMENT));
                if (!GetWindowPlacement(root, ref placement)) return false;
                return (placement.flags & 0x0002 /*WPF_RESTORETOMAXIMIZED*/) != 0 || placement.showCmd == 3 /*SW_SHOWMAXIMIZED*/;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 把顶层窗口还原到 <paramref name="maximized"/> 指定的状态并带到前台。
        /// 返回 ShowWindow 是否成功；没有句柄或调用失败时返回 false。
        /// 调用方还要补一次 Window.Current.Activate()，ShowWindow 只处理 Win32 侧的状态。
        /// </summary>
        internal static bool Restore(IntPtr handle, bool maximized)
        {
            if (handle == IntPtr.Zero) return false;
            try
            {
                var root = GetRootHandle(handle);
                var shown = ShowWindow(root, maximized ? 3 /*SW_SHOWMAXIMIZED*/ : 9 /*SW_RESTORE*/);
                SetForegroundWindow(root);
                return shown;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPLACEMENT
        {
            public uint length;
            public uint flags;
            public uint showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }
    }
}
