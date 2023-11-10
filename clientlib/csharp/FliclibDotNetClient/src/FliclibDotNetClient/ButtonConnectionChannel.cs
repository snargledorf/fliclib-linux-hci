using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FliclibDotNetClient
{
    /// <summary>
    /// ConnectionChannelRemovedEventArgs
    /// </summary>
    public class ConnectionChannelRemovedEventArgs : EventArgs
    {
        /// <summary>
        /// Reason for this connection channel being removed.
        /// </summary>
        public RemovedReason RemovedReason { get; internal set; }
    }

    /// <summary>
    /// ConnectionStatusChangedEventArgs
    /// </summary>
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// New connection status
        /// </summary>
        public ConnectionStatus ConnectionStatus { get; internal set; }

        /// <summary>
        /// If the connection status is Disconnected, this contains the reason. Otherwise this parameter is considered invalid.
        /// </summary>
        public DisconnectReason DisconnectReason { get; internal set; }
    }

    /// <summary>
    /// ButtonEventEventArgs
    /// </summary>
    public class ButtonEventEventArgs : EventArgs
    {
        /// <summary>
        /// The possible click type values depend on the event type
        /// </summary>
        public ClickType ClickType { get; internal set; }

        /// <summary>
        /// If this button event happened during the button was disconnected or not
        /// </summary>
        public bool WasQueued { get; internal set; }

        /// <summary>
        /// If this button event happened during the button was disconnected,
        /// this will be the number of seconds since that event happened (otherwise it will most likely be 0).
        /// Depending on your application, you might want to discard too old events.
        /// </summary>
        public uint TimeDiff { get; internal set; }
    }

    /// <summary>
    /// A button connection channel.
    /// 
    /// Register the events and add an instance of this class to a FlicClient with AddConnectionChannel.
    /// </summary>
    public class ButtonConnectionChannel
    {
        public const int DefaultAutoDisconnectTime = 511;

        /// <summary>
        /// Full Constructor with all options
        /// </summary>
        /// <param name="button">The button associated with this channel</param>
        /// <param name="latencyMode">Latency mode</param>
        /// <param name="autoDisconnectTime">Auto disconnect time</param>
        internal ButtonConnectionChannel(uint connId, FlicButton button, LatencyMode latencyMode = LatencyMode.NormalLatency, short autoDisconnectTime = DefaultAutoDisconnectTime)
        {
            ConnId = connId;
            Button = button ?? throw new ArgumentNullException(nameof(button), $"{nameof(button)} is null.");
            LatencyMode = latencyMode;
            AutoDisconnectTime = autoDisconnectTime;
        }

        public uint ConnId { get; }

        /// <summary>
        /// Gets the Bluetooth device address that is assigned to this connection channel
        /// </summary>
        public FlicButton Button { get; }

        /// <summary>
        /// Gets the latency mode for this connection channel
        /// </summary>
        public LatencyMode LatencyMode { get; private set; }

        /// <summary>
        /// Gets the auto disconnect time for this connection channel. The new value will be applied the next time the button connects.
        /// </summary>
        public short AutoDisconnectTime { get; private set; }

        public Task UpdateLatencyModeAsync(LatencyMode latencyMode, CancellationToken cancellationToken = default)
        {
            if (latencyMode == LatencyMode)
                return Task.CompletedTask;

            LatencyMode = latencyMode;

            return UpdateConnectionChannelModeParametersAsync(cancellationToken);
        }

        public Task UpdateAutoDisconnectTimeAsync(short autoDisconnectTime, CancellationToken cancellationToken = default)
        {
            if (autoDisconnectTime == AutoDisconnectTime)
                return Task.CompletedTask;

            AutoDisconnectTime = autoDisconnectTime;

            return UpdateConnectionChannelModeParametersAsync(cancellationToken);
        }

        private async Task UpdateConnectionChannelModeParametersAsync(CancellationToken cancellationToken)
        {
            await Button.FlicClient.UpdateConnectionChannelModeParametersAsync(this, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Event raised when the connection channel has been removed at the server
        /// </summary>
        public event EventHandler<ConnectionChannelRemovedEventArgs>? Removed;

        /// <summary>
        /// Event raised when the connection status has changed
        /// </summary>
        public event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

        /// <summary>
        /// Used to simply know when the button was pressed or released.
        /// </summary>
        public event EventHandler<ButtonEventEventArgs>? ButtonUpOrDown;

        /// <summary>
        /// Used if you want to distinguish between click and hold.
        /// </summary>
        public event EventHandler<ButtonEventEventArgs>? ButtonClickOrHold;

        /// <summary>
        /// Used if you want to distinguish between a single click and a double click.
        /// </summary>
        public event EventHandler<ButtonEventEventArgs>? ButtonSingleOrDoubleClick;

        /// <summary>
        /// Used if you want to distinguish between a single click, a double click and a hold.
        /// </summary>
        public event EventHandler<ButtonEventEventArgs>? ButtonSingleOrDoubleClickOrHold;

        protected internal virtual void OnRemoved(ConnectionChannelRemovedEventArgs e)
        {
            Removed?.Invoke(this, e);
        }

        protected internal virtual void OnConnectionStatusChanged(ConnectionStatusChangedEventArgs e)
        {
            ConnectionStatusChanged?.Invoke(this, e);
        }

        protected internal virtual void OnButtonUpOrDown(ButtonEventEventArgs e)
        {
            ButtonUpOrDown?.Invoke(this, e);
        }

        protected internal virtual void OnButtonClickOrHold(ButtonEventEventArgs e)
        {
            ButtonClickOrHold?.Invoke(this, e);
        }

        protected internal virtual void OnButtonSingleOrDoubleClick(ButtonEventEventArgs e)
        {
            ButtonSingleOrDoubleClick?.Invoke(this, e);
        }

        protected internal virtual void OnButtonSingleOrDoubleClickOrHold(ButtonEventEventArgs e)
        {
            ButtonSingleOrDoubleClickOrHold?.Invoke(this, e);
        }

        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            return Button.CloseConnectionAsync(this, cancellationToken);
        }
    }
}
