﻿using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FliclibDotNetClient
{
    public record GetInfoResponse(BluetoothControllerState bluetoothControllerState, Bdaddr myBdAddr,
                                           BdAddrType myBdAddrType, byte maxPendingConnections,
                                           short maxConcurrentlyConnectedButtons, byte currentPendingConnections,
                                           bool currentlyNoSpaceForNewConnection,
                                           FlicButton[]? verifiedButtons);

    public record GetButtonInfoResponse(Bdaddr Bdaddr, FlicButtonInfo ButtonInfo);

    /// <summary>
    /// NewVerifiedButtonEventArgs
    /// </summary>
    public class NewVerifiedButtonEventArgs : EventArgs
    {
        /// <summary>
        /// Bluetooth device address for new verified button
        /// </summary>
        public FlicButton? Button { get; internal set; }
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
        private readonly ConcurrentDictionary<uint, ButtonScanner> _scanners = new();

        private readonly ConcurrentDictionary<uint, TaskCompletionSource<EvtCreateConnectionChannelResponse>> _createConnectionChannelCompletionSources = new();
        private readonly ConcurrentDictionary<uint, ButtonConnectionChannel> _connectionChannels = new();

        private readonly ConcurrentDictionary<uint, ScanWizard> _scanWizards = new();

        private readonly ConcurrentDictionary<Bdaddr, TaskCompletionSource<EvtButtonDeleted>> _deleteButtonTaskCompletionSources = new();

        private readonly ConcurrentQueue<TaskCompletionSource<EvtGetInfoResponse>> _getInfoTaskCompletionSourceQueue = new();
        private readonly ConcurrentDictionary<Bdaddr, TaskCompletionSource<EvtGetButtonInfoResponse>> _getButtonInfoTaskCompletionSources = new();

        private readonly CancellationTokenSource handleEventsCancellationSource = new();
        
        private readonly string host;
        private readonly int port;
        private readonly TcpClient tcpClient;

        private FlicPacketReader? reader;
        private FlicPacketWriter? writer;

        private bool disposedValue;

        public FlicClient(string host, int port = 5551)
        {
            this.port = port;
            this.host = host;
            tcpClient = new TcpClient() { NoDelay = true };
        }

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

        public event EventHandler OnDisconnect;

        public event EventHandler<Exception> OnException;

        /// <summary>
        /// Raised when a button is deleted, or when this client tries to delete a non-existing button.
        /// </summary>
        public event EventHandler<ButtonDeletedEventArgs>? ButtonDeleted;

        public async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            await tcpClient.ConnectAsync(host, port, cancellationToken);

            var stream = tcpClient.GetStream();
            reader = new FlicPacketReader(stream);
            writer = new FlicPacketWriter(stream);

            _ = HandleEventsAsync(handleEventsCancellationSource.Token);
        }

        /// <summary>
        /// Initiates a disconnection of the FlicClient. The HandleEvents method will return once the disconnection is complete.
        /// </summary>
        public void Disconnect()
        {
            Dispose();

            FireOnDisconnectEvent();
        }

        public async Task<GetInfoResponse> GetInfoAsync(CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<EvtGetInfoResponse>();

            cancellationToken.Register(() => tcs.TrySetCanceled());

            _getInfoTaskCompletionSourceQueue.Enqueue(tcs);

            await SendCommandAsync(new CmdGetInfo(), cancellationToken).ConfigureAwait(false);

            var response = await tcs.Task.ConfigureAwait(false);

            return new(
                response.BluetoothControllerState,
                response.MyBdAddr,
                response.MyBdAddrType,
                response.MaxPendingConnections,
                response.MaxConcurrentlyConnectedButtons,
                response.CurrentPendingConnections,
                response.CurrentlyNoSpaceForNewConnection,
                response.BdAddrOfVerifiedButtons?.Select(bdaddr => new FlicButton(this, bdaddr)).ToArray());
        }

        public ButtonScanner CreateScanner()
        {
            return new ButtonScanner(this);
        }

        /// <summary>
        /// Adds and starts a ScanWizard.
        /// Events on the scan wizard will be raised as it makes progress. Eventually Completed will be raised.
        /// The scan wizard must not currently be running.
        /// </summary>
        /// <param name="scanWizard">A ScanWizard</param>
        public ScanWizard CreateScanWizard()
        {
            return new ScanWizard(this);
        }

        /// <summary>
        /// Requests info for a button.
        /// A null UUID will be sent to the callback if the button was not verified before.
        /// </summary>
        /// <param name="bdAddr">Bluetooth device address</param>
        /// <param name="callback">Callback to be invoked when the response arrives</param>
        internal async Task<FlicButtonInfo> GetButtonInfoAsync(FlicButton button, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<EvtGetButtonInfoResponse>();

            cancellationToken.Register(() => tcs.TrySetCanceled());

            _getButtonInfoTaskCompletionSources.TryAdd(button.Bdaddr, tcs);

            await SendCommandAsync(new CmdGetButtonInfo() { BdAddr = button.Bdaddr }, cancellationToken).ConfigureAwait(false);

            var response = await tcs.Task.ConfigureAwait(false);

            return new(response.Uuid, response.Color, response.SerialNumber, response.FlicVersion, response.FirmwareVersion);
        }

        /// <summary>
        /// Adds a raw scanner.
        /// The AdvertisementPacket event will be raised on the scanner for each advertisement packet received.
        /// The scanner must not already be added.
        /// </summary>
        /// <param name="buttonScanner">A ButtonScanner</param>
        internal ValueTask StartAsync(ButtonScanner buttonScanner, CancellationToken cancellationToken = default)
        {
            if (buttonScanner == null)
            {
                throw new ArgumentNullException(nameof(buttonScanner));
            }

            if (!_scanners.TryAdd(buttonScanner.ScanId, buttonScanner))
            {
                throw new ArgumentException("Button scanner already added", nameof(buttonScanner));
            }

            return SendCommandAsync(new CmdCreateScanner { ScanId = buttonScanner.ScanId }, cancellationToken);
        }

        internal ValueTask StartAsync(ScanWizard scanWizard, CancellationToken cancellationToken = default)
        {
            if (!_scanWizards.TryAdd(scanWizard.ScanWizardId, scanWizard))
            {
                throw new ArgumentException("Scan wizard already added");
            }

            return SendCommandAsync(new CmdCreateScanWizard { ScanWizardId = scanWizard.ScanWizardId }, cancellationToken);
        }

        /// <summary>
        /// Cancels a ScanWizard.
        /// The Completed event will be raised with status WizardCancelledByUser, if it already wasn't completed before the server received this command.
        /// </summary>
        /// <param name="scanWizard">A ScanWizard</param>
        internal ValueTask CancelAsync(ScanWizard scanWizard, CancellationToken cancellationToken = default)
        {
            if (scanWizard == null)
            {
                throw new ArgumentNullException(nameof(scanWizard));
            }

            return SendCommandAsync(new CmdCancelScanWizard { ScanWizardId = scanWizard.ScanWizardId }, cancellationToken);
        }

        /// <summary>
        /// Removes a raw scanner.
        /// No further AdvertisementPacket events will be raised.
        /// The scanner must be currently added.
        /// </summary>
        /// <param name="buttonScanner">A ButtonScanner that was previously added</param>
        internal ValueTask StopAsync(ButtonScanner buttonScanner, CancellationToken cancellationToken = default)
        {
            if (buttonScanner == null)
            {
                throw new ArgumentNullException(nameof(buttonScanner));
            }

            if (!_scanners.TryRemove(buttonScanner.ScanId, out ButtonScanner? _))
            {
                throw new ArgumentException("Button scanner was not added", nameof(buttonScanner));
            }

            return SendCommandAsync(new CmdRemoveScanner { ScanId = buttonScanner.ScanId }, cancellationToken);
        }

        /// <summary>
        /// Adds a connection channel.
        /// The CreateConnectionChannelResponse event will be raised with the response.
        /// If the response was success, button events will be raised when the button is pressed.
        /// </summary>
        /// <param name="channel">A ButtonConnectionChannel</param>
        internal async Task<ButtonConnectionChannel> OpenButtonConnectionChannelAsync(FlicButton button, LatencyMode latencyMode = LatencyMode.NormalLatency, short autoDisconnectTime = ButtonConnectionChannel.DefaultAutoDisconnectTime, CancellationToken cancellationToken = default)
        {
            if (button == null)
                throw new ArgumentNullException(nameof(button), $"{nameof(button)} is null.");

            var tcs = new TaskCompletionSource<EvtCreateConnectionChannelResponse>();
            cancellationToken.Register(() => tcs.TrySetCanceled());

            var command = new CmdCreateConnectionChannel { BdAddr = button.Bdaddr, LatencyMode = latencyMode, AutoDisconnectTime = autoDisconnectTime };

            if (!_createConnectionChannelCompletionSources.TryAdd(command.ConnId, tcs))
                throw new InvalidOperationException("Channel id already requested");

            await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

            var response = await tcs.Task.ConfigureAwait(false);

            if (response.Error != CreateConnectionChannelError.NoError)
                throw new InvalidOperationException(response.Error.ToString());

            var channel = new ButtonConnectionChannel(response.ConnId, button, latencyMode, autoDisconnectTime);

            if (!_connectionChannels.TryAdd(channel.ConnId, channel))
                throw new InvalidOperationException("Duplicate channel id");

            return channel;
        }

        /// <summary>
        /// Removes a connection channel.
        /// Button events will no longer be received after the server has received this command.
        /// </summary>
        /// <param name="channel">A ButtonConnectionChannel</param>
        internal ValueTask CloseButtonConnectionChannelAsync(ButtonConnectionChannel channel, CancellationToken cancellationToken = default)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));

            return SendCommandAsync(new CmdRemoveConnectionChannel { ConnId = channel.ConnId }, cancellationToken);
        }

        internal ValueTask UpdateConnectionChannelModeParametersAsync(ButtonConnectionChannel channel, CancellationToken cancellationToken = default)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));

            return SendCommandAsync(new CmdChangeModeParameters { ConnId = channel.ConnId, AutoDisconnectTime = channel.AutoDisconnectTime, LatencyMode = channel.LatencyMode }, cancellationToken);
        }

        /// <summary>
        /// Forces disconnect of a button.
        /// All connection channels among all clients the server has for this button will be removed.
        /// </summary>
        /// <param name="bdAddr">Bluetooth device address</param>
        internal ValueTask DisconnectAsync(FlicButton button, CancellationToken cancellationToken = default)
        {
            if (button == null)
                throw new ArgumentNullException(nameof(button), $"{nameof(button)} is null.");

            return SendCommandAsync(new CmdForceDisconnect { BdAddr = button.Bdaddr }, cancellationToken);
        }

        internal async Task DeleteAsync(FlicButton flicButton, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<EvtButtonDeleted>();

            cancellationToken.Register(() => tcs.TrySetCanceled());

            if (!_deleteButtonTaskCompletionSources.TryAdd(flicButton.Bdaddr, tcs))
                throw new InvalidOperationException($"Delete already requested for {flicButton.Bdaddr}");

            await SendCommandAsync(new CmdDeleteButton { BdAddr = flicButton.Bdaddr }, cancellationToken).ConfigureAwait(false);

            await tcs.Task.ConfigureAwait(false);
        }

        private ValueTask SendCommandAsync(FlicCommand command, CancellationToken cancellationToken = default)
        {
            if (disposedValue)
                throw new ObjectDisposedException(nameof(FlicClient));

            FlicPacket packet = command.ToPacket();

            if (writer == null)
                throw new InvalidOperationException("Client is not running");

            return writer.WritePacketAsync(packet, cancellationToken);
        }

        private async Task HandleEventsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (reader == null)
                        throw new InvalidOperationException("Client is not running");

                    FlicPacket packet = await reader.ReadPacketAsync(cancellationToken);

                    // Check if we have reached the end of the stream
                    if (packet == FlicPacket.None)
                        break;

                    DispatchPacket(packet);
                }
            }
            catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
            {
                // Ignore
            }
            catch (Exception ex)
            {
                Disconnect();
                FireOnExceptionEvent(ex);
            }
        }

        private void DispatchPacket(FlicPacket packet)
        {
            switch ((EventPacketOpCode)packet.OpCode)
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
                        
                        if (_createConnectionChannelCompletionSources.TryGetValue(pkt.ConnId, out var tcs))
                        {
                            tcs.TrySetResult(pkt);
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
                        FireChannelButtonEvent((EventPacketOpCode)packet.OpCode, channel, eventArgs);
                    }
                    break;
                case EventPacketOpCode.EVT_NEW_VERIFIED_BUTTON_OPCODE:
                    {
                        var pkt = new EvtNewVerifiedButton();
                        pkt.Parse(packet);
                        NewVerifiedButton?.Invoke(this, new NewVerifiedButtonEventArgs { Button = new FlicButton(this, pkt.BdAddr) });
                    }
                    break;
                case EventPacketOpCode.EVT_GET_INFO_RESPONSE_OPCODE:
                    {
                        var pkt = new EvtGetInfoResponse();
                        pkt.Parse(packet);

                        if (_getInfoTaskCompletionSourceQueue.TryDequeue(out TaskCompletionSource<EvtGetInfoResponse>? taskCompletionSource))
                            taskCompletionSource.TrySetResult(pkt);
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
                        if (_getButtonInfoTaskCompletionSources.TryRemove(pkt.BdAddr, out TaskCompletionSource<EvtGetButtonInfoResponse>? taskCompletionSource))
                            taskCompletionSource.TrySetResult(pkt);
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

                        if (_scanWizards.TryGetValue(pkt.ScanWizardId, out ScanWizard? wizard))
                        {
                            wizard.BdAddr = pkt.BdAddr;
                            wizard.Name = pkt.Name;
                            wizard.OnFoundPublicButton(new ScanWizardButtonInfoEventArgs { BdAddr = wizard.BdAddr, Name = wizard.Name });
                        }
                    }
                    break;
                case EventPacketOpCode.EVT_SCAN_WIZARD_BUTTON_CONNECTED_OPCODE:
                    {
                        var pkt = new EvtScanWizardButtonConnected();
                        pkt.Parse(packet);
                        if (_scanWizards.TryGetValue(pkt.ScanWizardId, out ScanWizard? wizard))
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
                            wizard.OnCompleted(eventArgs);
                        }
                    }
                    break;
                case EventPacketOpCode.EVT_BUTTON_DELETED_OPCODE:
                    {
                        var pkt = new EvtButtonDeleted();
                        pkt.Parse(packet);

                        if (_deleteButtonTaskCompletionSources.TryRemove(pkt.BdAddr, out TaskCompletionSource<EvtButtonDeleted>? tcs))
                            tcs.TrySetResult(pkt);
                        
                        // TODO Delete event on FlicButton class
                        ButtonDeleted?.Invoke(this, new ButtonDeletedEventArgs { BdAddr = pkt.BdAddr, DeletedByThisClient = pkt.DeletedByThisClient });
                    }
                    break;
            }
        }

        private static void FireChannelButtonEvent(EventPacketOpCode opCode, ButtonConnectionChannel channel, ButtonEventEventArgs eventArgs)
        {
            switch (opCode)
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
                default:
                    throw new InvalidOperationException($"EventPacketOpCode is not a click event: {opCode}");
            }
        }

        private void FireOnExceptionEvent(Exception ex)
        {
            OnException?.Invoke(this, ex);
        }

        private void FireOnDisconnectEvent()
        {
            OnDisconnect?.Invoke(this, EventArgs.Empty);
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    handleEventsCancellationSource.Cancel();
                    tcpClient.Close();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~FlicClient()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
