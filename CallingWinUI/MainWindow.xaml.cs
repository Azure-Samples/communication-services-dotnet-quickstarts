using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.ApplicationModel;
using WinRT.Interop;

namespace CallingQuickstart
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
            {
                IntPtr hWnd = WindowNative.GetWindowHandle(this);
                WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
                var m_AppWindow = AppWindow.GetFromWindowId(wndId);

                m_AppWindow.Title = $"{Package.Current.DisplayName} - Ready";
                m_AppWindow.TitleBar.BackgroundColor = Colors.SeaGreen;

                var size = new Windows.Graphics.SizeInt32(1000, 800);

                m_AppWindow.Resize(size);
            }
        }

        private void FrameLoaded(object sender, RoutedEventArgs e)
        {
            (sender as Frame).Navigate(typeof(MainPage), (sender as Frame).XamlRoot);
        }
    }
}
