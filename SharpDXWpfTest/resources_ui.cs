using System;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Input;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;


namespace image_match
{
    public class OutlineTextRender : SharpDX.DirectWrite.TextRenderer
    {
        readonly SharpDX.Direct2D1.Factory _factory;
        readonly RenderTarget _surface;
        readonly Brush _brush_stroke;
        readonly Brush _brush_fill;
        readonly float _stroke_width;

        public OutlineTextRender(RenderTarget surface, Brush brush_stroke, Brush brush_fill,float stroke_width)
        {
            _factory = surface.Factory;
            _surface = surface;
            _brush_stroke = brush_stroke;
            _brush_fill = brush_fill;
            _stroke_width = stroke_width;
        }

        public Result DrawGlyphRun(object clientDrawingContext, float baselineOriginX, float baselineOriginY, MeasuringMode measuringMode, SharpDX.DirectWrite.GlyphRun glyphRun, SharpDX.DirectWrite.GlyphRunDescription glyphRunDescription, ComObject clientDrawingEffect)
        {
            using (PathGeometry path = new PathGeometry(_factory))
            {
                using (GeometrySink sink = path.Open())
                {
                    glyphRun.FontFace.GetGlyphRunOutline(glyphRun.FontSize, glyphRun.Indices, glyphRun.Advances, glyphRun.Offsets, glyphRun.IsSideways, (glyphRun.BidiLevel % 2) > 0, sink);

                    sink.Close();
                }

                Matrix matrix = Matrix.Identity;
                matrix = matrix * Matrix.Translation(baselineOriginX, baselineOriginY, 0);

                TransformedGeometry transformedGeometry = new TransformedGeometry(_factory, path, matrix);
                _surface.DrawGeometry(transformedGeometry, _brush_stroke, _stroke_width);
                _surface.FillGeometry(transformedGeometry, _brush_fill);

            }
            return new Result();
        }

        public Result DrawInlineObject(object clientDrawingContext, float originX, float originY, SharpDX.DirectWrite.InlineObject inlineObject, bool isSideways, bool isRightToLeft, ComObject clientDrawingEffect)
        {
            return new Result();
        }

        public Result DrawStrikethrough(object clientDrawingContext, float baselineOriginX, float baselineOriginY, ref SharpDX.DirectWrite.Strikethrough strikethrough, ComObject clientDrawingEffect)
        {
            return new Result();
        }

        public Result DrawUnderline(object clientDrawingContext, float baselineOriginX, float baselineOriginY, ref SharpDX.DirectWrite.Underline underline, ComObject clientDrawingEffect)
        {
            return new Result();
        }

        public SharpDX.Matrix3x2 GetCurrentTransform(object clientDrawingContext)
        {
            return new SharpDX.Matrix3x2();
        }

        public float GetPixelsPerDip(object clientDrawingContext)
        {
            return 0;
        }

        public bool IsPixelSnappingDisabled(object clientDrawingContext)
        {
            return true; ;
        }

        public IDisposable Shadow
        {
            get
            {
                return null;
            }
            set
            {
                // throw new NotImplementedException();
            }
        }
       
        public void Dispose()
        {

        }
    }
    public class resources_ui : System.Windows.Controls.Image
    {
        public scene _scene;
        //wpf stuff
        System.Windows.Controls.ContextMenu image_menu;
        System.Windows.Controls.ContextMenu category_menu;
        System.Windows.Controls.ContextMenu category_view_menu;
        System.Windows.Controls.ContextMenu image_pool_menu;
        int context_menu_cat_index;
        int context_menu_im_index;

        System.Windows.Controls.Primitives.ScrollBar scroll_bar_scene;
        System.Windows.Controls.Primitives.ScrollBar scroll_bar_pool;
        System.Timers.Timer timer_move_scene_up;
        System.Timers.Timer timer_move_scene_down;
        System.Timers.Timer timer_move_pool_up;
        System.Timers.Timer timer_move_pool_down;
        Microsoft.Win32.OpenFileDialog ofd;
        System.Windows.Forms.ColorDialog cd;
        //System.Windows.Threading.DispatcherTimer render_timer;
        //System.Threading.Timer tooltip_timer;
        private System.Windows.Threading.DispatcherTimer tooltip_timer;
        System.Windows.Controls.ToolTip tooltip;


        bool mouse_interaction_enabled = true;
        bool show_help=false;
        bool show_help_animating=false;
        float help_transparency;
        float help_time_start;

        Action EmptyDelegate = new Action(() => { });
        System.Timers.Timer show_help_timer;

        //DX STUFF
        private SharpDX.Direct3D11.Device d3dDevice;
        private SharpDX.Direct2D1.Factory d2dFactory;
        private SharpDX.WIC.ImagingFactory wicFactory;
        private SharpDX.Direct2D1.RenderTarget d2d_render_target;
        private Texture2D RenderTarget;
        private DX10ImageSource D3DSurface;
        public dx_resources DX_RES;
        private Vector2 line_start;
        private Vector2 line_end;
        SharpDX.DirectWrite.TextFormat text_format;

        int targetWidth;
        int targetHeight;
        SharpDX.Direct2D1.SolidColorBrush white_brush;
        SharpDX.Direct2D1.SolidColorBrush black_brush;
        
        SharpDX.Direct2D1.SolidColorBrush pool_brush;
        SharpDX.Direct2D1.SolidColorBrush selection_brush_convex;
        SharpDX.Direct2D1.SolidColorBrush selection_brush_concave;
        SharpDX.Direct2D1.SolidColorBrush selection_border_brush;
        SharpDX.Direct2D1.SolidColorBrush selection_selected_brush;
        SharpDX.Direct2D1.SolidColorBrush help_brush;
        SharpDX.Direct2D1.SolidColorBrush gray_brush;
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
        
        //world space
        RectangleF[] moving_images_world_space;

        //screen_space
        RectangleF draw_rectangle;
        RectangleF divider_screen_space;
        RectangleF pool_screen_space;
        RectangleF []pool_images_screen_space;
        RectangleF[] moving_images_screen_space;
        RectangleF[] moving_images_screen_space_origin;
        RectangleF[] categories_screen_space;
        RectangleF [][] images_screen_space;
        int[] moving_images_source_cat_index;
        int[] moving_images_source_im_index;
        int margin=15;
        int divider_width=1;
        int divider_factor = 5;
        int min_category_height=250;
        int left_margin = 15;
        int right_margin = 15;

        //viewport_navigation
        float divider_position;
        float divider_position_to_right=250;
        float scroll_scene = 0;
        float scroll_pool = 0;

        //viewport_operations
        bool move_divider;
        int scene_min_space=100;
        int pool_min_space=100;
        float desired_scene_image_width=125;
        float desired_pool_image_width = 125;
        float desired_scene_image_width_min = 75;
        float desired_pool_image_width_min = 75;

        //selecting
        float selection_start_x;
        float selection_start_y;
        float selection_end_x;
        float selection_end_y;

        bool scroll_scene_down = false;
        bool scroll_scene_up = false;
        bool scroll_pool_down = false;
        bool scroll_pool_up = false;
        double scroll_speed_scene = 25.0;
        double scroll_speed_pool = 25.0;
        double move_marg = 25.0;
        bool selecting=false;
        double scene_scroll_bar_max;
        double pool_scroll_bar_max;
        bool calculating = false;

        //moving
        System.Windows.Point moving_screen_space_start;
        System.Windows.Point moving_screen_space_end;
        bool moving = false;
        float moving_image_actual_width=50;
        bool show_tooltip=false;

