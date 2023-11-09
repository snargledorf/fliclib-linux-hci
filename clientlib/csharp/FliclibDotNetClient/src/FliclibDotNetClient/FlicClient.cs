using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;

namespace FliclibDotNetClient
{
    /// <summary>
    /// Callback for GetInfo
    /// </summary>
    /// <param name="bluetoothControllerState">Bluetooth controller state</param>
    /// <param name="myBdAddr">The Bluetooth controller's device address</param>
    /// <param name="myBdAddrType">The type of the Bluetooth controller's device address</param>
    /// <param name="maxPendingConnections">The maximum number of pending connections (0 if unknown)</param>
    /// <param name="maxConcurrentlyConnectedButtons">The maximum number of concurrently connected buttons (-1 if unknown)</param>
    /// <param name="currentPendingConnections">Number of buttons that have at least one active connection channel</param>
    /// <param name="currentlyNoSpaceForNewConnection">Maximum number of connections is currently reached (only sent by Linux server implementation)</param>
    /// <param name="verifiedButtons">An array of verified buttons</param>
    public delegate void GetInfoResponseCallback(GetInfoResponse response);

    public record GetInfoResponse(BluetoothControllerState bluetoothControllerState, Bdaddr myBdAddr,
                                           BdAddrType myBdAddrType, byte maxPendingConnections,
                                           short maxConcurrentlyConnectedButtons, byte currentPendingConnections,
                                           bool currentlyNoSpaceForNewConnection,
                                           Bdaddr[]? verifiedButtons);

    /// <summary>
    /// Callback for GetButtonInfo
    /// </summary>
    /// <param name="bdAddr">The Bluetooth device address for the request</param>
    /// <param name="uuid">The UUID of the button. Will be null if the button was not verified before.</param>
    /// <param name="color">The color of the button. Will be null if unknown or the button was not verified before.</param>
    /// <param name="serialNumber">The serial number of the button. Will be null if the button was not verified before.</param>
    /// <param name="flicVersion">The Flic version (1 or 2). Will be 0 if the button was not verified before.</param>
    /// <param name="firmwareVersion">The firmware version of the button. Will be 0 if the button was not verified before.</param>
    public delegate void GetButtonInfoResponseCallback(GetButtonInfoResponse response);

    public record GetButtonInfoResponse(Bdaddr bdAddr, string? uuid, string? color, string? serialNumber, int flicVersion, uint firmwareVersion);

    /// <summary>
    /// NewVerifiedButtonEventArgs
    /// </summary>
    public class NewVerifiedButtonEventArgs : EventArgs
    {
        /// <summary>
        /// Bluetooth device address for new verified button
        /// </summary>
        public Bdaddr BdAddr { get; internal set; }
    }

    /// <summary>
    /// SpaceForNewConnectionEventArgs
    /// </summary>
    public class SpaceForNewConnectionEventArgs : EventArgs
    {
        /// <summary>
        /// The number of max concurrently connected buttons
        /// </summary>
        public byte MaxConcurrentlyConnectedButtons { get; internal set; }
    }

    /// <summary>
    /// BluetoothControllerStateChangeEventArgs
    /// </summary>
    public class BluetoothControllerStateChangeEventArgs : EventArgs
    {
        /// <summary>
        /// The new state of the Bluetooth controller
        /// </summary>
        public BluetoothControllerState State { get; internal set; }
    }

    /// <summary>
    /// ButtonDeletedEventArgs
    /// </summary>
    public class ButtonDeletedEventArgs : EventArgs
    {
        /// <summary>
        /// Bluetooth device address of removed button
        /// </summary>
        public Bdaddr BdAddr { get; internal set; }

        /// <summary>
        /// Whether or not the button was deleted by this client
        /// </summary>
        public bool DeletedByThisClient { get; internal set; }
    }

    /// <summary>
    /// Flic client class
    /// 
    /// For overview of the protocol and more detailed documentation, see the protocol documentation.
    /// 
    /// Create and connect a client to a server with Create or CreateAsync.
    /// Then call HandleEvents to start the event loop.
    /// </summary>
    public sealed class FlicClient : IDisposable
    {
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;

        private readonly byte[] _lengthReadBuf = new byte[2];

        private readonly ConcurrentDictionary<uint, ButtonScanner> _scanners = new();
        private readonly ConcurrentDictionary<uint, ButtonConnectionChannel> _connectionChannels = new();
        private readonly ConcurrentDictionary<uint, ScanWizard> _scanWizards = new();

