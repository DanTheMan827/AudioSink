using AudioSink.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.UI.ViewManagement;

namespace AudioSink
{
    /// <summary>
    /// Represents the context for the system tray icon and its menu.
    /// </summary>
    internal class TrayIconContext : ApplicationContext, IDisposable
    {
        private bool disposedValue;
        private NotifyIcon icon;
        private ContextMenuStrip menu = new ContextMenuStrip();
        private ToolStripMenuItem exitMenu = new ToolStripMenuItem("Exit");
        private ToolStripMenuItem autoStartMenu = new ToolStripMenuItem("Start at logon")
        {
            Checked = AutoStartManager.IsAutoStartEnabled
        };
        private ToolStripMenuItem reconnectToggleMenu = new ToolStripMenuItem("Reconnect at launch")
        {
            Checked = SettingsWrapper.ReconnectAtLaunch
        };
        private ToolStripMenuItem reconnectIntervalToggleMenu = new ToolStripMenuItem("Reconnect on interval")
        {
            Checked = SettingsWrapper.ReconnectOnInterval
        };
        private ToolStripMenuItem noDevicesMenu = new ToolStripMenuItem("No devices found")
        {
            Enabled = false
        };
        private ToolStripMenuItem connectMenu = new ToolStripMenuItem("Connect to Device")
        {
            Enabled = false
        };
        private Timer reconnectTimer = new Timer()
        {
            Interval = 1000
        };

        /// <summary>
        /// Initializes a new instance of the TrayIconContext class.
        /// </summary>
        public TrayIconContext()
        {
            // Register event handlers for WatcherService events.
            Program.Watcher.OnAdded += this.Watcher_OnAdded;
            Program.Watcher.OnRemoved += this.Watcher_OnRemoved;
            Program.Watcher.OnUpdated += this.Watcher_OnUpdated;

            // Detect current system theme.
            var uiSettings = new UISettings();
            var color = uiSettings.GetColorValue(UIColorType.Background);
            var isDark = color == Windows.UI.Color.FromArgb(255, 0, 0, 0);

            // Initialize the NotifyIcon.
            icon = new NotifyIcon()
            {
                Icon = isDark ? Resources.icon_white : Resources.icon_black,
                ContextMenuStrip = menu,
                Visible = true
            };

            icon.Click += this.Icon_Click;

            // Set the connect menu font.
            connectMenu.Font = new Font(connectMenu.Font, FontStyle.Bold);

            // Register the click handlers for the menu items.
            reconnectToggleMenu.Click += this.ReconnectToggleMenu_Click;
            reconnectIntervalToggleMenu.Click += this.ReconnectIntervalToggleMenu_Click;
            exitMenu.Click += this.ExitMenu_Click;
            autoStartMenu.Click += this.AutoStartMenu_Click;

            // Populate the initial menu.
            PopulateMenu(Program.Watcher);

            // Reconnect to any previous devices.
            Reconnect(SettingsWrapper.ReconnectAtLaunch, Program.Watcher.Devices.Values);

            this.reconnectTimer.Tick += this.ReconnectTimer_Tick;
            this.reconnectTimer.Enabled = SettingsWrapper.ReconnectOnInterval;
        }

        private void AutoStartMenu_Click(object sender, EventArgs e)
        {
            AutoStartManager.IsAutoStartEnabled = !AutoStartManager.IsAutoStartEnabled;
            autoStartMenu.Checked = AutoStartManager.IsAutoStartEnabled;
        }

        /// <summary>
        /// Event handler for the ReconnectTimer.Tick event.
        /// Triggers the reconnection process for all devices in the watcher.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ReconnectTimer_Tick(object sender, EventArgs e)
        {
            // Call the Reconnect method with all devices in the watcher.
            Reconnect(SettingsWrapper.ReconnectOnInterval, Program.Watcher.Devices.Values.ToArray());
        }

        /// <summary>
        /// Event handler for the ReconnectIntervalToggleMenu.Click event.
        /// Toggles the reconnect on interval setting and updates the UI accordingly.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ReconnectIntervalToggleMenu_Click(object sender, EventArgs e)
        {
            // Toggle the ReconnectOnInterval setting.
            SettingsWrapper.ReconnectOnInterval = !SettingsWrapper.ReconnectOnInterval;

            // Update the UI to reflect the new setting.
            reconnectIntervalToggleMenu.Checked = SettingsWrapper.ReconnectOnInterval;

            // Enable or disable the reconnect timer based on the setting.
            reconnectTimer.Enabled = SettingsWrapper.ReconnectOnInterval;
        }

