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

namespace image_match.help
{
    /// <summary>
    /// Interaction logic for help_window.xaml
    /// </summary>
    /// 
    
    public partial class help_window 
    {

        List<TreeViewItem> help_treeview_items;
        List<TreeViewItem> help_treeview_searched_items;
        List<TreeViewItem> help_treeview_items_original;
        List<string> help_treeview_items_original_strings;
        public help_window()
        {
            InitializeComponent();
            frame.Navigate(new Uri("help/overview/overview.xaml", UriKind.Relative));
            
            Closing += help_window_Closing;
            frame.LoadCompleted += frame_LoadCompleted;
            help_treeview_items = new List<TreeViewItem>();
            help_treeview_searched_items = new List<TreeViewItem>();
            help_treeview_items_original = new List<TreeViewItem>();
            help_treeview_items_original_strings = new List<string>();
            get_treeview_items(help_tree_view.Items);
            get_treeview_items_original(help_tree_view.Items);
            //(help_tree_view.Items[1] as TreeViewItem).IsEnabled =false;
        }
        
        bool load=false;
        void frame_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (load)
                return;
            load = true;
            //search_headings_tb.Text = "";
            String url = e.Uri.OriginalString.ToString().Replace("SharpDXWpfTest;component/","");
            //String tst=e.Uri.PathAndQuery;
            TreeViewItem a = FindNode(help_tree_view.Items, url);
            if (a == null)
            {
                load = false;
                return;
            }
            a.IsSelected = true;


            DependencyObject parent = a.Parent;
            while (true)
            {
                if (!(parent.GetType() == typeof(TreeViewItem)))
                    break;

                (parent as TreeViewItem).IsExpanded = true;
                parent = (parent as TreeViewItem).Parent;
            }
            load = false;
        }
        
        void help_window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        private void HelponselectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (load)
                return;
            load = true;
            TreeView str = (sender as TreeView);
            TreeViewItem str2 = str.SelectedItem as TreeViewItem;
            if (str2 == null)
            {
                load = false;
                return;
            }
            string h =str2.Tag as string;

