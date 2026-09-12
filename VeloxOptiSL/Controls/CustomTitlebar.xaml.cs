#nullable enable
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VeloxOptiSL.Controls
{
    public partial class CustomTitleBar : System.Windows.Controls.UserControl
    {
        public CustomTitleBar()
        {
            InitializeComponent();
        }

        private Window? GetParentWindow()
        {
            return Window.GetWindow(this);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var window = GetParentWindow();
            if (window == null) return;

            if (e.ClickCount == 2)
            {
                window.WindowState = window.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            else
            {
                window.DragMove();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            var window = GetParentWindow();
            if (window != null) window.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            var window = GetParentWindow();
            if (window != null)
            {
                window.WindowState = window.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            var window = GetParentWindow();
            window?.Close();
        }
    }
}