using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;


namespace image_match{
    public class matching_ui : System.Windows.Controls.Image 
    {

        //wpf stuff
        System.Windows.Controls.ContextMenu image_menu;
        System.Windows.Controls.ContextMenu viewport_menu;

        bool mouse_interaction_enabled=true;
        Action EmptyDelegate = new Action(() => { });
        System.Threading.WaitCallback move_rectangles_delayed;
        System.Threading.WaitCallback panning_action;
        System.Windows.Threading.DispatcherTimer render_timer;

        //DX STUFF
        private SharpDX.Direct3D11.Device d3dDevice;
        //private SharpDX.DXGI.Device dxgiDevice;
        private SharpDX.Direct2D1.Factory d2dFactory;
        private SharpDX.WIC.ImagingFactory wicFactory;
        private SharpDX.Direct2D1.RenderTarget d2d_render_target;
        private Texture2D RenderTarget;
        private DX10ImageSource D3DSurface;

        SharpDX.Direct2D1.Bitmap []rectangle_bitmaps;
        int targetWidth;
        int targetHeight;
        SharpDX.Direct2D1.SolidColorBrush white_brush;
        SharpDX.Direct2D1.SolidColorBrush black_brush;
        SharpDX.Direct2D1.SolidColorBrush connections_brush;
        SharpDX.Direct2D1.SolidColorBrush selection_brush_convex;
        SharpDX.Direct2D1.SolidColorBrush selection_brush_concave;
        SharpDX.Direct2D1.SolidColorBrush selection_border_brush;
        SharpDX.Direct2D1.SolidColorBrush selection_selected_brush;
        SharpDX.Direct2D1.SolidColorBrush grid_brush;
        SharpDX.Direct2D1.SolidColorBrush gray_brush;
        SharpDX.Direct2D1.Bitmap[] descriptor_image_plus;
        SharpDX.Direct2D1.Bitmap[] descriptor_image_minus;
        SharpDX.Direct2D1.Bitmap point_image;
        Vector2 draw_point_start;
        Vector2 draw_point_end;
        Vector2 end_point;
        Vector2 start_point;

        StrokeStyle strokeStyle;
        Query queryForCompletion;
        System.Threading.SpinWait spin = new System.Threading.SpinWait();
        
        Random rand;
        Color4 background;
        public bool draw;
        Stopwatch timer;
        float render_time_render;
        float render_time_screen;
        float fps;
        float start_time;

        
        //scene properties
        int cat_count;
        int points_count;
        int[] im_count;
        float[] points_image_space;

        int rectangle_count;
        int[] points_counts_in_rectagles;
        int[][] points_counts_in_rectagles_by_cat;
        int matches_count;
        int margin;
        string[] file_names;

        //dictionaries
        int[][] image_cat_2d_to_rectangle_1d;

        //world_space
        float[] rectangles_world_space;

        //screen_space
        float[] rectangle_zoom;
        Vector2 dpf_1;
        RectangleF draw_rectangle;
        Vector2[] points_screen_space;
        float[] rectangles_screen_space;
        float[] orientation_x;
        float[] orientation_y;
        float[] scale;
        bool[] sign;
        
        //matching
        int[] matches_st_index;
        int[] matches_nd_index;
        int[] matches_st_rectangle_index;
        int[] matches_nd_rectangle_index;
        //float[] match_fitness;
        public bool[] display_connection;
        public bool[] display_descriptors;
        public bool[] display_points;
        
        //viewport_navigation
        float displace_x;
        float displace_y;
        float init_displace_x;
        float init_displace_y;
        float init_displace_ss_x;
        float init_displace_ss_y;
        float zoom;
        float max_zoom;
        float min_zoom;

        //viewport move world space
        float init_move_ss_x ;
        float init_move_ss_y ;
        float current_move_ss_x;
        float current_move_ss_y;
        bool move_selection;
        float[] rectangles_world_space_origin; 

        //viewport_operations
        bool selecting;
        bool panning;

        
        //selections
        float selection_start_x;
        float selection_start_y;
        float selection_end_x;
        float selection_end_y;
        bool[] selected_rectangles;
        bool[] selected_rectangles_preview;

        System.Windows.Point pos;

