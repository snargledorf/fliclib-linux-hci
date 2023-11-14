using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Text;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Collections;

namespace FliclibDotNetClient
{
    internal enum EventPacketOpCode : byte
    {
        EvtAdvertisementPacket = 0,
        EvtCreateConnectionChannelResponse = 1,
        EvtConnectionStatusChanged = 2,
        EvtConnectionChannelRemoved = 3,
        EvtButtonUpDownEvent = 4,
        EvtButtonClickOrHoldEvent = 5,
        EvtButtonSingleOrDoubleClickEvent = 6,
        EvtButtonSingleOrDoubleOrHoldEvent = 7,
        EvtNewVerifiedButton = 8,
        EvtGetInfoResponse = 9,
        EvtNoSpaceForNewConnection = 10,
        EvtGotSpaceForNewConnection = 11,
        EvtBluetoothControllerStateChange = 12,
        EvtPingResponse = 13,
        EvtGetButtonInfoResponse = 14,
        EvtScanWizardFoundPrivateButton = 15,
        EvtScanWizardFoundPublicButton = 16,
        EvtScanWizardButtonConnected = 17,
        EvtScanWizardCompleted = 18,
        EvtButtonDeleted = 19,
    }

    internal struct EvtAdvertisement
    {
        public EvtAdvertisement(
            uint scanId,
            Bdaddr bdAddr,
            string? name,
            int rssi,
            bool isPrivate,
            bool alreadyVerified,
            bool alreadyConnectedToThisDevice,
            bool alreadyConnectedToOtherDevice)
            : this()
        {
            ScanId = scanId;
            BdAddr = bdAddr;
            Name = name;
            Rssi = rssi;
            IsPrivate = isPrivate;
            AlreadyVerified = alreadyVerified;
            AlreadyConnectedToThisDevice = alreadyConnectedToThisDevice;
            AlreadyConnectedToOtherDevice = alreadyConnectedToOtherDevice;
        }

        internal uint ScanId { get; }
        internal Bdaddr BdAddr { get; }
        internal string? Name { get; }
        internal int Rssi { get; }
        internal bool IsPrivate { get; }
        internal bool AlreadyVerified { get; }
        internal bool AlreadyConnectedToThisDevice { get; }
        internal bool AlreadyConnectedToOtherDevice { get; }

        public static EvtAdvertisement FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);

