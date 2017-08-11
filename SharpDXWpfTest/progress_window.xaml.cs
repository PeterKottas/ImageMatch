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
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class progress_window : Window
    {
        bool moving;
        String[][] local_task_name;
        String[] global_task_name;
        Point point_mouse_down_title_bar;
        Rect initial_window_position;
        Task cancelation_task;

        public progress_window(String[][] local_task_name, String[] global_task_name, Task cancel)
        {
            InitializeComponent();

            this.local_task_name = local_task_name;
            this.global_task_name = global_task_name;
            local_progress.Value = 0;
            global_progress.Value = 0;

            global_progress.Maximum = global_task_name.Length;
            local_progress.Maximum = local_task_name[0].Length-1;

            global_task.Text = global_task_name[0];
            local_task.Text = local_task_name[0][0];
            cancelation_task = cancel;
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
        public void progress()
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                int ind = (int)local_progress.Value;
                ind++;
                if (ind == local_progress.Maximum + 1)
                {
                    ind = 0;
                    int ind2 = (int) global_progress.Value;
                    ind2++;
                    if (ind2 == global_progress.Maximum)
                    {
                        this.Close();
                        return;
                    }
                    global_progress.Value = ind2;
                    local_progress.Maximum = local_task_name[(int)global_progress.Value].Length-1;
                }
                local_progress.Value = ind;
                global_task.Text = global_task_name[(int)global_progress.Value];
                local_task.Text = local_task_name[(int)global_progress.Value][(int)local_progress.Value];
            }), System.Windows.Threading.DispatcherPriority.Render, null);
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            cancelation_task.RunSynchronously();
            (sender as Button).IsEnabled = false;
        }
    }
}