        //WPF related
        public resources_ui()
        {

            if (IsInDesignMode)
                return;
            try
            {
                text_format = new SharpDX.DirectWrite.TextFormat(new SharpDX.DirectWrite.Factory(), "Segoe UI Light",SharpDX.DirectWrite.FontWeight.Normal,SharpDX.DirectWrite.FontStyle.Normal,SharpDX.DirectWrite.FontStretch.Normal, 18f);
            }
            catch (Exception)
            {
                text_format.Dispose();
                text_format = new SharpDX.DirectWrite.TextFormat(new SharpDX.DirectWrite.Factory(), "Arial", 14f);
                throw;
            }
            
            ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Title = "Load images";
            //ofd.FileName = "Open image(s)"; // Default file name
            //ofd.DefaultExt = ".*"; // Default file extension
            ofd.Filter = "All files (*.*)|*.*|JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg";
            ofd.Multiselect = true;

            cd = new System.Windows.Forms.ColorDialog();
            cd.AllowFullOpen = true;

            line_start.X = 0;
            DX_RES = new dx_resources();
            timer = new Stopwatch();
            timer.Start();
            _scene = new scene(DX_RES);
            //System.Windows.Media.CompositionTarget.Rendering += render;
            
            this.IsVisibleChanged += is_visible_changed;
            this.MouseDown += DPFCanvas_MouseDown;
            this.MouseMove += DPFCanvas_MouseMove;
            this.MouseUp += DPFCanvas_MouseUp;
            this.MouseWheel += DPFCanvas_MouseWheel;
            //this.KeyDown += resources_ui_KeyDown;
            this.IsEnabledChanged += resources_ui_IsEnabledChanged;
            this.Drop += resources_ui_Drop;

            //System.Windows.Controls.ToolTipService.SetBetweenShowDelay(this,5);
            //System.Windows.Controls.ToolTipService.SetPlacement(this, System.Windows.Controls.Primitives.PlacementMode.Left);
            tooltip = new System.Windows.Controls.ToolTip();
            tooltip.HasDropShadow = false;
            tooltip.Content = "test";
            tooltip.Closed += tooltip_Closed;
            tooltip.Opened += tooltip_Opened;
            tooltip_timer = new System.Windows.Threading.DispatcherTimer();
            tooltip_timer.Interval = TimeSpan.FromSeconds(1);
            tooltip_timer.Tick += delegate
            {
                if (this.IsMouseOver)
                {
                    //tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint;
                    //tooltip.PlacementTarget = 
                    tooltip.IsOpen = true;
                }
            };

            //tooltip.ToolTipOpening += tooltip_ToolTipOpening;
            
            //this.ToolTip = tooltip;
            //this.ToolTipOpening += resources_ui_ToolTipOpening;
            
            margin = 15;
            rand = new Random();
            background = new Color4((float)(203.0 / 255.0), (float)(203.0 / 255.0), (float)(203.0 / 255.0), 1f);

            image_menu = new System.Windows.Controls.ContextMenu();
            category_menu = new System.Windows.Controls.ContextMenu();
            category_view_menu = new System.Windows.Controls.ContextMenu();
            image_pool_menu = new System.Windows.Controls.ContextMenu();


            System.Windows.Controls.MenuItem image_menu_delete_image = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem image_menu_add_image = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem image_menu_replace_image = new System.Windows.Controls.MenuItem();

            image_menu_add_image.Header = "Insert image";
            image_menu_add_image.Name = "image_menu_add_image";
            image_menu_add_image.Click += context_menu_click;
            image_menu.Items.Add(image_menu_add_image);

            image_menu_delete_image.Header = "Delete image";
            image_menu_delete_image.Name = "image_menu_delete_image";
            image_menu_delete_image.Click += context_menu_click;
            image_menu.Items.Add(image_menu_delete_image);

            image_menu_replace_image.Header = "Replace image";
            image_menu_replace_image.Name = "image_menu_replace_image";
            image_menu_replace_image.Click += context_menu_click;
            image_menu.Items.Add(image_menu_replace_image);

            image_menu.IsVisibleChanged += context_menu_IsVisibleChanged;
            image_menu.HasDropShadow = true;

            System.Windows.Controls.MenuItem category_menu_add_image = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem category_menu_add_category = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem category_menu_delete_category = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem category_menu_color_category = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem category_menu_remove_zero_category = new System.Windows.Controls.MenuItem();

            category_menu_add_image.Header = "Load image";
            category_menu_add_image.Name = "category_menu_add_image";
            category_menu_add_image.Click += context_menu_click;
            category_menu.Items.Add(category_menu_add_image);

            category_menu_add_category.Header = "Add category";
            category_menu_add_category.Name = "category_menu_add_category";
            category_menu_add_category.Click += context_menu_click;
            category_menu.Items.Add(category_menu_add_category);

            category_menu_delete_category.Header = "Delete category";
            category_menu_delete_category.Name = "category_menu_delete_category";
            category_menu_delete_category.Click += context_menu_click;
            category_menu.Items.Add(category_menu_delete_category);

            category_menu_remove_zero_category.Header = "Remove empty categories";
            category_menu_remove_zero_category.Name = "viewport_menu_remove_zero_category";
            category_menu_remove_zero_category.Click += context_menu_click;
            category_menu.Items.Add(category_menu_remove_zero_category);

            category_menu_color_category.Header = "Change color";
            category_menu_color_category.Name = "category_menu_color_category";
            category_menu_color_category.Click += context_menu_click;
            category_menu.Items.Add(category_menu_color_category);

            category_menu.IsVisibleChanged += context_menu_IsVisibleChanged;
            category_menu.HasDropShadow = true;

            
            System.Windows.Controls.MenuItem image_pool_menu_add_image = new System.Windows.Controls.MenuItem();


            System.Windows.Controls.MenuItem image_pool_menu_color_category = new System.Windows.Controls.MenuItem();


            image_pool_menu_add_image.Header = "Load image";
            image_pool_menu_add_image.Name = "image_pool_menu_add_image";
            image_pool_menu_add_image.Click += context_menu_click;
            image_pool_menu.Items.Add(image_pool_menu_add_image);

            image_pool_menu_color_category.Header = "Change color";
            image_pool_menu_color_category.Name = "image_pool_menu_color_category";
            image_pool_menu_color_category.Click += context_menu_click;
            image_pool_menu.Items.Add(image_pool_menu_color_category);

            image_pool_menu.IsVisibleChanged += context_menu_IsVisibleChanged;
            image_pool_menu.HasDropShadow = true;
            

            System.Windows.Controls.MenuItem viewport_menu_add_category = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem viewport_menu_add_to_1_category = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem viewport_menu_add_to_multiple_category = new System.Windows.Controls.MenuItem();
            System.Windows.Controls.MenuItem viewport_menu_remove_zero_category = new System.Windows.Controls.MenuItem();

            viewport_menu_add_category.Header = "Add category";
            viewport_menu_add_category.Name = "viewport_menu_add_category";
            viewport_menu_add_category.Click += context_menu_click;
            category_view_menu.Items.Add(viewport_menu_add_category);

            viewport_menu_add_to_1_category.Header = "Load images to new category";
            viewport_menu_add_to_1_category.Name = "viewport_menu_add_to_1_category";
            viewport_menu_add_to_1_category.Click += context_menu_click;
            category_view_menu.Items.Add(viewport_menu_add_to_1_category);

            viewport_menu_add_to_multiple_category.Header = "Add images to new categories";
            viewport_menu_add_to_multiple_category.Name = "viewport_menu_add_to_multiple_category";
            viewport_menu_add_to_multiple_category.Click += context_menu_click;
            category_view_menu.Items.Add(viewport_menu_add_to_multiple_category);

            viewport_menu_remove_zero_category.Header = "Remove empty categories";
            viewport_menu_remove_zero_category.Name = "viewport_menu_remove_zero_category";
            viewport_menu_remove_zero_category.Click += context_menu_click;
            category_view_menu.Items.Add(viewport_menu_remove_zero_category);

            category_view_menu.IsVisibleChanged += context_menu_IsVisibleChanged;
            category_view_menu.HasDropShadow = true;


            d3dDevice = DX_RES.d3dDevice;
            wicFactory = DX_RES.wicFactory;
            d2dFactory = DX_RES.d2dFactory;
            this.D3DSurface = new DX10ImageSource();
            this.CreateAndBindTargets();
            this.Source = this.D3DSurface;

            queryForCompletion = new Query(d3dDevice, new QueryDescription() {Type = QueryType.Event, Flags = QueryFlags.None});


            gray_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(0f, 0f, 0f, 0.2f));
            black_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(0f, 0f, 0f, 1f));
            white_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(1f, 1f, 1f, 1f));
            pool_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, Color.Azure);
            selection_brush_convex = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(1f, 0f, 0f, 0.5f));
            selection_brush_concave = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(0f, 1f, 0f, 0.5f));
            selection_border_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(0f, 0f, 0f, 0.5f));
            selection_selected_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4((float)(17) / (float)(255), (float)(158) / (float)(255), (float)(218) / (float)(255), 1f));
            strokeStyle = new StrokeStyle(d2dFactory, new StrokeStyleProperties() { DashStyle = SharpDX.Direct2D1.DashStyle.Dash, LineJoin = LineJoin.Round });
            help_brush = new SharpDX.Direct2D1.SolidColorBrush(d2d_render_target, new Color4(0f, 0f, 0f, 1f));
            help_brush.Opacity = 0;



            int cat_count = 0;
            int im_count = 0;
            int pool_count = 0;

            categories_screen_space = new RectangleF[cat_count];
            images_screen_space = new RectangleF[cat_count][];
            pool_images_screen_space = new RectangleF[0];
            min_category_height = 250;


            string relative_path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var filePaths = System.IO.Directory.EnumerateFiles(relative_path + "\\test_images\\", "*.*", System.IO.SearchOption.TopDirectoryOnly).Where(s => s.EndsWith(".jpeg") || s.EndsWith(".JPEG") || s.EndsWith(".JPG") || s.EndsWith(".jpg") || s.EndsWith(".png") || s.EndsWith(".bmp")).ToArray();
            //var filteredFiles = System.IO.Directory.GetFiles(relative_path, "*.*").Where(file => file.ToLower().EndsWith("jpg") || file.ToLower().EndsWith("png")).ToList();
            //string[] filePaths = filteredFiles.ToArray();//System.IO.Directory.GetFiles(relative_path + "\\test_images\\"/*, "*.png"*/);
            int filePaths_len = filePaths.Length;

            for (int i = 0; i < cat_count; i++)
            {
                add_category();

                for (int j = 0; j < im_count; j++)
                {
                    add_image(i, filePaths[/*rand.Next(0, filePaths_len)*/(i * im_count + j) % filePaths_len]);
                }
            }
            for (int j = 0; j < pool_count; j++)
                add_image_pool(filePaths[rand.Next(0, filePaths_len)]);
            reinitialize_screen_space_buffers();
            //generate_screen_space();

            int spn = 35;
            timer_move_scene_up = new System.Timers.Timer(spn);
            timer_move_scene_down = new System.Timers.Timer(spn);
            timer_move_pool_up = new System.Timers.Timer(spn);
            timer_move_pool_down = new System.Timers.Timer(spn);

            timer_move_scene_up.Elapsed += timer_move_scene_up_Tick;
            timer_move_scene_down.Elapsed += timer_move_scene_down_Tick;
            timer_move_pool_up.Elapsed += timer_move_pool_up_Tick;
            timer_move_pool_down.Elapsed += timer_move_pool_down_Tick;

            /*render_timer = new System.Windows.Threading.DispatcherTimer();
            render_timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            render_timer.Tick += render_timer_Tick;*/
            
            this.Focusable = true;
            this.AllowDrop = true;
            //Keyboard.Focus(this);
            //this.Focus();

            show_help_timer = new System.Timers.Timer(10);
            show_help_timer.Elapsed += show_help_timer_Elapsed;

            
            draw = true;
            
        }
        void tooltip_Opened(object sender, RoutedEventArgs e)
        {
            int dest_cat_index;
            int dest_im_index;
            pointer_inside_image_check(out dest_cat_index, out dest_im_index);
            System.Windows.Point pos = System.Windows.Input.Mouse.GetPosition(this);
            if (pos.X - divider_width * divider_factor < divider_position && divider_position < pos.X + divider_width * divider_factor)
            {
                this.tooltip.Content = "Divider";
                return;
            }
            if (dest_cat_index == -2)
            {
                this.tooltip.Content = "Category view";
                return;
            }
            if (dest_cat_index == -1)
            {
                if (dest_im_index == -1)
                {
                    this.tooltip.Content = "Image pool";
                    return;
                }
                else
                {
                    this.tooltip.Content = "Image pool - " + System.IO.Path.GetFileName(_scene.images_pool[dest_im_index].path);
                    return;
                }
            }
            if (dest_cat_index >= 0)
            {
                if (dest_im_index == -1)
                {
                    this.tooltip.Content = "Category view " + (dest_cat_index + 1).ToString() + ". category";
                    return;
                }
                else
                {
                    this.tooltip.Content = "Category view " + (dest_cat_index + 1).ToString() + ". category - " + System.IO.Path.GetFileName( _scene.images[dest_cat_index][dest_im_index].path);
                    return;
                }
            }
            e.Handled = true;
        }
        public string get_help_url()
        {
            string url;
            int dest_cat_index;
            int dest_im_index;
            pointer_inside_image_check(out dest_cat_index, out dest_im_index);
            System.Windows.Point pos = System.Windows.Input.Mouse.GetPosition(this);
            if (pos.X - divider_width * divider_factor < divider_position && divider_position < pos.X + divider_width * divider_factor)
            {
                url="help/ui_elements/resources/divider.xaml";
                return url;
            }
            if (dest_cat_index == -2)
            {
                url = "help/resources/category_view.xaml";
                return url;
            }
            if (dest_cat_index == -1)
            {
                if (dest_im_index == -1)
                {
                    url = "help/resources/image_pool.xaml";
                    return url;
                }
                else
                {
                    url = "help/framework_elements/image.xaml";
                    return url;
                }
            }
            if (dest_cat_index >= 0)
            {
                if (dest_im_index == -1)
                {
                    url = "help/framework_elements/category.xaml";
                    return url;
                }
                else
                {
                    url = "help/framework_elements/image.xaml";
                    return url;
                }
            }
            return null;
        }
        void tooltip_Closed(object sender, RoutedEventArgs e)
        {
            
        }
        public void lock_ui(bool state)
        {
            calculating = state;
        }
        void UI_locked_message_box()
        {
            MessageBox.Show("You cannot use this operation while calculation is in progress", "Image match", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
        void show_help_timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (show_help)
            {
                help_brush.Opacity = (timer.ElapsedMilliseconds - help_time_start) / 1000f;
                if (help_transparency > 1)
                {
                    help_brush.Opacity = 1;
                    show_help = show_help_animating;
                    show_help_timer.Stop();
                }
            }
            else
            {
                help_brush.Opacity = (1000 - (timer.ElapsedMilliseconds - help_time_start)) / 1000f;
                if (help_transparency < 0)
                {
                    show_help = show_help_animating;
                    help_brush.Opacity = 0;
                    show_help_timer.Stop();
                }
            }
        }
        public void show_help_fnc(bool show)
        {
            show_tooltip = show;
            /*show_help_animating = true;
            show_help = show;
            help_time_start = timer.ElapsedMilliseconds;
            show_help_timer.Start();*/
        }
        void resources_ui_Drop(object sender, DragEventArgs e)
        {
            if (calculating)
            {
                UI_locked_message_box();
                return;
            }
            pointer_inside_image_check(out context_menu_cat_index, out context_menu_im_index,e.GetPosition(this));
            if (e.Data is System.Windows.DataObject &&((System.Windows.DataObject)e.Data).ContainsFileDropList())
            {
                bool ctrl = System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl);
                if (context_menu_cat_index == -2&&!ctrl)
                    add_category();
                foreach (string filePath in ((System.Windows.DataObject)e.Data).GetFileDropList())
                {
                    if (context_menu_cat_index == -2)
                    {
                        if (ctrl)
                        {
                            add_category();
                            add_image(_scene.images.Count - 1,filePath);
                        }
                        else
                            add_image(_scene.images.Count - 1, filePath);
                        
                    }
                    if(context_menu_cat_index==-1)
                    {
                        if(context_menu_im_index!=-1)
                        {
                            insert_image_pool(context_menu_im_index, filePath);
                        }
                        else
                        {
                            add_image_pool(filePath);
                        }
                    }
                    if (context_menu_cat_index >-1)
                    {
                        if (context_menu_im_index != -1)
                        {
                            insert_image(context_menu_cat_index,context_menu_im_index, filePath);
                        }
                        else
                        {
                            add_image(context_menu_cat_index,filePath);
                        }
                    }
                }
            }
            reinitialize_screen_space_buffers();
            generate_screen_space();
            scroll_bars_resize();
            render();
        }
        void resources_ui_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            render();
        }
        void render_timer_Tick(object sender, EventArgs e)
        {
            render();
        }
        public void resources_ui_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (calculating)
                {
                    UI_locked_message_box();
                    return;
                }
                image_menu_delete_image(); 
                render();
            }
        }
        void timer_move_pool_down_Tick(object sender, EventArgs e)
        {
            if (scroll_pool == pool_scroll_bar_max)
                return;
            if (scroll_pool + scroll_speed_pool > pool_scroll_bar_max)
            {
                scroll_pool = (float)pool_scroll_bar_max;
            }
            else
            {
                scroll_pool += (float)scroll_speed_pool;
                if(moving_screen_space_start.X>divider_position)
                    selection_start_y -= (float)scroll_speed_pool;
            }
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { scroll_bar_pool.Value = scroll_pool; generate_screen_space(); render(); }));
        }
        void timer_move_pool_up_Tick(object sender, EventArgs e)
        {
            if (scroll_pool == 0)
                return;
            if (scroll_pool - scroll_speed_pool < 0)
            {
                scroll_pool = 0;
            }
            else
            {
                scroll_pool -= (float)scroll_speed_pool;
                if(moving_screen_space_start.X>divider_position)
                    selection_start_y += (float)scroll_speed_pool;
            }
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { scroll_bar_pool.Value = scroll_pool; generate_screen_space(); render(); }));
        }
        void timer_move_scene_down_Tick(object sender, EventArgs e)
        {
            if (scroll_scene == scene_scroll_bar_max)
                return;
            if (scroll_scene + scroll_speed_scene > scene_scroll_bar_max)
            {
                scroll_scene = (float)scene_scroll_bar_max;
                //scroll_scene = (float)scene_scroll_bar_max;
            }
            else
            {
                //scroll_bar_scene.Value += scroll_speed_scene;
                scroll_scene += (float)scroll_speed_scene;
                if (moving_screen_space_start.X < divider_position)
                    selection_start_y -= (float)scroll_speed_scene;
            }
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { scroll_bar_scene.Value = scroll_scene; generate_screen_space(); render(); }));
        }
        void timer_move_scene_up_Tick(object sender, EventArgs e)
        {
            if (scroll_scene == 0)
                return;
            if (scroll_scene - scroll_speed_scene < 0)
            {
                scroll_scene = 0;
            }
            else
            {
                scroll_scene -= (float) scroll_speed_scene;
                if(moving_screen_space_start.X<divider_position)
                    selection_start_y += (float)scroll_speed_scene;
            }
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { scroll_bar_scene.Value = scroll_scene; generate_screen_space(); render(); }));
            
            
            
        }
        public void assign_scroll_bars(System.Windows.Controls.Primitives.ScrollBar scroll_bar_scene, System.Windows.Controls.Primitives.ScrollBar scroll_bar_pool)
        {
            this.scroll_bar_scene = scroll_bar_scene;
            this.scroll_bar_pool = scroll_bar_pool;
            this.scroll_bar_pool.Scroll+=scroll_bar_pool_Scroll;
            this.scroll_bar_scene.Scroll += scroll_bar_scene_Scroll;
            this.scroll_bar_scene.Value = 0;
            this.scroll_bar_pool.Value = 0;
        }
        void scroll_bar_pool_Scroll(object sender, System.EventArgs e)
        {
            scroll_pool = (float) scroll_bar_pool.Value;
            generate_screen_space();
            render();
        }
        void scroll_bar_scene_Scroll(object sender, System.EventArgs e)
        {
            scroll_scene = (float) scroll_bar_scene.Value;
            generate_screen_space();
            render();
        }
        private Color4 random_pastel_color()
        {
            return new Color4((float)rand.NextDouble() * 0.5f + 0.5f, (float)rand.NextDouble() * 0.5f + 0.5f, (float)rand.NextDouble() * 0.5f + 0.5f, 1f);
        }
        private Color4 color4_from_base_255(float red,float green,float blue)
        {
            return new Color4(red / 255f, green / 255f, blue / 255f, 1f);
        }
        private Color4 random_color_from_original(float original_weight,Color4 original_color)
        {
            float weight_inv = 1f - original_weight;
            return new Color4((float)rand.NextDouble() * weight_inv + original_weight * original_color.Red, (float)rand.NextDouble() * weight_inv + original_weight * original_color.Green, (float)rand.NextDouble() * weight_inv + original_weight * original_color.Blue, 1f);
        }
        ~resources_ui()  // destructor
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
            try
            {
                /*Dispatcher.Invoke(new Action(() =>
                {
                    Keyboard.Focus(this as UIElement);
                    this.Focus();
                    Keyboard.Focus(this as UIElement);
                    this.Focus();
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);*/
            }
            catch (Exception)
            {
                
                //throw;
            }
            
            //System.Windows.Media.CompositionTarget.Rendering += render_wpf;
            //render_timer.Start();
            //this.CreateAndBindTargets();
            Dispatcher.BeginInvoke(new Action(delegate
            {
                //render_timer.Start();
                this.CreateAndBindTargets();
                update_divider();
                generate_screen_space();
                scroll_bars_resize();
                render();
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle, null);
        }
        private void render_wpf(object sender, System.EventArgs e)
        {
            D3DSurface.Lock();
            D3DSurface.AddDirtyRect(new Int32Rect(0, 0, targetWidth, targetHeight));
            D3DSurface.Unlock();
        }
        private void Window_Unloaded()
        {
            if (matching_ui.IsInDesignMode)
                return;
            //render_timer.Stop();
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
        void scroll_bars_resize()
        {
            float res;
            if (categories_screen_space.Length == 0)
                res = 0;
            else
                res = categories_screen_space[categories_screen_space.Length - 1].Bottom - targetHeight + scroll_scene + min_category_height;
            bool changed = false;
            float p=0.25f;
            if(res>0)
            {
                if (scroll_bar_scene.Visibility != Visibility.Visible)
                    changed = true;
                scroll_bar_scene.Visibility = Visibility.Visible;
                if (res < scroll_bar_scene.Value)
                {
                    scroll_bar_scene.Value = res;
                    scroll_scene = res;
                }
                if (scroll_scene > res)
                    scroll_scene = res;
                scroll_bar_scene.Maximum = res;
                left_margin = 15;
                scroll_bar_scene.ViewportSize = (res) * p / (1 - p);
                scene_scroll_bar_max = scroll_bar_scene.Maximum;
            }
            else
            {
                if (scroll_bar_scene.Visibility == Visibility.Visible)
                {
                    changed = true;
                    scroll_bar_scene.Value = 0;
                    scroll_scene = 0;
                    scene_scroll_bar_max = 0;
                }
                scroll_bar_scene.Visibility = Visibility.Hidden;
                left_margin = 0;
            }
            if (pool_images_screen_space.Length == 0)
                res = 0;
            else
                res = pool_images_screen_space[pool_images_screen_space.Length - 1].Bottom - targetHeight + scroll_pool;
            if (res > 0)
            {
                if (scroll_bar_pool.Visibility != Visibility.Visible)
                {
                    changed = true;
                }
                scroll_bar_pool.Visibility = Visibility.Visible;
                if (res < scroll_bar_scene.Value)
                {
                    scroll_bar_pool.Value = res;
                    scroll_pool = res;
                }
                if (scroll_pool > res)
                    scroll_pool = res;
                scroll_bar_pool.Maximum = res;
                right_margin = 15;
                scroll_bar_pool.ViewportSize = (res) * p / (1 - p);
                pool_scroll_bar_max = scroll_bar_pool.Maximum;
            }
            else
            {
                if (scroll_bar_pool.Visibility == Visibility.Visible)
                {
                    changed = true;
                    scroll_bar_pool.Value = 0;
                    scroll_pool = 0;
                    pool_scroll_bar_max = 0;
                }
                scroll_bar_pool.Visibility = Visibility.Hidden;
                right_margin = 0;
            }
            //if (changed)
            generate_screen_space();
            
            
        }
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            //render_timer.Stop();
            this.CreateAndBindTargets();
            update_divider();
            generate_screen_space();
            scroll_bars_resize();
            //Dispatcher.BeginInvoke(EmptyDelegate, System.Windows.Threading.DispatcherPriority.Render, null);
            base.OnRenderSizeChanged(sizeInfo);
            render();
            //render_timer.Start();
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
        void pointer_inside_image_check(out int cat_index,out int im_index, System.Windows.Point check_pos = new System.Windows.Point())
        {
            cat_index = -2;
            im_index = 0;
            System.Windows.Point pos = check_pos;
            if (pos.X == 0 && pos.Y == 0)
                pos = System.Windows.Input.Mouse.GetPosition(this);

            int cat_count = images_screen_space.Length;

            if (pos.X<divider_position)
            {
                for (int i = 0; i < cat_count; i++)
                {
                    bool selected_cat = false;
                    if (pos.Y<categories_screen_space[i].Bottom&&pos.Y>categories_screen_space[i].Top)
                    {
                        selected_cat = true;
                        cat_index = i;
                        im_index = -1;
                    }
                    if (selected_cat)
                    {
                        int img_count = images_screen_space[i].Length;
                        for (int j = 0; j < img_count; j++)
                        {
                            if (pos.X > images_screen_space[i][j].Left && pos.X < images_screen_space[i][j].Right && pos.Y > images_screen_space[i][j].Top && pos.Y < images_screen_space[i][j].Bottom)
                            {
                                im_index = j;
                                return;
                            }
                        }
                        return;
                    }
                }
            }
            else
            {
                cat_index = -1;
                im_index = -1;
                int img_count_pool = pool_images_screen_space.Length;
                for (int i = 0; i < img_count_pool; i++)
                {
                    if (pos.X > pool_images_screen_space[i].Left && pos.X < pool_images_screen_space[i].Right && pos.Y > pool_images_screen_space[i].Top && pos.Y < pool_images_screen_space[i].Bottom)
                    {
                        im_index = i;
                    }
                }
            }
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

            RenderTargetProperties rtp = new RenderTargetProperties(new PixelFormat(Format.Unknown, SharpDX.Direct2D1.AlphaMode.Premultiplied));

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
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                System.Windows.Point pos = System.Windows.Input.Mouse.GetPosition(sender as IInputElement);



                if (pos.X - divider_width * divider_factor < divider_position && divider_position < pos.X + divider_width * divider_factor)
                {
                    move_divider = true;
                }
                else
                {
                    int dest_cat_index;
                    int dest_im_index;
                    pointer_inside_image_check(out dest_cat_index, out dest_im_index);

                    if (dest_cat_index == -2 || dest_im_index==-1)
                    {
                        selection_start_x = (float)pos.X;
                        selection_start_y = (float)pos.Y;
                        selection_end_x = selection_start_x;
                        selection_end_y = selection_start_y;
                        selecting = true;
                    }
                    else if (dest_cat_index == -1)
                    {
                        if (!_scene.images_pool[dest_im_index].selected)
                        {
                            selection_start_x = (float)pos.X;
                            selection_start_y = (float)pos.Y;
                            selection_end_x = selection_start_x;
                            selection_end_y = selection_start_y;
                            selecting = true;
                        }
                        else
                        {
                            moving_screen_space_start = Mouse.GetPosition(this);
                            moving_screen_space_end = Mouse.GetPosition(this);

                            if (calculating)
                            {
                                UI_locked_message_box();
                                return;
                            }

                            moving = true;
                            System.Windows.Input.Mouse.Capture(this);
                            start_moving();
                        }
                    }
                    else
                    {
                        if (_scene.images[dest_cat_index][dest_im_index].selected)
                        {
                            moving_screen_space_start = Mouse.GetPosition(this);
                            moving_screen_space_end = Mouse.GetPosition(this);
                            if (calculating)
                            {
                                UI_locked_message_box();
                                return;
                            }
                            moving = true;
                            System.Windows.Input.Mouse.Capture(this);
                            start_moving();
                        }
                        else
                        {
                            selection_start_x = (float)pos.X;
                            selection_start_y = (float)pos.Y;
                            selection_end_x = selection_start_x;
                            selection_end_y = selection_start_y;
                            selecting = true;
                        }
                    }
                }
                System.Windows.Input.Mouse.Capture(this);
                //render();
            }
            if (e.ChangedButton == System.Windows.Input.MouseButton.Right && e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                pointer_inside_image_check(out context_menu_cat_index, out context_menu_im_index);
                if(context_menu_cat_index==-2)
                {
                    if (calculating)
                    {
                        (category_view_menu.Items[0] as System.Windows.Controls.MenuItem).IsEnabled = false;
                        (category_view_menu.Items[1] as System.Windows.Controls.MenuItem).IsEnabled = false;
                        (category_view_menu.Items[2] as System.Windows.Controls.MenuItem).IsEnabled = false;
                        (category_view_menu.Items[3] as System.Windows.Controls.MenuItem).IsEnabled = false;
                    }
                    else
                    {
                        (category_view_menu.Items[0] as System.Windows.Controls.MenuItem).IsEnabled = true;
                        (category_view_menu.Items[1] as System.Windows.Controls.MenuItem).IsEnabled = true;
                        (category_view_menu.Items[2] as System.Windows.Controls.MenuItem).IsEnabled = true;
                        (category_view_menu.Items[3] as System.Windows.Controls.MenuItem).IsEnabled = true;
                    }
                    category_view_menu.PlacementTarget = this;
                    category_view_menu.IsOpen = true;
                    return;
                }
                else
                if (context_menu_im_index == -1)
                {
                    if (context_menu_cat_index==-1)
                    {
                        if (calculating)
                        {
                            (image_pool_menu.Items[0] as System.Windows.Controls.MenuItem).IsEnabled = false;
                            (image_pool_menu.Items[1] as System.Windows.Controls.MenuItem).IsEnabled = false;
                        }
                        else
                        {
                            (image_pool_menu.Items[0] as System.Windows.Controls.MenuItem).IsEnabled = true;
                            (image_pool_menu.Items[1] as System.Windows.Controls.MenuItem).IsEnabled = true;
                        }
                        image_pool_menu.PlacementTarget = this;
                        image_pool_menu.IsOpen = true;
                    }
                    else
                    {
                        if (calculating)
                        {
                            (category_menu.Items[0] as System.Windows.Controls.MenuItem).IsEnabled = false;
                            (category_menu.Items[1] as System.Windows.Controls.MenuItem).IsEnabled = false;
                            (category_menu.Items[2] as System.Windows.Controls.MenuItem).IsEnabled = false;
                            (category_menu.Items[3] as System.Windows.Controls.MenuItem).IsEnabled = false;
                        }
                        else
                        {
                            (category_menu.Items[0] as System.Windows.Controls.MenuItem).IsEnabled = true;
                            (category_menu.Items[1] as System.Windows.Controls.MenuItem).IsEnabled = true;
                            (category_menu.Items[2] as System.Windows.Controls.MenuItem).IsEnabled = true;
                            (category_menu.Items[3] as System.Windows.Controls.MenuItem).IsEnabled = true;
                        }
                        category_menu.PlacementTarget = this;
                        category_menu.IsOpen = true;
                    }
                    
                    
                    return;
                }
                else
                {
                    add_preview_selection();
                    int selected_count = number_of_selected();
                    if (selected_count>1)
                    {
                        if (calculating)
                        {
                            (image_menu.Items[0] as System.Windows.Controls.MenuItem).IsEnabled = false;
                            (image_menu.Items[1] as System.Windows.Controls.MenuItem).IsEnabled = false;
                            (image_menu.Items[2] as System.Windows.Controls.MenuItem).IsEnabled = false;
                        }
                        else
                        {
                            (image_menu.Items[0] as System.Windows.Controls.MenuItem).IsEnabled = false;
                            (image_menu.Items[1] as System.Windows.Controls.MenuItem).IsEnabled = true;
                            (image_menu.Items[2] as System.Windows.Controls.MenuItem).IsEnabled = false;
                        }
                        (image_menu.Items[1] as System.Windows.Controls.MenuItem).Header = "Delete images";
                    }
                    else
                    {
                        if (calculating)
                        {
                            (image_menu.Items[0] as System.Windows.Controls.MenuItem).IsEnabled = false;
                            (image_menu.Items[1] as System.Windows.Controls.MenuItem).IsEnabled = false;
                            (image_menu.Items[2] as System.Windows.Controls.MenuItem).IsEnabled = false;
                        }
                        else
                        {
                            (image_menu.Items[0] as System.Windows.Controls.MenuItem).IsEnabled = true;
                            (image_menu.Items[1] as System.Windows.Controls.MenuItem).IsEnabled = true;
                            (image_menu.Items[2] as System.Windows.Controls.MenuItem).IsEnabled = true;
                        }
                        (image_menu.Items[1] as System.Windows.Controls.MenuItem).Header = "Delete image";
                    }
                    image_menu.PlacementTarget = this;
                    image_menu.IsOpen = true;
                    return;
                }
            }

        }
        int move_count;
        void DPFCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!mouse_interaction_enabled)
                return;
            if (tooltip.IsOpen)
            {
                move_count++;
                if (move_count > 5)
                {
                    tooltip.IsOpen = false;
                    move_count = 0;
                }
            }
            if (show_tooltip)
            {
                tooltip_timer.Stop();
                tooltip_timer.Start();
            }
            System.Windows.Point pos = System.Windows.Input.Mouse.GetPosition(sender as IInputElement);
            if (move_divider)
            {
                float res = (float)(targetWidth - pos.X);
                divider_position_to_right = res;
                if (divider_position_to_right < pool_min_space)
                    divider_position_to_right = pool_min_space;
                if (pos.X < scene_min_space)
                    divider_position_to_right = (float)(targetWidth - pool_min_space);
                   
                update_divider();
                generate_screen_space();
                scroll_bars_resize();
                render();
                return;
            }
            if (selecting)
            {
                
                if (pos.X < divider_position)
                {
                    if (scroll_pool_up)
                    {
                        timer_move_pool_up.Stop();
                        scroll_pool_up = false;
                    }
                    if (scroll_pool_down)
                    {
                        timer_move_pool_down.Stop();
                        scroll_pool_down = false;
                    }
                    if (pos.Y < move_marg)
                    {
                        if (!scroll_scene_up)
                        {
                            timer_move_scene_up.Start();
                            scroll_scene_up = true;
                        }
                    }
                    else
                    {
                        if (scroll_scene_up)
                        {
                            timer_move_scene_up.Stop();
                            scroll_scene_up = false;
                        }
                    }
                    if (pos.Y > targetHeight - move_marg)
                    {
                        if (!scroll_scene_down)
                        {
                            timer_move_scene_down.Start();
                            scroll_scene_down = true;
                        }
                    }
                    else
                    {
                        if (scroll_scene_down)
                        {
                            timer_move_scene_down.Stop();
                            scroll_scene_down = false;
                        }
                    }
                }
                else
                {
                    if (scroll_scene_up)
                    {
                        timer_move_scene_up.Stop();
                        scroll_scene_up = false;
                    }
                    if (scroll_scene_down)
                    {
                        timer_move_scene_down.Stop();
                        scroll_scene_down = false;
                    }
                    if (pos.Y < move_marg)
                    {
                        if (!scroll_pool_up)
                        {
                            timer_move_pool_up.Start();
                            scroll_pool_up = true;
                        }
                    }
                    else
                    {
                        if (scroll_pool_up)
                        {
                            timer_move_pool_up.Stop();
                            scroll_pool_up = false;
                        }
                    }
                    if (pos.Y > targetHeight - move_marg)
                    {
                        if (!scroll_pool_down)
                        {
                            timer_move_pool_down.Start();
                            scroll_pool_down = true;
                        }
                    }
                    else
                    {
                        if (scroll_pool_down)
                        {
                            timer_move_pool_down.Stop();
                            scroll_pool_down = false;
                        }
                    }
                }
                selection_end_x = (float)pos.X;
                selection_end_y = (float)pos.Y;
                generate_screen_space();
                preview_selection();
                render();
                return;
                //this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, EmptyDelegate);
            }
            if(moving)
            {
                if (pos.X < divider_position)
                {
                    if (scroll_pool_up)
                    {
                        timer_move_pool_up.Stop();
                        scroll_pool_up = false;
                    }
                    if (scroll_pool_down)
                    {
                        timer_move_pool_down.Stop();
                        scroll_pool_down = false;
                    }
                    if (pos.Y < move_marg)
                    {
                        if (!scroll_scene_up)
                        {
                            timer_move_scene_up.Start();
                            scroll_scene_up = true;
                        }
                    }
                    else
                    {
                        if (scroll_scene_up)
                        {
                            timer_move_scene_up.Stop();
                            scroll_scene_up = false;
                        }
                    }
                    if (pos.Y > targetHeight - move_marg)
                    {
                        if (!scroll_scene_down)
                        {
                            timer_move_scene_down.Start();
                            scroll_scene_down = true;
                        }
                    }
                    else
                    {
                        if (scroll_scene_down)
                        {
                            timer_move_scene_down.Stop();
                            scroll_scene_down = false;
                        }
                    }
                }
                else
                {
                    if (scroll_scene_up)
                    {
                        timer_move_scene_up.Stop();
                        scroll_scene_up = false;
                    }
                    if (scroll_scene_down)
                    {
                        timer_move_scene_down.Stop();
                        scroll_scene_down = false;
                    }
                    if (pos.Y < move_marg)
                    {
                        if (!scroll_pool_up)
                        {
                            timer_move_pool_up.Start();
                            scroll_pool_up = true;
                        }
                    }
                    else
                    {
                        if (scroll_pool_up)
                        {
                            timer_move_pool_up.Stop();
                            scroll_pool_up = false;
                        }
                    }
                    if (pos.Y > targetHeight - move_marg)
                    {
                        if (!scroll_pool_down)
                        {
                            timer_move_pool_down.Start();
                            scroll_pool_down = true;
                        }
                    }
                    else
                    {
                        if (scroll_pool_down)
                        {
                            timer_move_pool_down.Stop();
                            scroll_pool_down = false;
                        }
                    }
                }
                moving_screen_space_end = pos;
                move_images();
                render();
            }
            if (!move_divider)
            {
                if (pos.X - divider_width * divider_factor < divider_position && divider_position < pos.X + divider_width * divider_factor)
                    this.Cursor = Cursors.SizeWE;
                else
                    this.Cursor = Cursors.Arrow;
            }
            if(preview_selection())
                render();
        }
        void DPFCanvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!mouse_interaction_enabled)
                return;
            if (move_divider || moving || selecting)
                Mouse.Capture(null);

            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) && selecting && e.ClickCount == 1)
                add_preview_selection();
            else
                if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftAlt) && selecting && e.ClickCount == 1)
                    remove_preview_selection();
                else
                    if (selecting && e.ClickCount == 1)
                        select_preview_selection();
            if (moving)
            {
                end_moving();
                moving = false;
            }
            if (scroll_scene_up)
                timer_move_scene_up.Stop();
            if (scroll_scene_down)
                timer_move_scene_down.Stop();
            if (scroll_pool_up)
                timer_move_pool_up.Stop();
            if (scroll_pool_down)
                timer_move_pool_down.Stop();
            
            move_divider = false;
            selecting = false;
            
            render();
        }
        void DPFCanvas_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            System.Windows.Point pos = System.Windows.Input.Mouse.GetPosition(sender as IInputElement);
            if (!mouse_interaction_enabled)
                return;
            bool shift = System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift);
            if (shift)
            {
                if (pos.X < divider_position)
                {
                    float cat_width = divider_position - left_margin;
                    double scene_image_per_stair = Math.Round(cat_width / (desired_scene_image_width));
                    if (e.Delta < 0)
                    {
                        desired_scene_image_width = cat_width / ((float)(scene_image_per_stair + 1)) - 1f;
                    }
                    else
                    {
                        if (scene_image_per_stair > 1)
                            desired_scene_image_width = cat_width / ((float)(scene_image_per_stair - 1)) + 1f;
                    }
                    if (desired_scene_image_width < desired_scene_image_width_min)
                        desired_scene_image_width = desired_scene_image_width_min;
                    if (desired_scene_image_width > targetWidth - divider_position_to_right)
                        desired_scene_image_width = targetWidth - divider_position_to_right;
                }
                else
                {
                    float cat_width = divider_position_to_right - right_margin;
                    double scene_image_per_stair = Math.Round(cat_width / (desired_pool_image_width));
                    if (e.Delta < 0)
                    {
                        desired_pool_image_width = cat_width / ((float)(scene_image_per_stair + 1)) - 1f;
                    }
                    else
                    {
                        if (scene_image_per_stair > 1)
                            desired_pool_image_width = cat_width / ((float)(scene_image_per_stair - 1)) + 1f;
                    }
                    if (desired_pool_image_width < desired_pool_image_width_min)
                        desired_pool_image_width = desired_pool_image_width_min;
                    if (desired_pool_image_width > divider_position_to_right)
                        desired_pool_image_width = divider_position_to_right;
                }
            }
            else
            {
                float move=20;
                if (e.Delta > 0)
                {
                    move *= -1;
                }
                if (pos.X < divider_position)
                {
                    if (scroll_bar_scene.Value + move < 0)
                    {
                        scroll_bar_scene.Value = 0;
                        scroll_scene = 0;
                    }
                    else if (scroll_bar_scene.Value + move > scroll_bar_scene.Maximum)
                    {
                        scroll_bar_scene.Value = scroll_bar_scene.Maximum;
                        scroll_scene = (float) scroll_bar_scene.Maximum;
                    }
                    else
                    {
                        scroll_bar_scene.Value += move;
                        scroll_scene += move;
                    }
                }
                else
                {
                    if (scroll_bar_pool.Value + move < 0)
                    {
                        scroll_bar_pool.Value = 0;
                        scroll_pool = 0;
                    }
                    else if (scroll_bar_pool.Value + move > scroll_bar_pool.Maximum)
                    {
                        scroll_bar_pool.Value = scroll_bar_pool.Maximum;
                        scroll_pool = (float)scroll_bar_pool.Maximum;
                    }
                    else
                    {
                        scroll_bar_pool.Value += move;
                        scroll_pool += move;
                    }
                }
            }
            generate_screen_space();
            scroll_bars_resize();
            render();
        }
        //context menu
        void context_menu_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(sender as System.Windows.Controls.ContextMenu).IsOpen)
            {
                mouse_interaction_enabled = true;
            }
            else
            {
                mouse_interaction_enabled = false;
            }
        }
        void image_menu_add_image()
        {
            //draw = false;
            if ((bool)ofd.ShowDialog())
            {
                if (context_menu_cat_index > -1)
                    foreach (String file in ofd.FileNames)
                        insert_image(context_menu_cat_index, context_menu_im_index, file);
                else
                    foreach (String file in ofd.FileNames)
                        insert_image_pool(context_menu_im_index, file);
            }
            reinitialize_screen_space_buffers();
            generate_screen_space();
            scroll_bars_resize();
            //draw = true;
            render();
        }
        void image_menu_delete_image()
        {
            
            draw = false;
            bool[] remove_cat = new bool[_scene.images.Count];
            for (int i = 0; i < _scene.images.Count; i++)
            {
                remove_cat[i] = false;
                for (int j = _scene.images[i].Count-1; j > -1; j--)
                {
                    if (_scene.images[i][j].selected)
                        remove_image(i, j);
                    if (_scene.images[i].Count == 0)
                        remove_cat[i] = true;
                }
            }
            for (int i = _scene.images_pool.Count - 1; i > -1; i--)
            {
                if (_scene.images_pool[i].selected)
                    remove_image_pool(i);
            }
            for (int i = _scene.images.Count - 1; i > -1; i--)
                if (remove_cat[i])
                    remove_category(i);
           
            reinitialize_screen_space_buffers();
            generate_screen_space();
            scroll_bars_resize();
            
            draw = true;
            render();
        }
        void image_menu_replace_image()
        {
            draw = false;
            if ((bool)ofd.ShowDialog())
            {
                if (context_menu_cat_index > -1)
                {
                    remove_image(context_menu_cat_index, context_menu_im_index);
                    insert_image(context_menu_cat_index, context_menu_im_index, ofd.FileNames[0]);
                }
                else
                {
                    remove_image_pool(context_menu_im_index);
                    insert_image_pool(context_menu_im_index, ofd.FileNames[0]);
                }
            }
            reinitialize_screen_space_buffers();
            generate_screen_space();
            scroll_bars_resize();
            draw = true;
            render();
        }
        void category_menu_add_image()
        {
            //draw = false;
            if ((bool)ofd.ShowDialog())
            {
                foreach (String file in ofd.FileNames)
                    add_image(context_menu_cat_index, file);
            }
            reinitialize_screen_space_buffers();
            generate_screen_space();
            scroll_bars_resize();
            render();
            //draw = true;
        }
        void image_pool_menu_add_image()
        {
            //draw = false;
            if ((bool)ofd.ShowDialog())
            {
                foreach (String file in ofd.FileNames)
                    add_image_pool(file);
            }
            reinitialize_screen_space_buffers();
            generate_screen_space();
            scroll_bars_resize();
            render();
            //draw = true;
        }
        void category_menu_add_category()
        {
            draw = false;
            insert_category(context_menu_cat_index);
            reinitialize_screen_space_buffers();
            generate_screen_space();
            scroll_bars_resize();
            draw = true;
            render();
        }
        void category_menu_delete_category()
        {
            draw = false;
            /*if (_scene.images.Count < 3)
            {
                MessageBox.Show("You cannot delete this category because there always have to be at least two categories left in the scene", "Image match", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                draw = true;
                return;
            }
            else
            {*/
                remove_category(context_menu_cat_index);
                reinitialize_screen_space_buffers();
                generate_screen_space();
                scroll_bars_resize();
            //}
            draw = true;
            render();
        }
        void category_menu_color_category()
        {
            //draw = false;
            //render();
            //render_timer.Stop();
            if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _scene.categories_brush[context_menu_cat_index].Color = color4_from_base_255(cd.Color.R, cd.Color.G, cd.Color.B);
            }
            //generate_screen_space();
            //draw = true;
            render();
        }
        void image_pool_menu_color_category()
        {
            //draw = false;
            //render();
            //render_timer.Stop();
            if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                pool_brush.Color = color4_from_base_255(cd.Color.R, cd.Color.G, cd.Color.B);
            }
            //generate_screen_space();
            //draw = true;
            render();
        }
        void viewport_menu_add_category()
        {
            draw = false;
            add_category();
            reinitialize_screen_space_buffers();
            generate_screen_space();
            scroll_bars_resize();
            draw = true;
            render();
        }
        void viewport_menu_add_to_1_category()
        {
            draw = false;
            add_category();
            if ((bool)ofd.ShowDialog())
            {
                foreach (String file in ofd.FileNames)
                    add_image(_scene.images.Count - 1, file);
            }
            reinitialize_screen_space_buffers();
            generate_screen_space();
            scroll_bars_resize();
            draw = true;
            render();
        }
        void viewport_menu_add_to_multiple_category()
        {
            draw = false;
            if ((bool)ofd.ShowDialog())
            {
                foreach (String file in ofd.FileNames)
                {
                    add_category();
                    add_image(_scene.images.Count - 1, file);
                }
            }
            reinitialize_screen_space_buffers();
            generate_screen_space();
            scroll_bars_resize();
            draw = true;
            render();
        }
        void viewport_menu_remove_zero_category()
        {
            draw = false;
            //System.Threading.Thread.Sleep(100);
            for (int i = _scene.images.Count - 1; i > -1; i--)
            {
                if (_scene.images[i].Count == 0)
                {
                    /*if (_scene.images.Count < 3)
                    {
                        reinitialize_screen_space_buffers();
                        generate_screen_space();
                        scroll_bars_resize();
                        draw = true;
                        MessageBox.Show("You cannot delete this category because there always have to be at least two categories present in the scene", "Error while deleting category", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }*/
                    remove_category(i);
                }
            }
            reinitialize_screen_space_buffers();
            generate_screen_space();
            scroll_bars_resize();
            draw = true;
            render();
        }
        void context_menu_click(object sender, System.EventArgs e)
        {
            switch ((string)(((System.Windows.Controls.MenuItem)sender).Name))
            {
                case "image_menu_add_image":
                    image_menu_add_image();
                    break;
                case "image_menu_delete_image":
                    image_menu_delete_image();
                    break;
                case "image_menu_replace_image":
                    image_menu_replace_image();
                    break;
                case "category_menu_add_image":
                    category_menu_add_image();
                    break;
                case "category_menu_add_category":
                    category_menu_add_category();
                    break;
                case "category_menu_delete_category":
                    category_menu_delete_category();
                    break;
                case "category_menu_color_category":
                    category_menu_color_category();
                    break;
                case "viewport_menu_add_category":
                    viewport_menu_add_category();
                    break;
                case "viewport_menu_add_to_1_category":
                    viewport_menu_add_to_1_category();
                    break;
                case "viewport_menu_add_to_multiple_category":
                    viewport_menu_add_to_multiple_category();
                    break;
                case "viewport_menu_remove_zero_category":
                    viewport_menu_remove_zero_category();
                    break;
                case "image_pool_menu_add_image":
                    image_pool_menu_add_image();
                    break;
                case "image_pool_menu_color_category":
                    image_pool_menu_color_category();
                    break;
            }
        }
        //rendering
        private void update_divider()
        {
            
            divider_position = targetWidth - divider_position_to_right;
            if (divider_position < scene_min_space)
                divider_position = scene_min_space;
            divider_screen_space.Left = divider_position - divider_width;
            divider_screen_space.Top = 0;
            divider_screen_space.Right = divider_position + divider_width;
            divider_screen_space.Bottom = targetHeight;

        }
        void generate_screen_space()
        {
            float start_time2 = timer.ElapsedMilliseconds;
            int cat_count = categories_screen_space.Length;
            float cat_width = divider_position - left_margin;
            int scene_image_per_stair = (int) Math.Round(cat_width / (desired_scene_image_width));
            if (scene_image_per_stair < 1)
            {
                scene_image_per_stair = 1;
            }
            float actual_scene_image_width = (float)(cat_width - margin * (scene_image_per_stair + 1)) / (float)scene_image_per_stair;
            if (scene_image_per_stair == 0)
            {
                scene_image_per_stair = 1;
                actual_scene_image_width = cat_width - 2 * margin;
            }
            
            for (int i = 0; i < cat_count; i++)
            {
                float bottom;
                if (i == 0)
                    bottom = -scroll_scene;
                else
                    bottom =categories_screen_space[i - 1].Bottom;
                if (images_screen_space[i].Length == 0)
                {
                    categories_screen_space[i].Left = 0f;
                    categories_screen_space[i].Top = bottom;
                    categories_screen_space[i].Right = divider_screen_space.Left;
                    categories_screen_space[i].Bottom = categories_screen_space[i].Top + min_category_height;
                }
                else
                {
                    categories_screen_space[i].Left = 0f;
                    categories_screen_space[i].Right = divider_screen_space.Left;
                    categories_screen_space[i].Top = bottom;
                    int img_count = images_screen_space[i].Length;
                    int stairs = img_count / scene_image_per_stair;
                    if (img_count % scene_image_per_stair!=0)
                        stairs++;
                    float height_acumulate = margin;
                    for (int j = 0; j < stairs; j++)
                    {
                        float max_height = float.MinValue;
                        int scene_image_per_scene_current = scene_image_per_stair;
                        if (j == stairs - 1 && img_count % scene_image_per_stair != 0)
                            scene_image_per_scene_current = img_count % scene_image_per_stair;
                        for (int u = 0; u < scene_image_per_scene_current; u++)
                        {
                            float zoom = actual_scene_image_width / (float)_scene.images[i][j * scene_image_per_stair+u].width;
                            float res=_scene.images[i][j * scene_image_per_stair+u].height * zoom;
                            if (res>max_height)
                            {
                                max_height = res;
                            }
                        }
                        for (int u = 0; u < scene_image_per_scene_current; u++)
                        {
                            float last_right;
                            if(u==0)
                                last_right=margin+left_margin;
                            else
                                last_right=images_screen_space[i][j * scene_image_per_stair+u-1].Right+margin;
                            float zoom = actual_scene_image_width / (float)_scene.images[i][j * scene_image_per_stair+u].width;
                            images_screen_space[i][j * scene_image_per_stair + u].Left = last_right;
                            float im_height=_scene.images[i][j * scene_image_per_stair+u].height * zoom;
                            images_screen_space[i][j * scene_image_per_stair + u].Top = bottom + height_acumulate+(max_height-im_height)/2f;
                            images_screen_space[i][j * scene_image_per_stair + u].Right = images_screen_space[i][j * scene_image_per_stair + u].Left + actual_scene_image_width;
                            images_screen_space[i][j * scene_image_per_stair + u].Bottom = images_screen_space[i][j * scene_image_per_stair + u].Top + im_height;
                        }

                        height_acumulate += max_height;
                        height_acumulate += margin;
                    }
                    
                    categories_screen_space[i].Bottom = bottom + height_acumulate;
                }
            }

            pool_screen_space.Left = divider_screen_space.Right;
            pool_screen_space.Top = 0f;
            pool_screen_space.Right = targetWidth;
            pool_screen_space.Bottom = targetHeight;

            
           
            int img_count_pool = pool_images_screen_space.Length;
            

            cat_width = divider_position_to_right - right_margin;
            scene_image_per_stair = (int)Math.Round(cat_width / (desired_pool_image_width));
            if (scene_image_per_stair<1)
            {
                scene_image_per_stair = 1;   
            }
            actual_scene_image_width = (float)(cat_width - margin * (scene_image_per_stair + 1)) / (float)scene_image_per_stair;
            int stairs_pool = img_count_pool / scene_image_per_stair;

            if (img_count_pool % scene_image_per_stair != 0)
                stairs_pool++;
            float height_acumulate_pool = margin;
            for (int j = 0; j < stairs_pool; j++)
            {
                float max_height = float.MinValue;
                int scene_image_per_scene_current = scene_image_per_stair;
                if (j == stairs_pool - 1 && img_count_pool % scene_image_per_stair != 0)
                    scene_image_per_scene_current = img_count_pool % scene_image_per_stair;
                for (int u = 0; u < scene_image_per_scene_current; u++)
                {
                    float zoom = actual_scene_image_width / (float)_scene.images_pool[j * scene_image_per_stair + u].width;
                    float res = _scene.images_pool[j * scene_image_per_stair + u].height * zoom;
                    if (res > max_height)
                    {
                        max_height = res;
                    }
                }
                for (int u = 0; u < scene_image_per_scene_current; u++)
                {
                    float last_right;
                    if (u == 0)
                        last_right = margin+divider_position;
                    else
                        last_right = pool_images_screen_space[j * scene_image_per_stair + u - 1].Right + margin;
                    float zoom = actual_scene_image_width / (float)_scene.images_pool[j * scene_image_per_stair + u].width;
                    pool_images_screen_space[j * scene_image_per_stair + u].Left = last_right;
                    float im_height = _scene.images_pool[j * scene_image_per_stair + u].height * zoom;
                    pool_images_screen_space[j * scene_image_per_stair + u].Top = -scroll_pool + height_acumulate_pool + (max_height - im_height) / 2f;
                    pool_images_screen_space[j * scene_image_per_stair + u].Right = pool_images_screen_space[j * scene_image_per_stair + u].Left + actual_scene_image_width;
                    pool_images_screen_space[j * scene_image_per_stair + u].Bottom = pool_images_screen_space[j * scene_image_per_stair + u].Top + im_height;
                }

                height_acumulate_pool += max_height;
                height_acumulate_pool += margin;
            }
        

            render_time_screen = timer.ElapsedMilliseconds - start_time2;
            
        }
        private void render1(object sender, System.EventArgs e)
        { }
        public void strokeText(string text, float x, float y, float maxWidth, SharpDX.Direct2D1.RenderTarget _surface, SharpDX.Direct2D1.Brush brush_stroke, SharpDX.Direct2D1.Brush brush_fill, SharpDX.DirectWrite.TextFormat format, float stroke_width)
        {
            // http://msdn.microsoft.com/en-us/library/windows/desktop/dd756692(v=vs.85).aspx

            // FIXME make field
            SharpDX.DirectWrite.Factory factory = new SharpDX.DirectWrite.Factory(SharpDX.DirectWrite.FactoryType.Shared);

            //SharpDX.DirectWrite.TextFormat format = new SharpDX.DirectWrite.TextFormat(factory, "Verdana", 20);
            SharpDX.DirectWrite.TextLayout layout = new SharpDX.DirectWrite.TextLayout(factory, text, format, 500, 1000);





                var render = new OutlineTextRender(_surface, brush_stroke,brush_fill,stroke_width);
                
                

                layout.Draw(render, x, y);

                //_surface.DrawTextLayout(new Vector2(x,y), layout, brush);

                //_surface.DrawGlyphRun(new DrawingPointF(x, y), run, brush, MeasuringMode.Natural);

        }
        public void render(/*object sender, System.EventArgs e*/)
        {
            /*if(targetWidth!=this.ActualWidth||targetHeight!=this.ActualHeight)
            {
                this.CreateAndBindTargets();
                update_divider();
                generate_screen_space();
                scroll_bars_resize();
            }*/
            if (draw)
            {

                //Debug.WriteLine("render start");
            
                fps = (timer.ElapsedMilliseconds - start_time);//timer.ElapsedMilliseconds - start_time;//0.9f * render_time_render + 100f / (float)(timer.ElapsedMilliseconds - start_time);
                //D3DSurface.TryLock(new Duration(new TimeSpan(0,0,0,0,5)));

                D3DSurface.Lock();
                start_time = timer.ElapsedMilliseconds;
                d2d_render_target.BeginDraw();
                d2d_render_target.Clear(background);
                bool once = true;

                
                
                int cat_count = categories_screen_space.Length;
                if ((show_help || show_help_animating) && cat_count==0)
                {
                    draw_rectangle.Left = margin + left_margin;
                    draw_rectangle.Top = margin;
                    draw_rectangle.Right = divider_position - margin;
                    draw_rectangle.Bottom = targetHeight - margin;
                    d2d_render_target.DrawText("Right mouse button click : enter context menu where you can add new category\n\nDrop images from folder : images are added into one new empty category.\n\nDrop images from folder + CTRL : put each image in separate new category.\n\nLeft mouse button : select images by box selection (inclusive - box from left to right, exclusive - box from right to left)\n\nLeft mouse button + CTRL : Add to selection\n\nLeft mouse button + ALT : remove from selection\n\nLeft mouse button + selected image : move selected images\n\nMouse wheel : scroll categories or image pool, depending on cursor position\n\nMouse wheel + SHIFT : zoom categories or image pool, depending on cursor position\n\n\nMore info : This area displays categories. Categories are used to contain images for later points and matches generating. Each category holds zero and more images. These are only compared against images in other categories, not against each other.", text_format, draw_rectangle, help_brush);
                }
                for (int i = 0; i < cat_count; i++)
                {

                    d2d_render_target.FillRectangle(categories_screen_space[i], _scene.categories_brush[i]);
                    if (_scene.images[i].Count == 0)
                    {
                        
                        draw_rectangle.Left = categories_screen_space[i].Left + margin + left_margin;
                        draw_rectangle.Top = categories_screen_space[i].Top + margin;
                        draw_rectangle.Right = categories_screen_space[i].Right - margin;
                        draw_rectangle.Bottom = categories_screen_space[i].Bottom - margin;
                        
                        line_start.Y = categories_screen_space[i].Bottom;
                        line_end.X = categories_screen_space[i].Right;
                        line_end.Y = line_start.Y;
                        d2d_render_target.DrawLine(line_start, line_end, black_brush);

                        if (show_help||show_help_animating)
                        {
                            if (once)
                            {
                                //strokeText("This is category.Categories are used to containg images for later points and matches generating. Each category holds zero and more images. These are only compared against images in other categories, not against each other.Right click to enter context menu where you can load images.", draw_rectangle.Left, draw_rectangle.Top, draw_rectangle.Width, d2d_render_target, white_brush, black_brush,text_format,2f);
                                d2d_render_target.DrawText("This is category.\nCategories are used to contain images for later points and matches generating. Each category holds zero and more images. These are only compared against images in other categories, not against each other.\n\nRight click to enter context menu where you can load images or drop them to the canvas from folder.", text_format, draw_rectangle, help_brush);
                                once = false;
                            }
                            else
                                d2d_render_target.DrawText("Right click to enter context menu where you can load images or drop them to the canvas from folder.", text_format, draw_rectangle, help_brush);
                        }
                    }
                    else
                    {
                        line_start.Y = categories_screen_space[i].Bottom;
                        line_end.X = categories_screen_space[i].Right;
                        line_end.Y = line_start.Y;
                        d2d_render_target.DrawLine(line_start, line_end, black_brush);
                        int img_count = _scene.images[i].Count;
                        for (int j = 0; j < img_count; j++)
                        {
                            d2d_render_target.DrawBitmap(_scene.images[i][j].bitmap, images_screen_space[i][j], 1f, BitmapInterpolationMode.Linear);
                        }
                    }
                }
                int img_count_pool = _scene.images_pool.Count;
                
                d2d_render_target.FillRectangle(pool_screen_space, pool_brush);
                if (show_help || show_help_animating)
                {
                    if (_scene.images_pool.Count == 0)
                    {
                        draw_rectangle.Left = pool_screen_space.Left + margin + left_margin;
                        draw_rectangle.Top = pool_screen_space.Top + margin;
                        draw_rectangle.Right = pool_screen_space.Right - margin;
                        draw_rectangle.Bottom = pool_screen_space.Bottom - margin;
                        d2d_render_target.DrawText("This is image pool.\nYou can drop spare images here for later use.", text_format, draw_rectangle, help_brush);
                    }
                }
                
                d2d_render_target.FillRectangle(divider_screen_space, black_brush);
                for (int j = 0; j < img_count_pool; j++)
                {
                    d2d_render_target.DrawBitmap(_scene.images_pool[j].bitmap, pool_images_screen_space[j], 1f, BitmapInterpolationMode.Linear);
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
                    for (int i = 0; i < cat_count; i++)
                    {
                        int img_count = _scene.images[i].Count;
                        for (int j = 0; j < img_count; j++)
                        {
                            if (_scene.images[i][j].selected_preview)
                            {
                                draw_rectangle.Left = images_screen_space[i][j].Left - 1;
                                draw_rectangle.Top = images_screen_space[i][j].Top - 1;
                                draw_rectangle.Right = images_screen_space[i][j].Right + 1;
                                draw_rectangle.Bottom = images_screen_space[i][j].Bottom + 1;
                                d2d_render_target.DrawRectangle(draw_rectangle, selection_selected_brush);
                            }
                            else
                            {
                                draw_rectangle.Left = images_screen_space[i][j].Left - 1;
                                draw_rectangle.Top = images_screen_space[i][j].Top - 1;
                                draw_rectangle.Right = images_screen_space[i][j].Right + 1;
                                draw_rectangle.Bottom = images_screen_space[i][j].Bottom + 1;
                                d2d_render_target.DrawRectangle(draw_rectangle, gray_brush);
                            }
                        }
                    }
                    img_count_pool = _scene.images_pool.Count;
                    for (int i = 0; i < img_count_pool; i++)
                    {
                        if (_scene.images_pool[i].selected_preview)
                        {
                            draw_rectangle.Left = pool_images_screen_space[i].Left - 1;
                            draw_rectangle.Top = pool_images_screen_space[i].Top - 1;
                            draw_rectangle.Right = pool_images_screen_space[i].Right + 1;
                            draw_rectangle.Bottom = pool_images_screen_space[i].Bottom + 1;
                            d2d_render_target.DrawRectangle(draw_rectangle, selection_selected_brush);
                        }
                        else
                        {
                            draw_rectangle.Left = pool_images_screen_space[i].Left - 1;
                            draw_rectangle.Top = pool_images_screen_space[i].Top - 1;
                            draw_rectangle.Right = pool_images_screen_space[i].Right + 1;
                            draw_rectangle.Bottom = pool_images_screen_space[i].Bottom + 1;
                            d2d_render_target.DrawRectangle(draw_rectangle, gray_brush);
                        }
                    }
                }
                else if (ctrl || none)
                {
                    for (int i = 0; i < cat_count; i++)
                    {
                        int img_count = _scene.images[i].Count;
                        for (int j = 0; j < img_count; j++)
                        {
                            if (_scene.images[i][j].selected || _scene.images[i][j].selected_preview)
                            {
                                draw_rectangle.Left = images_screen_space[i][j].Left - 1;
                                draw_rectangle.Top = images_screen_space[i][j].Top - 1;
                                draw_rectangle.Right = images_screen_space[i][j].Right + 1;
                                draw_rectangle.Bottom = images_screen_space[i][j].Bottom + 1;
                                d2d_render_target.DrawRectangle(draw_rectangle, selection_selected_brush);
                            }
                            else
                            {
                                draw_rectangle.Left = images_screen_space[i][j].Left - 1;
                                draw_rectangle.Top = images_screen_space[i][j].Top - 1;
                                draw_rectangle.Right = images_screen_space[i][j].Right + 1;
                                draw_rectangle.Bottom = images_screen_space[i][j].Bottom + 1;
                                d2d_render_target.DrawRectangle(draw_rectangle, gray_brush);
                            }
                        }
                    }
                    img_count_pool = _scene.images_pool.Count;
                    for (int i = 0; i < img_count_pool; i++)
                    {
                        if (_scene.images_pool[i].selected || _scene.images_pool[i].selected_preview)
                        {
                            draw_rectangle.Left = pool_images_screen_space[i].Left - 1;
                            draw_rectangle.Top = pool_images_screen_space[i].Top - 1;
                            draw_rectangle.Right = pool_images_screen_space[i].Right + 1;
                            draw_rectangle.Bottom = pool_images_screen_space[i].Bottom + 1;
                            d2d_render_target.DrawRectangle(draw_rectangle, selection_selected_brush);
                        }
                        else
                        {
                            draw_rectangle.Left = pool_images_screen_space[i].Left - 1;
                            draw_rectangle.Top = pool_images_screen_space[i].Top - 1;
                            draw_rectangle.Right = pool_images_screen_space[i].Right + 1;
                            draw_rectangle.Bottom = pool_images_screen_space[i].Bottom + 1;
                            d2d_render_target.DrawRectangle(draw_rectangle, gray_brush);
                        }
                    }
                }
                else if (alt)
                {
                    for (int i = 0; i < cat_count; i++)
                    {
                        int img_count = _scene.images[i].Count;
                        for (int j = 0; j < img_count; j++)
                        {
                            if (_scene.images[i][j].selected && !_scene.images[i][j].selected_preview)
                            {
                                draw_rectangle.Left = images_screen_space[i][j].Left - 1;
                                draw_rectangle.Top = images_screen_space[i][j].Top - 1;
                                draw_rectangle.Right = images_screen_space[i][j].Right + 1;
                                draw_rectangle.Bottom = images_screen_space[i][j].Bottom + 1;
                                d2d_render_target.DrawRectangle(draw_rectangle, selection_selected_brush);
                            }
                            else
                            {
                                draw_rectangle.Left = images_screen_space[i][j].Left - 1;
                                draw_rectangle.Top = images_screen_space[i][j].Top - 1;
                                draw_rectangle.Right = images_screen_space[i][j].Right + 1;
                                draw_rectangle.Bottom = images_screen_space[i][j].Bottom + 1;
                                d2d_render_target.DrawRectangle(draw_rectangle, gray_brush);
                            }
                        }
                    }
                    img_count_pool = _scene.images_pool.Count;
                    for (int i = 0; i < img_count_pool; i++)
                    {
                        if (_scene.images_pool[i].selected && !_scene.images_pool[i].selected_preview)
                        {
                            draw_rectangle.Left = pool_images_screen_space[i].Left - 1;
                            draw_rectangle.Top = pool_images_screen_space[i].Top - 1;
                            draw_rectangle.Right = pool_images_screen_space[i].Right + 1;
                            draw_rectangle.Bottom = pool_images_screen_space[i].Bottom + 1;
                            d2d_render_target.DrawRectangle(draw_rectangle, selection_selected_brush);
                        }
                        else
                        {
                            draw_rectangle.Left = pool_images_screen_space[i].Left - 1;
                            draw_rectangle.Top = pool_images_screen_space[i].Top - 1;
                            draw_rectangle.Right = pool_images_screen_space[i].Right + 1;
                            draw_rectangle.Bottom = pool_images_screen_space[i].Bottom + 1;
                            d2d_render_target.DrawRectangle(draw_rectangle, gray_brush);
                        }
                    }
                }
                if (moving)
                {
                    int moving_length = moving_images_world_space.Length;
                    for (int i = 0; i < moving_length; i++)
                    {
                        d2d_render_target.DrawBitmap(_scene.images_moving[i].bitmap, moving_images_screen_space[i], 0.5f, BitmapInterpolationMode.Linear);
                    }
                }
                

                draw_rectangle.Left = 20;
                draw_rectangle.Right = 500;
                draw_rectangle.Top = targetHeight - 60;
                draw_rectangle.Bottom = targetHeight;
                //d2d_render_target.DrawText("RENDER_TIME = " + render_time_render.ToString("G4") + "\nSCREEN_SPACE_TIME = " + render_time_screen.ToString("G4") + "\nWPF_RENDER_TIME = " + (fps).ToString("G4"), new SharpDX.DirectWrite.TextFormat(new SharpDX.DirectWrite.Factory(), "Arial", 14f), draw_rectangle, black_brush);
            
            d2d_render_target.EndDraw();

            d3dDevice.ImmediateContext.End(queryForCompletion);
            while (!d3dDevice.ImmediateContext.IsDataAvailable(queryForCompletion))
            {
                spin.SpinOnce();
            }

            //device.Flush();

            //wait_for_rendering_to_complete();

            D3DSurface.AddDirtyRect(new Int32Rect(0, 0, targetWidth, targetHeight));
            D3DSurface.Unlock();
            render_time_render = (int)(timer.ElapsedMilliseconds - start_time);//timer.ElapsedMilliseconds - start_time;//0.9f * render_time_render + 100f / (float)(timer.ElapsedMilliseconds - start_time);
            //Debug.WriteLine("render end");
            }
        }
        //moving
        void generate_moving_pattern()
        {
            int moving_length=moving_images_world_space.Length;
            double help = Math.Sqrt(moving_length);
            double columns_d = Math.Floor(help);
            if (columns_d != help)
                columns_d +=1;
            int columns = (int)columns_d;
            int stairs = moving_length / columns;
            int rest = moving_length % columns;
            int moving_index;
            float height_acumulate = 0;
            float moving_margin = 5;
            for (int i = 0; i < stairs; i++)
            {
               
                moving_index = i * columns;

                float max_height = float.MinValue;
                
                for (int u = 0; u < columns; u++)
                {
                    float zoom = moving_image_actual_width / (float)_scene.images_moving[moving_index].width;
                    float res = _scene.images_moving[moving_index].height * zoom;
                    if (res > max_height)
                    {
                        max_height = res;
                    }
                    moving_index++;
                }
                moving_index = i * columns;
                for (int u = 0; u < columns; u++)
                {
                    float last_right;
                    if (u == 0)
                        last_right = 0;
                    else
                        last_right = moving_images_world_space[moving_index-1].Right + moving_margin;
                    float zoom = moving_image_actual_width / (float)_scene.images_moving[moving_index].width;
                    moving_images_world_space[moving_index].Left = last_right;
                    float im_height = _scene.images_moving[moving_index].height * zoom;
                    moving_images_world_space[moving_index].Top = height_acumulate + (max_height - im_height) / 2f;
                    moving_images_world_space[moving_index].Right = moving_images_world_space[moving_index].Left + moving_image_actual_width;
                    moving_images_world_space[moving_index].Bottom = moving_images_world_space[moving_index].Top + im_height;
                    moving_index++;
                }

                height_acumulate += max_height;
                height_acumulate += moving_margin;
            }
            if (rest != 0)
            {
                int start_index_last_stair = stairs * columns;
                int end_index_last_stair = start_index_last_stair + rest;

                float bottom = height_acumulate;
                    
                float max_height = float.MinValue;

                for (int u = start_index_last_stair; u < end_index_last_stair; u++)
                {
                    float zoom = moving_image_actual_width / (float)_scene.images_moving[u].width;
                    float res = _scene.images_moving[u].height * zoom;
                    if (res > max_height)
                    {
                        max_height = res;
                    }
                    
                }

                for (int u = start_index_last_stair; u < end_index_last_stair; u++)
                {
                    float last_right;
                    if (u == start_index_last_stair)
                        last_right = 0;
                    else
                        last_right = moving_images_world_space[u-1].Right + moving_margin;
                    float zoom = moving_image_actual_width / (float)_scene.images_moving[u].width;
                    moving_images_world_space[u].Left = last_right;
                    float im_height = _scene.images_moving[u].height * zoom;
                    moving_images_world_space[u].Top = bottom + (max_height - im_height) / 2f;
                    moving_images_world_space[u].Right = moving_images_world_space[u].Left + moving_image_actual_width;
                    moving_images_world_space[u].Bottom = moving_images_world_space[u].Top + im_height;
                }

                height_acumulate += max_height;
            }
            float width = columns * moving_image_actual_width + (columns - 1) * moving_margin;
            float centroidx = width / 2.0f;
            float centroidy = height_acumulate / 2.0f;
            for (int i = 0; i < moving_length; i++)
            {
                moving_images_world_space[i].Left -= centroidx;
                moving_images_world_space[i].Right -= centroidx;
                moving_images_world_space[i].Top -= centroidy;
                moving_images_world_space[i].Bottom -= centroidy;
            }
        }
        void generate_moving_images_screen_space()
        {
            int moving_length = moving_images_world_space.Length;
            float disp_x = (float)(moving_screen_space_start.X - moving_screen_space_end.X);
            float disp_y = (float)(moving_screen_space_start.Y - moving_screen_space_end.Y);
            for (int i = 0; i < moving_length; i++)
            {
                moving_images_screen_space[i].Left = moving_images_world_space[i].Left - disp_x + (float) moving_screen_space_start.X;
                moving_images_screen_space[i].Right = moving_images_world_space[i].Right - disp_x + (float) moving_screen_space_start.X;
                moving_images_screen_space[i].Top = moving_images_world_space[i].Top - disp_y + (float)moving_screen_space_start.Y;
                moving_images_screen_space[i].Bottom = moving_images_world_space[i].Bottom - disp_y + (float)moving_screen_space_start.Y;
            }
        }
        void start_moving()
        {
            int selected_count = number_of_selected();
            if (selected_count == 0)
            {
                moving = false;
                return;
            }
            moving_images_world_space = new RectangleF[selected_count];
            moving_images_screen_space = new RectangleF[selected_count];
            moving_images_screen_space_origin = new RectangleF[selected_count];
            moving_images_source_cat_index = new int[selected_count];
            moving_images_source_im_index = new int[selected_count];
            int cat_length = _scene.images.Count;
            bool[] remove_cat = new bool[cat_length];
            int pool_length = _scene.images_pool.Count;
            int selected_index=0;
            int removed_count = 0;
            for (int i = 0; i < cat_length; i++)
            {
                remove_cat[i] = false;
                int cat_im_length = _scene.images[i].Count;
                for (int j = cat_im_length-1; j > -1; j--)
                {
                    if (_scene.images[i+removed_count][j].selected)
                    {
                        move_to_moving_add_image(i + removed_count, j);
                        moving_images_source_cat_index[selected_index] = i;
                        moving_images_source_im_index[selected_index] = j;
                        selected_index++;
                        if (_scene.images[i].Count == 0)
                        {
                            remove_cat[i] = true;
                        }
                    }
                }
            }
            for (int i = pool_length-1; i >-1; i--)
            {
                if (_scene.images_pool[i].selected)
                {
                    move_to_moving_from_pool_add_image(i);
                    moving_images_source_cat_index[selected_index] = -1;
                    moving_images_source_im_index[selected_index] = i;
                    selected_index++;
                }
            }
            for (int i = cat_length-1; i > -1; i--)
                if (remove_cat[i])
                    remove_category(i);
            reinitialize_screen_space_buffers();
            generate_moving_pattern();
            generate_moving_images_screen_space();
            generate_screen_space();
            scroll_bars_resize();
            generate_screen_space();
        }
        void move_images()
        {
            generate_moving_images_screen_space();
        }
        void end_moving()
        {
            int dest_cat_index;
            int dest_im_index;
            pointer_inside_image_check(out dest_cat_index, out dest_im_index);

            int selected_count = _scene.images_moving.Count;
            if (dest_cat_index==-2)
            {
                bool ctrl = System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl);
                if (!ctrl)
                    add_category();
                for (int i = 0; i < selected_count; i++)
                {
                    if (ctrl)
                    {
                        add_category();
                        move_from_moving_add_image(0, _scene.images.Count - 1);
                    }
                    else
                        move_from_moving_add_image(0, _scene.images.Count - 1);
                }
                /*
                for (int i = 0; i < selected_count; i++)
                {
                    if (moving_images_source_cat_index[i]<0)
                    {
                        move_from_moving_to_pool_insert_image(0, moving_images_source_im_index[i]);
                    }
                    else
                    {
                        move_from_moving_insert_image(0,moving_images_source_cat_index[i],moving_images_source_im_index[i]);
                    }
                }*/
            }
            else if (dest_cat_index == -1)
            {
                for (int i = 0; i < selected_count; i++)
                {
                    if (dest_im_index < 0)
                    {
                        move_from_moving_to_pool_add_image(0);
                    }
                    else
                    {
                        move_from_moving_to_pool_insert_image(0, dest_im_index);
                    }
                }
            }
            else
            {
                for (int i = 0; i < selected_count; i++)
                {
                    if (dest_im_index < 0)
                    {
                        move_from_moving_add_image(0, dest_cat_index);
                    }
                    else
                    {
                        move_from_moving_insert_image(0, dest_cat_index, dest_im_index);
                    }
                }
            }

            
            reinitialize_screen_space_buffers();
            generate_screen_space();
            scroll_bars_resize();
        }
        void reinitialize_screen_space_buffers()
        {
            int cat_count = _scene.images.Count;
            categories_screen_space = new RectangleF[cat_count];
            images_screen_space = new RectangleF[cat_count][];
            pool_images_screen_space = new RectangleF [_scene.images_pool.Count];
            
            for (int i = 0; i < cat_count; i++)
            {
                images_screen_space[i] = new RectangleF[_scene.images[i].Count];
            }
        }
        public void remove_image(int cat_index, int im_index)
        {
            _scene.remove_image(cat_index, im_index);
            render();
        }
        public void add_image(int cat_index, string file_name)
        {
            _scene.add_image(cat_index, file_name);
        }
        public void insert_image(int cat_index, int im_index, string file_name)
        {
            _scene.insert_image(cat_index, im_index, file_name);
        }
        public void move_add_image(int cat_index_source, int im_index_source, int cat_index_dest)
        {
            _scene.move_add_image(cat_index_source, im_index_source,cat_index_dest);
        }
        public void move_insert_image(int cat_index_source, int im_index_source, int cat_index_dest, int im_index_dest)
        {
            _scene.move_insert_image(cat_index_source, im_index_source, cat_index_dest, im_index_dest);
        }
        public void add_category()
        {
            _scene.add_category();
        }
        public void remove_category(int cat_index)
        { 
            _scene.remove_category(cat_index);
        }
        public void insert_category(int cat_index)
        {
            _scene.insert_category(cat_index);
        }
        public void move_insert_category(int cat_index_source, int cat_index_dest)
        {
            _scene.move_insert_category(cat_index_source, cat_index_dest);
        }
        public void remove_image_pool(int im_index)
        {
            _scene.remove_image_pool(im_index);
        }
        public void add_image_pool(string file_name)
        {
            _scene.add_image_pool(file_name);
        }
        public void insert_image_pool(int im_index, string file_name)
        {
            _scene.insert_image_pool(im_index, file_name);
        }
        public void move_to_pool_add_image(int cat_index_source, int im_index_source)
        {
            _scene.move_to_pool_add_image(cat_index_source, im_index_source);
        }
        public void move_to_pool_insert_image(int cat_index_source, int im_index_source, int im_index_dest)
        {
            _scene.move_to_pool_insert_image(cat_index_source, im_index_source, im_index_dest);
        }
        public void move_from_pool_add_image(int cat_index_source, int im_index_source)
        {
            _scene.move_from_pool_add_image(cat_index_source, im_index_source);
        }
        public void move_from_pool_insert_image(int cat_index_source, int im_index_source, int im_index_dest)
        {
            _scene.move_from_pool_insert_image(cat_index_source, im_index_source, im_index_dest);
        }
        public void move_to_moving_add_image(int cat_index_dest, int im_index_source)
        {
            _scene.move_to_moving_add_image(cat_index_dest, im_index_source);
        }
        public void move_from_moving_add_image(int im_index_source,int cat_index_dest)
        {
            _scene.move_from_moving_add_image(im_index_source, cat_index_dest);
        }
        public void move_from_moving_insert_image(int im_index_source,int cat_index_dest , int im_index_dest)
        {
            _scene.move_from_moving_insert_image(im_index_source, cat_index_dest, im_index_dest);
        }
        public void move_from_moving_to_pool_add_image(int im_index_source)
        {
            _scene.move_from_moving_to_pool_add_image(im_index_source);
        }
        public void move_from_moving_to_pool_insert_image(int im_index_source, int im_index_dest)
        {
            _scene.move_from_moving_to_pool_insert_image(im_index_source, im_index_dest);
        }
        public void move_to_moving_from_pool_add_image(int im_index_source)
        {
            _scene.move_to_moving_from_pool_add_image(im_index_source);
        }
        public void moving_remove_image(int im_index)
        {
            _scene.moving_remove_image(im_index);
            render();
        }
        //selecting
        bool preview_selection()
        {
            System.Windows.Point pos = System.Windows.Input.Mouse.GetPosition(this);
            bool changed = false;
            int cat_count = images_screen_space.Length;
            if (selecting)
            {
                float left_selection, top_selection, right_selection, bottom_selection;
                left_selection = Math.Min(selection_start_x, selection_end_x);
                right_selection = Math.Max(selection_start_x, selection_end_x);
                top_selection = Math.Min(selection_start_y, selection_end_y);
                bottom_selection = Math.Max(selection_start_y, selection_end_y);
                for (int i = 0; i < cat_count; i++)
                {
                    int img_count = images_screen_space[i].Length;
                    for (int j = 0; j < img_count; j++)
                    {
                        if (selection_start_x < selection_end_x)
                        {
                            if (!(left_selection > images_screen_space[i][j].Right || right_selection < images_screen_space[i][j].Left|| top_selection > images_screen_space[i][j].Bottom || bottom_selection < images_screen_space[i][j].Top))
                            {
                                if (_scene.images[i][j].selected_preview)
                                    _scene.images[i][j].selected_preview = true;
                                else
                                {
                                    _scene.images[i][j].selected_preview = true;
                                    changed = true;
                                }
                            }
                            else
                            {
                                _scene.images[i][j].selected_preview = false;
                            }
                        }
                        else
                        {
                            if ((left_selection < images_screen_space[i][j].Left && right_selection > images_screen_space[i][j].Right && top_selection < images_screen_space[i][j].Top && bottom_selection > images_screen_space[i][j].Bottom))
                            {
                                if (_scene.images[i][j].selected_preview)
                                    _scene.images[i][j].selected_preview = true;
                                else
                                {
                                    _scene.images[i][j].selected_preview = true;
                                    changed = true;
                                }
                            }
                            else
                            {
                                _scene.images[i][j].selected_preview = false;
                            }
                        }
                    }
                }
                int pool_img_count = pool_images_screen_space.Length;

                for (int i = 0; i < pool_img_count; i++)
                {
                    if (selection_start_x < selection_end_x)
                    {
                        if (!(left_selection > pool_images_screen_space[i].Right || right_selection < pool_images_screen_space[i].Left || top_selection > pool_images_screen_space[i].Bottom || bottom_selection < pool_images_screen_space[i].Top))
                        {
                            if (_scene.images_pool[i].selected_preview)
                                _scene.images_pool[i].selected_preview = true;
                            else
                            {
                                _scene.images_pool[i].selected_preview = true;
                                changed = true;
                            }
                        }
                        else
                        {
                            _scene.images_pool[i].selected_preview = false;
                        }
                    }
                    else
                    {
                        if ((left_selection < pool_images_screen_space[i].Left && right_selection > pool_images_screen_space[i].Right && top_selection < pool_images_screen_space[i].Top && bottom_selection > pool_images_screen_space[i].Bottom))
                        {
                            if (_scene.images_pool[i].selected_preview)
                                _scene.images_pool[i].selected_preview = true;
                            else
                            {
                                _scene.images_pool[i].selected_preview = true;
                                changed = true;
                            }
                        }
                        else
                        {
                            _scene.images_pool[i].selected_preview = false;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < cat_count; i++)
                {
                    int img_count = images_screen_space[i].Length;
                    for (int j = 0; j < img_count; j++)
                    {
                        if (pos.X > images_screen_space[i][j].Left && pos.X < images_screen_space[i][j].Right && pos.Y > images_screen_space[i][j].Top && pos.Y < images_screen_space[i][j].Bottom)
                        {
                            if (_scene.images[i][j].selected_preview)
                            {
                                _scene.images[i][j].selected_preview = true;
                            }
                            else
                            {
                                _scene.images[i][j].selected_preview = true;
                                changed = true;
                            }
                        }
                        else
                        {
                            if (!_scene.images[i][j].selected_preview)
                                _scene.images[i][j].selected_preview = false;
                            else
                            {
                                _scene.images[i][j].selected_preview = false;
                                changed = true;
                            }
                        }
                    }
                }
                int img_count_pool = pool_images_screen_space.Length;
                for (int i = 0; i < img_count_pool; i++)
                {
                    if (pos.X > pool_images_screen_space[i].Left && pos.X < pool_images_screen_space[i].Right && pos.Y > pool_images_screen_space[i].Top && pos.Y < pool_images_screen_space[i].Bottom)
                    {
                        if (_scene.images_pool[i].selected_preview)
                        {
                            _scene.images_pool[i].selected_preview = true;
                        }
                        else
                        {
                            _scene.images_pool[i].selected_preview = true;
                            changed = true;
                        }
                    }
                    else
                    {
                        if (!_scene.images_pool[i].selected_preview)
                            _scene.images_pool[i].selected_preview = false;
                        else
                        {
                            _scene.images_pool[i].selected_preview = false;
                            changed = true;
                        }
                    }
                }
            }
            return changed;
        }
        void select_preview_selection()
        {
            int cat_count = images_screen_space.Length;
            for (int i = 0; i < cat_count; i++)
            {
                int cat_im_count = images_screen_space[i].Length;
                for (int j = 0; j < cat_im_count; j++)
                {
                    _scene.images[i][j].selected = _scene.images[i][j].selected_preview;
                }
            }
            int pool_im_count = pool_images_screen_space.Length;
            for (int i = 0; i < pool_im_count; i++)
            {
                _scene.images_pool[i].selected = _scene.images_pool[i].selected_preview;
            }
        }
        void remove_preview_selection()
        {
            int cat_count = images_screen_space.Length;
            for (int i = 0; i < cat_count; i++)
            {
                int cat_im_count = images_screen_space[i].Length;
                for (int j = 0; j < cat_im_count; j++)
                {
                    if (_scene.images[i][j].selected_preview)
                        _scene.images[i][j].selected = !_scene.images[i][j].selected_preview;
                }
            }
            int pool_im_count = pool_images_screen_space.Length;
            for (int i = 0; i < pool_im_count; i++)
            {
                if (_scene.images_pool[i].selected_preview)
                    _scene.images_pool[i].selected = !_scene.images_pool[i].selected_preview;
            }
        }
        void add_preview_selection()
        {
            int cat_count = images_screen_space.Length;
            for (int i = 0; i < cat_count; i++)
            {
                int cat_im_count = images_screen_space[i].Length;
                for (int j = 0; j < cat_im_count; j++)
                {
                    if (_scene.images[i][j].selected_preview)
                    {
                        _scene.images[i][j].selected = _scene.images[i][j].selected_preview;
                    }
                }
            }
            int pool_im_count = pool_images_screen_space.Length;
            for (int i = 0; i < pool_im_count; i++)
            {
                if (_scene.images_pool[i].selected_preview)
                {
                    _scene.images_pool[i].selected = _scene.images_pool[i].selected_preview;
                }
            }
        }
        int number_of_selected()
        {
            int cat_length = _scene.images.Count;
            int pool_length = _scene.images_pool.Count;
            int selected_count = 0;
            for (int i = 0; i < cat_length; i++)
            {
                int cat_im_length = _scene.images[i].Count;
                for (int j = 0; j < cat_im_length; j++)
                {
                    if (_scene.images[i][j].selected)
                        selected_count++;
                }
            }
            for (int i = 0; i < pool_length; i++)
            {
                if (_scene.images_pool[i].selected)
                    selected_count++;
            }
            return selected_count;
        }
    }
}
