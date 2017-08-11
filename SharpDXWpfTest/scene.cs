using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace image_match
{
    public class connection
    {
        public connection(int connection_id, int cat_id,int image_id,int point_id,float x,float y)
        {
            this.connection_id = connection_id;
            this.cat_id = cat_id;
            this.image_id = image_id;
            this.point_id = point_id;
            this.x = x;
            this.y = y;
        }
        public connection()
        {
            this.connection_id = -1;
            this.image_id = -1;
            this.point_id = -1;
            this.x = 0;
            this.y = 0;
        }
        public int connection_id { get; set; }
        public int cat_id { get; set; }
        public int image_id { get; set; }
        public int point_id { get; set; }
        public float x { get; set; }
        public float y { get; set; }
    }
    public class scene
    {
        public ObservableCollection<connection> connections;
        public  List<List<image_sol>> images;
        public List<image_sol> images_pool;
        public List<image_sol> images_moving;
        Random rand;

        public List<int> st_point_cat_index;
        public List<int> st_point_im_index;
        public List<int> st_point_pt_index;

        public List<int> nd_point_cat_index;
        public List<int> nd_point_im_index;
        public List<int> nd_point_pt_index;
        public List<bool> solved_connection; 
        public int matches_count;
        //public List<float> match_fitness;
        public dx_resources DX_RES;
        public List<SharpDX.Direct2D1.SolidColorBrush> categories_brush;
        public delegate void progress_callback();
        int connection_id_index;
        public int connection_id_length;
        public bool connections_ready;

        /*public static void Sort<TSource, TKey>(this Collection<TSource> source, Func<TSource, TKey> keySelector)
        {
            List<TSource> sortedList = source.OrderBy(keySelector).ToList();
            source.Clear();
            foreach (var sortedItem in sortedList)
                source.Add(sortedItem);
        }*/
        public scene (dx_resources DX_RES)
        {
            this.DX_RES = DX_RES;
            //SharpDX.Direct2D1.Bitmap
            connections = new ObservableCollection<connection>();
            
            
            /*connections.Add(new connection(2,0,0,5,6));
            connections.Add(new connection(1, 0, 0, 5, 6));
            connections.Add(new connection(3, 0, 0, 5, 6));
            connections.Sort((x, y) => x.connection_id.CompareTo(y.connection_id));*/
            
            string relative_path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string[] filePaths = System.IO.Directory.GetFiles(relative_path + "\\test_images\\"/*, "*.png"*/);
            int filePaths_len = filePaths.Length;
            rand = new Random();
            //int categories_count = 3;
            images = new List<List<image_sol>>();
            images_pool = new List<image_sol>();
            images_moving = new List<image_sol>();
            categories_brush = new List<SolidColorBrush>();

            st_point_cat_index = new List<int>();
            st_point_im_index = new List<int>();
            st_point_pt_index = new List<int>();

            nd_point_cat_index = new List<int>();
            nd_point_im_index = new List<int>();
            nd_point_pt_index = new List<int>();

            solved_connection = new List<bool>();
            //match_fitness = new List<float>();
            /*images_count[0] = 3;
            images_count[1] = 1;
            images_count[2] = 2;*/
            
            

            /*for (int i = 0; i < categories_count; i++)
            {
                int images_count = 5;//rand.Next(1, 5);
                images.Add(new System.Collections.Generic.List<image_sol>());
                for (int j = 0; j < images_count; j++)
                {
                    images[i].Add( new image_sol(filePaths[rand.Next(0, filePaths_len)], DX_RES.d2d_render_target));
                }
            }*/
            /*images[0].Add(new image_sol(filePaths[rand.Next(0, filePaths_len)], DX_RES.d2d_render_target));
            images[0].Add(new image_sol(filePaths[rand.Next(0, filePaths_len)], DX_RES.d2d_render_target));

            images[1].Add(new image_sol(filePaths[rand.Next(0, filePaths_len)], DX_RES.d2d_render_target));*/
            
        }
        public void connections_rows_deleted()
        {

            
            int connections_length = connections.Count();
            
            if (connections_length == 0)
            {
                matches_count = 0;
                st_point_cat_index.Clear();
                st_point_im_index.Clear();
                st_point_pt_index.Clear();
                nd_point_cat_index.Clear();
                nd_point_im_index.Clear();
                nd_point_pt_index.Clear();
                solved_connection.Clear();
                for (int i = 0; i < images.Count(); i++)
                {
                    for (int j = 0; j < images.Count(); j++)
                    {
                        int points_count = images[i][j].points_count;
                        for (int k = 0; k < points_count; k++)
                        {
                            images[i][j].connection_id[k] = -1;
                        }
                    }
                }
            }
            else
            {
                                
                for (int i = connections_length-1; i >1 ; i--)
                {
                    if (i == connections.Count - 1)
                    {
                        if (connections[i].connection_id != connections[i - 1].connection_id)
                        {
                            connections.RemoveAt(i);
                        }
                    }
                    else
                    {
                        if (!(connections[i].connection_id == connections[i - 1].connection_id || connections[i].connection_id == connections[i + 1].connection_id))
                        {
                            connections.RemoveAt(i);
                        }
                    }
                }
                if (connections.Count() > 1)
                {
                    if (connections[0].connection_id != connections[1].connection_id)
                    {
                        connections.RemoveAt(0);
                    }
                }

                connections_length = connections.Count();
                int index = 1;
                int original_index = connections[0].connection_id;
                connections[0].connection_id = index;
                for (int i = 1; i < connections_length; i++)
                {
                    if(connections[i].connection_id==original_index)
                    {
                        connections[i].connection_id = index;
                    }
                    else
                    {
                        original_index = connections[i].connection_id;
                        index++;
                        connections[i].connection_id = index;
                    }
                }
                
                /*for (int i = 0; i < images.Count(); i++)
                {
                    for (int j = 0; j < images.Count(); j++)
                    {
                        int points_count = images[i][j].points_count;
                        for (int k = 0; k < points_count; k++)
                        {
                            images[i][j].connection_id[k] = -1;
                        }
                    }
                }
                for (int i = 1; i < connections_length; i++)
                {
                    images[connections[i].cat_id-1][connections[i].image_id-1].connection_id[connections[i].point_id-1]=0;
                }*/
                //create_connections();
                connection_id_length = index;
            }
            

        }
        void create_connections_from(int cat_index,int image_index,int point_index)
        {

            for (int i = 0; i < matches_count; i++)
            {
                if(!solved_connection[i])
                {
                    int cat = st_point_cat_index[i], im, pt;
                    if (cat == cat_index)
                    {
                        im = st_point_im_index[i];
                        if (im == image_index)
                        {
                            pt = st_point_pt_index[i];
                            if (pt == point_index)
                            {
                                cat = nd_point_cat_index[i];
                                im = nd_point_im_index[i];
                                pt = nd_point_pt_index[i];
                                if (images[cat][im].connection_id[pt] == 0)
                                {
                                    solved_connection[i] = true;
                                    images[cat][im].connection_id[pt] = connection_id_index;
                                    create_connections_from(cat, im, pt);
                                }
                            }
                        }
                        cat = nd_point_cat_index[i];
                    }
                    cat = nd_point_cat_index[i];
                    if (cat == cat_index && !solved_connection[i])
                    {
                        im = nd_point_im_index[i];
                        if (im == image_index)
                        {
                            pt = nd_point_pt_index[i];
                            if (pt == point_index)
                            {
                                cat = st_point_cat_index[i];
                                im = st_point_im_index[i];
                                pt = st_point_pt_index[i];
                                if (images[cat][im].connection_id[pt]==0)
                                {
                                    solved_connection[i] = true;
                                    images[cat][im].connection_id[pt] = connection_id_index;
                                    create_connections_from(cat, im, pt);
                                }
                                    

                            }
                        }
                        cat = nd_point_cat_index[i];
                    }
                }
            }
        }
        public void create_connections()
        {
            connection_id_index=1;
            for (int u = 0; u < images.Count; u++)
            {
                for (int v = 0; v < images[u].Count; v++)
                {
                    int points_count1 = images[u][v].points_count;
                    int[] connection_id1 = images[u][v].connection_id;
                    for (int i = 0; i < points_count1; i++)
                    {
                        if(connection_id1[i]==0)
                        {
                            connection_id1[i]=connection_id_index;
                            create_connections_from(u, v, i);
                            connection_id_index++;
                        }
                    }
                }
            }
            connections.Clear();
            for (int u = 0; u < images.Count; u++)
            {
                for (int v = 0; v < images[u].Count; v++)
                {
                    int points_count1 = images[u][v].points_count;
                    int[] connection_id1 = images[u][v].connection_id;
                    float[] points1 = images[u][v].points;
                    for (int i = 0; i < points_count1; i++)
                    {
                        if (connection_id1[i] > 0)
                        {
                            connections.Add(new connection(connection_id1[i], u, v , i , points1[i * 2], points1[i * 2 + 1]));
                        }
                    }
                }
            }
            connection_id_length = connection_id_index - 1;
            //connections.Sort((x, y) => x.connection_id.CompareTo(y.connection_id));
            var sortedList = connections.OrderBy(a => a.connection_id).ToList();
            connections.Clear();
            foreach (var sortedItem in sortedList)
                connections.Add(sortedItem);
            connections_ready = true;

            
        }
        private Color4 random_pastel_color()
        {
            return new Color4((float)rand.NextDouble() * 0.5f + 0.5f, (float)rand.NextDouble() * 0.5f + 0.5f, (float)rand.NextDouble() * 0.5f + 0.5f, 1f);
        }
        private Color4 color4_from_base_255(float red, float green, float blue)
        {
            return new Color4(red / 255f, green / 255f, blue / 255f, 1f);
        }
        private Color4 random_color_from_original(float original_weight, Color4 original_color)
        {
            float weight_inv = 1f - original_weight;
            return new Color4((float)rand.NextDouble() * weight_inv + original_weight * original_color.Red, (float)rand.NextDouble() * weight_inv + original_weight * original_color.Green, (float)rand.NextDouble() * weight_inv + original_weight * original_color.Blue, 1f);
        }
        public void generate_false_matches(int matches_count)
        {
            this.matches_count = matches_count;// rand.Next(50, 100);
            int non_zero_count=0;
            
            for (int i = 0; i < images.Count; i++)
            {
                if (images[i].Count>0)
                {
                    non_zero_count++;
                }
            }
            if (non_zero_count<2)
            {
                this.matches_count = 0;// rand.Next(50, 100);
                return;
            }
            int[] non_zero_cat = new int[non_zero_count];
            non_zero_count = 0;

            for (int i = 0; i < images.Count; i++)
            {
                if (images[i].Count > 0)
                {
                    non_zero_cat[non_zero_count] = i;
                    non_zero_count++;
                }
            }
            connections.Clear();
            st_point_cat_index.Clear();
            st_point_im_index.Clear();
            st_point_pt_index.Clear();
            nd_point_cat_index.Clear();
            nd_point_im_index.Clear();
            nd_point_pt_index.Clear();

            for (int i = 0; i < matches_count; i++)
            {
                //match_fitness.Add((float)rand.NextDouble());
                st_point_cat_index.Add(non_zero_cat[rand.Next(non_zero_count)]);
                st_point_im_index.Add(rand.Next(images[st_point_cat_index[i]].Count));
                st_point_pt_index.Add(rand.Next(images[st_point_cat_index[i]][st_point_im_index[i]].points_count));
                images[st_point_cat_index[i]][st_point_im_index[i]].connection_id[st_point_pt_index[i]] = 0;
                int next_cat_ind;
                while (true)
                {
                    next_cat_ind = non_zero_cat[rand.Next(non_zero_count)];
                    if (next_cat_ind != st_point_cat_index[i])
                        break;
                }
                nd_point_cat_index.Add(next_cat_ind);
                nd_point_im_index.Add(rand.Next(images[nd_point_cat_index[i]].Count));
                nd_point_pt_index.Add(rand.Next(images[nd_point_cat_index[i]][nd_point_im_index[i]].points_count));
                images[nd_point_cat_index[i]][nd_point_im_index[i]].connection_id[nd_point_pt_index[i]] = 0;
            }
            solved_connection.Clear() ;
            for (int i = 0; i < matches_count; i++)
            {
                solved_connection.Add(false);
            }
        }
        bool cancel = false;
        public void cancel_matching()
        {
            if (calculating)
                cancel = true;
        }
        bool calculating = false;
        public void clear_connections()
        {
            connections.Clear();
            st_point_cat_index.Clear();
            st_point_im_index.Clear();
            st_point_pt_index.Clear();
            nd_point_cat_index.Clear();
            nd_point_im_index.Clear();
            nd_point_pt_index.Clear();
        }
        public void generate_matches(progress_callback progress)
        {
            calculating = true;
            
            int match_ind = 0;

            System.Collections.Generic.List<int> cat_ind = new List<int>();
            System.Collections.Generic.List<int> im_ind = new List<int>();
            System.Collections.Generic.List<int> cat_ind_2 = new List<int>();
            System.Collections.Generic.List<int> im_ind_2 = new List<int>();

            int arr_length = 0;
            for (int u = 0; u < images.Count - 1; u++)
            {
                for (int v = 0; v < images[u].Count; v++)
                {
                    for (int k = u+1; k < images.Count; k++)
                    {
                        for (int l = 0; l < images[k].Count; l++)
                        {
                            cat_ind.Add(u);
                            im_ind.Add(v);
                            cat_ind_2.Add(k);
                            im_ind_2.Add(l);
                            arr_length++;
                        }
                    }
                }
            }
            object lock_matching = new object();
            //System.Threading.Tasks.Parallel.For(0, arr_length, (i, loopstate) =>
            for (int i = 0; i < arr_length; i++)
            {
                int points_count1 = images[cat_ind[i]][im_ind[i]].points_count;
                float[] descriptor1 = images[cat_ind[i]][im_ind[i]].desctriptor;
                bool[] sign1 = images[cat_ind[i]][im_ind[i]].sign;
                int[] connection_id1 = images[cat_ind[i]][im_ind[i]].connection_id;
                int points_count2 = images[cat_ind_2[i]][im_ind_2[i]].points_count;
                float[] descriptor2 = images[cat_ind_2[i]][im_ind_2[i]].desctriptor;
                bool[] sign2 = images[cat_ind_2[i]][im_ind_2[i]].sign;
                int[] connection_id2 = images[cat_ind_2[i]][im_ind_2[i]].connection_id;
                
                //for (int l = 0; l < points_count1; l++)
                System.Threading.Tasks.Parallel.For(0, points_count1, (l, loopstate) =>
                {
                            
                    float best = 99999999999999;
                    float second_best = 1; ;
                    int best_index = 0;
                    for (int j = 0; j < points_count2; j++)
                    {
                        if (sign1[l] == sign2[j])
                        {
                            float sum = 0;
                            for (int h = 0; h < 64; h++)
                            {
                                sum += (float)Math.Pow((double)(descriptor1[l * 64 + h] - descriptor2[j * 64 + h]), 2.0);
                            }
                            sum = (float)Math.Sqrt((double)sum);
                            if (sum < best)
                            {
                                second_best = best;
                                best = sum;
                                best_index = j;
                            }
                        }
                    }
                    if (best < 0.25 && best / second_best < 0.8)
                    {
                        lock (lock_matching)
                        {
                            st_point_cat_index.Add(cat_ind[i]);
                            st_point_im_index.Add(im_ind[i]);
                            st_point_pt_index.Add(l);
                            connection_id1[l] = 0;

                            nd_point_cat_index.Add(cat_ind_2[i]);
                            nd_point_im_index.Add(im_ind_2[i]);
                            nd_point_pt_index.Add(best_index);
                            connection_id2[best_index] = 0;
                            match_ind++;
                        }
                        //match_fitness.Add((float)rand.NextDouble());
                    }
                });
                if (cancel)
                    break;    
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                {
                    progress();
                }), System.Windows.Threading.DispatcherPriority.Render);

            }
            /*for (int u = 0; u < images.Count-1; u++)
            {
                for (int v = 0; v < images[u].Count; v++)
                {
                    int points_count1 = images[u][v].points_count;
                    float []descriptor1 = images[u][v].desctriptor;
                    bool[] sign1 = images[u][v].sign;
                    int[] connection_id1 = images[u][v].connection_id;
                    System.Threading.Tasks.Parallel.For(u+1, images.Count, k =>
                    {
                        
                        System.Threading.Tasks.Parallel.For (0, images[k].Count,l=>
                        {
                            
                        });
                        
                    });
                    
                }
            }*/
            this.matches_count = match_ind;
            solved_connection.Clear();
            for (int i = 0; i < matches_count; i++)
            {
                solved_connection.Add(false);
            }
            cancel = false;
            calculating = false;
        }
        public void remove_image(int cat_index, int im_index)
        {
            images[cat_index][im_index].dispose();
            images[cat_index].RemoveAt(im_index);
            matches_count = 0;
            connections_ready = false;
        }
        public void add_image(int cat_index,string file_name)
        {
            images[cat_index].Add(new image_sol(file_name, DX_RES.d2d_render_target));
            matches_count = 0;
            connections_ready = false;
        }
        public void insert_image(int cat_index, int im_index, string file_name)
        {
            images[cat_index].Insert(im_index,new image_sol(file_name, DX_RES.d2d_render_target));
            matches_count = 0;
            connections_ready = false;
        }
        public void move_add_image(int cat_index_source, int im_index_source, int cat_index_dest)
        {
            image_sol res = images[cat_index_source][im_index_source];
            images[cat_index_source].RemoveAt(im_index_source);
            images[cat_index_dest].Add(res);
            matches_count = 0;
            connections_ready = false;
        }
        public void move_insert_image(int cat_index_source, int im_index_source, int cat_index_dest, int im_index_dest)
        {
            image_sol res = images[cat_index_source][im_index_source];
            images[cat_index_source].RemoveAt(im_index_source);
            images[cat_index_dest].Insert(im_index_dest,res);
            matches_count = 0;
            connections_ready = false;
        }
        public void remove_image_pool(int im_index)
        {
            images_pool[im_index].dispose();
            images_pool.RemoveAt(im_index);
            matches_count = 0;
            connections_ready = false;
        }
        public void add_image_pool(string file_name)
        {
            images_pool.Add(new image_sol(file_name, DX_RES.d2d_render_target));
            matches_count = 0;
            connections_ready = false;
        }
        public void insert_image_pool(int im_index, string file_name)
        {
            images_pool.Insert(im_index, new image_sol(file_name, DX_RES.d2d_render_target));
            matches_count = 0;
            connections_ready = false;
        }
        public void move_to_pool_add_image(int cat_index_source, int im_index_source)
        {
            image_sol res = images[cat_index_source][im_index_source];
            images[cat_index_source].RemoveAt(im_index_source);
            images_pool.Add(res);
            matches_count = 0;
            connections_ready = false;
        }
        public void move_to_pool_insert_image(int cat_index_source, int im_index_source, int im_index_dest)
        {
            image_sol res = images[cat_index_source][im_index_source];
            images[cat_index_source].RemoveAt(im_index_source);
            images_pool.Insert(im_index_dest, res);
            matches_count = 0;
            connections_ready = false;
        }
        public void move_from_pool_add_image(int cat_index_source, int im_index_source)
        {
            image_sol res = images_pool[im_index_source];
            images_pool.RemoveAt(im_index_source);
            images[cat_index_source].Add(res);
            matches_count = 0;
            connections_ready = false;
        }
        public void move_from_pool_insert_image(int cat_index_source, int im_index_source, int im_index_dest)
        {
            image_sol res = images_pool[im_index_source];
            images_pool.RemoveAt(im_index_source);
            images[cat_index_source].Insert(im_index_dest,res);
            matches_count = 0;
            connections_ready = false;
        }
        public void move_from_pool_to_moving_add_image(int im_index_source)
        {
            image_sol res = images_pool[im_index_source];
            images_pool.RemoveAt(im_index_source);
            images_moving.Add(res);
            matches_count = 0;
            connections_ready = false;
        }
        public void moving_remove_image(int im_index)
        {
            images_moving[im_index].dispose();
            images_moving.RemoveAt(im_index);
            matches_count = 0;
            connections_ready = false;
        }
        public void move_to_moving_add_image(int cat_index_source, int im_index_source)
        {
            image_sol res = images[cat_index_source][im_index_source];
            images[cat_index_source].RemoveAt(im_index_source);
            images_moving.Add(res);
            matches_count = 0;
            connections_ready = false;
        }
        public void move_to_moving_from_pool_add_image(int im_index_source)
        {
            image_sol res = images_pool[im_index_source];
            images_pool.RemoveAt(im_index_source);
            images_moving.Add(res);
            matches_count = 0;
            connections_ready = false;
        }
        public void move_from_moving_add_image(int im_index_source,int cat_index_dest)
        {
            image_sol res = images_moving[im_index_source];
            images_moving.RemoveAt(im_index_source);
            images[cat_index_dest].Add(res);
            matches_count = 0;
            connections_ready = false;
        }
        public void move_from_moving_insert_image(int im_index_source, int cat_index_dest, int im_index_dest)
        {
            image_sol res = images_moving[im_index_source];
            images_moving.RemoveAt(im_index_source);
            images[cat_index_dest].Insert(im_index_dest, res);
            matches_count = 0;
            connections_ready = false;
        }
        public void move_from_moving_to_pool_add_image(int im_index_source)
        {
            image_sol res = images_moving[im_index_source];
            images_moving.RemoveAt(im_index_source);
            images_pool.Add(res);
            matches_count = 0;
            connections_ready = false;
        }
        public void move_from_moving_to_pool_insert_image(int im_index_source, int im_index_dest)
        {
            image_sol res = images_moving[im_index_source];
            images_moving.RemoveAt(im_index_source);
            images_pool.Insert(im_index_dest, res);
            matches_count = 0;
            connections_ready = false;
        }
        public void add_category()
        {
            images.Add(new List<image_sol>());
            categories_brush.Add(new SharpDX.Direct2D1.SolidColorBrush(DX_RES.d2d_render_target, random_color_from_original(0.8f, /*color4_from_base_255(137, 201, 238)*/color4_from_base_255(224, 214, 255f))));
            matches_count = 0;
            connections_ready = false;
        }
        public void remove_category(int cat_index)
        {
            categories_brush[cat_index].Dispose();
            categories_brush.RemoveAt(cat_index);
            for (int i = 0; i < images[cat_index].Count; i++)
            {
                images[cat_index][i].dispose();
            }
            images[cat_index].Clear();
            images.RemoveAt(cat_index);
            matches_count = 0;
            connections_ready = false;
        }
        public void insert_category(int cat_index)
        {
            categories_brush.Insert(cat_index, new SharpDX.Direct2D1.SolidColorBrush(DX_RES.d2d_render_target, random_color_from_original(0.8f, color4_from_base_255(224, 214, 255f))));
            images.Insert(cat_index,new List<image_sol>());
            matches_count = 0;
            connections_ready = false;
        }
        public void move_insert_category(int cat_index_source, int cat_index_dest)
        {
            SolidColorBrush res = categories_brush[cat_index_source];
            categories_brush.RemoveAt(cat_index_source);
            categories_brush.Insert(cat_index_dest, res);
            List<image_sol> res2 = images[cat_index_source];
            images[cat_index_source].RemoveAt(cat_index_source);
            images.Insert(cat_index_dest, res2);
            matches_count = 0;
            connections_ready = false;
        }
    }
}