            return new EvtAdvertisement(
                reader.ReadUInt32(),
                reader.ReadBdAddr(),
                reader.ReadString(reader.ReadByte()),
                reader.ReadSByte(),
                reader.ReadBoolean(),
                reader.ReadBoolean(),
                reader.ReadBoolean(),
                reader.ReadBoolean());
        }
    }

    internal struct EvtCreateConnectionChannelResponse
    {
        public EvtCreateConnectionChannelResponse(uint connId, CreateConnectionChannelError error, ConnectionStatus connectionStatus)
            : this()
        {
            ConnId = connId;
            Error = error;
            ConnectionStatus = connectionStatus;
        }

        internal uint ConnId { get; }
        internal CreateConnectionChannelError Error { get; }
        internal ConnectionStatus ConnectionStatus { get; }

        public static EvtCreateConnectionChannelResponse FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);
            return new(
                reader.ReadUInt32(),
                (CreateConnectionChannelError)reader.ReadByte(),
                (ConnectionStatus)reader.ReadByte());
        }
    }

    internal struct EvtConnectionStatusChanged
    {
        public EvtConnectionStatusChanged(uint connId, ConnectionStatus connectionStatus, DisconnectReason disconnectReason)
            : this()
        {
            ConnId = connId;
            ConnectionStatus = connectionStatus;
            DisconnectReason = disconnectReason;
        }

        internal uint ConnId { get; }
        internal ConnectionStatus ConnectionStatus { get; }
        internal DisconnectReason DisconnectReason { get; }

        public static EvtConnectionStatusChanged FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);
            return new(reader.ReadUInt32(), (ConnectionStatus)reader.ReadByte(), (DisconnectReason)reader.ReadByte());
        }
    }

    internal struct EvtConnectionChannelRemoved
    {
        public EvtConnectionChannelRemoved(uint connId, RemovedReason removedReason)
            : this()
        {
            ConnId = connId;
            RemovedReason = removedReason;
        }

        internal uint ConnId { get; }
        internal RemovedReason RemovedReason { get; }

        public static EvtConnectionChannelRemoved FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);
            return new(reader.ReadUInt32(), (RemovedReason)reader.ReadByte());
        }
    }

    internal struct EvtButtonClick
    {
        public EvtButtonClick(uint connId, ClickType clickType, bool wasQueued, uint timeDiff)
            : this()
        {
            ConnId = connId;
            ClickType = clickType;
            WasQueued = wasQueued;
            TimeDiff = timeDiff;
        }

        internal uint ConnId { get; }
        internal ClickType ClickType { get; }
        internal bool WasQueued { get; }
        internal uint TimeDiff { get; }

        public static EvtButtonClick FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);
            return new(reader.ReadUInt32(), (ClickType)reader.ReadByte(), reader.ReadBoolean(), reader.ReadUInt32());
        }
    }

    internal struct EvtNewVerifiedButton
    {
        public EvtNewVerifiedButton(Bdaddr bdAddr)
            : this()
        {
            BdAddr = bdAddr;
        }

        internal Bdaddr BdAddr { get; }

        public static EvtNewVerifiedButton FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);
            return new(reader.ReadBdAddr());
        }
    }

    internal struct EvtGetInfoResponse
    {
        public EvtGetInfoResponse(
            BluetoothControllerState bluetoothControllerState,
            Bdaddr controllerBdAddr,
            BdAddrType controllerBdAddType,
            byte maxPendingConnections,
            short maxConcurrentlyConnectedButtons,
            byte currentPendingConnections,
            bool currentlyNoSpaceForNewConnection,
            Bdaddr[] verifiedButtonBdAddrs)
        {
            BluetoothControllerState = bluetoothControllerState;
            ControllerBdAddr = controllerBdAddr;
            ControllerBdAddrType = controllerBdAddType;
            MaxPendingConnections = maxPendingConnections;
            MaxConcurrentlyConnectedButtons = maxConcurrentlyConnectedButtons;
            CurrentPendingConnections = currentPendingConnections;
            CurrentlyNoSpaceForNewConnection = currentlyNoSpaceForNewConnection;
            VerifiedButtonBdAddrs = verifiedButtonBdAddrs;
        }

        internal BluetoothControllerState BluetoothControllerState { get; }
        internal Bdaddr ControllerBdAddr { get; }
        internal BdAddrType ControllerBdAddrType { get; }
        internal byte MaxPendingConnections { get; }
        internal short MaxConcurrentlyConnectedButtons { get; }
        internal byte CurrentPendingConnections { get; }
        internal bool CurrentlyNoSpaceForNewConnection { get; }
        internal Bdaddr[] VerifiedButtonBdAddrs { get; }

        public static EvtGetInfoResponse FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);

            var bluetoothControllerState = (BluetoothControllerState)reader.ReadByte();
            var controllerBdAddr = reader.ReadBdAddr();
            var controllerBdAddrType = (BdAddrType)reader.ReadByte();
            var maxPendingConnections = reader.ReadByte();
            var maxConcurrentlyConnectedButtons = reader.ReadInt16();
            var currentPendingConnections = reader.ReadByte();
            var currentlyNoSpaceForNewConnection = reader.ReadBoolean();

            var verifiedButtonsCount = reader.ReadUInt16();
            var verifiedButtonBdAddrs = new Bdaddr[verifiedButtonsCount];

            for (var i = 0; i < verifiedButtonsCount; i++)
                verifiedButtonBdAddrs[i] = reader.ReadBdAddr();

            return new(
                bluetoothControllerState,
                controllerBdAddr,
                controllerBdAddrType,
                maxPendingConnections,
                maxConcurrentlyConnectedButtons,
                currentPendingConnections,
                currentlyNoSpaceForNewConnection,
                verifiedButtonBdAddrs);
        }
    }

    internal struct EvtNoSpaceForNewConnection
    {
        public EvtNoSpaceForNewConnection(byte maxConcurrentlyConnectedButtons)
            : this()
        {
            MaxConcurrentlyConnectedButtons = maxConcurrentlyConnectedButtons;
        }

        internal byte MaxConcurrentlyConnectedButtons { get; }

        public static EvtNoSpaceForNewConnection FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);
            return new(reader.ReadByte());
        }
    }

    internal struct EvtGotSpaceForNewConnection
    {
        public EvtGotSpaceForNewConnection(byte maxConcurrentlyConnectedButtons)
            : this()
        {
            MaxConcurrentlyConnectedButtons = maxConcurrentlyConnectedButtons;
        }

        internal byte MaxConcurrentlyConnectedButtons { get; }

        public static EvtGotSpaceForNewConnection FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);
            return new(reader.ReadByte());
        }
    }

    internal struct EvtBluetoothControllerStateChange
    {
        public EvtBluetoothControllerStateChange(BluetoothControllerState state)
            : this()
        {
            State = state;
        }

        internal BluetoothControllerState State { get; }

        public static EvtBluetoothControllerStateChange FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);
            return new((BluetoothControllerState)reader.ReadByte());
        }
    }

    internal struct EvtPingResponse
    {
        public EvtPingResponse(uint pingId)
            : this()
        {
            PingId = pingId;
        }

        internal uint PingId { get; }

        public static EvtPingResponse FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);
            return new(reader.ReadUInt32());
        }
    }

    internal struct EvtGetButtonInfoResponse
    {
        private static readonly byte[] EmptyUuid = new byte[16];

        public EvtGetButtonInfoResponse(Bdaddr bdAddr, string? uuid)
            : this(bdAddr, uuid, default, default, default, default)
        {
        }

        public EvtGetButtonInfoResponse(Bdaddr bdAddr, string? uuid, string? color, string? serialNumber, int flicVersion, uint firmwareVersion)
            : this()
        {
            BdAddr = bdAddr;
            Uuid = uuid;
            Color = color;
            SerialNumber = serialNumber;
            FlicVersion = flicVersion;
            FirmwareVersion = firmwareVersion;
        }

        internal Bdaddr BdAddr { get; }
        internal string? Uuid { get; }
        internal string? Color { get; }
        internal string? SerialNumber { get; }
        internal int FlicVersion { get; }
        internal uint FirmwareVersion { get; }

        public static EvtGetButtonInfoResponse FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);
            var bdAddr = reader.ReadBdAddr();

            ReadOnlySpan<byte> uuidBytes = reader.ReadBytes(16);
            if (uuidBytes.Length != 16)
                throw new EndOfStreamException("End of stream while reading Uuid");

            string? uuid;
            if (uuidBytes.SequenceEqual(EmptyUuid))
                uuid = null;
            else
                uuid = Convert.ToHexString(uuidBytes);

            // For old protocol
            if (reader.IsComplete)
                return new(bdAddr, uuid);

            int colorLen = reader.ReadByte();
            var color = reader.ReadString(colorLen);
            if (colorLen == 0)
                color = null;

            int serialNumberLen = reader.ReadByte();
            var serialNumber = reader.ReadString(serialNumberLen);
            if (serialNumber.Length == 0)
                serialNumber = null;

            var flicVersion = reader.ReadByte();
            var firmwareVersion = reader.ReadUInt32();

            return new(bdAddr, uuid, color, serialNumber, flicVersion, firmwareVersion);
        }
    }

    internal struct EvtScanWizardFoundPrivateButton
    {
        public EvtScanWizardFoundPrivateButton(uint scanWizardId)
            : this()
        {
            ScanWizardId = scanWizardId;
        }

        internal uint ScanWizardId { get; }

        public static EvtScanWizardFoundPrivateButton FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);
            return new(reader.ReadUInt32());
        }
    }

    internal struct EvtScanWizardFoundPublicButton
    {
        public EvtScanWizardFoundPublicButton(uint scanWizardId, Bdaddr bdAddr, string name)
            : this()
        {
            ScanWizardId = scanWizardId;
            BdAddr = bdAddr;
            Name = name;
        }

        internal uint ScanWizardId { get; }
        internal Bdaddr BdAddr { get; }
        internal string Name { get; }

        public static EvtScanWizardFoundPublicButton FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);
            return new(reader.ReadUInt32(), reader.ReadBdAddr(), reader.ReadString(reader.ReadByte()));
        }
    }

    internal struct EvtScanWizardButtonConnected
    {
        public EvtScanWizardButtonConnected(uint scanWizardId)
            : this()
        {
            ScanWizardId = scanWizardId;
        }

        internal uint ScanWizardId { get; }

        public static EvtScanWizardButtonConnected FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);
            return new(reader.ReadUInt32());
        }
    }

    internal struct EvtScanWizardCompleted
    {
        public EvtScanWizardCompleted(uint scanWizardId, ScanWizardResult result)
            : this()
        {
            ScanWizardId = scanWizardId;
            Result = result;
        }

        internal uint ScanWizardId { get; }
        internal ScanWizardResult Result { get; }

        public static EvtScanWizardCompleted FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);
            return new(reader.ReadUInt32(), (ScanWizardResult)reader.ReadByte());
        }
    }

    internal struct EvtButtonDeleted
    {
        public EvtButtonDeleted(Bdaddr bdAddr, bool deletedByThisClient) 
            : this()
        {
            BdAddr = bdAddr;
            DeletedByThisClient = deletedByThisClient;
        }

        internal Bdaddr BdAddr { get; }
        internal bool DeletedByThisClient { get; }

        public static EvtButtonDeleted FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);
            return new(reader.ReadBdAddr(), reader.ReadBoolean());
        }
    }
}
