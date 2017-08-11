using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;

namespace image_match
{
    
    class connections_ui: System.Windows.Controls.Image 
    {
        private SharpDX.Direct3D11.Device d3dDevice;
        private SharpDX.Direct2D1.Factory d2dFactory;
        private SharpDX.WIC.ImagingFactory wicFactory;
        private SharpDX.Direct2D1.RenderTarget d2d_render_target;
        private Texture2D RenderTarget;
        private DX10ImageSource D3DSurface;
        System.Windows.Controls.DataGrid data_grid;

        int targetWidth;
        int targetHeight;
        SharpDX.Direct2D1.SolidColorBrush white_brush;
        SharpDX.Direct2D1.SolidColorBrush black_brush;
        SharpDX.Direct2D1.SolidColorBrush gray_brush;
        SharpDX.Direct2D1.SolidColorBrush odd_brush;
        SharpDX.Direct2D1.SolidColorBrush even_brush;
        SharpDX.Direct2D1.SolidColorBrush selection_brush_convex;
        SharpDX.Direct2D1.SolidColorBrush selection_brush_concave;
        SharpDX.Direct2D1.SolidColorBrush selection_border_brush;
        SharpDX.Direct2D1.SolidColorBrush selection_selected_brush;
        StrokeStyle strokeStyle;

        System.Timers.Timer timer_panning;
        System.Timers.Timer timer_move_up;
        System.Timers.Timer timer_move_down;
        System.Timers.Timer timer_move_left;
        System.Timers.Timer timer_move_right;

        float selection_start_x;
        float selection_start_y;
        float selection_end_x;
        float selection_end_y;

        bool scroll_down = false;
        bool scroll_up = false;
        bool scroll_left = false;
        bool scroll_right = false;

        float scroll_speed_pan = 5;
        float scroll_speed = 25;
        float min_zoom = 0.1f;
        float max_zoom = 10;
        float min_zoom_image = 0.1f;
        float max_zoom_image = 10;



        bool selecting = false;

        Color4 background;
        RectangleF draw_rectangle;
        RectangleF draw_rectangle_2;
        RectangleF draw_rectangle_3;
        RectangleF source_rectangle;
        SharpDX.Direct2D1.Bitmap point_image;

        Query queryForCompletion;
        System.Threading.SpinWait spin = new System.Threading.SpinWait();
        Action EmptyDelegate = new Action(() => { });

        float render_time_render;
        float fps;
        float start_time;
        Random rand;
        Stopwatch timer;
        public scene _scene;

        double scroll_vertical;
        double scroll_horizontal;

        float image_width;
        float image_height;
        float image_margin_vertical=15;
        float image_margin_horizontal=15;
        float left_margin=18;
        float bottom_margin = 18;
        float maximum_width;
        float maximum_height;
        float scroll_bar_vertical_maximum;
        float scroll_bar_horizontal_maximum;

        System.Windows.Controls.Primitives.ScrollBar scroll_bar_vertical;
        System.Windows.Controls.Primitives.ScrollBar scroll_bar_horizontal;

        int connections_count;
        int[] connections_lengths;
        int[][] connections_cat;
        int[][] connections_im;
        int[][] connections_pt;
        RectangleF[][] draw_rectangles;
        bool[][] selected_rectangles;
        bool[][] selected_rectangles_preview;
        float[][] rectangles_zoom;

        bool draw;
        bool panning=false;
        System.Windows.Point initial_point;
        System.Windows.Point point;
        double initial_scroll_vertical;
        double initial_scroll_horizontal;
        float initial_image_zoom_area = 50;
        float ui_zoom=1f;
        float scroll_margin = 25;
        bool zooming_disabled = false;
        bool selecting_in_data_grid_disabled = false;
        public static bool IsInDesignMode
        {
            get
            {
                DependencyProperty prop = DesignerProperties.IsInDesignModeProperty;
                bool isDesignMode = (bool)DependencyPropertyDescriptor.FromProperty(prop, typeof(FrameworkElement)).Metadata.DefaultValue;
                return isDesignMode;
            }
        }
        public connections_ui()
        {
            if (IsInDesignMode)
                return;
            timer = new Stopwatch();
            timer.Start();
            rand = new Random();
            background = new Color4((float)(203.0 / 255.0), (float)(203.0 / 255.0), (float)(203.0 / 255.0), 1f);
            this.IsVisibleChanged += connections_ui_IsVisibleChanged;
            this.D3DSurface = new DX10ImageSource();
            this.Source = D3DSurface;
            image_width = 100;
            image_height = 100;
            this.MouseDown += connections_ui_MouseDown;
            this.MouseMove += connections_ui_MouseMove;
            this.MouseUp += connections_ui_MouseUp;
            this.MouseWheel += connections_ui_MouseWheel;

            int spn = 35;
            timer_move_up = new System.Timers.Timer(spn);
            timer_move_down = new System.Timers.Timer(spn);
            timer_move_left = new System.Timers.Timer(spn);
            timer_move_right = new System.Timers.Timer(spn);
            timer_panning = new System.Timers.Timer(spn);

            timer_move_up.Elapsed += timer_move_up_Elapsed;
            timer_move_down.Elapsed += timer_move_down_Elapsed;
            timer_move_left.Elapsed += timer_move_left_Elapsed;
            timer_move_right.Elapsed += timer_move_right_Elapsed;
            timer_panning.Elapsed += timer_panning_Elapsed;
            //this.PreviewKeyDown += connections_ui_PreviewKeyDown;
        }

