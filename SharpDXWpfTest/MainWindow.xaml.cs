
namespace image_match
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public class ValueConverter : IValueConverter
    {
        public object Convert(
                    object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((int)value % 2==0)
                return new SolidColorBrush(Color.FromArgb(255,240,240,240));
            return new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            
        }

        public object ConvertBack(
                    object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
    public partial class MainWindow : Window
    {
        public scene scene;
        string resize_grip_name;
        Point resize_start_point;
        Rect resize_start_rectangle;
        int min_width=500;
        int min_height=250;
        double original_width;
        Point point_mouse_down_title_bar;
        Rect initial_window_position;
        bool moving = false;
        System.Threading.Thread calc_thread;
        progress_window prog_window;
        IntPtr windowHandle;
        image_match.help.help_window help_win;
        bool cancelation_token = false;
        ini_parser MyIni;
        image_match.startup_help.startup_help start_help;
        bool show_tooltip = false;
        private static object _syncLock = new object();
        login login_window;
        public MainWindow()
        {
            //App.splashScreen.AddMessage("Loading components");
            InitializeComponent();
            this.Focusable = true;
            this.Unloaded += MainWindow_Unloaded;
            windowHandle = new System.Windows.Interop.WindowInteropHelper(this as Window).Handle;
            //windowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            r_ui.assign_scroll_bars(scroll_bar_scene,scroll_bar_pool);
            con_ui.assign_scroll_bars(scroll_bar_vertical_connections, scroll_bar_horizontal_connections);
            con_ui.assign_data_grid(connections_datagrid);
            help_win = new help.help_window();
            //help_win.Owner = this;
            MyIni = new ini_parser();
            scene = r_ui._scene;
            BindingOperations.EnableCollectionSynchronization(scene.connections, _syncLock);
            connections_datagrid.ItemsSource = scene.connections;
            this.StateChanged+=MainWindow_StateChanged;
            
            //App.splashScreen.LoadComplete();
            //MyIni.Write("show_startup_help", "1");
            /*System.Diagnostics.Process.Start("dxdiag", "/x dxv.xml");
            while (!System.IO.File.Exists("dxv.xml"))
                Thread.Sleep(1000);
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.Load("dxv.xml");
            System.Xml.XmlNode dxd = doc.SelectSingleNode("//DxDiag");
            System.Xml.XmlNode dxv = dxd.SelectSingleNode("//DirectXVersion");

            int a ;
            a = Convert.ToInt32(dxv.InnerText.Split(' ')[1]);
            int b = a;*/
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public RECT(System.Drawing.Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom) { }

            public int X
            {
                get { return Left; }
                set { Right -= (Left - value); Left = value; }
            }

            public int Y
            {
                get { return Top; }
                set { Bottom -= (Top - value); Top = value; }
            }

            public int Height
            {
                get { return Bottom - Top; }
                set { Bottom = value + Top; }
            }

            public int Width
            {
                get { return Right - Left; }
                set { Right = value + Left; }
            }

            public System.Drawing.Point Location
            {
                get { return new System.Drawing.Point(Left, Top); }
                set { X = value.X; Y = value.Y; }
            }

            public System.Drawing.Size Size
            {
                get { return new System.Drawing.Size(Width, Height); }
                set { Width = value.Width; Height = value.Height; }
            }

            public static implicit operator System.Drawing.Rectangle(RECT r)
            {
                return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
            }

            public static implicit operator RECT(System.Drawing.Rectangle r)
            {
                return new RECT(r);
            }

            public static bool operator ==(RECT r1, RECT r2)
            {
                return r1.Equals(r2);
            }

            public static bool operator !=(RECT r1, RECT r2)
            {
                return !r1.Equals(r2);
            }

            public bool Equals(RECT r)
            {
                return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
            }

            public override bool Equals(object obj)
            {
                if (obj is RECT)
                    return Equals((RECT)obj);
                else if (obj is System.Drawing.Rectangle)
                    return Equals(new RECT((System.Drawing.Rectangle)obj));
                return false;
            }

            public override int GetHashCode()
            {
                return ((System.Drawing.Rectangle)this).GetHashCode();
            }

            public override string ToString()
            {
                return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
            }
        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {

            switch (msg)
            {
                case 0x0083:
                    if (wParam != IntPtr.Zero)
                    {
                        handled = true;
                        var client = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));
                        client.Bottom -= 1;
                        Marshal.StructureToPtr(client, lParam, false);
                    }
                    break;
            }
            return IntPtr.Zero;
        }
        void MainWindow_StateChanged (Object sender,EventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Maximized)
                app_grid.Margin = new Thickness(8);
            else
                app_grid.Margin = new Thickness(0);
        }
        void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            help_win.Close();
            if (start_help!=null)
                start_help.Close();
            Application.Current.Shutdown();
            //throw new NotImplementedException();
        }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (tab_control.SelectedIndex == 3 && (e.SystemKey == Key.LeftAlt || e.Key == Key.LeftCtrl))
            {
                con_ui.render();
                base.OnKeyDown(e);
                return;
            }
            if (tab_control.SelectedIndex == 2 && (e.SystemKey == Key.LeftAlt || e.Key == Key.LeftCtrl))
            {
                m_ui.render();
                base.OnKeyDown(e);
                return;
            }
            if (tab_control.SelectedIndex == 0 && (e.SystemKey == Key.LeftAlt || e.Key == Key.LeftCtrl))
            {
                r_ui.render();
                base.OnKeyDown(e);
                return;
            }
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Alt)
                e.Handled = true;
            Point pt = Mouse.GetPosition(this);

            HitTestResult result = VisualTreeHelper.HitTest(this, pt);
            if (result == null)
                return;
            var element = result.VisualHit;
            if (tab_control.SelectedIndex == 0 && e.Key == Key.Delete)
            {
                (element as resources_ui).resources_ui_KeyDown(element as object, e);
                base.OnKeyDown(e);
                return;
            }
            if (tab_control.SelectedIndex == 3 && (e.SystemKey == Key.LeftAlt || e.Key == Key.LeftCtrl))
            {
                con_ui.render();
                base.OnKeyDown(e);
                return;
            }
            if (tab_control.SelectedIndex == 2 && (e.SystemKey == Key.LeftAlt || e.Key == Key.LeftCtrl))
            {
                m_ui.render();
                base.OnKeyDown(e);
                return;
            }
            if (tab_control.SelectedIndex == 0 && (e.SystemKey == Key.LeftAlt || e.Key == Key.LeftCtrl))
            {
                r_ui.render();
                base.OnKeyDown(e);
                return;
            }
         
            while (true)
            {
                if (element.GetType() == typeof(System.Windows.Controls.Primitives.ScrollBar) || element.GetType() == typeof(Button) || element.GetType() == typeof(resources_ui) || element.GetType() == typeof(matching_ui) )
                    break;
                element = VisualTreeHelper.GetParent(element);
                if (element == null)
                {
                    base.OnKeyDown(e);
                    return;
                }
            }
            if(element.GetType() == typeof(resources_ui) && tab_control.SelectedIndex==0)
            {
                if (e.Key==Key.H)
                    help_win.navigate_to((element as resources_ui).get_help_url());
                base.OnKeyDown(e);
                return;
            }
            
            string url = element.GetValue(FrameworkElement.TagProperty) as string;
            if (e.Key == Key.H)
                help_win.navigate_to(url);
            //System.Diagnostics.Debug.WriteLine(url);
            base.OnKeyDown(e);
        }
        private void close_window(object sender, RoutedEventArgs e)
        {
            //help_win.Close();
            //start_help.Close();
            this.Close();
        }
        private void minimize_window(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void maximize_window(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {

                //app_grid.Margin = new Thickness(0);
                original_width = this.Width;
                this.WindowState = WindowState.Maximized;
            }
            else
            {

                //app_grid.Margin = new Thickness(0);
                
                this.WindowState = WindowState.Normal;
            }
        }
        
        private void generate_button_click(object sender, RoutedEventArgs e)
        {
            if (scene.images.Count < 2)
                return;
            r_ui.lock_ui(true);
            click_once_solution();
            //tab_item_matching.IsEnabled = true;
            
            //scene.generate_false_matches(System.Convert.ToInt32(matches_count_tb.Text));
            
        }
        void click_once_solution()
        {
            scene = r_ui._scene;

            string [][] local_tasks=new string [scene.images.Count*3-1][];
            string [] global_tasks = new string [scene.images.Count*3-1];
            for (int i = 0; i < scene.images.Count; i++)
            {
                local_tasks[i] = new string[scene.images[i].Count];
                for (int j = 0; j < scene.images[i].Count; j++)
                {
                    local_tasks[i][j] = "Detecting : " + (i + 1).ToString() + ". cat - " + (j + 1).ToString() + ". image";
                }
                global_tasks[i] = "Detecting " + (i + 1).ToString() + ". category";
            }

            for (int i = 0; i < scene.images.Count; i++)
			{
                local_tasks[scene.images.Count+i] = new string[scene.images[i].Count];
                for (int j = 0; j < scene.images[i].Count; j++)
			    {
                    local_tasks[scene.images.Count +i][j] = "Describing : " + (i + 1).ToString() + ". cat - " + (j + 1).ToString() + ". image";
			    }
                global_tasks[scene.images.Count +i] = "Describing " + (i + 1).ToString() + ". category";
			}
            for (int i = 0; i < scene.images.Count-1; i++)
            {
                
                int count = 0;
                for (int j = i+1; j < scene.images.Count; j++)
                    count += scene.images[j].Count;
                count *= scene.images[i].Count;
                local_tasks[scene.images.Count*2 + i] = new string[count];
                int index = 0;
                for (int j = 0; j < scene.images[i].Count; j++)
			    {
                    
                    for (int u = i+1; u < scene.images.Count; u++)
			        {
			            for (int v = 0; v < scene.images[u].Count; v++)
			            {
                            local_tasks[scene.images.Count*2 + i][index] = "Matching : " + (i + 1).ToString() + ". cat - " + (j + 1).ToString() + ". image <-->" + (u + 1).ToString() + ". cat - " + (v + 1).ToString() + ". image";
                            index++;
			            }
			        }
			    }

                global_tasks[scene.images.Count*2 + i] = "Matching " + (i + 1).ToString() + ". category";
            }


            prog_window = new progress_window(local_tasks, global_tasks, new Task(() => { cancelation_token = true; scene.cancel_matching(); }));
            prog_window.Show();
            calc_thread = new Thread(new ThreadStart(click_once_solution_thread_start));
            calc_thread.Priority = ThreadPriority.Highest;
            calc_thread.Start();
        }
        //delegate void progress_callback();
        void cancel_work()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                prog_window.Close();
                r_ui.lock_ui(false);
            }), System.Windows.Threading.DispatcherPriority.Render);
            cancelation_token = false;
            calc_thread.Abort();
            return;
        }
        void click_once_solution_thread_start()
        {
            scene.clear_connections();
            System.Collections.Generic.List<int> cat_ind = new List<int>();
            System.Collections.Generic.List<int> im_ind = new List<int>();
            int img_count = 0;
            for (int i = 0; i < scene.images.Count; i++)
            {
                for (int j = 0; j < scene.images[i].Count; j++)
                {
                    cat_ind.Add(i);
                    im_ind.Add(j);
                    img_count++;
                }
            }
            System.Threading.Tasks.Parallel.For(0, img_count, (i,loopstate) =>
            {
                scene.images[cat_ind[i]][im_ind[i]].build_integral_image();
                scene.images[cat_ind[i]][im_ind[i]].detect_points();

                prog_window.progress();
                if (cancelation_token)
                    loopstate.Stop();
            });
            if (cancelation_token)
            {
                cancel_work();
                return;
            }

            System.Threading.Tasks.Parallel.For(0, img_count, (i, loopstate) =>
            {
                scene.images[cat_ind[i]][im_ind[i]].describe_points();
                scene.images[cat_ind[i]][im_ind[i]].get_render_data();
                prog_window.progress();
                //scene.images[i][j].generate_false_points();
                if (cancelation_token)
                    loopstate.Stop();
            });
            if (cancelation_token)
            {
                cancel_work();
                return;
            }
            scene.progress_callback prog_calb = prog_window.progress;
            scene.generate_matches(prog_calb);
            scene.create_connections();
            
            Dispatcher.Invoke(new Action(() =>
            {
                r_ui.lock_ui(false);
                scene = r_ui._scene;
                m_ui.update_scene(scene);
                connections_datagrid.Items.Refresh();
                prog_window.Close();
                cancelation_token = false;
                calc_thread.Abort();
                return;
            }), System.Windows.Threading.DispatcherPriority.Render);
            //prog_window.progress();
        }
       
        bool unsnap = false;
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount==2)
            {
                if (this.WindowState == WindowState.Normal)
                {

                    //app_grid.Margin = new Thickness(0);
                    original_width = this.Width;
                    initial_window_position.X = this.Left;
                    initial_window_position.Y = this.Top;
                    initial_window_position.Width = this.Width;
                    initial_window_position.Height = this.Height;
                    this.WindowState = WindowState.Maximized;
                }
                else
                {

                    //app_grid.Margin = new Thickness(0);
                
                    this.WindowState = WindowState.Normal;
                }
                return;
            }
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                
                
                if (this.WindowState==WindowState.Normal)
                {
                    initial_window_position.X = this.Left;
                    initial_window_position.Y = this.Top;
                    initial_window_position.Width = this.Width;
                    initial_window_position.Height = this.Height;
                    original_width = this.Width;
                }
                else
                {
                    //app_grid.Margin = new Thickness(0);
                    unsnap = true;
                    return;
                    
                }
                
                //Mouse.Capture(sender as UIElement);
                //moving = true;
                DragMove();
            }
        }
        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (unsnap && e.LeftButton == MouseButtonState.Pressed)
            {
                double help = Mouse.GetPosition(sender as UIElement).Y;
                this.Top = PointToScreen(Mouse.GetPosition(null)).Y - help;
                this.Left = PointToScreen(Mouse.GetPosition(null)).X - original_width / 2;
                this.WindowState = System.Windows.WindowState.Normal;

                
                DragMove();
                unsnap = false;
            }
            if (moving)
            {
                Point point_mouse_down_title_bar_current = PointToScreen(Mouse.GetPosition(null));
                if (!(this.WindowState == System.Windows.WindowState.Normal))
                {
                    double distance = Math.Sqrt(Math.Pow(point_mouse_down_title_bar_current.X - point_mouse_down_title_bar.X, (double)2) + Math.Pow(point_mouse_down_title_bar_current.Y - point_mouse_down_title_bar.Y, (double)2));
                    min_width++;
                    if (distance > 5)
                    {
                        //app_grid.Margin = new Thickness(0);
                        RestorePath.Visibility = Visibility.Collapsed;
                        MaximisePath.Visibility = Visibility.Visible;
                        double help = Mouse.GetPosition(sender as UIElement).Y;
                        this.Top = PointToScreen(Mouse.GetPosition(null)).Y - help;
                        this.Left = PointToScreen(Mouse.GetPosition(null)).X - original_width / 2;
                        this.WindowState = System.Windows.WindowState.Normal;
                        initial_window_position.X = this.Left;
                        initial_window_position.Y = this.Top;
                        initial_window_position.Width = this.Width;
                        initial_window_position.Height = this.Height;
                    }
                }
                /*else
                {
                    
                    if (point_mouse_down_title_bar_current.X < 5)
                    {
                        this.Top = 0;
                        this.Left = 0;
                        this.Width = (System.Windows.SystemParameters.PrimaryScreenWidth) / 2;
                        this.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
                    }
                    else if (point_mouse_down_title_bar_current.X > System.Windows.SystemParameters.PrimaryScreenWidth - 5)
                    {
                        this.Top = 0;
                        this.Left = (System.Windows.SystemParameters.PrimaryScreenWidth) / 2;
                        this.Width = (System.Windows.SystemParameters.PrimaryScreenWidth) / 2;
                        this.Height = System.Windows.SystemParameters.PrimaryScreenHeight+10;
                    }
                    else if (point_mouse_down_title_bar_current.Y < 5)
                    {
                        this.Top = 0;
                        this.Left = 0;
                        this.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
                        this.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
                    }
                    else
                    {
                        this.Left = initial_window_position.X + point_mouse_down_title_bar_current.X - point_mouse_down_title_bar.X;
                        this.Top = initial_window_position.Y + point_mouse_down_title_bar_current.Y - point_mouse_down_title_bar.Y;
                        if(this.Width!=initial_window_position.Width)
                        {
                            this.Width = initial_window_position.Width;
                            this.Height = initial_window_position.Height;
                        }
                    }
                        //DragMove();
                }*/
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
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource is TabControl)
            {
                if ((sender as TabControl).SelectedIndex == 0)
                {

                }
                else if ((sender as TabControl).SelectedIndex == 1)
                {

                }
                else if ((sender as TabControl).SelectedIndex == 2)
                {

                    scene = r_ui._scene;

                    m_ui.update_scene(scene);
                }
                else if ((sender as TabControl).SelectedIndex == 3)
                {

                    scene = r_ui._scene;

                    con_ui.update_scene(scene);
                    
                }
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            windowHandle = new System.Windows.Interop.WindowInteropHelper(this as Window).Handle;
            /*Dispatcher.Invoke(new Action(() =>
            {
                Keyboard.Focus(resources_tab_item as UIElement);
                resources_tab_item.Focus();
                Keyboard.Focus(r_ui as UIElement);
                r_ui.Focus();
            }),System.Windows.Threading.DispatcherPriority.ApplicationIdle);*/
            
            Style _style = null;
            if (Microsoft.Windows.Shell.SystemParameters2.Current.IsGlassEnabled == true)
            {
                _style = (Style)Resources["GadgetStyle"];
            }
            this.Style = _style;
            login_window = new login();
            login_window.ShowDialog();
            if (!(login_window.DialogResult.HasValue && login_window.DialogResult.Value))
                this.Close();
            if (Convert.ToInt32(MyIni.Read("show_startup_help")) == 1)
            {
                start_help = new startup_help.startup_help(help_win);
                start_help.Owner = this;
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    start_help.Show();
                }), System.Windows.Threading.DispatcherPriority.Background, null);
            }
        }
        private void generate_fake_button_click(object sender, RoutedEventArgs e)
        {
            //tab_item_matching.IsEnabled = true;
            scene = r_ui._scene;
            for (int i = 0; i < scene.images.Count; i++)
            {
                for (int j = 0; j < scene.images[i].Count; j++)
                {
                    //scene.images[i][j].generate_points();
                    scene.images[i][j].generate_false_points();
                }
            }
            //scene.generate_matches(System.Convert.ToInt32(matches_count_tb.Text));
            scene.generate_false_matches(System.Convert.ToInt32(matches_count_tb3.Text));
            scene.create_connections();
        }
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Normal)
            {
                
                RestorePath.Visibility = Visibility.Collapsed;
                MaximisePath.Visibility = Visibility.Visible;
                
            }
            else if (this.WindowState == System.Windows.WindowState.Maximized)
            {
                RestorePath.Visibility = Visibility.Visible;
                MaximisePath.Visibility = Visibility.Collapsed;
            }
        }
        private void show_help_button_Click(object sender, RoutedEventArgs e)
        {
            if (!help_win.IsVisible)
                help_win.Show();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            show_tooltip = (bool)(sender as CheckBox).IsChecked;
            r_ui.show_help_fnc(show_tooltip);
        }

        private void tooltip_opening(object sender, ToolTipEventArgs e)
        {
            if (!show_tooltip)
                e.Handled = true;
        }

        

        

        

        }
}