        private readonly ConcurrentQueue<TaskCompletionSource<GetInfoResponse>> _getInfoTaskCompletionSourceQueue = new();
        private readonly ConcurrentQueue<TaskCompletionSource<GetButtonInfoResponse>> _getButtonInfoResponseCallbackQueue = new();

        /// <summary>
        /// Raised when a new button is verified at the server (initiated by any client)
        /// </summary>
        public event EventHandler<NewVerifiedButtonEventArgs>? NewVerifiedButton;
        
        /// <summary>
        /// Raised when the Bluetooth controller status changed, for example when it is plugged or unplugged or for any other reason becomes available / unavailable.
        /// During the controller is Detached, no scan events or button events will be received.
        /// </summary>
        public event EventHandler<BluetoothControllerStateChangeEventArgs>? BluetoothControllerStateChange;

        /// <summary>
        /// This event will be raised when the maximum number of concurrent connections has been reached (only sent by the Linux server implementation).
        /// </summary>
        public event EventHandler<SpaceForNewConnectionEventArgs>? NoSpaceForNewConnection;

        /// <summary>
        /// This event will be raised when the number of concurrent connections has decreased from the maximum by one (only sent by the Linux server implementation).
        /// </summary>
        public event EventHandler<SpaceForNewConnectionEventArgs>? GotSpaceForNewConnection;

        /// <summary>
        /// Raised when a button is deleted, or when this client tries to delete a non-existing button.
        /// </summary>
        public event EventHandler<ButtonDeletedEventArgs>? ButtonDeleted;

        private FlicClient(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
        }

        /// <summary>
        /// Connects to a server
        /// </summary>
        /// <param name="host">Hostname or IP address</param>
        /// <returns>A connected FlicClient</returns>
        /// <exception cref="System.Net.Sockets.SocketException">If a connection couldn't be established</exception>
        public static Task<FlicClient> CreateAsync(string host, CancellationToken cancellationToken = default)
        {
            return CreateAsync(host, 5551, cancellationToken);
        }

        /// <summary>
        /// Connects to a server
        /// </summary>
        /// <param name="host">Hostname or IP address</param>
        /// <param name="port">Port</param>
        /// <returns>A connected FlicClient</returns>
        /// <exception cref="System.Net.Sockets.SocketException">If a connection couldn't be established</exception>
        public static async Task<FlicClient> CreateAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            var tcpClient = new TcpClient() { NoDelay = true };
            await tcpClient.ConnectAsync(host, port, cancellationToken);

            FlicClient client = new(tcpClient);

            _ = client.HandleEventsAsync(cancellationToken);

            return client;
        }

        /// <summary>
        /// Initiates a disconnection of the FlicClient. The HandleEvents method will return once the disconnection is complete.
        /// </summary>
        public void Disconnect()
        {
            Dispose();
        }

        /// <summary>
        /// Disposes the client.
        /// The socket will be closed. If you for some reason want to close the socket before you call HandleEvents, execute this.
        /// Otherwise you should rather call Disconnect to make a more graceful disconnection.
        /// </summary>
        public void Dispose()
        {
            Debug.Assert(_tcpClient != null, $"{nameof(_tcpClient)} is null");
            Debug.Assert(_stream != null, $"{nameof(_stream)} is null");

            try
            {
                _tcpClient.Close();
                _stream.Close();
            }
            catch
            {
                // ignored
            }
        }

        public async Task<GetInfoResponse> GetInfoAsync(CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<GetInfoResponse>();

            cancellationToken.Register(() => tcs.TrySetCanceled());

            _getInfoTaskCompletionSourceQueue.Enqueue(tcs);

            await SendPacketAsync(new CmdGetInfo(), cancellationToken).ConfigureAwait(false);

            return await tcs.Task.ConfigureAwait(false);
        }

