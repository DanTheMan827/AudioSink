using AudioSink.Properties;
using System.Collections.ObjectModel;
using System.Linq;

namespace AudioSink
{
    internal static class SettingsWrapper
    {
        public static ObservableCollection<string> ConnectedDevices { get; private set; }
        public static bool ReconnectAtLaunch
        {
            get => Settings.Default.ReconnectAtLaunch;
            set
            {
                Settings.Default.ReconnectAtLaunch = value;
                Save();
            }
        }
        public static bool ReconnectOnInterval
        {
            get => Settings.Default.ReconnectOnInterval;
            set
            {
                Settings.Default.ReconnectOnInterval = value;
                Save();
            }
        }

        static SettingsWrapper()
        {
            ConnectedDevices = new ObservableCollection<string>();

            if (!string.IsNullOrWhiteSpace(Settings.Default.ConnectedDevices))
            {
                foreach (var item in Settings.Default.ConnectedDevices.Split('\0'))
                {
                    ConnectedDevices.Add(item);
                }
            }

            ConnectedDevices.CollectionChanged += ConnectedDevices_CollectionChanged;
        }

        private static void ConnectedDevices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Settings.Default.ConnectedDevices = string.Join("\0", ConnectedDevices.ToArray());
            Save();
        }

        private static void Save() => Settings.Default.Save();
    }
}
