using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using WinRT.Interop;

namespace CallingQuickstart
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private Microsoft.UI.Windowing.AppWindow m_AppWindow;

        public MainWindow()
        {
            this.InitializeComponent();

            if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
            {
                IntPtr hWnd = WindowNative.GetWindowHandle(this);
                WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
                m_AppWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(wndId);

                m_AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;

                var size = new Windows.Graphics.SizeInt32(1000, 710);
                m_AppWindow.ResizeClient(size);
            }
        }

        private void OnFrameLoaded(object sender, RoutedEventArgs e)
        {
            (sender as Frame).Navigate(typeof(MainPage), (sender as Frame).XamlRoot);
        }

        private void OnSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            MainFrame.Width = e.Size.Width;
            MainFrame.Height = e.Size.Height;
        }
    }
}