        void timer_panning_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            
            if (initial_point.X < point.X)
            {
                scroll_horizontal += (point.X - initial_point.X)/scroll_speed_pan;
            }
            else
            {
                scroll_horizontal += (point.X - initial_point.X) / scroll_speed_pan;
            }
            if (initial_point.Y < point.Y)
            {
                scroll_vertical += (point.Y - initial_point.Y) / scroll_speed_pan;
            }
            else
            {
                scroll_vertical += (point.Y - initial_point.Y) / scroll_speed_pan;
            }
            if (scroll_horizontal < 0)
                scroll_horizontal = 0;
            if (scroll_vertical < 0)
                scroll_vertical = 0;
            if (scroll_horizontal > scroll_bar_horizontal_maximum)
                scroll_horizontal = scroll_bar_horizontal_maximum;
            if (scroll_vertical > scroll_bar_vertical_maximum)
                scroll_vertical = scroll_bar_vertical_maximum;
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { scroll_bar_horizontal.Value = scroll_horizontal; scroll_bar_vertical.Value = scroll_vertical; render(); }));
            //render();
        }
        void timer_move_right_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (scroll_horizontal == scroll_bar_horizontal_maximum)
                return;
            if (scroll_horizontal + scroll_speed > scroll_bar_horizontal_maximum)
            {
                scroll_horizontal = (float)scroll_bar_horizontal_maximum;
            }
            else
            {
                scroll_horizontal += (float)scroll_speed;
                selection_start_x -= (float)scroll_speed;
            }
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { scroll_bar_horizontal.Value = scroll_horizontal; render(); }));
            //generate_screen_space();
        }
        void timer_move_left_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (scroll_horizontal == 0)
                return;
            if (scroll_horizontal - scroll_speed < 0)
            {
                scroll_horizontal = (float)0;
            }
            else
            {
                scroll_horizontal -= (float)scroll_speed;
                selection_start_x += (float)scroll_speed;
            }
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { scroll_bar_horizontal.Value = scroll_horizontal; render(); }));
        }
        void timer_move_down_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (scroll_vertical == scroll_bar_vertical_maximum)
                return;
            if (scroll_vertical + scroll_speed > scroll_bar_vertical_maximum)
            {
                scroll_vertical = (float)scroll_bar_vertical_maximum;
            }
            else
            {
                scroll_vertical += (float)scroll_speed;
                selection_start_y -= (float)scroll_speed;
            }
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { scroll_bar_vertical.Value = scroll_vertical; render(); }));
        }
        void timer_move_up_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (scroll_vertical == 0)
                return;
            if (scroll_vertical - scroll_speed < 0)
            {
                scroll_vertical = (float)0;
            }
            else
            {
                scroll_vertical -= (float)scroll_speed;
                selection_start_y += (float)scroll_speed;
            }
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { scroll_bar_vertical.Value = scroll_vertical; render(); }));
        }
        void pointer_inside_image_check(out int cat_index, out int im_index)
        {
            cat_index = -1;
            im_index = -1;

            System.Windows.Point pos = System.Windows.Input.Mouse.GetPosition(this);

            int cat_count = draw_rectangles.Length;

            
            for (int i = 0; i < cat_count; i++)
            {
                int img_count = draw_rectangles[i].Length;
                for (int j = 0; j < img_count; j++)
                {
                    if (pos.X > -scroll_horizontal + draw_rectangles[i][j].Left && pos.X < -scroll_horizontal + draw_rectangles[i][j].Right && pos.Y > -scroll_vertical + draw_rectangles[i][j].Top && pos.Y < -scroll_vertical + draw_rectangles[i][j].Bottom)
                    {
                        cat_index = i;
                        im_index = j;
                        return;
                    }
                }

            }
            
        }
        int count_vertical_margins()
        {
            int margin_count;
            

            System.Windows.Point pos = System.Windows.Input.Mouse.GetPosition(this);

            int cat_count = draw_rectangles.Length;


            for (int i = 0; i < cat_count; i++)
            {
                if (pos.Y < -scroll_vertical + draw_rectangles[i][0].Bottom)
                {
                    return (i + 1);
                }
            }
            return cat_count + 1;
        }
        int count_horizontal_margins()
        {
            int margin_count;


            System.Windows.Point pos = System.Windows.Input.Mouse.GetPosition(this);

            int cat_count = draw_rectangles.Length;


            for (int i = 0; i < 99999999; i++)
            {
                if (pos.X < -scroll_horizontal + left_margin + (i + 1) * image_margin_horizontal + (i+1) * image_width * ui_zoom)
                {
                    return (i+1);
                }
            }
            return cat_count + 1;
        }
        void connections_ui_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (!draw)
                return;
            bool shift = System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift);
            bool ctrl = System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl);
            if (shift || ctrl)
            {
                int cat, im;
                pointer_inside_image_check(out cat, out im);
               
                if(shift)
                {
                    float factor;
                    if (e.Delta > 0)
                        factor = 1.2f;
                    else
                        factor = (float)(1 / 1.2);
                    if ((ui_zoom * factor < min_zoom && factor < 1) || (ui_zoom * factor > max_zoom && factor > 1))
                        return;
                    double disp_help;
                    point = System.Windows.Input.Mouse.GetPosition(this as IInputElement);
                    int vertical_margin_count,horizontal_margin_count;
                    vertical_margin_count=count_vertical_margins();
                    horizontal_margin_count = count_horizontal_margins();
                    disp_help = scroll_horizontal - horizontal_margin_count*image_margin_horizontal-left_margin;
                    disp_help += (point.X );
                    disp_help *= factor;
                    scroll_horizontal = disp_help - (point.X) + horizontal_margin_count * image_margin_horizontal+left_margin;

                    disp_help = scroll_vertical - vertical_margin_count * image_margin_vertical;
                    disp_help += point.Y;
                    disp_help *= factor;
                    scroll_vertical = disp_help - point.Y + vertical_margin_count * image_margin_vertical;

                    //disp_help *= factor;

                    ui_zoom *= factor;

                    generate_screen_space();
                    update_scroll_bars();
                    if (scroll_horizontal < 0)
                        scroll_horizontal = 0;
                    if (scroll_vertical < 0)
                        scroll_vertical = 0;
                    if (scroll_horizontal > scroll_bar_horizontal_maximum)
                        scroll_horizontal = scroll_bar_horizontal_maximum;
                    if (scroll_vertical > scroll_bar_vertical_maximum)
                        scroll_vertical = scroll_bar_vertical_maximum;
                    scroll_bar_horizontal.Value = scroll_horizontal;
                    scroll_bar_vertical.Value = scroll_vertical;
                }
                else if (cat!=-1)
                {
                    float factor;
                    if (e.Delta > 0)
                        factor = 1.2f;
                    else
                        factor = (float)(1 / 1.2);
                    if ((rectangles_zoom[cat][im] * factor < min_zoom && factor < 1) || (rectangles_zoom[cat][im] * factor > max_zoom && factor > 1))
                        return;
                    rectangles_zoom[cat][im] *= factor;
                }
                render();
                return;

            }
            float move = 20;
            if (e.Delta > 0)
            {
                move *= -1;
            }
            scroll_vertical += move;
            if (scroll_vertical < 0)
                scroll_vertical = 0;
            if (scroll_vertical > scroll_bar_vertical_maximum)
                scroll_vertical = scroll_bar_vertical_maximum;
            scroll_bar_vertical.Value = scroll_vertical;
            render();
        }
        void connections_ui_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!draw)
                return;
            if(panning)
            {
                System.Windows.Input.Mouse.Capture(null);
                panning = false;
                timer_panning.Stop();
                this.Cursor = System.Windows.Input.Cursors.Arrow;
            }
            if(selecting)
            {
                selecting_in_data_grid_disabled = true;
                System.Windows.Input.Mouse.Capture(null);
                selecting = false;
                if (scroll_up)
                    timer_move_up.Stop();
                if (scroll_down)
                    timer_move_down.Stop();
                if (scroll_left)
                    timer_move_left.Stop();
                if (scroll_right)
                    timer_move_right.Stop();
                if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) && e.ClickCount == 1)
                    add_preview_selection();
                else
                    if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftAlt)  && e.ClickCount == 1)
                        remove_preview_selection();
                    else
                        if (e.ClickCount == 1)
                            select_preview_selection();
                select_in_data_grid();
                selecting_in_data_grid_disabled = false;
            }
            render();
        }
        public void assign_data_grid(System.Windows.Controls.DataGrid data_grid)
        {
            this.data_grid = data_grid;
            this.data_grid.PreviewKeyUp += data_grid_PreviewKeyUp;
            this.data_grid.PreviewKeyDown += data_grid_PreviewKeyDown;
            this.data_grid.SelectionChanged += data_grid_SelectionChanged;
            this.data_grid.MouseDoubleClick += data_grid_MouseDoubleClick;
        }

        void data_grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (data_grid.SelectedItems.Count > 0 && !selecting_in_data_grid_disabled)
            {
                int length = data_grid.SelectedItems.Count;
                connection first_con = (data_grid.SelectedItems[length - 1] as connection);
                int cat_index = first_con.connection_id;
                int length2 = _scene.connections.Count;
                int im_index = 0;
                //if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftAlt))
                {
                    for (int i = 0; i < length2; i++)
                    {
                        if (_scene.connections[i].connection_id == cat_index)
                        {
                            if (_scene.connections[i].cat_id == first_con.cat_id && _scene.connections[i].image_id == first_con.image_id && _scene.connections[i].point_id == first_con.point_id)
                            {
                                zoom_to_item(cat_index - 1, im_index);
                                break;
                            }
                            im_index++;
                        }
                    }
                }
            }
        }
        void data_grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                zooming_disabled = true;
        }
        void zoom_to_item(int cat_index, int im_index)
        {
            if (zooming_disabled)
                return;
            scroll_vertical = (cat_index ) * image_margin_vertical + cat_index * image_height * ui_zoom;

            scroll_horizontal =  (im_index ) * image_margin_horizontal + im_index * image_width * ui_zoom;
                    

            if (scroll_horizontal < 0)
                scroll_horizontal = 0;
            if (scroll_vertical < 0)
                scroll_vertical = 0;
            if (scroll_horizontal > scroll_bar_horizontal_maximum)
                scroll_horizontal = scroll_bar_horizontal_maximum;
            if (scroll_vertical > scroll_bar_vertical_maximum)
                scroll_vertical = scroll_bar_vertical_maximum;
            
            int cat_count = draw_rectangles.Length;
            /*for (int i = 0; i < cat_count; i++)
            {
                int cat_im_count = draw_rectangles[i].Length;
                for (int j = 0; j < cat_im_count; j++)
                {
                    selected_rectangles[i][j] = false;
                }
            }*/
            //selected_rectangles[cat_index][im_index] = true;

            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { scroll_bar_horizontal.Value = scroll_horizontal; scroll_bar_vertical.Value = scroll_vertical; render(); }));
        }
        void data_grid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if(data_grid.SelectedItems.Count>0&&!selecting_in_data_grid_disabled)
            {
                int length = data_grid.SelectedItems.Count;
                connection first_con = (data_grid.SelectedItems[length-1] as connection);
                int cat_index = first_con.connection_id;
                int length2 = _scene.connections.Count;
                int im_index=0;
                /*if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftAlt))
                {
                    for (int i = 0; i < length2; i++)
                    {
                        if (_scene.connections[i].connection_id == cat_index)
                        {
                            if (_scene.connections[i].cat_id == first_con.cat_id && _scene.connections[i].image_id == first_con.image_id && _scene.connections[i].point_id == first_con.point_id)
                            {
                                zoom_to_item(cat_index - 1, im_index);
                                break;
                            }
                            im_index++;
                        }
                    }
                }*/
                int cat_count = draw_rectangles.Length;
                for (int i = 0; i < cat_count; i++)
                {
                    int cat_im_count = draw_rectangles[i].Length;
                    for (int j = 0; j < cat_im_count; j++)
                    {
                        selected_rectangles[i][j] = false;
                    }
                }
                foreach (var item in data_grid.SelectedItems)
                {
                    int index = data_grid.Items.IndexOf(item);
                    if((item as connection).connection_id==1)
                    {
                        selected_rectangles[0][index] = true;
                    }
                    else
                    {
                        int index_nd=index-1;
                        connection con_nd=_scene.connections[index_nd];
                        int local_count = 0;
                        while(con_nd.connection_id==(item as connection).connection_id)
                        {
                            local_count++;
                            index_nd--;
                            con_nd = _scene.connections[index_nd];
                        }
                        selected_rectangles[(item as connection).connection_id-1][local_count] = true;
                    }
                }
                render();
            }
        }
        void data_grid_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() => { _scene.connections_rows_deleted(); data_grid.Items.Refresh(); update_scene(_scene); zooming_disabled = false; assign_row_colors(); }));
            }
        }
        public static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                    return (T)child;
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }
        public static System.Windows.Controls.DataGridCell GetCell(System.Windows.Controls.DataGrid dataGrid, System.Windows.Controls.DataGridRow rowContainer, int column)
        {
            if (rowContainer != null)
            {
                System.Windows.Controls.Primitives.DataGridCellsPresenter presenter = FindVisualChild<System.Windows.Controls.Primitives.DataGridCellsPresenter>(rowContainer);
                if (presenter == null)
                {
                    /* if the row has been virtualized away, call its ApplyTemplate() method 
                     * to build its visual tree in order for the DataGridCellsPresenter
                     * and the DataGridCells to be created */
                    rowContainer.ApplyTemplate();
                    presenter = FindVisualChild<System.Windows.Controls.Primitives.DataGridCellsPresenter>(rowContainer);
                }
                if (presenter != null)
                {
                    System.Windows.Controls.DataGridCell cell = presenter.ItemContainerGenerator.ContainerFromIndex(column) as System.Windows.Controls.DataGridCell;
                    if (cell == null)
                    {
                        /* bring the column into view
                         * in case it has been virtualized away */
                        dataGrid.ScrollIntoView(rowContainer, dataGrid.Columns[column]);
                        cell = presenter.ItemContainerGenerator.ContainerFromIndex(column) as System.Windows.Controls.DataGridCell;
                    }
                    return cell;
                }
            }
            return null;
        }
        void select_in_data_grid()
        {
            zooming_disabled = true;
            data_grid.SelectedItems.Clear();
            int index = 0;
            for (int i = 0; i < connections_count; i++)
            {
                int length = connections_lengths[i];
                for (int j = 0; j < length; j++)
                {
                    if(selected_rectangles[i][j])
                    {
                        object item = data_grid.Items[index]; //=Product X
                        data_grid.SelectedItems.Add(item);

                        System.Windows.Controls.DataGridRow row = data_grid.ItemContainerGenerator.ContainerFromIndex(index) as System.Windows.Controls.DataGridRow;
                        if (row == null)
                        {
                            data_grid.ScrollIntoView(item);
                            row = data_grid.ItemContainerGenerator.ContainerFromIndex(index) as System.Windows.Controls.DataGridRow;
                        }
                        if (row != null)
                        {
                            System.Windows.Controls.DataGridCell cell = GetCell(data_grid, row, 0);
                            if (cell != null)
                                cell.Focus();
                        }
                        //data_grid.SelectedItems.Add(_scene.connections[index] as object);
                    }
                    index++;
                }
            }
            zooming_disabled = false;
        }
        void connections_ui_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!draw)
                return;
            if(panning)
            {
                /*point = System.Windows.Input.Mouse.GetPosition(sender as IInputElement);
                scroll_horizontal = initial_scroll_horizontal + (initial_point.X - point.X);
                scroll_vertical = initial_scroll_vertical + (initial_point.Y - point.Y);
                if (scroll_horizontal < 0)
                    scroll_horizontal = 0;
                if (scroll_vertical < 0)
                    scroll_vertical = 0;
                if (scroll_horizontal > scroll_bar_horizontal_maximum)
                    scroll_horizontal = scroll_bar_horizontal_maximum;
                if (scroll_vertical > scroll_bar_vertical_maximum)
                    scroll_vertical = scroll_bar_vertical_maximum;
                scroll_bar_horizontal.Value = scroll_horizontal;
                scroll_bar_vertical.Value = scroll_vertical;
                render();*/
                point = System.Windows.Input.Mouse.GetPosition(this);
                return;
            }
            if(selecting)
            {
                System.Windows.Point pos = System.Windows.Input.Mouse.GetPosition(sender as IInputElement);

                if (pos.Y < scroll_margin)
                {
                    if (!scroll_up)
                    {
                        timer_move_up.Start();
                        scroll_up = true;
                    }
                }
                else
                {
                    if (scroll_up)
                    {
                        timer_move_up.Stop();
                        scroll_up = false;
                    }
                }

                if (pos.Y > targetHeight-scroll_margin)
                {
                    if (!scroll_down)
                    {
                        timer_move_down.Start();
                        scroll_down = true;
                    }
                }
                else
                {
                    if (scroll_down)
                    {
                        timer_move_down.Stop();
                        scroll_down = false;
                    }
                }

                if (pos.X < scroll_margin)
                {
                    if (!scroll_left)
                    {
                        timer_move_left.Start();
                        scroll_left = true;
                    }
                }
                else
                {
                    if (scroll_left)
                    {
                        timer_move_left.Stop();
                        scroll_left = false;
                    }
                }

                if (pos.X > targetWidth - scroll_margin)
                {
                    if (!scroll_right)
                    {
                        timer_move_right.Start();
                        scroll_right = true;
                    }
                }
                else
                {
                    if (scroll_right)
                    {
                        timer_move_right.Stop();
                        scroll_right = false;
                    }
                }


                selection_end_x = (float)pos.X;
                selection_end_y = (float)pos.Y;
                preview_selection();
                render();
                return;
            }
            if (preview_selection())
                render();
        }
        void connections_ui_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!draw)
                return;
            if (e.ChangedButton == System.Windows.Input.MouseButton.Middle)
            {
                this.Cursor = System.Windows.Input.Cursors.ScrollAll;
                panning = true;
                initial_point = System.Windows.Input.Mouse.GetPosition(sender as IInputElement);
                point = initial_point;
                /*initial_scroll_horizontal = scroll_horizontal;
                initial_scroll_vertical = scroll_vertical;*/
                timer_panning.Start();
                System.Windows.Input.Mouse.Capture(sender as IInputElement);
                return;
            }
            if(e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                System.Windows.Point pos = System.Windows.Input.Mouse.GetPosition(sender as IInputElement);
                selecting = true;
                selection_start_x = (float)pos.X;
                selection_start_y = (float)pos.Y;
                selection_end_x = selection_start_x;
                selection_end_y = selection_start_y;
                System.Windows.Input.Mouse.Capture(this);
            }
        }
        void connections_ui_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible)
                render();
        }
        public void assign_scroll_bars(System.Windows.Controls.Primitives.ScrollBar scroll_bar_vertical, System.Windows.Controls.Primitives.ScrollBar scroll_bar_horizontal)
        {
            this.scroll_bar_vertical = scroll_bar_vertical;
            this.scroll_bar_horizontal = scroll_bar_horizontal;
            this.scroll_bar_horizontal.Scroll += scroll_bar_horizontal_Scroll; ;
            this.scroll_bar_vertical.Scroll += scroll_bar_vertical_Scroll; ;
            this.scroll_bar_vertical.Value = 0;
            this.scroll_bar_horizontal.Value = 0;
        }
        void scroll_bar_vertical_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            scroll_vertical = scroll_bar_vertical.Value;
            render();
        }
        void scroll_bar_horizontal_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            scroll_horizontal = scroll_bar_horizontal.Value;
            render();
        }
        void scroll_bar_horizontal_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
        }
        void scroll_bar_vertical_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
        }
        void generate_screen_space()
        {
            for (int i = 0; i < connections_count; i++)
            {
                for (int j = 0; j < connections_lengths[i]; j++)
                {
                    draw_rectangles[i][j].Top = (i + 1) * image_margin_vertical + i * image_height * ui_zoom;
                    draw_rectangles[i][j].Bottom = draw_rectangles[i][j].Top + image_height * ui_zoom;
                    draw_rectangles[i][j].Left = left_margin + (j + 1) * image_margin_horizontal + j * image_width * ui_zoom;
                    draw_rectangles[i][j].Right = draw_rectangles[i][j].Left + image_width * ui_zoom;
                }
            }
        }
        void update_scroll_bars()
        {
            int max_width=0;
            for (int i = 0; i < connections_count; i++)
            {
                if (connections_lengths[i] > max_width)
                    max_width = connections_lengths[i];
            }
            maximum_width = (max_width + 1) * image_margin_horizontal + max_width * image_width*ui_zoom;
            maximum_height = (connections_count + 1) * image_margin_vertical + connections_count * image_height*ui_zoom;

            if (maximum_height < targetHeight)
            {
                left_margin = 0;
            }
            else
            {
                left_margin = 18;
            }
            if (maximum_width < targetWidth)
            {
                bottom_margin = 0;
            }
            else
            {
                bottom_margin = 18;
            }

            maximum_width = left_margin + (max_width + 1) * image_margin_horizontal + max_width * image_width*ui_zoom;
            maximum_height = bottom_margin + (connections_count + 1) * image_margin_vertical + connections_count * image_height*ui_zoom;

            if (maximum_height < targetHeight)
            {
                scroll_bar_vertical.Visibility = Visibility.Collapsed;
                scroll_bar_vertical.Maximum = 0;
                left_margin = 0;
            }
            else
            {
                scroll_bar_vertical.Maximum = maximum_height - targetHeight;
                scroll_bar_vertical.Visibility = Visibility.Visible;
                left_margin = 18;
            }
            if (maximum_width < targetWidth)
            {
                scroll_bar_horizontal.Visibility = Visibility.Collapsed;
                scroll_bar_horizontal.Maximum = 0;
                bottom_margin = 0;
            }
            else
            {
                scroll_bar_horizontal.Maximum = maximum_width - targetWidth;
                scroll_bar_horizontal.Visibility = Visibility.Visible;
                bottom_margin = 18;
            }

            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new Action(delegate
            {
                if(scroll_bar_vertical.IsVisible)
                {
                    if(!scroll_bar_horizontal.IsVisible)
                    {
                        scroll_bar_vertical.Margin = new Thickness(0.0);
                    }
                    else
                    {
                        scroll_bar_vertical.Margin = new Thickness(0.0, 0.0, 0.0,18.0);
                        scroll_bar_horizontal.Margin = new Thickness(18.0,0.0, 0.0, 0.0);
                    }
                }
                else
                {
                    scroll_bar_horizontal.Margin = new Thickness(0.0);
                }
            }), System.Windows.Threading.DispatcherPriority.Render, null);

            scroll_bar_vertical_maximum = (float)scroll_bar_vertical.Maximum;
            scroll_bar_horizontal_maximum = (float)scroll_bar_horizontal.Maximum;
            


            float p = 0.25f;
            scroll_bar_vertical.ViewportSize = (maximum_height) * p / (1 - p);
            scroll_bar_horizontal.ViewportSize = (maximum_width) * p / (1 - p);
        }
        public void update_scene(scene sol)
        {
            _scene = sol;


            draw = false;

            d3dDevice = sol.DX_RES.d3dDevice;
            wicFactory = sol.DX_RES.wicFactory;
            d2dFactory = sol.DX_RES.d2dFactory;

            point_image = sol.DX_RES.point_image;
            this.CreateAndBindTargets();

            if (black_brush != null)
            {
                black_brush.Dispose();
                white_brush.Dispose();
                selection_brush_convex.Dispose();
                selection_brush_concave.Dispose();
                selection_border_brush.Dispose();
                selection_selected_brush.Dispose();
                strokeStyle.Dispose();
                gray_brush.Dispose();
                odd_brush.Dispose();
                even_brush.Dispose();
                
                queryForCompletion.Dispose();
            }

            queryForCompletion = new Query(d3dDevice, new QueryDescription() { Type = QueryType.Event, Flags = QueryFlags.None });
            black_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(0f, 0f, 0f, 1f));
            odd_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(0f, 0f, 0f, 0.05f));
            even_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(1f, 1f, 1f, 0.05f));
            gray_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(0f, 0f, 0f, 0.2f));
            white_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(1f, 1f, 1f, 1f));
            selection_brush_convex = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(1f, 0f, 0f, 0.5f));
            selection_brush_concave = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(0f, 1f, 0f, 0.5f));
            selection_border_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(0f, 0f, 0f, 0.5f));
            selection_selected_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4((float)(17) / (float)(255), (float)(158) / (float)(255), (float)(218) / (float)(255), 1f));
            strokeStyle = new StrokeStyle(d2dFactory, new StrokeStyleProperties() { DashStyle = SharpDX.Direct2D1.DashStyle.Dash, LineJoin = LineJoin.Round });
            


            targetWidth = (int)this.ActualWidth;
            targetHeight = (int)this.ActualHeight;

            //Window_Loaded();
            draw = true;
            if (!_scene.connections_ready)
            {
                draw = false;
                return;
            }
            connections_count = _scene.connection_id_length;
            draw_rectangles = new RectangleF[connections_count][];
            connections_lengths = new int[connections_count];
            connections_cat = new int[connections_count][];
            connections_im = new int[connections_count][];
            connections_pt = new int[connections_count][];
            selected_rectangles = new bool[connections_count][];
            selected_rectangles_preview = new bool[connections_count][];
            rectangles_zoom = new float[connections_count][];

            int index = 1;
            int local_count=0;
            for (int i = 0; i < _scene.connections.Count; i++)
            {
                
                if (index == _scene.connections[i].connection_id)
                {
                    local_count++;
                }
                else
                {
                    connections_lengths[index-1] = local_count;
                    connections_cat[index - 1] = new int[local_count];
                    connections_im[index - 1] = new int[local_count];
                    connections_pt[index - 1] = new int[local_count];
                    draw_rectangles[index - 1] = new RectangleF[local_count];
                    selected_rectangles[index - 1] = new bool[local_count];
                    selected_rectangles_preview[index - 1] = new bool[local_count];
                    rectangles_zoom[index - 1] = new float[local_count];
                    index++;
                    local_count = 1;
                }
            }
            if (connections_count!=0)
            {
                connections_lengths[index - 1] = local_count;
                connections_cat[index - 1] = new int[local_count];
                connections_im[index - 1] = new int[local_count];
                connections_pt[index - 1] = new int[local_count];
                draw_rectangles[index - 1] = new RectangleF[local_count];
                selected_rectangles[index - 1] = new bool[local_count];
                selected_rectangles_preview[index - 1] = new bool[local_count];
                rectangles_zoom[index - 1] = new float[local_count];
            }

            index = 1;
            local_count = 0;
            for (int i = 0; i < _scene.connections.Count; i++)
            {
                
                if (index == _scene.connections[i].connection_id)
                {
                    connections_cat[index - 1][local_count] =  _scene.connections[i].cat_id;
                    connections_im[index - 1][local_count] =  _scene.connections[i].image_id;
                    connections_pt[index - 1][local_count] = _scene.connections[i].point_id;
                    selected_rectangles[index - 1][local_count] = false;
                    selected_rectangles_preview[index - 1][local_count] = false;
                    rectangles_zoom[index - 1][local_count] = 1f;
                    local_count++;
                }
                else
                {
                    index++;
                    local_count = 0;
                    connections_cat[index - 1][local_count] = _scene.connections[i].cat_id;
                    connections_im[index - 1][local_count] = _scene.connections[i].image_id;
                    connections_pt[index - 1][local_count] = _scene.connections[i].point_id;
                    selected_rectangles[index - 1][local_count] = false;
                    selected_rectangles_preview[index - 1][local_count] = false;
                    rectangles_zoom[index - 1][local_count] = 1f;
                    local_count++;
                }
            }

            for (int i = 0; i < connections_count; i++)
            {
                for (int j = 0; j < connections_lengths[i]; j++)
                {
                    draw_rectangles[i][j].Top = (i + 1) * image_margin_vertical + i * image_height*ui_zoom;
                    draw_rectangles[i][j].Bottom = draw_rectangles[i][j].Top + image_height * ui_zoom;
                    draw_rectangles[i][j].Left = left_margin + (j + 1) * image_margin_horizontal + j * image_width * ui_zoom;
                    draw_rectangles[i][j].Right = draw_rectangles[i][j].Left + image_width * ui_zoom;
                }
            }

            //assign_row_colors();
            update_scroll_bars();
            generate_screen_space();
            update_scroll_bars();
            render();
            //fit_scene();
        }
        void assign_row_colors()
        {
            /*System.Windows.Media.SolidColorBrush dark_gray = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0,0,0,25));
            System.Windows.Media.SolidColorBrush light_gray = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255,255,255,25));
            System.Windows.Controls.Primitives.IItemContainerGenerator generator = data_grid.ItemContainerGenerator;
            generator.StartAt(new System.Windows.Controls.Primitives.GeneratorPosition(0, 0), System.Windows.Controls.Primitives.GeneratorDirection.Forward);
            foreach (connection item in data_grid.ItemsSource)
            {
                var row =generator.GenerateNext();
                //System.Windows.Controls.DataGridRow row = data_grid.ItemContainerGenerator.ContainerFromItem(item) as System.Windows.Controls.DataGridRow;
                if (item.connection_id % 2 == 0)
                {
                    (row as System.Windows.Controls.DataGridRow).Background = dark_gray;
                }
                else
                {
                    (row as System.Windows.Controls.DataGridRow).Background = light_gray;
                }
            }*/
        }
        bool preview_selection()
        {
            if (!draw)
                return false;
            System.Windows.Point pos = System.Windows.Input.Mouse.GetPosition(this);
            bool changed = false;
            int cat_count = draw_rectangles.Length;
            if (selecting)
            {
                float left_selection, top_selection, right_selection, bottom_selection;
                left_selection = Math.Min(selection_start_x, selection_end_x);
                right_selection = Math.Max(selection_start_x, selection_end_x);
                top_selection = Math.Min(selection_start_y, selection_end_y);
                bottom_selection = Math.Max(selection_start_y, selection_end_y);
                for (int i = 0; i < cat_count; i++)
                {
                    int img_count = draw_rectangles[i].Length;
                    for (int j = 0; j < img_count; j++)
                    {
                        if (selection_start_x < selection_end_x)
                        {
                            if (!(left_selection > -scroll_horizontal + draw_rectangles[i][j].Right || right_selection < -scroll_horizontal + draw_rectangles[i][j].Left || top_selection > - scroll_vertical + draw_rectangles[i][j].Bottom || bottom_selection < - scroll_vertical+ draw_rectangles[i][j].Top))
                            {
                                if (selected_rectangles_preview[i][j])
                                    selected_rectangles_preview[i][j] = true;
                                else
                                {
                                    selected_rectangles_preview[i][j] = true;
                                    changed = true;
                                }
                            }
                            else
                            {
                                selected_rectangles_preview[i][j] = false;
                            }
                        }
                        else
                        {
                            if ((left_selection < -scroll_horizontal + draw_rectangles[i][j].Left && right_selection > -scroll_horizontal + draw_rectangles[i][j].Right && top_selection < scroll_vertical + draw_rectangles[i][j].Top && bottom_selection > scroll_vertical + draw_rectangles[i][j].Bottom))
                            {
                                if (selected_rectangles_preview[i][j])
                                    selected_rectangles_preview[i][j] = true;
                                else
                                {
                                    selected_rectangles_preview[i][j] = true;
                                    changed = true;
                                }
                            }
                            else
                            {
                                selected_rectangles_preview[i][j] = false;
                            }
                        }
                    }
                }
                
            }
            else
            {
                for (int i = 0; i < cat_count; i++)
                {
                    int img_count = draw_rectangles[i].Length;
                    for (int j = 0; j < img_count; j++)
                    {
                        if (pos.X > -scroll_horizontal + draw_rectangles[i][j].Left && pos.X < -scroll_horizontal + draw_rectangles[i][j].Right && pos.Y > -scroll_vertical + draw_rectangles[i][j].Top && pos.Y < -scroll_vertical + draw_rectangles[i][j].Bottom)
                        {
                            if (selected_rectangles_preview[i][j])
                            {
                                selected_rectangles_preview[i][j] = true;
                            }
                            else
                            {
                                selected_rectangles_preview[i][j] = true;
                                changed = true;
                            }
                        }
                        else
                        {
                            if (!selected_rectangles_preview[i][j])
                                selected_rectangles_preview[i][j] = false;
                            else
                            {
                                selected_rectangles_preview[i][j] = false;
                                changed = true;
                            }
                        }
                    }
                }
                
            }
            return changed;
        }
        void select_preview_selection()
        {
            if (!draw)
                return;
            int cat_count = draw_rectangles.Length;
            for (int i = 0; i < cat_count; i++)
            {
                int cat_im_count = draw_rectangles[i].Length;
                for (int j = 0; j < cat_im_count; j++)
                {
                    selected_rectangles[i][j] = selected_rectangles_preview[i][j];
                }
            }
            
        }
        void remove_preview_selection()
        {
            if (!draw)
                return;
            int cat_count = draw_rectangles.Length;
            for (int i = 0; i < cat_count; i++)
            {
                int cat_im_count = draw_rectangles[i].Length;
                for (int j = 0; j < cat_im_count; j++)
                {
                    if (selected_rectangles_preview[i][j])
                        selected_rectangles[i][j] = !selected_rectangles_preview[i][j];
                }
            }
            
        }
        void add_preview_selection()
        {
            if (!draw)
                return;
            int cat_count = draw_rectangles.Length;
            for (int i = 0; i < cat_count; i++)
            {
                int cat_im_count = draw_rectangles[i].Length;
                for (int j = 0; j < cat_im_count; j++)
                {
                    if (selected_rectangles_preview[i][j])
                    {
                        selected_rectangles[i][j] = selected_rectangles_preview[i][j];
                    }
                }
            }
            
        }
        float zoom_loc;
        float zoom_factor_loc_horizontal;
        float zoom_factor_loc_vertical;
        float orig_width;
        float orig_height;
        bool draw_ghost;
        public void render(/*object sender, System.EventArgs e*/)
        {

            D3DSurface.Lock();

            fps = (timer.ElapsedMilliseconds - start_time);//timer.ElapsedMilliseconds - start_time;//0.9f * render_time_render + 100f / (float)(timer.ElapsedMilliseconds - start_time);
            start_time = timer.ElapsedMilliseconds;
            d2d_render_target.BeginDraw();
            d2d_render_target.Clear(background);
            //d2d_render_target.PushAxisAlignedClip(new RectangleF(50f, 50f, 250f, 150f), AntialiasMode.Aliased);
            if (draw&&_scene.connections_ready)
            {
                float point_x, point_y;
                //System.Threading.Thread.Sleep(1000);  
                connections_count = draw_rectangles.Length;
                for (int i = 0; i < connections_count; i++)
                {
                    draw_rectangle.Top = -(float)scroll_vertical + draw_rectangles[i][0].Top;
                    draw_rectangle.Bottom = -(float)scroll_vertical + draw_rectangles[i][0].Bottom;
                    if (draw_rectangle.Top < targetHeight && draw_rectangle.Bottom > 0)
                    {
                        draw_rectangle.Top -= image_margin_vertical / 2f;
                        draw_rectangle.Left = 0;
                        draw_rectangle.Right = targetWidth;
                        draw_rectangle.Bottom += image_margin_vertical / 2f;
                        
                        if(i%2==0)
                        {
                            d2d_render_target.FillRectangle(draw_rectangle, even_brush);
                        }
                        else
                        {
                            d2d_render_target.FillRectangle(draw_rectangle, odd_brush);
                        }
                        for (int j = 0; j < connections_lengths[i]; j++)
                        {
                            draw_rectangle.Top = -(float)scroll_vertical + draw_rectangles[i][j].Top;
                            draw_rectangle.Bottom = -(float)scroll_vertical + draw_rectangles[i][j].Bottom;
                            draw_rectangle.Left = -(float)scroll_horizontal + draw_rectangles[i][j].Left;
                            draw_rectangle.Right = -(float)scroll_horizontal + draw_rectangles[i][j].Right;

                            draw_rectangle_2.Left = draw_rectangle.Left + image_width * ui_zoom / 2f - 5f;
                            draw_rectangle_2.Right = draw_rectangle.Right - image_width * ui_zoom / 2 + 5f;
                            draw_rectangle_2.Top = draw_rectangle.Top + image_height * ui_zoom / 2f - 5f;
                            draw_rectangle_2.Bottom = draw_rectangle.Bottom - image_height * ui_zoom / 2f + 5f;

                            point_x = _scene.images[connections_cat[i][j]][connections_im[i][j]].points[connections_pt[i][j] * 2];
                            point_y = _scene.images[connections_cat[i][j]][connections_im[i][j]].points[connections_pt[i][j] * 2 + 1];

                            zoom_loc = rectangles_zoom[i][j];
                            source_rectangle.Left = point_x - (initial_image_zoom_area) / zoom_loc;
                            source_rectangle.Right = point_x + (initial_image_zoom_area) / zoom_loc;
                            source_rectangle.Top = point_y - (initial_image_zoom_area) / zoom_loc;
                            source_rectangle.Bottom = point_y + (initial_image_zoom_area) / zoom_loc;

                            //d2d_render_target.FillRectangle(draw_rectangle, black_brush);
                            zoom_factor_loc_horizontal = image_width*ui_zoom / (initial_image_zoom_area * 2 / zoom_loc);
                            zoom_factor_loc_vertical = image_height * ui_zoom / (initial_image_zoom_area * 2 / zoom_loc);
                            orig_width = _scene.images[connections_cat[i][j]][connections_im[i][j]].width;
                            orig_height = _scene.images[connections_cat[i][j]][connections_im[i][j]].width;
                            draw_ghost = false;
                            draw_rectangle_3 = draw_rectangle;
                            if (source_rectangle.Left < 0)
                            {
                                draw_rectangle_3.Left = draw_rectangle.Left + (image_width * ui_zoom / 2f - point_x * (zoom_factor_loc_horizontal));
                                draw_ghost = true;
                            }
                            if (source_rectangle.Top < 0)
                            {
                                draw_rectangle_3.Top = draw_rectangle.Top + (image_height * ui_zoom / 2f - point_y * (zoom_factor_loc_vertical));
                                draw_ghost = true;
                            }
                            if (source_rectangle.Right > orig_width)
                            {
                                draw_rectangle_3.Right = draw_rectangle.Right - (image_width * ui_zoom / 2f - (orig_width - point_x) * (zoom_factor_loc_horizontal));
                                draw_ghost = true;
                            }

                            if (source_rectangle.Bottom > orig_height)
                            {
                                draw_rectangle_3.Bottom = draw_rectangle.Bottom - (image_height * ui_zoom / 2f - (orig_height - point_y) * (zoom_factor_loc_vertical));
                                draw_ghost = true;
                            }
                            if (draw_ghost)
                            {
                                d2d_render_target.FillRectangle(draw_rectangle, gray_brush);
                                d2d_render_target.DrawBitmap(_scene.images[connections_cat[i][j]][connections_im[i][j]].bitmap, draw_rectangle_3, 1f, SharpDX.Direct2D1.BitmapInterpolationMode.Linear, source_rectangle);
                            }
                            else
                                d2d_render_target.DrawBitmap(_scene.images[connections_cat[i][j]][connections_im[i][j]].bitmap, draw_rectangle, 1f, SharpDX.Direct2D1.BitmapInterpolationMode.Linear, source_rectangle);



                            d2d_render_target.DrawBitmap(point_image, draw_rectangle_2, 1f, SharpDX.Direct2D1.BitmapInterpolationMode.Linear);
                        }
                    }
                }

                if (selecting)
                {
                    draw_rectangle.Left = selection_start_x;
                    draw_rectangle.Right = selection_end_x;
                    draw_rectangle.Top = selection_start_y;
                    draw_rectangle.Bottom = selection_end_y;
                    if (selection_start_x < selection_end_x)
                    {
                        //m_d2dRenderTarget.DrawText(draw_rectangle.Left.ToString() + "," + draw_rectangle.Right.ToString() + "," + draw_rectangle.Top.ToString() + "," + draw_rectangle.Bottom.ToString() , new SharpDX.DirectWrite.TextFormat(new SharpDX.DirectWrite.Factory(), "Arial", 14f), draw_rectangle, black_brush);
                        d2d_render_target.FillRectangle(draw_rectangle, selection_brush_concave);
                    }
                    else
                    {
                        d2d_render_target.FillRectangle(draw_rectangle, selection_brush_convex);
                    }
                    d2d_render_target.DrawRectangle(draw_rectangle, selection_border_brush, 1f, strokeStyle);
                }

                bool ctrl = System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl);
                bool alt = System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftAlt);
                bool none = (!ctrl && !alt);
                if (none && selecting)
                {
                    for (int i = 0; i < connections_count; i++)
                    {
                        int img_count = draw_rectangles[i].Length;
                        for (int j = 0; j < img_count; j++)
                        {
                            if (selected_rectangles_preview[i][j])
                            {
                                draw_rectangle.Left =-(float)scroll_horizontal+ draw_rectangles[i][j].Left - 1;
                                draw_rectangle.Top =-(float)scroll_vertical + draw_rectangles[i][j].Top - 1;
                                draw_rectangle.Right = -(float)scroll_horizontal + draw_rectangles[i][j].Right + 1;
                                draw_rectangle.Bottom = -(float)scroll_vertical + draw_rectangles[i][j].Bottom + 1;
                                d2d_render_target.DrawRectangle(draw_rectangle, selection_selected_brush);
                            }
                            else
                            {
                                draw_rectangle.Left = -(float)scroll_horizontal + draw_rectangles[i][j].Left - 1;
                                draw_rectangle.Top = -(float)scroll_vertical + draw_rectangles[i][j].Top - 1;
                                draw_rectangle.Right = -(float)scroll_horizontal + draw_rectangles[i][j].Right + 1;
                                draw_rectangle.Bottom = -(float)scroll_vertical + draw_rectangles[i][j].Bottom + 1;
                                d2d_render_target.DrawRectangle(draw_rectangle, gray_brush);
                            }
                        }
                    }
                    
                }
                else if (ctrl || none)
                {
                    for (int i = 0; i < connections_count; i++)
                    {
                        int img_count = draw_rectangles[i].Length;
                        for (int j = 0; j < img_count; j++)
                        {
                            if (selected_rectangles[i][j] || selected_rectangles_preview[i][j])
                            {
                                draw_rectangle.Left = -(float)scroll_horizontal + draw_rectangles[i][j].Left - 1;
                                draw_rectangle.Top = -(float)scroll_vertical + draw_rectangles[i][j].Top - 1;
                                draw_rectangle.Right = -(float)scroll_horizontal + draw_rectangles[i][j].Right + 1;
                                draw_rectangle.Bottom = -(float)scroll_vertical + draw_rectangles[i][j].Bottom + 1;
                                d2d_render_target.DrawRectangle(draw_rectangle, selection_selected_brush);
                            }
                            else
                            {
                                draw_rectangle.Left = -(float)scroll_horizontal + draw_rectangles[i][j].Left - 1;
                                draw_rectangle.Top = -(float)scroll_vertical + draw_rectangles[i][j].Top - 1;
                                draw_rectangle.Right = -(float)scroll_horizontal + draw_rectangles[i][j].Right + 1;
                                draw_rectangle.Bottom = -(float)scroll_vertical + draw_rectangles[i][j].Bottom + 1;
                                d2d_render_target.DrawRectangle(draw_rectangle, gray_brush);
                            }
                        }
                    }
                    
                }
                else if (alt)
                {
                    for (int i = 0; i < connections_count; i++)
                    {
                        int img_count = draw_rectangles[i].Length;
                        for (int j = 0; j < img_count; j++)
                        {
                            if (selected_rectangles[i][j] && !selected_rectangles_preview[i][j])
                            {
                                draw_rectangle.Left = -(float)scroll_horizontal + draw_rectangles[i][j].Left - 1;
                                draw_rectangle.Top = -(float)scroll_vertical + draw_rectangles[i][j].Top - 1;
                                draw_rectangle.Right = -(float)scroll_horizontal + draw_rectangles[i][j].Right + 1;
                                draw_rectangle.Bottom = -(float)scroll_vertical + draw_rectangles[i][j].Bottom + 1;
                                d2d_render_target.DrawRectangle(draw_rectangle, selection_selected_brush);
                            }
                            else
                            {
                                draw_rectangle.Left = -(float)scroll_horizontal + draw_rectangles[i][j].Left - 1;
                                draw_rectangle.Top = -(float)scroll_vertical + draw_rectangles[i][j].Top - 1;
                                draw_rectangle.Right = -(float)scroll_horizontal + draw_rectangles[i][j].Right + 1;
                                draw_rectangle.Bottom = -(float)scroll_vertical + draw_rectangles[i][j].Bottom + 1;
                                d2d_render_target.DrawRectangle(draw_rectangle, gray_brush);
                            }
                        }
                    }
                }
                
                draw_rectangle.Left = 20;
                draw_rectangle.Right = 500;
                draw_rectangle.Top = targetHeight - 60;
                draw_rectangle.Bottom = targetHeight;
                //d2d_render_target.DrawText("RENDER_TIME = " + render_time_render.ToString("G4")  + "\nWPF_RENDER_TIME = " + (fps).ToString("G4"), new SharpDX.DirectWrite.TextFormat(new SharpDX.DirectWrite.Factory(), "Arial", 14f), draw_rectangle, black_brush);
            }

            //d2d_render_target.PopAxisAlignedClip();
            d2d_render_target.EndDraw();

            d3dDevice.ImmediateContext.End(queryForCompletion);
            //FPS = 0;
            while (!d3dDevice.ImmediateContext.IsDataAvailable(queryForCompletion))
            {
                //FPS += 1;
                spin.SpinOnce();
            }
            //System.Threading.Thread.Sleep(50);
            //device.Flush();

            //wait_for_rendering_to_complete();

            D3DSurface.AddDirtyRect(new Int32Rect(0, 0, targetWidth, targetHeight));
            D3DSurface.Unlock();
            render_time_render = (timer.ElapsedMilliseconds - start_time);//timer.ElapsedMilliseconds - start_time;//0.9f * render_time_render + 100f / (float)(timer.ElapsedMilliseconds - start_time);
            //Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, EmptyDelegate);

            /*int sleep_time = 0;
            sleep_time = (int) Math.Max((double)(20 - fps), 0.0);
            if (sleep_time!=0)
                System.Threading.Thread.Sleep(sleep_time);*/
        }
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            this.CreateAndBindTargets();
            base.OnRenderSizeChanged(sizeInfo);
            update_scroll_bars();
            render();
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, EmptyDelegate);

        }
        private void CreateAndBindTargets()
        {
            this.D3DSurface.SetRenderTargetDX10(null);
            if (d2d_render_target != null)
            {
                d2d_render_target.Dispose();
                this.RenderTarget.Dispose();
                /*white_brush.Dispose();
                black_brush.Dispose();
                connections_brush.Dispose();
                selection_brush_convex.Dispose();
                selection_brush_concave.Dispose();
                selection_border_brush.Dispose();
                black_brush.Dispose();
                strokeStyle.Dispose();*/
            }
            //Disposer.SafeDispose(ref this.d2d_render_target);
            //Disposer.SafeDispose(ref this.m_d2dFactory);
            //Disposer.SafeDispose(ref this.RenderTarget);

            targetWidth = Math.Max((int)this.ActualWidth, 100);
            targetHeight = Math.Max((int)this.ActualHeight, 100);

            Texture2DDescription colordesc = new Texture2DDescription
            {
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Format = Format.B8G8R8A8_UNorm,
                Width = targetWidth,
                Height = targetHeight,
                MipLevels = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.Shared,
                CpuAccessFlags = CpuAccessFlags.None,
                ArraySize = 1
            };
            this.RenderTarget = new Texture2D(d3dDevice, colordesc);

            Surface surface = this.RenderTarget.QueryInterface<Surface>();

            RenderTargetProperties rtp = new RenderTargetProperties(new SharpDX.Direct2D1.PixelFormat(Format.Unknown, SharpDX.Direct2D1.AlphaMode.Premultiplied));

            d2d_render_target = new RenderTarget(d2dFactory, surface, rtp);
            //d2d_render_target.AntialiasMode = AntialiasMode.PerPrimitive;

            this.D3DSurface.SetRenderTargetDX10(this.RenderTarget);
            d3dDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, targetWidth, targetHeight, 0.0f, 1.0f));



            surface.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

        }
        
    }
    
}