            try
            {
                if (h != null)
                    frame.Navigate(new Uri(h, UriKind.Relative));
            }
            catch (Exception)
            {
                MessageBox.Show("Could not load \"" + h + "\"", "Image match", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            load = false;
        }
        private TreeViewItem FindNode(ItemCollection items, String value)
        {
            //System.Diagnostics.Debug.WriteLine(value);
            TreeViewItem oResult = null;

            foreach (var oItem in items)
            {
                TreeViewItem oTreeViewItem = (TreeViewItem)oItem;

                if ((String)oTreeViewItem.Tag == value) { oResult = oTreeViewItem; break; }

                if (oTreeViewItem.Items.Count > 0)
                {
                    oResult = FindNode(oTreeViewItem.Items, value);

                    if (oResult != null) { break; }
                }
            }
            return oResult;
        }
        /*public System.Windows.Controls.TreeViewItem FromID(string itemId, TreeViewItem rootNode)
        {
            foreach(TreeNode node in rootNode.Nodes)
            {
                if(node.Tag.Equals(itemId)) 
                    return node;
                TreeNode next = FromID(itemId, node);
                if(next != null) 
                    return next;
            }
            return null;
        }*/

        public void navigate_to(string url)
        {
            if ((url == null || url == ""))
                return;
            search_tb.Text = "";
            if(!this.IsVisible)
                this.Show();
            try
            {
                frame.Navigate(new Uri(url, UriKind.Relative)); 
            }
            catch (Exception)
            {
                MessageBox.Show("Could not load \"" + url +"\"" , "Image match", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            TreeViewItem a = FindNode(help_tree_view.Items, url);
            if (a == null)
                return;
            a.IsSelected = true;


            DependencyObject parent = a.Parent;
            while (true)
            {
                if (!(parent.GetType() == typeof(TreeViewItem)))
                    break;
                
                (parent as TreeViewItem).IsExpanded = true;
                parent = (parent as TreeViewItem).Parent;
            }
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            SharpDXWpfTest.icon_helper.RemoveIcon(this);
        }
        private void get_treeview_items(ItemCollection items)
        {

            foreach (var oItem in items)
            {
                TreeViewItem oTreeViewItem = (TreeViewItem)oItem;
                DependencyObject parent = oTreeViewItem.Parent;
                string parent_string="";
                while (true)
                {
                    if (!(parent.GetType() == typeof(TreeViewItem)))
                        break;
                    parent_string = (parent as TreeViewItem).Header.ToString() + " - " + parent_string;
                    parent = (parent as TreeViewItem).Parent;
                }
                if (parent_string.Length>3)
                    parent_string = parent_string.Remove(parent_string.Length - 3); ;
                TreeViewItem new_it = new TreeViewItem();
                new_it.Tag = oTreeViewItem.Tag;

                string name = oTreeViewItem.Header.ToString();

                if (parent_string != "")
                    name += " ( " + parent_string + " )";

                new_it.Header = name;
                help_treeview_items.Add(new_it);
                if (oTreeViewItem.Tag != null)
                {
                    string text = System.IO.File.ReadAllText(oTreeViewItem.Tag.ToString()).Replace("\n", " ");
                    string regex = " ?\\<.*?\\>";
                    string output = System.Text.RegularExpressions.Regex.Replace(text, regex, "");
                    output = System.Text.RegularExpressions.Regex.Replace(output, @"\s+", " ");
                    help_treeview_items_original_strings.Add(output);
                }
                else
                {
                    help_treeview_items_original_strings.Add("");
                }

                if (oTreeViewItem.Items.Count > 0)
                {
                    get_treeview_items(oTreeViewItem.Items);
                }
                
                
            }

        }
        private void get_treeview_items_original(ItemCollection items)
        {

            foreach (var oItem in items)
            {
                TreeViewItem oTreeViewItem = (TreeViewItem)oItem;
                help_treeview_items_original.Add(oTreeViewItem);
            }

        }
        void search_heading(string search_string, bool? and, bool? or, bool? phrase)
        {
            help_treeview_searched_items.Clear();
            if ((bool)phrase)
            {
                foreach (var item in help_treeview_items)
                {
                    string[] words = item.Header.ToString().ToLower().Split('(');
                    if (item.Tag != null)
                    {
                        if (words[0].Contains(search_string.ToLower()))
                        {
                            help_treeview_searched_items.Add(item);
                        }
                    }
                    
                }
                return;
            }
            if ((bool)or)
            {
                foreach (var item in help_treeview_items)
                {
                    string[] words = item.Header.ToString().ToLower().Split('(');
                    string[] words2 = search_string.ToLower().Split(' ');
                    bool contain = false;
                    foreach (var word in words2)
                    {
                        if (item.Tag == null)
                        {
                            contain = false;
                            break;
                        }
                        if (words[0].Contains(word.ToLower()))
                        {
                            contain = true;
                            break;
                        }

                    }
                    if (contain)
                        help_treeview_searched_items.Add(item);

                }
            }
            if ((bool)and)
            {
                foreach (var item in help_treeview_items)
                {
                    string[] words = item.Header.ToString().ToLower().Split('(');
                    string[] words2 = search_string.ToLower().Split(' ');
                    bool contain = true;
                    foreach (var word in words2)
                    {
                        if (item.Tag == null)
                        {
                            contain = false;
                            break;
                        }
                        if (!words[0].Contains(word.ToLower()) )
                        {
                            contain = false;
                            break;
                        }
                    }
                    if(contain)
                        help_treeview_searched_items.Add(item);
                }
            }
            
        }

        void search_pages(string search_string, bool? and, bool? or, bool? phrase)
        {
            help_treeview_searched_items.Clear();
            if((bool)phrase)
            {
                for (int i = 0; i < help_treeview_items_original_strings.Count; i++)
                {
                    if (help_treeview_items_original_strings[i].ToLower().Contains(search_string.ToLower()))
                        help_treeview_searched_items.Add(help_treeview_items[i]);
                }
                return;
            }
            if ((bool)or)
            {
                string[] search_strings = search_string.Split(' ');
                for (int i = 0; i < help_treeview_items_original_strings.Count; i++)
                {
                    bool contain = false;
                    for (int j = 0; j < search_strings.Length; j++)
                    {
                        if (help_treeview_items_original_strings[i].ToLower().Contains(search_strings[j].ToLower()))
                        {
                            contain = true;
                            break;
                        }
                    }
                    if (contain)
                    {
                        help_treeview_searched_items.Add(help_treeview_items[i]);
                    }
                }
            }
            if ((bool)and)
            {
                string[] search_strings = search_string.Split(' ');
                for (int i = 0; i < help_treeview_items_original_strings.Count; i++)
                {
                    bool contain = true;
                    for (int j = 0; j < search_strings.Length; j++)
                    {
                        if (!help_treeview_items_original_strings[i].ToLower().Contains(search_strings[j].ToLower()))
                        {
                            contain = false;
                            break;
                        }
                    }
                    if (contain)
                    {
                        help_treeview_searched_items.Add(help_treeview_items[i]);
                    }
                }
            }
        }
        void reset_treeview()
        {
            foreach (var item in help_treeview_items_original)
            {
                help_tree_view.Items.Add(item);
            }
        }
        private void window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            help_tree_view.MaxHeight = this.Height - 150;
        }
        private void search_pages_words_tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            search(); 
        }

        void search()
        {
            try
            {
                help_tree_view.Items.Clear();
            }
            catch (Exception)
            {

                return;
            }
            
            if (search_tb.Text == "")
            {
                reset_treeview();
                return;
            }
            if ((bool)rb_pages.IsChecked)
                search_pages(search_tb.Text, rb_and.IsChecked, rb_or.IsChecked, rb_phrase.IsChecked);
            if ((bool)rb_headings.IsChecked)
                search_heading(search_tb.Text, rb_and.IsChecked, rb_or.IsChecked, rb_phrase.IsChecked);


            foreach (var item in help_treeview_searched_items)
            {
                TreeViewItem tvw = new TreeViewItem();
                TextBlock txt = new TextBlock();
                txt.FontSize = 12;
                string[] list = (item.Header as string).Split('(');
                txt.Inlines.Add(new Bold(new Run(list[0])));
                if (list.Length != 1)
                    txt.Inlines.Add("(" + list[1]);


                tvw.Header = txt;
                tvw.Tag = item.Tag;
                help_tree_view.Items.Add(tvw);
            }
        }

        private void rb_Checked(object sender, RoutedEventArgs e)
        {
            search();
        }
    }
}