        //WPF related
        public matching_ui()
        {
            if (IsInDesignMode)
                return;
            timer = new Stopwatch();
            timer.Start();

            //System.Windows.Media.CompositionTarget.Rendering += render;
           
            this.IsVisibleChanged += is_visible_changed;
            this.MouseDown += DPFCanvas_MouseDown;
            this.MouseMove += DPFCanvas_MouseMove;
            this.MouseUp += DPFCanvas_MouseUp;
            this.MouseWheel += DPFCanvas_MouseWheel;

            margin = 15;
            dpf_1.X = 50;
            dpf_1.Y = 50;
            rand = new Random();
            background = new Color4((float)(203.0 / 255.0), (float)(203.0 / 255.0), (float)(203.0 / 255.0), 1f);
            margin = 15;

            draw = false;
            displace_x = margin;
            displace_y = margin;
            zoom = 1;
            
            image_menu = new System.Windows.Controls.ContextMenu();
            viewport_menu = new System.Windows.Controls.ContextMenu();


            System.Windows.Controls.MenuItem show_image_connections = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem hide_image_connections = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem solo_connections = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem hide_connections = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem show_connections = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem show_descriptors = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem hide_descriptors = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem show_points = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem hide_points = new System.Windows.Controls.MenuItem();

            System.Windows.Controls.MenuItem points = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem connections = new System.Windows.Controls.MenuItem();

            connections.Header = "Connections";
            //connections.Click += context_menu_click;
            

            points.Header = "Points";
            //points.Click += context_menu_click;
            

            show_image_connections.Header = "Show image conections";
            show_image_connections.Click += context_menu_click;
            connections.Items.Add(show_image_connections);

            hide_image_connections.Header = "Hide image conections";
            hide_image_connections.Click += context_menu_click;
            connections.Items.Add(hide_image_connections);

            show_connections.Header = "Show conections";
            show_connections.Click += context_menu_click;
            connections.Items.Add(show_connections);

            hide_connections.Header = "Hide conections";
            hide_connections.Click += context_menu_click;
            connections.Items.Add(hide_connections);

            solo_connections.Header = "Solo conections";
            solo_connections.Click += context_menu_click;
            connections.Items.Add(solo_connections);

            show_points.Header = "Show points";
            show_points.Click += context_menu_click;
            points.Items.Add(show_points);

            hide_points.Header = "Hide points";
            hide_points.Click += context_menu_click;
            points.Items.Add(hide_points);

            show_descriptors.Header = "Show descriptors";
            show_descriptors.Click += context_menu_click;
            points.Items.Add(show_descriptors);

            hide_descriptors.Header = "Hide descriptors";
            hide_descriptors.Click += context_menu_click;
            points.Items.Add(hide_descriptors);

            image_menu.Items.Add(connections);
            image_menu.Items.Add(points);

            image_menu.IsVisibleChanged += context_menu_IsVisibleChanged;
            image_menu.HasDropShadow = true;

            System.Windows.Controls.MenuItem zoom_extents = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem rearrange_images = new System.Windows.Controls.MenuItem();

            zoom_extents.Header = "Zoom extents";
            zoom_extents.Click += context_menu_click;
            viewport_menu.Items.Add(zoom_extents);

            rearrange_images.Header = "Rearrange images";
            rearrange_images.Click += context_menu_click;
            viewport_menu.Items.Add(rearrange_images);

            viewport_menu.IsVisibleChanged += viewport_menu_is_visible_changed;
            viewport_menu.HasDropShadow = true;

            /*d3dDevice = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware, SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport);
            wicFactory = new SharpDX.WIC.ImagingFactory();
            d2dFactory = new SharpDX.Direct2D1.Factory();*/
            this.D3DSurface = new DX10ImageSource();
            //this.CreateAndBindTargets();
            this.Source = this.D3DSurface;

            

            move_rectangles_delayed = state =>
            {
                move_rectangles();
                generate_screen_space(true);
            };



            panning_action = state =>
            {
                while (panning)
                {
                    displace_x = init_displace_x + (float)pos.X - init_displace_ss_x;
                    displace_y = init_displace_y + (float)pos.Y - init_displace_ss_y;
                    generate_screen_space(true);
                    preview_selection(pos,false);
                }
            };
            render_timer = new System.Windows.Threading.DispatcherTimer(/*System.Windows.Threading.DispatcherPriority.Background,Dispatcher*/);
            render_timer.Interval = new TimeSpan(0, 0, 0, 0, 0);
            render_timer.Tick += render_timer_Tick;

        }
        void render_timer_Tick(object sender, EventArgs e)
        {
            render();
        }
        ~matching_ui()  // destructor
        {
            /*this.D3DSurface.SetRenderTargetDX10(null);
            d2d_render_target.Dispose();
            this.RenderTarget.Dispose();
            black_brush.Dispose();
            white_brush.Dispose();
            connections_brush.Dispose();
            selection_brush_convex.Dispose();
            selection_brush_concave.Dispose();
            selection_border_brush.Dispose();
            selection_selected_brush.Dispose();
            strokeStyle.Dispose();
            D3DSurface.Dispose();*/
        }       
        private void Window_Loaded()
        {
            if (matching_ui.IsInDesignMode)
                return;
            if (d3dDevice == null)
                return;
            
            //System.Windows.Media.CompositionTarget.Rendering += render;
            //render_timer.Start();
            render();
            //fit_scene();
            Dispatcher.BeginInvoke(new Action(delegate
            {
                /*for (int i = 0; i < 1000; i++)
                {
                    render();
                }*/
                fit_scene();
            }), System.Windows.Threading.DispatcherPriority.Background, null);
            
        }
        private void Window_Unloaded()
        {
            if (matching_ui.IsInDesignMode)
                return;
            //render_timer.Stop();
            //System.Windows.Media.CompositionTarget.Rendering -= render;
        }
        void is_visible_changed(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                Window_Loaded();
            }
            else
            {
                Window_Unloaded();
            }
        }
        public static bool IsAnyKeyDown()
        {
            var values = Enum.GetValues(typeof(System.Windows.Input.Key));

            foreach (var v in values)
            {
                if (((System.Windows.Input.Key)v) != System.Windows.Input.Key.None)
                {
                    if (System.Windows.Input.Keyboard.IsKeyDown((System.Windows.Input.Key)v))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            this.CreateAndBindTargets();
            set_max_min_zoom();
            

            base.OnRenderSizeChanged(sizeInfo);
            render();
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, EmptyDelegate);
            
        }
        public static bool IsInDesignMode
        {
            get
            {
                DependencyProperty prop = DesignerProperties.IsInDesignModeProperty;
                bool isDesignMode = (bool)DependencyPropertyDescriptor.FromProperty(prop, typeof(FrameworkElement)).Metadata.DefaultValue;
                return isDesignMode;
            }
        }
        //helper functions
        int max(int[] array)
        {
            int max = 0;
            for (int i = 0; i < array.Length; i++)
            {
                if (max < array[i])
                {
                    max = array[i];
                }
            }
            return max;
        }
        int pointer_inside_selected_rectangle_check(out int index)
        {
            pos = System.Windows.Input.Mouse.GetPosition(this);
            index = -1;
            for (int i = 0; i < rectangle_count; i++)
            {
                if (pos.X > rectangles_screen_space[i * 4] && pos.X < rectangles_screen_space[i * 4 + 2] && pos.Y > rectangles_screen_space[i * 4 + 1] && pos.Y < rectangles_screen_space[i * 4 + 3])
                {
                    index = i;
                    if (selected_rectangles[i] && selected_rectangles_preview[i])
                    {
                        return 1;
                    }
                    if (selected_rectangles_preview[i])
                    {
                        return 2;
                    }
                }
            }
            return 0;
        }
        //scene manipulation
        void sort_rectangles()
        {
            float[] old_world_rect = new float[rectangle_count * 4];
            for (int i = 0; i < rectangle_count * 4; i++)
            {
                old_world_rect[i] = rectangles_world_space[i];
            }
            float[] new_world_rect = new float[rectangle_count * 4];


            


            int rect_index = 0;
            float max_width = 0;
            float max_width_total = 0;
            float height_total;
            for (int i = 0; i < cat_count; i++)
            {

                height_total = 0;
                for (int j = 0; j < im_count[i]; j++)
                {
                    new_world_rect[rect_index * 4] = max_width + max_width_total + i * margin;
                    new_world_rect[rect_index * 4 + 1] = height_total + j * margin;
                    height_total += (long)rectangles_world_space[rect_index * 4 + 3];
                    rect_index++;
                }
                max_width_total += max_width;
                max_width = 0;
                for (int j = 0; j < im_count[i]; j++)
                {
                    if (rectangles_world_space[image_cat_2d_to_rectangle_1d[i][j] * 4 + 3] > max_width)
                        max_width = rectangles_world_space[image_cat_2d_to_rectangle_1d[i][j] * 4 + 2];
                }
            }

            System.Threading.WaitCallback delayed = state =>
            {
                int loop_length = 120;//(int) Math.Min((double)render_time_render / (double)2, (double)35);
                render_timer.Start();

                for (int i = 0; i <= loop_length; i++)
                {
                    float alpha = (float)(Math.Sin((double)i / (double)loop_length * Math.PI - Math.PI / (double)2) / (double)2 + (double)0.5);
                    for (int j = 0; j < rectangle_count; j++)
                    {
                        rectangles_world_space[j * 4] = alpha * new_world_rect[j * 4] + (1f - alpha) * old_world_rect[j * 4];
                        rectangles_world_space[j * 4 + 1] = alpha * new_world_rect[j * 4 + 1] + (1f - alpha) * old_world_rect[j * 4 + 1];
                    }
                    generate_screen_space(true);
                    //render();
                    //this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, EmptyDelegate);
                    System.Threading.Thread.Sleep(5);
                    //this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, EmptyDelegate);
                }
                render_timer.Stop();

            };
            System.Threading.ThreadPool.QueueUserWorkItem(delayed);
        }
        void fit_scene()
        {
            int width = targetWidth - 2 * margin;
            int height = targetHeight - 2 * margin;
            //generate_screen_space(true);
            float min_left = float.MaxValue;
            float min_top = float.MaxValue;
            float max_right = float.MinValue;
            float max_bottom = float.MinValue;
            float zoom_old, zoom_new, displace_x_old, displace_y_old, displace_x_new, displace_y_new;
            for (int i = 0; i < rectangle_count; i++)
            {
                float top, bottom, left, right;
                top = rectangles_world_space[i * 4 + 1];
                left = rectangles_world_space[i * 4 + 0];
                bottom = top + rectangles_world_space[i * 4 + 3];
                right = left + rectangles_world_space[i * 4 + 2];
                if (top < min_top)
                    min_top = top;
                if (left < min_left)
                    min_left = left;
                if (right > max_right)
                    max_right = right;
                if (bottom > max_bottom)
                    max_bottom = bottom;
            }
            float width_ws = max_right - min_left;
            float height_ws = max_bottom - min_top;

            zoom_old = zoom;
            displace_x_old = displace_x;
            displace_y_old = displace_y;

            zoom_new = Math.Min(width / width_ws, height / height_ws);
            displace_x_new = margin - (width_ws * zoom_new - width) / 2 - min_left * zoom_new;
            displace_y_new = margin - (height_ws * zoom_new - height) / 2 - min_top * zoom_new;

            System.Threading.WaitCallback delayed = state =>
            {
                render_timer.Start();
                int loop_length =120;//(int) Math.Min((double)render_time_render / (double)2, (double)35);
                Action EmptyDelegate = new Action(() => { });
                for (int i = 0; i <= loop_length; i++)
                {
                    float alpha = (float)(Math.Sin((double)i / (double)loop_length * Math.PI - Math.PI / (double)2) / (double)2 + (double)0.5);
                    for (int j = 0; j < rectangle_count; j++)
                    {
                        zoom = alpha * zoom_new + (1f - alpha) * zoom_old;
                        displace_x = alpha * displace_x_new + (1f - alpha) * displace_x_old;
                        displace_y = alpha * displace_y_new + (1f - alpha) * displace_y_old;
                    }
                    generate_screen_space(true);
                    //render();
                    //this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, EmptyDelegate);

                    System.Threading.Thread.Sleep(5);
                    //render();
                    //this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => {render(); }));
                }
                render_timer.Stop();
            };

            System.Threading.ThreadPool.QueueUserWorkItem(delayed);
        }
        void connections_show_by_image()
        {
            for (int i = 0; i < matches_count; i++)
            {
                //display_connection[i] = false;
                for (int j = 0; j < rectangle_count; j++)
                {
                    if (selected_rectangles[j] || selected_rectangles_preview[j])
                    {
                        if (matches_st_rectangle_index[i] == j || matches_nd_rectangle_index[i] == j)
                        {
                            display_connection[i] = true;
                        }
                    }
                }
            }
            //render();
        }
        void connections_hide_by_image()
        {
            for (int i = 0; i < matches_count; i++)
            {
                //display_connection[i] = false;
                for (int j = 0; j < rectangle_count; j++)
                {
                    if (selected_rectangles[j] || selected_rectangles_preview[j])
                    {
                        if (matches_st_rectangle_index[i] == j || matches_nd_rectangle_index[i] == j)
                        {
                            display_connection[i] = false;
                        }
                    }
                }
            }
            //render();
        }
        void connections_hide()
        {
            for (int i = 0; i < matches_count; i++)
            {
                bool left_vis = false;
                bool right_vis = false;
                for (int j = 0; j < rectangle_count; j++)
                {
                    if (selected_rectangles[j] || selected_rectangles_preview[j])
                    {
                        if (matches_st_rectangle_index[i] == j )
                        {
                            left_vis = true;
                        }
                        if (matches_nd_rectangle_index[i] == j)
                        {
                            right_vis = true;
                        }
                    }
                }
                if (right_vis && left_vis)
                    display_connection[i] = false;
            }
            //render();
        }
        void connections_show()
        {
            for (int i = 0; i < matches_count; i++)
            {
                bool left_vis = false;
                bool right_vis = false;
                for (int j = 0; j < rectangle_count; j++)
                {
                    if (selected_rectangles[j] || selected_rectangles_preview[j])
                    {
                        if (matches_st_rectangle_index[i] == j)
                        {
                            left_vis = true;
                        }
                        if (matches_nd_rectangle_index[i] == j)
                        {
                            right_vis = true;
                        }
                    }
                }
                if (right_vis && left_vis)
                    display_connection[i] = true;
            }
            //render();
        }
        void connections_solo()
        {
            for (int i = 0; i < matches_count; i++)
            {
                bool left_vis = false;
                bool right_vis = false;
                for (int j = 0; j < rectangle_count; j++)
                {
                    if (selected_rectangles[j] || selected_rectangles_preview[j])
                    {
                        if (matches_st_rectangle_index[i] == j)
                        {
                            left_vis = true;
                        }
                        if (matches_nd_rectangle_index[i] == j)
                        {
                            right_vis = true;
                        }
                    }
                }
                if (right_vis && left_vis)
                    display_connection[i] = true;
                else
                {
                    display_connection[i] = false;
                }
            }
            //render();
        }
        void set_max_min_zoom()
        {
            int width = targetWidth - 2 * margin;
            int height = targetHeight - 2 * margin;
            generate_screen_space(true);
            float min_left = float.MaxValue;
            float min_top = float.MaxValue;
            float max_right = float.MinValue;
            float max_bottom = float.MinValue;
            for (int i = 0; i < rectangle_count; i++)
            {
                float top, bottom, left, right;
                top = rectangles_world_space[i * 4 + 1];
                left = rectangles_world_space[i * 4 + 0];
                bottom = top + rectangles_world_space[i * 4 + 3];
                right = left + rectangles_world_space[i * 4 + 2];
                if (top < min_top)
                    min_top = top;
                if (left < min_left)
                    min_left = left;
                if (right > max_right)
                    max_right = right;
                if (bottom > max_bottom)
                    max_bottom = bottom;
            }
            float width_ws = max_right - min_left;
            float height_ws = max_bottom - min_top;
            float fit_zoom = Math.Min(width / width_ws, height / height_ws);
            max_zoom = 10;
            min_zoom = 0.1f * fit_zoom;
            if (min_zoom <= 0)
                min_zoom = 0.1f;
        }
        void move_initialize()
        {
            for (int i = 0; i < rectangle_count * 4; i++)
            {
                rectangles_world_space_origin[i] = rectangles_world_space[i];
            }
        }
        void move_rectangles()
        {
            if (move_selection)
            {
                for (int i = 0; i < rectangle_count; i++)
                {
                    if (selected_rectangles[i])
                    {
                        rectangles_world_space[i * 4] = rectangles_world_space_origin[i * 4] + (current_move_ss_x - init_move_ss_x) / zoom;
                        rectangles_world_space[i * 4 + 1] = rectangles_world_space_origin[i * 4 + 1] + (current_move_ss_y - init_move_ss_y) / zoom;
                    }
                }
            }
            else
            {
                for (int i = 0; i < rectangle_count; i++)
                {
                    if (selected_rectangles_preview[i])
                    {
                        rectangles_world_space[i * 4] = rectangles_world_space_origin[i * 4] + (current_move_ss_x - init_move_ss_x) / zoom;
                        rectangles_world_space[i * 4 + 1] = rectangles_world_space_origin[i * 4 + 1] + (current_move_ss_y - init_move_ss_y) / zoom;
                    }
                }
            }
        }
        //data functions
        public void update_scene(scene sol)
        {
            draw = false;
            
            d3dDevice = sol.DX_RES.d3dDevice;
            wicFactory = sol.DX_RES.wicFactory;
            d2dFactory = sol.DX_RES.d2dFactory;
            descriptor_image_plus = sol.DX_RES.descriptor_image_plus;
            descriptor_image_minus = sol.DX_RES.descriptor_image_minus;
            point_image = sol.DX_RES.point_image;
            
            this.CreateAndBindTargets();

            if(black_brush!=null)
            {
                black_brush.Dispose();
                gray_brush.Dispose();
                white_brush.Dispose();
                connections_brush.Dispose();
                selection_brush_convex.Dispose();
                selection_brush_concave.Dispose();
                selection_border_brush.Dispose();
                selection_selected_brush.Dispose();
                strokeStyle.Dispose();
                grid_brush.Dispose();
                queryForCompletion.Dispose();
            }
            
            queryForCompletion = new Query(d3dDevice, new QueryDescription() { Type = QueryType.Event, Flags = QueryFlags.None });
            gray_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(0f, 0f, 0f, 0.2f));
            black_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(0f, 0f, 0f, 1f));
            white_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(1f, 1f, 1f, 1f));
            connections_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(0f, 0f, 0f, 1f));
            selection_brush_convex = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(1f, 0f, 0f, 0.5f));
            selection_brush_concave = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(0f, 1f, 0f, 0.5f));
            selection_border_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(0f, 0f, 0f, 0.5f));
            selection_selected_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4((float)(17) / (float)(255), (float)(158) / (float)(255), (float)(218) / (float)(255), 1f));
            strokeStyle = new StrokeStyle(d2dFactory, new StrokeStyleProperties() { DashStyle = SharpDX.Direct2D1.DashStyle.Dash, LineJoin = LineJoin.Round });
            grid_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(0f, 0f, 0f, 1f));


            
            targetWidth = (int)this.ActualWidth;
            targetHeight = (int)this.ActualHeight;
            selecting = false;
            panning = false;



            matches_count = sol.matches_count;
            cat_count = sol.images.Count;
            points_count = 0;
            im_count = new int[cat_count];
            rectangle_count = 0;

            for (int i = 0; i < cat_count; i++)
            {
                rectangle_count += sol.images[i].Count;
            }
            file_names = new string[rectangle_count];
            points_counts_in_rectagles_by_cat = new int[cat_count][];
            int rect_index = 0;
            points_counts_in_rectagles = new int[rectangle_count + 1];
            image_cat_2d_to_rectangle_1d = new int[cat_count][];
            int image_cat_2d_to_rectangle_1d_helper = 0;
            for (int i = 0; i < cat_count; i++)
            {
                im_count[i] = sol.images[i].Count;
                points_counts_in_rectagles_by_cat[i] = new int[im_count[i]];
                image_cat_2d_to_rectangle_1d[i] = new int[im_count[i]];
                for (int j = 0; j < im_count[i]; j++)
                {
                    file_names[rect_index] = sol.images[i][j].path;
                    image_cat_2d_to_rectangle_1d[i][j] = image_cat_2d_to_rectangle_1d_helper;
                    points_counts_in_rectagles_by_cat[i][j] = points_count;
                    points_counts_in_rectagles[rect_index] = points_count;
                    points_count += sol.images[i][j].points_count;
                    rect_index++;
                    image_cat_2d_to_rectangle_1d_helper++;
                }

            }
            

            points_counts_in_rectagles[rectangle_count] = points_count;

            points_image_space = new float[points_count * 2];
            orientation_x = new float[points_count];
            orientation_y = new float[points_count];
            scale = new float[points_count];
            sign = new bool[points_count];

            int point_index = 0;
            for (int i = 0; i < cat_count; i++)
            {
                for (int j = 0; j < im_count[i]; j++)
                {
                    int count = sol.images[i][j].points_count;
                    float[] points_loc = sol.images[i][j].points;
                    float[] points_loc_orientation = sol.images[i][j].orientation;
                    float[] points_loc_scale = sol.images[i][j].scale;
                    bool[] points_loc_sign = sol.images[i][j].sign;
                    for (int k = 0; k < count; k++)
                    {
                        points_image_space[point_index * 2] = points_loc[k * 2];
                        points_image_space[point_index * 2 + 1] = points_loc[k * 2 + 1];
                        orientation_x[point_index] = (float)Math.Cos((double)points_loc_orientation[k]) * 2.5f * points_loc_scale[k];
                        orientation_y[point_index] = (float)Math.Sin((double)points_loc_orientation[k]) * 2.5f * points_loc_scale[k];
                        scale[point_index] = 5f* points_loc_scale[k];
                        sign[point_index] = points_loc_sign[k];
                        point_index++;
                    }
                }
            }
            selected_rectangles = new bool[rectangle_count];
            selected_rectangles_preview = new bool[rectangle_count];
            display_descriptors = new bool[rectangle_count];
            display_points = new bool[rectangle_count];
            rectangle_zoom = new float[rectangle_count];
            for (int i = 0; i < rectangle_count; i++)
            {
                rectangle_zoom[i] = 1;
                display_descriptors[i] = false;
                display_points[i] = false;
                selected_rectangles[i] = false;
                selected_rectangles_preview[i] = false;
            }
            rectangles_world_space = new float[rectangle_count * 4];
            rectangles_screen_space = new float[rectangle_count * 4];
            rectangles_world_space_origin = new float[rectangle_count * 4];
            rect_index = 0;
            long max_width = 0;
            long max_width_total = 0;
            long height_total;
            for (int i = 0; i < cat_count; i++)
            {

                height_total = 0;
                for (int j = 0; j < im_count[i]; j++)
                {
                    rectangles_world_space[rect_index * 4] = max_width + max_width_total + i * margin;
                    rectangles_world_space[rect_index * 4 + 1] = height_total + j * margin;
                    rectangles_world_space[rect_index * 4 + 2] = sol.images[i][j].width;
                    rectangles_world_space[rect_index * 4 + 3] = sol.images[i][j].height;
                    height_total += sol.images[i][j].height;
                    rect_index++;
                }
                max_width_total += max_width;
                max_width = 0;
                for (int j = 0; j < im_count[i]; j++)
                {
                    if (sol.images[i][j].width > max_width)
                        max_width = sol.images[i][j].width;
                }
            }
            points_screen_space = new Vector2[points_count];
            matches_st_index = new int[matches_count];
            matches_nd_index = new int[matches_count];
            matches_st_rectangle_index = new int[matches_count];
            matches_nd_rectangle_index = new int[matches_count];
            display_connection = new bool[matches_count];
            //match_fitness = new float[matches_count];
            for (int i = 0; i < matches_count; i++)
            {
                display_connection[i] = true;
                //match_fitness[i] = sol.match_fitness[i];
                matches_st_index[i] = points_counts_in_rectagles_by_cat[sol.st_point_cat_index[i]][sol.st_point_im_index[i]] + sol.st_point_pt_index[i];
                matches_nd_index[i] = points_counts_in_rectagles_by_cat[sol.nd_point_cat_index[i]][sol.nd_point_im_index[i]] + sol.nd_point_pt_index[i];
                matches_st_rectangle_index[i] = image_cat_2d_to_rectangle_1d[sol.st_point_cat_index[i]][sol.st_point_im_index[i]];
                matches_nd_rectangle_index[i] = image_cat_2d_to_rectangle_1d[sol.nd_point_cat_index[i]][sol.nd_point_im_index[i]];
            }
            //draw = true;
            
            rectangle_bitmaps = new SharpDX.Direct2D1.Bitmap[rectangle_count];
            for (int i = 0; i < cat_count; i++)
            {
                for (int j = 0; j < sol.images[i].Count; j++)
                {
                    rectangle_bitmaps[image_cat_2d_to_rectangle_1d[i][j]] = sol.images[i][j].bitmap;
                }
            }
            set_max_min_zoom();
            //Window_Loaded();
            draw = true;
            render();
            //fit_scene();
        }
        //directX functions
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
        //mouse functions
        void DPFCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!mouse_interaction_enabled)
                return;
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left && e.ClickCount == 2)
            {
                sort_rectangles();
                return;
            }
            if (e.ChangedButton == System.Windows.Input.MouseButton.Right && e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                int index;
                if (pointer_inside_selected_rectangle_check(out index) == 0)
                {
                    viewport_menu.PlacementTarget = this;
                    viewport_menu.IsOpen = true;
                }
                else
                {
                    image_menu.PlacementTarget = this;
                    image_menu.IsOpen = true;
                }
                return;
            }
            if (e.ChangedButton == System.Windows.Input.MouseButton.Middle && e.ClickCount == 2)
            {
                fit_scene();
                return;
            }
            if (e.ChangedButton == System.Windows.Input.MouseButton.Middle && e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                pos = System.Windows.Input.Mouse.GetPosition(sender as IInputElement);
                init_displace_ss_x = (float)pos.X;
                init_displace_ss_y = (float)pos.Y;
                init_displace_x = displace_x;
                init_displace_y = displace_y;
                panning = true;
                System.Windows.Input.Mouse.Capture(this);
                System.Threading.ThreadPool.QueueUserWorkItem(panning_action);
                //this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, panning_action);
                return;
            }
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left && e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                pos = System.Windows.Input.Mouse.GetPosition(sender as IInputElement);
                int index;
                int result = pointer_inside_selected_rectangle_check(out index);
                Debug.WriteLine("down" + result.ToString());
                if (result == 1)
                {
                    init_move_ss_x = (float)pos.X;
                    init_move_ss_y = (float)pos.Y;
                    move_selection = true;
                    move_initialize();
                    System.Windows.Input.Mouse.Capture(this);
                    return;
                }

                move_selection = false;
                selection_start_x = (float)pos.X;
                selection_start_y = (float)pos.Y;
                selection_end_x = selection_start_x;
                selection_end_y = selection_start_y;
                selecting = true;
                System.Windows.Input.Mouse.Capture(this);
                //preview_selection(pos, false);
                return;
            }
            /*if (e.ChangedButton == System.Windows.Input.MouseButton.Left && e.ClickCount == 2)
            {
                select_all();
                selecting = false;
                return;
                //render();
            }*/
            //render_timer.Start();
        }
        void DPFCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!mouse_interaction_enabled)
                return;
            pos = System.Windows.Input.Mouse.GetPosition(this);
            if (panning)
            {
                //System.Windows.Point pos = System.Windows.Input.Mouse.GetPosition(this);
                
                //generate_screen_space(true);

                displace_x = init_displace_x + (float)pos.X - init_displace_ss_x;
                displace_y = init_displace_y + (float)pos.Y - init_displace_ss_y;

                //render();
                //render();
                //this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, EmptyDelegate);
                render();
                return;
            }
            if (move_selection)
            {
                //System.Windows.Point pos = System.Windows.Input.Mouse.GetPosition(this);
                current_move_ss_x = (float)pos.X;
                current_move_ss_y = (float)pos.Y;
                //System.Threading.ThreadPool.QueueUserWorkItem(move_rectangles_delayed);
                move_rectangles();
                generate_screen_space(true);
               
                render();
                return;
            }
            if (selecting)
            {
                //System.Windows.Point pos = System.Windows.Input.Mouse.GetPosition(this);
                selection_end_x = (float)pos.X;
                selection_end_y = (float)pos.Y;
                //generate_screen_space(false);
                
                /*Dispatcher.BeginInvoke(new Action(delegate
                {
                    preview_selection(pos);
                }), System.Windows.Threading.DispatcherPriority.Render, null);*/
                preview_selection(pos,false);
                render();
                return;
            }


            //System.Windows.Point pos = System.Windows.Input.Mouse.GetPosition(this);
            preview_selection(pos,true);
            //this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, EmptyDelegate);
            //if (preview_selection()||middle_down || left_down || right_down || true)
            //render();
        }
        void DPFCanvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!mouse_interaction_enabled)
                return;
            
            
            Debug.WriteLine("up");
            if (panning)
            {
                /*System.Windows.Point pos = System.Windows.Input.Mouse.GetPosition(sender as IInputElement);
                displace_x = init_displace_x + (float)pos.X - init_displace_ss_x;
                displace_y = init_displace_y + (float)pos.Y - init_displace_ss_y;
                generate_screen_space(true);*/

                panning = false;
                System.Windows.Input.Mouse.Capture(null);
                
                //render();
            }
            if (move_selection)
            {
                /*System.Windows.Point pos = System.Windows.Input.Mouse.GetPosition(sender as IInputElement);
                current_move_ss_x = (float)pos.X;
                current_move_ss_y = (float)pos.Y;
                move_rectangles();
                generate_screen_space(true);*/
                //render();
                move_selection = false;
                System.Windows.Input.Mouse.Capture(null);
            }
            if (selecting)
            {
                pos = System.Windows.Input.Mouse.GetPosition(sender as IInputElement);
                
                selection_end_x = (float)pos.X;
                selection_end_y = (float)pos.Y;
                if (selection_end_x == selection_start_x)
                    selection_end_x += 1;
                if (selection_end_y == selection_start_y)
                    selection_end_y += 1;
                preview_selection(pos,false);
                generate_screen_space(true);

                

                System.Windows.Input.Mouse.Capture(null);
                selecting = false;
                if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) && e.ClickCount == 1)
                    add_preview_selection();
                else
                    if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftAlt)  && e.ClickCount == 1)
                        remove_preview_selection();
                    else
                        select_preview_selection();
            }
            //render_timer.Stop();
            render();
        }
        void DPFCanvas_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {

            if (!mouse_interaction_enabled)
                return;
            bool shift = System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift);
            if (!shift)
            {
                float factor;
                if (e.Delta > 0)
                    factor = 1.2f;
                else
                    factor = (float)(1 / 1.2);
                if ((zoom * factor < min_zoom && factor < 1) || (zoom * factor > max_zoom && factor > 1))
                    return;
                float disp_help;
                pos = System.Windows.Input.Mouse.GetPosition(sender as IInputElement);

                disp_help = displace_x;
                disp_help -= (float)pos.X;
                disp_help *= factor;
                displace_x = disp_help + (float)pos.X;

                disp_help = displace_y;
                disp_help -= (float)pos.Y;
                disp_help *= factor;
                displace_y = disp_help + (float)pos.Y;

                disp_help *= factor;

                zoom *= factor;
                if (panning)
                {
                    init_displace_x = displace_x;
                    init_displace_y = displace_y;
                    init_displace_ss_x = (float)pos.X;
                    init_displace_ss_y = (float)pos.Y;
                }
            }
            else
            {
                int index;
                pointer_inside_selected_rectangle_check(out index);
                if(index!=-1)
                {
                    float factor;
                    if (e.Delta > 0)
                        factor = 1.2f;
                    else
                        factor = (float)(1 / 1.2);
                    if ((rectangle_zoom[index] * factor < min_zoom && factor < 1) || (rectangle_zoom[index] * factor > max_zoom && factor > 1))
                        return;
                    rectangle_zoom[index] *= factor;
                }
            }
            /*Dispatcher.BeginInvoke(new Action(delegate
            {
            }), System.Windows.Threading.DispatcherPriority.Render, null);*/
            generate_screen_space(true);
            
            render();

        }
        //context menu
        void viewport_menu_is_visible_changed(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!image_menu.IsOpen)
                mouse_interaction_enabled = true;
            else
            {
                mouse_interaction_enabled = false;
            }
        }
        void context_menu_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!image_menu.IsOpen)
            {
                mouse_interaction_enabled = true;
                render();
            }
            else
            {
                int selected_count = 0;
                for (int i = 0; i < rectangle_count; i++)
                {
                    if (selected_rectangles[i] || selected_rectangles_preview[i])
                        selected_count++;
                }
                if (selected_count < 2)
                {
                    ((image_menu.Items[0] as System.Windows.Controls.MenuItem).Items[2] as System.Windows.Controls.MenuItem).IsEnabled = false;
                    ((image_menu.Items[0] as System.Windows.Controls.MenuItem).Items[3] as System.Windows.Controls.MenuItem).IsEnabled = false;
                    ((image_menu.Items[0] as System.Windows.Controls.MenuItem).Items[4] as System.Windows.Controls.MenuItem).IsEnabled = false;
                }
                else
                {
                    ((image_menu.Items[0] as System.Windows.Controls.MenuItem).Items[2] as System.Windows.Controls.MenuItem).IsEnabled = true;
                    ((image_menu.Items[0] as System.Windows.Controls.MenuItem).Items[3] as System.Windows.Controls.MenuItem).IsEnabled = true;
                    ((image_menu.Items[0] as System.Windows.Controls.MenuItem).Items[4] as System.Windows.Controls.MenuItem).IsEnabled = true;
                }
                mouse_interaction_enabled = false;
            }
        }
        void context_menu_click(object sender, System.EventArgs e)
        {
            switch ((string)(((System.Windows.Controls.MenuItem) sender ).Header))
	        {
                case "Show image conections":
                    connections_show_by_image();
                    break;
                case "Hide image conections":
                    connections_hide_by_image();
                    break;
                case "Hide conections":
                    connections_hide();
                    break;
                case "Show conections":
                    connections_show();
                    break;
                case "Solo conections":
                    connections_solo();
                    break;
                case "Show descriptors":
                    show_descriptors();
                    break;
                case "Hide descriptors":
                    hide_descriptors();
                    break;
                case "Show points":
                    show_points();
                    break;
                case "Hide points":
                    hide_points();
                    break;
                case "Zoom extents":
                    fit_scene();
                    break;
                case "Rearrange images":
                    sort_rectangles();
                    break;
	        }
            //mouse_interaction_enabled = true;
        }
        //rendering
        void show_descriptors()
        {
            for (int i = 0; i < rectangle_count; i++)
            {
                if (selected_rectangles[i] || selected_rectangles_preview[i])
                    display_descriptors[i] = true;
            }
        }
        void hide_descriptors()
        {
            for (int i = 0; i < rectangle_count; i++)
            {
                if (selected_rectangles[i] || selected_rectangles_preview[i])
                    display_descriptors[i] = false;
            }
        }
        void show_points()
        {
            for (int i = 0; i < rectangle_count; i++)
            {
                if (selected_rectangles[i] || selected_rectangles_preview[i])
                    display_points[i] = true;
            }
        }
        void hide_points()
        {
            for (int i = 0; i < rectangle_count; i++)
            {
                if (selected_rectangles[i] || selected_rectangles_preview[i])
                    display_points[i] = false;
            }
        }
        void generate_screen_space(bool points)
        {
            float start_time2 = timer.ElapsedMilliseconds;
            for (int i = 0; i < rectangle_count; i++)
            {
                float rec_zoom_loc = rectangle_zoom[i];
                rectangles_screen_space[i * 4] = displace_x + rectangles_world_space[i * 4] * zoom ;
                rectangles_screen_space[i * 4 + 1] = displace_y + rectangles_world_space[i * 4 + 1] * zoom ;
                rectangles_screen_space[i * 4 + 2] = rectangles_screen_space[i * 4] + rectangles_world_space[i * 4 + 2] * zoom * rec_zoom_loc;
                rectangles_screen_space[i * 4 + 3] = rectangles_screen_space[i * 4 + 1] + rectangles_world_space[i * 4 + 3] * zoom * rec_zoom_loc;

                int from = points_counts_in_rectagles[i];
                int to = points_counts_in_rectagles[i + 1];
                float left = rectangles_screen_space[i * 4];
                float top = rectangles_screen_space[i * 4 + 1];
                if (points)
                {
                    for (int j = from; j < to; j++)
                    {
                        points_screen_space[j].X = points_image_space[j * 2] * zoom * rec_zoom_loc + left;
                        points_screen_space[j].Y = points_image_space[j * 2 + 1] * zoom* rec_zoom_loc + top;
                    }
                }
            }
            render_time_screen = timer.ElapsedMilliseconds - start_time2;
        }
        private void render1(object sender, System.EventArgs e)
        { }
        public void render(/*object sender, System.EventArgs e*/)
        {
            
            D3DSurface.Lock();
            
            fps = (timer.ElapsedMilliseconds - start_time);//timer.ElapsedMilliseconds - start_time;//0.9f * render_time_render + 100f / (float)(timer.ElapsedMilliseconds - start_time);
            start_time = timer.ElapsedMilliseconds;
            d2d_render_target.BeginDraw();
            d2d_render_target.Clear(background);
            if (draw)
            {
                //System.Threading.Thread.Sleep(1000);  
               
                float scale_loc;
                int point_image_index;
                connections_brush.Opacity = 1f;
                int depth=1;
                
                {
                    grid_brush.Opacity = 0.1f;//0.05f+((float)j / (depth - 1))/5f;
                    float top = (targetHeight - displace_y) / (zoom);
                    float bottom = (-displace_y) / (zoom);
                    float y_step = (float)Math.Pow(2.0, Math.Floor(Math.Log(top - bottom) / Math.Log(2.0)) - 3);
                    float start = bottom - (float)Math.IEEERemainder(bottom, y_step);
                    if (start < bottom)
                        start += y_step;
                    for (float i = start ; i < top; i += y_step)
                    {
                        float pos = i * zoom + displace_y;
                        //float pos2 = (i + y_step / 2) * zoom + displace_y;

                        draw_point_start.X = 0.5f;
                        draw_point_start.Y = (int)pos + 0.5f;

                        draw_point_end.X = targetWidth + 0.5f;
                        draw_point_end.Y = (int)pos + 0.5f;

                        d2d_render_target.DrawLine(draw_point_start, draw_point_end, grid_brush);
                    }
                }
                
                {
                    grid_brush.Opacity = 0.1f; //0.05f + ((float)j / (depth - 1)) / 5f;
                    float right = (targetWidth - displace_x) / (zoom);
                    float left = (-displace_x) / (zoom);
                    float x_step = (float)Math.Pow(2.0, Math.Floor(Math.Log(((targetHeight - displace_y) / (zoom)) - ((-displace_y) / (zoom))) / Math.Log(2.0)) - 3);
                    float start = left - (float)Math.IEEERemainder(left, x_step);
                    if (start < left)
                        start += x_step;
                    for (float i = start; i < right; i += x_step)
                    {
                        float pos = i * zoom + displace_x;
                        //float pos2 = (i + y_step / 2) * zoom + displace_y;

                        draw_point_start.X = (int)pos+0.5f;
                        draw_point_start.Y = 0.5f;

                        draw_point_end.X = (int)pos + 0.5f;
                        draw_point_end.Y = targetHeight + 0.5f;

                        d2d_render_target.DrawLine(draw_point_start, draw_point_end, grid_brush);
                    }
                }
                

                for (int i = 0; i < rectangle_count; i++)
                {
                    draw_rectangle.Left = rectangles_screen_space[i * 4];
                    draw_rectangle.Top = rectangles_screen_space[i * 4 + 1];
                    draw_rectangle.Right = rectangles_screen_space[i * 4 + 2];
                    draw_rectangle.Bottom = rectangles_screen_space[i * 4 + 3];
                    //d2d_render_target.FillRectangle(draw_rectangle, white_brush);
                    d2d_render_target.DrawBitmap(rectangle_bitmaps[i], draw_rectangle, 1f, SharpDX.Direct2D1.BitmapInterpolationMode.Linear);
                    //d2d_render_target.DrawRectangle(draw_rectangle, white_brush);
                    draw_rectangle.Left = rectangles_screen_space[i * 4] - 1;
                    draw_rectangle.Top = rectangles_screen_space[i * 4 + 1] - 1;
                    draw_rectangle.Right = rectangles_screen_space[i * 4 + 2] + 1;
                    draw_rectangle.Bottom = rectangles_screen_space[i * 4 + 3] + 1;
                    d2d_render_target.DrawRectangle(draw_rectangle, gray_brush);
                    
                    if (display_descriptors[i])
                    {
                        int from = points_counts_in_rectagles[i];
                        int to = points_counts_in_rectagles[i + 1];
                        for (int j = from; j < to; j++)
                        {
                            scale_loc = scale[j] * zoom;
                            start_point = points_screen_space[j];
                            if (!(start_point.X + scale_loc < 0 || start_point.Y + scale_loc < 0 || start_point.X - scale_loc > targetWidth || start_point.Y - scale_loc > targetWidth))
                            {
                                scale_loc = scale[j] * zoom;
                                if (scale_loc < 16)
                                    point_image_index = 0;
                                else if (scale_loc < 32)
                                    point_image_index = 1;
                                else if (scale_loc < 64)
                                    point_image_index = 2;
                                else if (scale_loc < 128)
                                    point_image_index = 3;
                                else if (scale_loc < 256)
                                    point_image_index = 4;
                                else
                                    point_image_index = 5;
                                draw_rectangle.X = start_point.X - scale_loc / 2f;
                                draw_rectangle.Y = start_point.Y - scale_loc / 2f;
                                draw_rectangle.Width = scale_loc;
                                draw_rectangle.Height = scale_loc;
                                end_point.X = start_point.X + orientation_x[j] * zoom;
                                end_point.Y = start_point.Y + orientation_y[j] * zoom;
                                if (sign[j])
                                {
                                    d2d_render_target.DrawLine(start_point, end_point, white_brush,1f);
                                    d2d_render_target.DrawBitmap(descriptor_image_plus[point_image_index], draw_rectangle, 1f, SharpDX.Direct2D1.BitmapInterpolationMode.Linear);
                                }
                                else
                                {
                                    d2d_render_target.DrawLine(start_point, end_point, black_brush, 1f);
                                    d2d_render_target.DrawBitmap(descriptor_image_minus[point_image_index], draw_rectangle, 1f, SharpDX.Direct2D1.BitmapInterpolationMode.Linear);
                                }
                            }
                        }
                    }
                    if (display_points[i])
                    {
                        int from = points_counts_in_rectagles[i];
                        int to = points_counts_in_rectagles[i + 1];
                        for (int j = from; j < to; j++)
                        {
                            start_point = points_screen_space[j];
                            if (!(start_point.X  < 0 || start_point.Y  < 0 || start_point.X  > targetWidth || start_point.Y  > targetWidth))
                            {
                                
                                draw_rectangle.X = start_point.X - 10f / 2f;
                                draw_rectangle.Y = start_point.Y - 10f / 2f;
                                draw_rectangle.Width = 10;
                                draw_rectangle.Height = 10;


                                d2d_render_target.DrawBitmap(point_image, draw_rectangle, 1f, SharpDX.Direct2D1.BitmapInterpolationMode.Linear);
                                
                            }
                        }
                    }
                }


                connections_brush.Opacity = 0.5f;
                if(zoom>1f)
                {
                    for (int i = 0; i < matches_count; i++)
                    {
                        if (display_connection[i])
                        {
                            //connections_brush.Opacity = match_fitness[i];
                            d2d_render_target.DrawLine(points_screen_space[matches_st_index[i]], points_screen_space[matches_nd_index[i]], connections_brush,zoom);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < matches_count; i++)
                    {
                        if (display_connection[i])
                        {
                            //connections_brush.Opacity = match_fitness[i];
                            d2d_render_target.DrawLine(points_screen_space[matches_st_index[i]], points_screen_space[matches_nd_index[i]], connections_brush);
                        }
                    }
                }
                
                bool ctrl = System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl);
                bool alt = System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftAlt);
                bool none = (!ctrl && !alt);
                if (none && selecting)
                {
                    for (int i = 0; i < rectangle_count; i++)
                    {
                        if (none && selecting)
                        {
                            if (selected_rectangles_preview[i])
                            {
                                draw_rectangle.Left = rectangles_screen_space[i * 4] - 1;
                                draw_rectangle.Top = rectangles_screen_space[i * 4 + 1] - 1;
                                draw_rectangle.Right = rectangles_screen_space[i * 4 + 2] + 1;
                                draw_rectangle.Bottom = rectangles_screen_space[i * 4 + 3] + 1;
                                d2d_render_target.DrawRectangle(draw_rectangle, selection_selected_brush);
                            }
                            
                        }
                    }
                }
                else if (ctrl || none)
                {
                    for (int i = 0; i < rectangle_count; i++)
                    {
                        if (selected_rectangles[i] || selected_rectangles_preview[i])
                        {
                            draw_rectangle.Left = rectangles_screen_space[i * 4] - 1;
                            draw_rectangle.Top = rectangles_screen_space[i * 4 + 1] - 1;
                            draw_rectangle.Right = rectangles_screen_space[i * 4 + 2] + 1;
                            draw_rectangle.Bottom = rectangles_screen_space[i * 4 + 3] + 1;
                            d2d_render_target.DrawRectangle(draw_rectangle, selection_selected_brush);
                        }
                       
                    }
                }
                else if (alt)
                {
                    for (int i = 0; i < rectangle_count; i++)
                    {
                        if (selected_rectangles[i] && !selected_rectangles_preview[i])
                        {
                            draw_rectangle.Left = rectangles_screen_space[i * 4] - 1;
                            draw_rectangle.Top = rectangles_screen_space[i * 4 + 1] - 1;
                            draw_rectangle.Right = rectangles_screen_space[i * 4 + 2] + 1;
                            draw_rectangle.Bottom = rectangles_screen_space[i * 4 + 3] + 1;
                            d2d_render_target.DrawRectangle(draw_rectangle, selection_selected_brush);
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
                draw_rectangle.Left = 20;
                draw_rectangle.Right = 500;
                draw_rectangle.Top = targetHeight - 60;
                draw_rectangle.Bottom = targetHeight;
                //d2d_render_target.DrawText("RENDER_TIME = " + render_time_render.ToString("G4") + "\nSCREEN_SPACE_TIME = " + render_time_screen.ToString("G4") + "\nWPF_RENDER_TIME = " + (fps).ToString("G4"), new SharpDX.DirectWrite.TextFormat(new SharpDX.DirectWrite.Factory(), "Arial", 14f), draw_rectangle, black_brush);
            }
            
            //long h, k;
            //m_d2dRenderTarget.Flush();
            d2d_render_target.EndDraw();

            d3dDevice.ImmediateContext.End(queryForCompletion);
            //FPS = 0;
            while (!d3dDevice.ImmediateContext.IsDataAvailable(queryForCompletion))
            {
                //FPS += 1;
                spin.SpinOnce();
            }

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
        //selecting
        void select_all()
        {
            for (int i = 0; i < rectangle_count; i++)
            {
                selected_rectangles[i] = true;
            }
        }
        void select_preview_selection()
        {
            for (int i = 0; i < rectangle_count; i++)
            {
                selected_rectangles[i] = selected_rectangles_preview[i];
            }
        }
        void remove_preview_selection()
        {
            for (int i = 0; i < rectangle_count; i++)
            {
                if (selected_rectangles_preview[i])
                {
                    selected_rectangles[i] = !selected_rectangles_preview[i];
                }
            }
        }
        void add_preview_selection()
        {
            for (int i = 0; i < rectangle_count; i++)
            {
                if (selected_rectangles_preview[i])
                {
                    selected_rectangles[i] = selected_rectangles_preview[i];
                }
            }
        }
        bool preview_selection(System.Windows.Point pos,bool render_)
        {
            bool changed = false;
            if (selecting)
            {
                for (int i = 0; i < rectangle_count; i++)
                {
                    float left_selection, top_selection, right_selection, bottom_selection;
                    left_selection = Math.Min(selection_start_x, selection_end_x);
                    right_selection = Math.Max(selection_start_x, selection_end_x);
                    top_selection = Math.Min(selection_start_y, selection_end_y);
                    bottom_selection = Math.Max(selection_start_y, selection_end_y);
                    if (selection_start_x < selection_end_x)
                    {
                        if (!(left_selection > rectangles_screen_space[i * 4 + 2] || right_selection < rectangles_screen_space[i * 4] || top_selection > rectangles_screen_space[i * 4 + 3] || bottom_selection < rectangles_screen_space[i * 4 + 1]))
                        {
                            if (!selected_rectangles_preview[i])
                            {
                                selected_rectangles_preview[i] = true;
                                changed = true;
                            }
                        }
                        else
                        {
                            selected_rectangles_preview[i] = false;
                        }
                    }
                    else
                    {
                        if ((left_selection < rectangles_screen_space[i * 4] && right_selection > rectangles_screen_space[i * 4 + 2] && top_selection < rectangles_screen_space[i * 4 + 1] && bottom_selection > rectangles_screen_space[i * 4 + 3]))
                        {
                            if (!selected_rectangles_preview[i])
                            {
                                selected_rectangles_preview[i] = true;
                                changed = true;
                            }
                        }
                        else
                        {
                            selected_rectangles_preview[i] = false;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < rectangle_count; i++)
                {
                    if (pos.X > rectangles_screen_space[i * 4] && pos.X < rectangles_screen_space[i * 4 + 2] && pos.Y > rectangles_screen_space[i * 4 + 1] && pos.Y < rectangles_screen_space[i * 4 + 3])
                    {
                        if (!selected_rectangles_preview[i])
                        {
                            selected_rectangles_preview[i] = true;
                            changed = true;
                        }
                    }
                    else
                    {
                        if (selected_rectangles_preview[i])
                        {
                            selected_rectangles_preview[i] = false;
                            changed = true;
                        }
                    }
                }
            }
            if (changed && render_)
                render();
            return changed;
        }
    }
}
