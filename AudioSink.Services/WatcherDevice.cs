using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;

namespace AudioSink.Services
{
    /// <summary>
    /// Represents a device watcher for audio devices.
    /// </summary>
    public class WatcherDevice : IDisposable
    {
        /// <summary>
        /// A string representing the identity of the device.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Gets the name of the device.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Gets a value indicating whether the device is currently connected.
        /// </summary>
        public bool IsConnected => playbackConnection?.State == AudioPlaybackConnectionState.Opened;

        private AudioPlaybackConnection playbackConnection;

        private object _lock = new object();
        private bool disposedValue;

        /// <summary>
        /// Represents the method that handles state change events.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        public delegate void StateChanged(WatcherDevice sender);

        private List<StateChanged> delegates = new List<StateChanged>();
        private event StateChanged _OnStateChanged;

        /// <summary>
        /// Event that occurs when the state of the device changes.
        /// </summary>
        public event StateChanged OnStateChanged
        {
            add
            {
                _OnStateChanged += value;
                delegates.Add(value);
            }
            remove
            {
                _OnStateChanged -= value;
                delegates.Remove(value);
            }
        }

        /// <summary>
        /// Initializes a new instance of the WatcherDevice class with the specified device information.
        /// </summary>
        /// <param name="deviceInformation">The device information for the WatcherDevice.</param>
        public WatcherDevice(DeviceInformation deviceInformation)
        {
            this.Id = deviceInformation.Id;
            this.Name = deviceInformation.Name;
        }

        /// <summary>
        /// Asynchronously connects to the audio device.
        /// </summary>
        public async Task ConnectAsync()
        {
            await Start();
            var openResults = await playbackConnection?.OpenAsync();

            if (openResults.Status != AudioPlaybackConnectionOpenResultStatus.Success)
            {
                throw new Exception(openResults.Status.ToString(), openResults.ExtendedError);
            }
        }

        private async Task Start()
        {
            lock (_lock)
            {
                if (playbackConnection != null)
                {
                    return;
                }

                playbackConnection = AudioPlaybackConnection.TryCreateFromId(this.Id);
            }

            if (playbackConnection != null)
            {
                // The device has an available audio playback connection. 
                playbackConnection.StateChanged += this.AudioPlaybackConnection_ConnectionStateChanged;
                await playbackConnection.StartAsync();
            }
        }

        private void AudioPlaybackConnection_ConnectionStateChanged(AudioPlaybackConnection sender, object args) => _OnStateChanged?.Invoke(this);

        /// <summary>
        /// Asynchronously disconnects from the audio device.
        /// </summary>
        public async Task DisconnectAsync() => await Task.Run(Disconnect);

        /// <summary>
        /// Disconnects from the audio device.
        /// </summary>
        public void Disconnect()
        {
            lock (_lock)
            {
                if (playbackConnection != null)
                {
                    playbackConnection?.Dispose();
                    playbackConnection = null;
                }
            }

            _OnStateChanged?.Invoke(this);
        }

        /// <summary>
        /// Releases all resources used by the WatcherDevice.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disconnect();
                }

                // TODO: Free unmanaged resources (if any) and override finalizer

                // TODO: Set large fields to null

                disposedValue = true;
            }
        }

        /// <summary>
        /// Removes all event handlers registered for the OnStateChanged event.
        /// </summary>
        public void RemoveAllEvents()
        {
            foreach (var item in delegates.ToArray())
            {
                OnStateChanged -= item;
            }
        }

        /// <summary>
        /// Releases all resources used by the WatcherDevice.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);

            // Suppress finalization to avoid unnecessary resource cleanup
            GC.SuppressFinalize(this);
        }
    }
}