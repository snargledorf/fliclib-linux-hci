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
    internal struct EvtAdvertisement
    {
        public EvtAdvertisement(
            uint scanId,
            BluetoothAddress bdAddr,
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
        internal BluetoothAddress BdAddr { get; }
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
                reader.ReadBluetoothAddress(),
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
                reader.ReadEnum<CreateConnectionChannelError>(),
                reader.ReadEnum<ConnectionStatus>());
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
            return new(reader.ReadUInt32(), reader.ReadEnum<ConnectionStatus>(), reader.ReadEnum<DisconnectReason>());
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
            return new(reader.ReadUInt32(), reader.ReadEnum<RemovedReason>());
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
            return new(reader.ReadUInt32(), reader.ReadEnum<ClickType>(), reader.ReadBoolean(), reader.ReadUInt32());
        }
    }

    internal struct EvtNewVerifiedButton
    {
        public EvtNewVerifiedButton(BluetoothAddress bdAddr)
            : this()
        {
            BdAddr = bdAddr;
        }

        internal BluetoothAddress BdAddr { get; }

        public static EvtNewVerifiedButton FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);
            return new(reader.ReadBluetoothAddress());
        }
    }

    internal struct EvtGetInfoResponse
    {
        public EvtGetInfoResponse(
            BluetoothControllerState bluetoothControllerState,
            BluetoothAddress controllerBdAddr,
            BdAddrType controllerBdAddType,
            byte maxPendingConnections,
            short maxConcurrentlyConnectedButtons,
            byte currentPendingConnections,
            bool currentlyNoSpaceForNewConnection,
            BluetoothAddress[] verifiedButtonBdAddrs)
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
        internal BluetoothAddress ControllerBdAddr { get; }
        internal BdAddrType ControllerBdAddrType { get; }
        internal byte MaxPendingConnections { get; }
        internal short MaxConcurrentlyConnectedButtons { get; }
        internal byte CurrentPendingConnections { get; }
        internal bool CurrentlyNoSpaceForNewConnection { get; }
        internal BluetoothAddress[] VerifiedButtonBdAddrs { get; }

        public static EvtGetInfoResponse FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);

            var bluetoothControllerState = reader.ReadEnum<BluetoothControllerState>();
            var controllerBdAddr = reader.ReadBluetoothAddress();
            var controllerBdAddrType = reader.ReadEnum<BdAddrType>();
            var maxPendingConnections = reader.ReadByte();
            var maxConcurrentlyConnectedButtons = reader.ReadInt16();
            var currentPendingConnections = reader.ReadByte();
            var currentlyNoSpaceForNewConnection = reader.ReadBoolean();

            var verifiedButtonsCount = reader.ReadUInt16();
            var verifiedButtonBdAddrs = new BluetoothAddress[verifiedButtonsCount];

            for (var i = 0; i < verifiedButtonsCount; i++)
                verifiedButtonBdAddrs[i] = reader.ReadBluetoothAddress();

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
            return new(reader.ReadEnum<BluetoothControllerState>());
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

        public EvtGetButtonInfoResponse(BluetoothAddress bdAddr, string? uuid)
            : this(bdAddr, uuid, default, default, default, default)
        {
        }

        public EvtGetButtonInfoResponse(BluetoothAddress bdAddr, string? uuid, string? color, string? serialNumber, int flicVersion, uint firmwareVersion)
            : this()
        {
            BdAddr = bdAddr;
            Uuid = uuid;
            Color = color;
            SerialNumber = serialNumber;
            FlicVersion = flicVersion;
            FirmwareVersion = firmwareVersion;
        }

        internal BluetoothAddress BdAddr { get; }
        internal string? Uuid { get; }
        internal string? Color { get; }
        internal string? SerialNumber { get; }
        internal int FlicVersion { get; }
        internal uint FirmwareVersion { get; }

        public static EvtGetButtonInfoResponse FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);
            var bdAddr = reader.ReadBluetoothAddress();

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
        public EvtScanWizardFoundPublicButton(uint scanWizardId, BluetoothAddress bdAddr, string name)
            : this()
        {
            ScanWizardId = scanWizardId;
            BdAddr = bdAddr;
            Name = name;
        }

        internal uint ScanWizardId { get; }
        internal BluetoothAddress BdAddr { get; }
        internal string Name { get; }

        public static EvtScanWizardFoundPublicButton FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);
            return new(reader.ReadUInt32(), reader.ReadBluetoothAddress(), reader.ReadString(reader.ReadByte()));
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
            return new(reader.ReadUInt32(), reader.ReadEnum<ScanWizardResult>());
        }
    }

    internal struct EvtButtonDeleted
    {
        public EvtButtonDeleted(BluetoothAddress bdAddr, bool deletedByThisClient) 
            : this()
        {
            BdAddr = bdAddr;
            DeletedByThisClient = deletedByThisClient;
        }

        internal BluetoothAddress BdAddr { get; }
        internal bool DeletedByThisClient { get; }

        public static EvtButtonDeleted FromPacket(FlicPacket packet)
        {
            using var reader = new FlicPacketParser(packet);
            return new(reader.ReadBluetoothAddress(), reader.ReadBoolean());
        }
    }
}