        /// <summary>
        /// Event handler for the ReconnectToggleMenu.Click event.
        /// Toggles the reconnect at launch setting and updates the UI accordingly.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ReconnectToggleMenu_Click(object sender, EventArgs e)
        {
            // Toggle the ReconnectAtLaunch setting.
            SettingsWrapper.ReconnectAtLaunch = !SettingsWrapper.ReconnectAtLaunch;

            // Update the UI to reflect the new setting.
            reconnectToggleMenu.Checked = SettingsWrapper.ReconnectAtLaunch;
        }

        /// <summary>
        /// Event handler for the tray icon click.
        /// </summary>
        private void Icon_Click(object sender, EventArgs e)
        {
            menu.Show(Control.MousePosition);
        }

        /// <summary>
        /// Event handler for the "Exit" menu item click.
        /// </summary>
        private void ExitMenu_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// Populates the context menu based on the current state of audio devices.
        /// </summary>
        private void PopulateMenu(WatcherService watcher)
        {
            lock (menu)
            {
                menu.Items.Clear();

                var haveDevices = watcher.Devices.Count > 0;

                menu.Items.Add(haveDevices ? connectMenu : noDevicesMenu);
                menu.Items.Add(new ToolStripSeparator());

                foreach (var device in watcher.Devices.OrderBy(e => e.Value.Name).Select(e => e.Value))
                {
                    device.RemoveAllEvents();

                    var deviceMenuItem = new ToolStripMenuItem()
                    {
                        Text = device.Name,
                        Tag = device,
                        Checked = device.IsConnected
                    };

                    deviceMenuItem.Click += this.DeviceMenuItem_Click;

                    device.OnStateChanged += sender =>
                    {
                        deviceMenuItem.Checked = sender.IsConnected;
                        menu.Invoke(new Action(menu.Refresh));
                    };

                    menu.Items.Add(deviceMenuItem);
                }

                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(reconnectToggleMenu);
                menu.Items.Add(reconnectIntervalToggleMenu);
                menu.Items.Add(autoStartMenu);
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(exitMenu);
            }
        }

        /// <summary>
        /// Event handler for audio device menu item click.
        /// </summary>
        private async void DeviceMenuItem_Click(object sender, EventArgs e)
        {
            var tag = (sender as ToolStripMenuItem)?.Tag as WatcherDevice;

            if (tag != null)
            {
                try
                {

                    // Toggle connection state of the selected device.
                    if (!tag.IsConnected)
                    {
                        await tag.ConnectAsync();

                        if (!SettingsWrapper.ConnectedDevices.Contains(tag.Id))
                        {
                            SettingsWrapper.ConnectedDevices.Add(tag.Id);
                        }
                    }
                    else
                    {
                        tag.Disconnect();

                        if (SettingsWrapper.ConnectedDevices.Contains(tag.Id))
                        {
                            SettingsWrapper.ConnectedDevices.Remove(tag.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Display an error message if an exception occurs.
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Event handler for WatcherService's Updated event.
        /// </summary>
        private void Watcher_OnUpdated(WatcherService sender, WatcherDevice device)
        {
            // Update the context menu based on the updated state.
            PopulateMenu(sender);
        }

        /// <summary>
        /// Event handler for WatcherService's Removed event.
        /// </summary>
        private void Watcher_OnRemoved(WatcherService sender, WatcherDevice device)
        {
            // Update the context menu based on the removed device.
            PopulateMenu(sender);
        }

        /// <summary>
        /// Event handler for WatcherService's Added event.
        /// </summary>
        private async void Watcher_OnAdded(WatcherService sender, WatcherDevice device)
        {
            // Update the context menu based on the added device.
            PopulateMenu(sender);
        }

        private async Task Reconnect(bool connectCondition, IEnumerable<WatcherDevice> devices) => await Reconnect(connectCondition, devices.ToArray());

        /// <summary>
        /// Connect to a device if the reconnect setting is enabled, the device is not already connected, and the device is contained is the list of previously connected devices.
        /// </summary>
        /// <param name="devices">The device to connect to.</param>
        private async Task Reconnect(bool connectCondition, params WatcherDevice[] devices)
        {
            foreach (var device in devices)
            {
                try
                {
                    if (connectCondition && !device.IsConnected && SettingsWrapper.ConnectedDevices.Contains(device.Id))
                    {
                        await device.ConnectAsync();
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }

        /// <summary>
        /// Overrides the Dispose method to clean up resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Unregister event handlers when disposing.
                    Program.Watcher.OnAdded -= this.Watcher_OnAdded;
                    Program.Watcher.OnRemoved -= this.Watcher_OnRemoved;
                    Program.Watcher.OnUpdated -= this.Watcher_OnUpdated;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Explicit implementation of the IDisposable interface.
        /// </summary>
        public new void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
