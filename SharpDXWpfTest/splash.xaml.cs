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
    /// Interaction logic for splash.xaml
    /// </summary>
    public interface ISplashScreen
    {
        void AddMessage(string message);
        void LoadComplete();
    }
    public partial class splash : Window, ISplashScreen
    {
        public splash()
        {
            InitializeComponent();
        }
        public void AddMessage(string message)
        {
            Dispatcher.Invoke((Action)delegate()
            {
                this.text.Text = message;
            });
        }

        public void LoadComplete()
        {
            Dispatcher.InvokeShutdown();
        }
    }
}