        /// <summary>
        /// Requests info for a button.
        /// A null UUID will be sent to the callback if the button was not verified before.
        /// </summary>
        /// <param name="bdAddr">Bluetooth device address</param>
        /// <param name="callback">Callback to be invoked when the response arrives</param>
        public async Task<GetButtonInfoResponse> GetButtonInfoAsync(Bdaddr bdAddr, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<GetButtonInfoResponse>();

            cancellationToken.Register(() => tcs.TrySetCanceled());

            _getButtonInfoResponseCallbackQueue.Enqueue(tcs);

            await SendPacketAsync(new CmdGetButtonInfo() { BdAddr = bdAddr }, cancellationToken).ConfigureAwait(false);

            return await tcs.Task.ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a raw scanner.
        /// The AdvertisementPacket event will be raised on the scanner for each advertisement packet received.
        /// The scanner must not already be added.
        /// </summary>
        /// <param name="buttonScanner">A ButtonScanner</param>
        public Task AddScannerAsync(ButtonScanner buttonScanner, CancellationToken cancellationToken = default)
        {
            if (buttonScanner == null)
            {
                throw new ArgumentNullException(nameof(buttonScanner));
            }

            if (!_scanners.TryAdd(buttonScanner.ScanId, buttonScanner))
            {
                throw new ArgumentException("Button scanner already added", nameof(buttonScanner));
            }

            return SendPacketAsync(new CmdCreateScanner { ScanId = buttonScanner.ScanId }, cancellationToken);
        }

        /// <summary>
        /// Removes a raw scanner.
        /// No further AdvertisementPacket events will be raised.
        /// The scanner must be currently added.
        /// </summary>
        /// <param name="buttonScanner">A ButtonScanner that was previously added</param>
        public Task RemoveScanner(ButtonScanner buttonScanner, CancellationToken cancellationToken = default)
        {
            if (buttonScanner == null)
            {
                throw new ArgumentNullException(nameof(buttonScanner));
            }

            if (!_scanners.TryRemove(buttonScanner.ScanId, out ButtonScanner? _))
            {
                throw new ArgumentException("Button scanner was not added", nameof(buttonScanner));
            }

            return SendPacketAsync(new CmdRemoveScanner { ScanId = buttonScanner.ScanId }, cancellationToken);
        }

        /// <summary>
        /// Adds and starts a ScanWizard.
        /// Events on the scan wizard will be raised as it makes progress. Eventually Completed will be raised.
        /// The scan wizard must not currently be running.
        /// </summary>
        /// <param name="scanWizard">A ScanWizard</param>
        public Task AddScanWizardAsync(ScanWizard scanWizard, CancellationToken cancellationToken = default)
        {
            if (scanWizard == null)
            {
                throw new ArgumentNullException(nameof(scanWizard));
            }

            if (!_scanWizards.TryAdd(scanWizard.ScanWizardId, scanWizard))
            {
                throw new ArgumentException("Scan wizard already added");
            }

            return SendPacketAsync(new CmdCreateScanWizard { ScanWizardId = scanWizard.ScanWizardId }, cancellationToken);
        }

        /// <summary>
        /// Cancels a ScanWizard.
        /// The Completed event will be raised with status WizardCancelledByUser, if it already wasn't completed before the server received this command.
        /// </summary>
        /// <param name="scanWizard">A ScanWizard</param>
        public Task CancelScanWizardAsync(ScanWizard scanWizard, CancellationToken cancellationToken = default)
        {
            if (scanWizard == null)
            {
                throw new ArgumentNullException(nameof(scanWizard));
            }

            return SendPacketAsync(new CmdCancelScanWizard { ScanWizardId = scanWizard.ScanWizardId }, cancellationToken);
        }

        /// <summary>
        /// Adds a connection channel.
        /// The CreateConnectionChannelResponse event will be raised with the response.
        /// If the response was success, button events will be raised when the button is pressed.
        /// </summary>
        /// <param name="channel">A ButtonConnectionChannel</param>
        public Task AddConnectionChannelAsync(ButtonConnectionChannel channel, CancellationToken cancellationToken = default)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));

            if (!_connectionChannels.TryAdd(channel.ConnId, channel))
                throw new ArgumentException("Connection channel already added");

            channel.flicClient = this;

            return SendPacketAsync(new CmdCreateConnectionChannel { ConnId = channel.ConnId, BdAddr = channel.BdAddr, LatencyMode = channel.LatencyMode, AutoDisconnectTime = channel.AutoDisconnectTime }, cancellationToken);
        }

        /// <summary>
        /// Removes a connection channel.
        /// Button events will no longer be received after the server has received this command.
        /// </summary>
        /// <param name="channel">A ButtonConnectionChannel</param>
        public Task RemoveConnectionChannelAsync(ButtonConnectionChannel channel, CancellationToken cancellationToken = default)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));

            return SendPacketAsync(new CmdRemoveConnectionChannel { ConnId = channel.ConnId }, cancellationToken);
        }

        public Task UpdateConnectionChannelModeParametersAsync(ButtonConnectionChannel channel, CancellationToken cancellationToken = default)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));

            return SendPacketAsync(new CmdChangeModeParameters { ConnId = channel.ConnId, AutoDisconnectTime = channel.AutoDisconnectTime, LatencyMode = channel.LatencyMode }, cancellationToken);
        }

        /// <summary>
        /// Forces disconnect of a button.
        /// All connection channels among all clients the server has for this button will be removed.
        /// </summary>
        /// <param name="bdAddr">Bluetooth device address</param>
        public Task ForceDisconnectAsync(Bdaddr bdAddr, CancellationToken cancellationToken = default)
        {
            if (bdAddr == Bdaddr.Blank)
                throw new ArgumentNullException(nameof(bdAddr), $"{nameof(bdAddr)} is null.");

            return SendPacketAsync(new CmdForceDisconnect { BdAddr = bdAddr }, cancellationToken);
        }

        private async Task SendPacketAsync(CommandPacket packet, CancellationToken cancellationToken)
        {
            Debug.Assert(_tcpClient != null, $"{nameof(_tcpClient)} is null");

            if (!_tcpClient.Connected)
                return;

            var buf = packet.Construct();

            await SendBufferAsync(buf, cancellationToken).ConfigureAwait(false);
        }

        private async Task SendBufferAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            Debug.Assert(_stream != null, $"{nameof(_stream)} is null");

            try
            {
                await _stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException)
            {
                Disconnect();
                return;
            }
        }

        public async Task HandleEventsAsync(CancellationToken cancellationToken = default)
        {
            Debug.Assert(_tcpClient != null, $"{nameof(_tcpClient)} is null");
            Debug.Assert(_stream != null, $"{nameof(_stream)} is null");

            byte[]? pkt = null;
            Memory<byte> packetBuffer;
            try
            {
                while (_tcpClient.Connected)
                {
                    try
                    {
                        var buffer = _lengthReadBuf.AsMemory();

                        var received = await _stream.ReadAsync(buffer, cancellationToken);
                        if (received == 0)
                            break;

                        // Need at least 2 bytes for length
                        if (received == 1)
                        {
                            received = await _stream.ReadAsync(buffer[1..], cancellationToken);
                            if (received == 0)
                                break;
                        }

                        var len = buffer.Span[0] | (buffer.Span[1] << 8);

                        if (len == 0)
                            continue;

                        if (pkt != null)
                            ArrayPool<byte>.Shared.Return(pkt);

                        pkt = ArrayPool<byte>.Shared.Rent(len);
                        packetBuffer = pkt.AsMemory(0, len);

                        var pos = 0;
                        while (pos < len)
                        {
                            received = await _stream.ReadAsync(packetBuffer[pos..], cancellationToken);
                            if (received == 0)
                            {
                                break;
                            }
                            pos += received;
                        }
                    }
                    catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or TaskCanceledException or OperationCanceledException)
                    {
                        break;
                    }

                    DispatchPacket(packetBuffer);
                }
            }
            finally
            {
                if (pkt != null)
                    ArrayPool<byte>.Shared.Return(pkt);
            }
        }

        private void DispatchPacket(ReadOnlyMemory<byte> packet)
        {
            EventPacketOpCode opcode = (EventPacketOpCode)packet.Span[0];
            packet = packet[1..];

            switch (opcode)
            {
                case EventPacketOpCode.EVT_ADVERTISEMENT_PACKET_OPCODE:
                    {
                        var pkt = new EvtAdvertisementPacket();
                        pkt.Parse(packet);
                        
                        if (_scanners.TryGetValue(pkt.ScanId, out ButtonScanner? scanner))
                            scanner.OnAdvertisementPacket(new AdvertisementPacketEventArgs { BdAddr = pkt.BdAddr, Name = pkt.Name, Rssi = pkt.Rssi, IsPrivate = pkt.IsPrivate, AlreadyVerified = pkt.AlreadyVerified, AlreadyConnectedToThisDevice = pkt.AlreadyConnectedToThisDevice, AlreadyConnectedToOtherDevice = pkt.AlreadyConnectedToOtherDevice });
                    }
                    break;
                case EventPacketOpCode.EVT_CREATE_CONNECTION_CHANNEL_RESPONSE_OPCODE:
                    {
                        var pkt = new EvtCreateConnectionChannelResponse();
                        pkt.Parse(packet);
                        if (pkt.Error != CreateConnectionChannelError.NoError)
                        {
                            _connectionChannels.TryRemove(pkt.ConnId, out _);
                        }
                        else
                        {
                            var channel = _connectionChannels[pkt.ConnId];
                            channel.OnCreateConnectionChannelResponse(new CreateConnectionChannelResponseEventArgs { Error = pkt.Error, ConnectionStatus = pkt.ConnectionStatus });
                        }
                    }
                    break;
                case EventPacketOpCode.EVT_CONNECTION_STATUS_CHANGED_OPCODE:
                    {
                        var pkt = new EvtConnectionStatusChanged();
                        pkt.Parse(packet);

                        var channel = _connectionChannels[pkt.ConnId];
                        channel.OnConnectionStatusChanged(new ConnectionStatusChangedEventArgs { ConnectionStatus = pkt.ConnectionStatus, DisconnectReason = pkt.DisconnectReason });
                    }
                    break;
                case EventPacketOpCode.EVT_CONNECTION_CHANNEL_REMOVED_OPCODE:
                    {
                        var pkt = new EvtConnectionChannelRemoved();
                        pkt.Parse(packet);
                        if (_connectionChannels.TryRemove(pkt.ConnId, out ButtonConnectionChannel? channel))
                            channel.OnRemoved(new ConnectionChannelRemovedEventArgs { RemovedReason = pkt.RemovedReason });
                    }
                    break;
                case EventPacketOpCode.EVT_BUTTON_UP_OR_DOWN_OPCODE:
                case EventPacketOpCode.EVT_BUTTON_CLICK_OR_HOLD_OPCODE:
                case EventPacketOpCode.EVT_BUTTON_SINGLE_OR_DOUBLE_CLICK_OPCODE:
                case EventPacketOpCode.EVT_BUTTON_SINGLE_OR_DOUBLE_CLICK_OR_HOLD_OPCODE:
                    {
                        var pkt = new EvtButtonEvent();
                        pkt.Parse(packet);
                        var channel = _connectionChannels[pkt.ConnId];
                        var eventArgs = new ButtonEventEventArgs { ClickType = pkt.ClickType, WasQueued = pkt.WasQueued, TimeDiff = pkt.TimeDiff };
                        switch (opcode)
                        {
                            case EventPacketOpCode.EVT_BUTTON_UP_OR_DOWN_OPCODE:
                                channel.OnButtonUpOrDown(eventArgs);
                                break;
                            case EventPacketOpCode.EVT_BUTTON_CLICK_OR_HOLD_OPCODE:
                                channel.OnButtonClickOrHold(eventArgs);
                                break;
                            case EventPacketOpCode.EVT_BUTTON_SINGLE_OR_DOUBLE_CLICK_OPCODE:
                                channel.OnButtonSingleOrDoubleClick(eventArgs);
                                break;
                            case EventPacketOpCode.EVT_BUTTON_SINGLE_OR_DOUBLE_CLICK_OR_HOLD_OPCODE:
                                channel.OnButtonSingleOrDoubleClickOrHold(eventArgs);
                                break;
                        }
                    }
                    break;
                case EventPacketOpCode.EVT_NEW_VERIFIED_BUTTON_OPCODE:
                    {
                        var pkt = new EvtNewVerifiedButton();
                        pkt.Parse(packet);
                        NewVerifiedButton?.Invoke(this, new NewVerifiedButtonEventArgs { BdAddr = pkt.BdAddr });
                    }
                    break;
                case EventPacketOpCode.EVT_GET_INFO_RESPONSE_OPCODE:
                    {
                        var pkt = new EvtGetInfoResponse();
                        pkt.Parse(packet);

                        if (_getInfoTaskCompletionSourceQueue.TryDequeue(out TaskCompletionSource<GetInfoResponse>? taskCompletionSource))
                            taskCompletionSource.TrySetResult(new(pkt.BluetoothControllerState, pkt.MyBdAddr, pkt.MyBdAddrType, pkt.MaxPendingConnections, pkt.MaxConcurrentlyConnectedButtons, pkt.CurrentPendingConnections, pkt.CurrentlyNoSpaceForNewConnection, pkt.BdAddrOfVerifiedButtons));
                    }
                    break;
                case EventPacketOpCode.EVT_NO_SPACE_FOR_NEW_CONNECTION_OPCODE:
                    {
                        var pkt = new EvtNoSpaceForNewConnection();
                        pkt.Parse(packet);
                        NoSpaceForNewConnection?.Invoke(this, new SpaceForNewConnectionEventArgs { MaxConcurrentlyConnectedButtons = pkt.MaxConcurrentlyConnectedButtons });
                    }
                    break;
                case EventPacketOpCode.EVT_GOT_SPACE_FOR_NEW_CONNECTION_OPCODE:
                    {
                        var pkt = new EvtGotSpaceForNewConnection();
                        pkt.Parse(packet);
                        GotSpaceForNewConnection?.Invoke(this, new SpaceForNewConnectionEventArgs { MaxConcurrentlyConnectedButtons = pkt.MaxConcurrentlyConnectedButtons });
                    }
                    break;
                case EventPacketOpCode.EVT_BLUETOOTH_CONTROLLER_STATE_CHANGE_OPCODE:
                    {
                        var pkt = new EvtBluetoothControllerStateChange();
                        pkt.Parse(packet);
                        BluetoothControllerStateChange?.Invoke(this, new BluetoothControllerStateChangeEventArgs { State = pkt.State });
                    }
                    break;
                case EventPacketOpCode.EVT_GET_BUTTON_INFO_RESPONSE_OPCODE:
                    {
                        var pkt = new EvtGetButtonInfoResponse();
                        pkt.Parse(packet);
                        if (_getButtonInfoResponseCallbackQueue.TryDequeue(out TaskCompletionSource<GetButtonInfoResponse>? taskCompletionSource))
                            taskCompletionSource.TrySetResult(new(pkt.BdAddr, pkt.Uuid, pkt.Color, pkt.SerialNumber, pkt.FlicVersion, pkt.FirmwareVersion));
                    }
                    break;
                case EventPacketOpCode.EVT_SCAN_WIZARD_FOUND_PRIVATE_BUTTON_OPCODE:
                    {
                        var pkt = new EvtScanWizardFoundPrivateButton();
                        pkt.Parse(packet);
                        _scanWizards[pkt.ScanWizardId].OnFoundPrivateButton();
                    }
                    break;
                case EventPacketOpCode.EVT_SCAN_WIZARD_FOUND_PUBLIC_BUTTON_OPCODE:
                    {
                        var pkt = new EvtScanWizardFoundPublicButton();
                        pkt.Parse(packet);
                        var wizard = _scanWizards[pkt.ScanWizardId];
                        wizard.BdAddr = pkt.BdAddr;
                        wizard.Name = pkt.Name;
                        wizard.OnFoundPublicButton(new ScanWizardButtonInfoEventArgs { BdAddr = wizard.BdAddr, Name = wizard.Name });
                    }
                    break;
                case EventPacketOpCode.EVT_SCAN_WIZARD_BUTTON_CONNECTED_OPCODE:
                    {
                        var pkt = new EvtScanWizardButtonConnected();
                        pkt.Parse(packet);
                        var wizard = _scanWizards[pkt.ScanWizardId];
                        wizard.OnButtonConnected(new ScanWizardButtonInfoEventArgs { BdAddr = wizard.BdAddr, Name = wizard.Name });
                    }
                    break;
                case EventPacketOpCode.EVT_SCAN_WIZARD_COMPLETED_OPCODE:
                    {
                        var pkt = new EvtScanWizardCompleted();
                        pkt.Parse(packet);

                        if (_scanWizards.TryRemove(pkt.ScanWizardId, out ScanWizard? wizard))
                        {
                            var eventArgs = new ScanWizardCompletedEventArgs { BdAddr = wizard.BdAddr, Name = wizard.Name, Result = pkt.Result };
                            wizard.BdAddr = Bdaddr.Blank;
                            wizard.Name = null;
                            wizard.OnCompleted(eventArgs);
                        }
                    }
                    break;
                case EventPacketOpCode.EVT_BUTTON_DELETED_OPCODE:
                    {
                        var pkt = new EvtButtonDeleted();
                        pkt.Parse(packet);
                        ButtonDeleted?.Invoke(this, new ButtonDeletedEventArgs { BdAddr = pkt.BdAddr, DeletedByThisClient = pkt.DeletedByThisClient });
                    }
                    break;
            }
        }
    }
}
