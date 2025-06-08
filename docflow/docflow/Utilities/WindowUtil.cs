using Microsoft.UI.Windowing;
using Microsoft.UI;
using System;
using Windows.Graphics;
using WinRT.Interop;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;

namespace docflow.Utilities
{
    public static class WindowUtil
    {
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private static (int Width, int Height) GetPrimaryScreenSize()
        {
            int width = GetSystemMetrics(0);
            int height = GetSystemMetrics(1);
            return (width, height);
        }

        public static void MaximizeWindow(Window window)
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(window);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            var (width, height) = GetPrimaryScreenSize();
            appWindow.Resize(new SizeInt32(width, height));

            appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            OverlappedPresenter presenter = (OverlappedPresenter)appWindow.Presenter;
            presenter.Maximize();
        }
    }
}
