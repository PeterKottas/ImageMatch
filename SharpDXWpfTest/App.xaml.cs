using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace image_match
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 
    public partial class App : Application
    {
        /*public static ISplashScreen splashScreen;

        private System.Threading.ManualResetEvent ResetSplashCreated;
        private System.Threading.Thread SplashThread;
        protected override void OnStartup(StartupEventArgs e)
        {
            // ManualResetEvent acts as a block. It waits for a signal to be set.
            ResetSplashCreated = new System.Threading.ManualResetEvent(false);

            // Create a new thread for the splash screen to run on
            SplashThread = new System.Threading.Thread(ShowSplash);
            SplashThread.SetApartmentState(System.Threading.ApartmentState.STA);
            SplashThread.IsBackground = true;
            SplashThread.Name = "Splash Screen";
            SplashThread.Start();

            // Wait for the blocker to be signaled before continuing. This is essentially the same as: while(ResetSplashCreated.NotSet) {}
            ResetSplashCreated.WaitOne();
            base.OnStartup(e);
        }

        private void ShowSplash()
        {
            // Create the window
            splash animatedSplashScreenWindow = new splash();
            splashScreen = animatedSplashScreenWindow;

            // Show it
            animatedSplashScreenWindow.Show();

            // Now that the window is created, allow the rest of the startup to run
            ResetSplashCreated.Set();
            System.Windows.Threading.Dispatcher.Run();
        }*/
    }
}
