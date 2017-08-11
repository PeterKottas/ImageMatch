using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace image_match.startup_help
{
    /// <summary>
    /// Interaction logic for startup_help.xaml
    /// </summary>
    public partial class startup_help : Window
    {
        int current_index;
        int count_down=7;
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        string[] index_strings = { "introduction", "introduction2", "introduction3", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" };
        int ind_max = 15;
        help.help_window help_dialog;
        System.Windows.Threading.DispatcherTimer close_timer;
        public startup_help(help.help_window help_dialog)
        {
            this.help_dialog = help_dialog;
            InitializeComponent();
            close_timer = new System.Windows.Threading.DispatcherTimer();
            close_timer.Tick += close_timer_Tick;
            close_timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            close_button.IsEnabled = false;
            current_index = 0;
            frame.Navigate(new Uri("startup_help/pages/" + index_strings[current_index] + ".xaml", UriKind.Relative));
            close_button.Content = "Close (" + (count_down).ToString() + ")";
        }

        void close_timer_Tick(object sender, EventArgs e)
        {
            count_down--;
            if(count_down==0)
            {
                close_button.IsEnabled = true;
                close_button.Content = "Close";
                close_timer.Stop();
                var hWnd = new WindowInteropHelper(this);
                var sysMenu = GetSystemMenu(hWnd.Handle, false);
                EnableMenuItem(sysMenu, SC_CLOSE, MF_BYCOMMAND | MF_ENABLED);
                return;
            }
            close_button.Content = "Close (" + count_down.ToString() + ")";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (current_index == ind_max-1)
                next_button.IsEnabled = true;
            current_index--;
            frame.Navigate(new Uri("startup_help/pages/" + index_strings[current_index] + ".xaml", UriKind.Relative));
            if (current_index == 0)
                back_button.IsEnabled = false;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (current_index == 0)
                back_button.IsEnabled = true;
            current_index++;
            frame.Navigate(new Uri("startup_help/pages/" + index_strings[current_index] + ".xaml", UriKind.Relative));
            if (current_index == ind_max-1)
                next_button.IsEnabled = false;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var MyIni = new ini_parser();
            if ((bool)((CheckBox)sender).IsChecked)
                MyIni.Write("show_startup_help", "0");
            else
                MyIni.Write("show_startup_help", "1");
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        public void navigate(string url)
        {
            help_dialog.navigate_to(url);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        private const uint MF_BYCOMMAND = 0x00000000;
        private const uint MF_GRAYED = 0x00000001;
        private const uint MF_ENABLED = 0x00000000;
        private const uint SC_CLOSE = 0xF060;
        private const int WM_SHOWWINDOW = 0x00000018;


        private void close_button_Loaded(object sender, RoutedEventArgs e)
        {
            /*var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);*/
            var hWnd = new WindowInteropHelper(this);
            var sysMenu = GetSystemMenu(hWnd.Handle, false);
            EnableMenuItem(sysMenu, SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
            
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            SharpDXWpfTest.icon_helper.RemoveIcon(this);
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            close_timer.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
