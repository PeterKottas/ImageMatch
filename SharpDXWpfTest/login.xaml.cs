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

namespace image_match
{
    /// <summary>
    /// Interaction logic for login.xaml
    /// </summary>
    public partial class login : Window
    {

        bool moving;
        Point point_mouse_down_title_bar;
        Rect initial_window_position;

        public login()
        {
            InitializeComponent();
        }
        private void minimize_window(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                point_mouse_down_title_bar = PointToScreen(Mouse.GetPosition(null));
                if (!(this.WindowState == WindowState.Maximized))
                {
                    initial_window_position.X = this.Left;
                    initial_window_position.Y = this.Top;
                    initial_window_position.Width = this.Width;
                    initial_window_position.Height = this.Height;
                }

                Mouse.Capture(sender as UIElement);
                moving = true;
            }
        }
        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (moving)
            {
                Point point_mouse_down_title_bar_current = PointToScreen(Mouse.GetPosition(null));
                this.Left = initial_window_position.X + point_mouse_down_title_bar_current.X - point_mouse_down_title_bar.X;
                this.Top = initial_window_position.Y + point_mouse_down_title_bar_current.Y - point_mouse_down_title_bar.Y;

            }
        }
        private void TitleBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            
            if (moving)
            {
                Mouse.Capture(null);
                moving = false;
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void search_tb_Copy_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (((PasswordBox)sender).Password.Length == 0)
                pass_help.Visibility = System.Windows.Visibility.Visible;
            else
                pass_help.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void Button_Click2(object sender, RoutedEventArgs e)
        {
            if(search_tb.Text=="peto"&&search_tb_Copy.Password=="ahoj")
            {
                DialogResult = true;
                status.Text = "Enter credentials";
                this.Close();
            }
            else
            {
                status.Text = "Wrong credentials";
            }

            
        }
    }
}
