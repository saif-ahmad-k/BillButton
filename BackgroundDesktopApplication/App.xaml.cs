using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BackgroundDesktopApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //EventManager.RegisterClassHandler(typeof(Window), Window.PreviewKeyUpEvent, new KeyEventHandler(OnWindowKeyUp));
        }
        //private void OnWindowKeyUp(object source, KeyEventArgs e)
        //{
        //    if (e.Key == Key.LeftShift)
        //    {
        //        Application.Current.MainWindow.WindowState = WindowState.Normal;
        //    }
        //}
    }
}
