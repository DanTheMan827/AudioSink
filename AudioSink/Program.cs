using AudioSink.Services;
using System;
using System.Windows.Forms;

namespace AudioSink
{
    internal static class Program
    {
        internal static WatcherService Watcher = new WatcherService();

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new TrayIconContext());
        }
    }
}