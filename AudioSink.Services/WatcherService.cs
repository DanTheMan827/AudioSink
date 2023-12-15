using System;
using System.Collections.Generic;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;

namespace AudioSink.Services
{
    /// <summary>
    /// Service for watching audio playback devices and handling device-related events.
    /// </summary>
    public class WatcherService : IDisposable
    {
        private DeviceWatcher watcher;
        private bool disposedValue;

        // Dictionary to store WatcherDevices using device ID as the key.
        private Dictionary<string, WatcherDevice> _Devices = new Dictionary<string, WatcherDevice>();
        public IReadOnlyDictionary<string, WatcherDevice> Devices => _Devices;

        // Events triggered when the watcher detects changes in devices.
        public delegate void WatcherEvent(WatcherService sender, WatcherDevice device);
        public event WatcherEvent OnUpdated;
        public event WatcherEvent OnRemoved;
        public event WatcherEvent OnAdded;

        /// <summary>
        /// Constructor for WatcherService. Initializes the device watcher and registers event handlers.
        /// </summary>
        public WatcherService()
        {
            // Create a device watcher for audio playback devices.
            watcher = DeviceInformation.CreateWatcher(AudioPlaybackConnection.GetDeviceSelector());

            // Register event handlers before starting the watcher.
            watcher.Added += this.Watcher_Added;
            watcher.Removed += this.Watcher_Removed;
            watcher.Updated += this.Watcher_Updated;

            // Start the watcher.
            watcher.Start();
        }

        /// <summary>
        /// Event handler for the Updated event of the device watcher.
        /// </summary>
        private void Watcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            // Invoke the OnUpdated event with null device information.
            OnUpdated?.Invoke(this, null);
        }

        /// <summary>
        /// Event handler for the Removed event of the device watcher.
        /// </summary>
        private void Watcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            WatcherDevice device;

            // Check if the device is in the dictionary.
            if (_Devices.TryGetValue(args.Id, out device))
            {
                // Remove the device from the dictionary and invoke the OnRemoved event.
                _Devices.Remove(args.Id);
                OnRemoved?.Invoke(this, device);
            }
        }

        /// <summary>
        /// Event handler for the Added event of the device watcher.
        /// </summary>
        private void Watcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            // Create a new WatcherDevice from the provided DeviceInformation.
            var device = new WatcherDevice(args);

            // Add the device to the dictionary and invoke the OnAdded event.
            _Devices.Add(args.Id, device);
            OnAdded?.Invoke(this, device);
        }

        /// <summary>
        /// Disposes of the WatcherService and stops the device watcher.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Stop the device watcher when disposing.
                    watcher.Stop();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        /// <summary>
        /// Public method to dispose of the WatcherService.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}