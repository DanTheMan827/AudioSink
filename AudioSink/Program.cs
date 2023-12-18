using AudioSink.Services;
using System;
using System.Threading;
using System.Windows.Forms;

namespace AudioSink
{
    internal static class Program
    {
        internal static WatcherService Watcher = new WatcherService();
        private const string mutexName = "857c80d5-9d2d-4f75-8210-154b0d694fee";
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (Mutex mutex = new Mutex(true, mutexName, out bool createdNew))
            {
                if (createdNew)
                {
                    // To customize application configuration such as set high DPI settings or default font,
                    // see https://aka.ms/applicationconfiguration.
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    Application.Run(new TrayIconContext());
                }
                else
                {
                    MessageBox.Show("Only one instance of AudioSink can be run at a time.");
                }
            }
        }
    }
}