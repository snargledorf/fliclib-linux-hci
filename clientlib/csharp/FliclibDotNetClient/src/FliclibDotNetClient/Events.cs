using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Text;
using System.Threading;

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

    internal abstract class EventPacket
    {
        internal void Parse(FlicPacket packet)
        {
            var stream = new ReadOnlyMemoryStream(packet.Data);
            ParseInternal(new BinaryReader(stream));
        }

        protected abstract void ParseInternal(BinaryReader reader);
    }

    internal class EvtAdvertisementPacket : EventPacket
    {
        internal uint ScanId { get; private set; }
        internal Bdaddr BdAddr { get; private set; }
        internal string? Name { get; private set; }
        internal int Rssi { get; private set; }
        internal bool IsPrivate { get; private set; }
        internal bool AlreadyVerified { get; private set; }
        internal bool AlreadyConnectedToThisDevice { get; private set; }
        internal bool AlreadyConnectedToOtherDevice { get; private set; }

        protected override void ParseInternal(BinaryReader reader)
        {
            ScanId = reader.ReadUInt32();
            BdAddr = reader.ReadBdaddr();

            int nameLen = reader.ReadByte();
            Name = reader.ReadFlicString(nameLen);

            Rssi = reader.ReadSByte();
            IsPrivate = reader.ReadBoolean();
            AlreadyVerified = reader.ReadBoolean();
            AlreadyConnectedToThisDevice = reader.ReadBoolean();
            AlreadyConnectedToOtherDevice = reader.ReadBoolean();
        }
    }

    internal class EvtCreateConnectionChannelResponse : EventPacket
    {
        internal uint ConnId { get; private set; }
        internal CreateConnectionChannelError Error { get; private set; }
        internal ConnectionStatus ConnectionStatus { get; private set; }

        protected override void ParseInternal(BinaryReader reader)
        {
            ConnId = reader.ReadUInt32();
            Error = (CreateConnectionChannelError)reader.ReadByte();
            ConnectionStatus = (ConnectionStatus)reader.ReadByte();
        }
    }

    internal class EvtConnectionStatusChanged : EventPacket
    {
        internal uint ConnId { get; private set; }
        internal ConnectionStatus ConnectionStatus { get; private set; }
        internal DisconnectReason DisconnectReason { get; private set; }

        protected override void ParseInternal(BinaryReader reader)
        {
            ConnId = reader.ReadUInt32();
            ConnectionStatus = (ConnectionStatus)reader.ReadByte();
            DisconnectReason = (DisconnectReason)reader.ReadByte();
        }
    }

    internal class EvtConnectionChannelRemoved : EventPacket
    {
        internal uint ConnId { get; private set; }
        internal RemovedReason RemovedReason { get; private set; }

        protected override void ParseInternal(BinaryReader reader)
        {
            ConnId = reader.ReadUInt32();
            RemovedReason = (RemovedReason)reader.ReadByte();
        }
    }

    internal class EvtButtonEvent : EventPacket
    {
        internal uint ConnId { get; private set; }
        internal ClickType ClickType { get; private set; }
        internal bool WasQueued { get; private set; }
        internal uint TimeDiff { get; private set; }

        protected override void ParseInternal(BinaryReader reader)
        {
            ConnId = reader.ReadUInt32();
            ClickType = (ClickType)reader.ReadByte();
            WasQueued = reader.ReadBoolean();
            TimeDiff = reader.ReadUInt32();
        }
    }

    internal class EvtNewVerifiedButton : EventPacket
    {
        internal Bdaddr BdAddr { get; private set; }

        protected override void ParseInternal(BinaryReader reader)
        {
            BdAddr = reader.ReadBdaddr();
        }
    }

    internal class EvtGetInfoResponse : EventPacket
    {
        internal BluetoothControllerState BluetoothControllerState { get; private set; }
        internal Bdaddr MyBdAddr { get; private set; }
        internal BdAddrType MyBdAddrType { get; private set; }
        internal byte MaxPendingConnections { get; private set; }
        internal short MaxConcurrentlyConnectedButtons { get; private set; }
        internal byte CurrentPendingConnections { get; private set; }
        internal bool CurrentlyNoSpaceForNewConnection { get; private set; }
        internal Bdaddr[]? BdAddrOfVerifiedButtons { get; private set; }

        protected override void ParseInternal(BinaryReader reader)
        {
            BluetoothControllerState = (BluetoothControllerState)reader.ReadByte();
            MyBdAddr = reader.ReadBdaddr();
            MyBdAddrType = (BdAddrType)reader.ReadByte();
            MaxPendingConnections = reader.ReadByte();
            MaxConcurrentlyConnectedButtons = reader.ReadInt16();
            CurrentPendingConnections = reader.ReadByte();
            CurrentlyNoSpaceForNewConnection = reader.ReadBoolean();

            var nbVerifiedButtons = reader.ReadUInt16();
            BdAddrOfVerifiedButtons = new Bdaddr[nbVerifiedButtons];

            for (var i = 0; i < nbVerifiedButtons; i++)
            {
                BdAddrOfVerifiedButtons[i] = reader.ReadBdaddr();
            }
        }
    }

    internal class EvtNoSpaceForNewConnection : EventPacket
    {
        internal byte MaxConcurrentlyConnectedButtons { get; private set; }

        protected override void ParseInternal(BinaryReader reader)
        {
            MaxConcurrentlyConnectedButtons = reader.ReadByte();
        }
    }

    internal class EvtGotSpaceForNewConnection : EventPacket
    {
        internal byte MaxConcurrentlyConnectedButtons { get; private set; }

        protected override void ParseInternal(BinaryReader reader)
        {
            MaxConcurrentlyConnectedButtons = reader.ReadByte();
        }
    }

    internal class EvtBluetoothControllerStateChange : EventPacket
    {
        internal BluetoothControllerState State { get; private set; }

        protected override void ParseInternal(BinaryReader reader)
        {
            State = (BluetoothControllerState)reader.ReadByte();
        }
    }

    internal class EvtPingResponse : EventPacket
    {
        internal uint PingId { get; private set; }

        protected override void ParseInternal(BinaryReader reader)
        {
            PingId = reader.ReadUInt32();
        }
    }

    internal class EvtGetButtonInfoResponse : EventPacket
    {
        private static readonly byte[] EmptyUuid = new byte[16];

        internal Bdaddr BdAddr { get; private set; }
        internal string? Uuid { get; private set; }
        internal string? Color { get; private set; }
        internal string? SerialNumber { get; private set; }
        internal int FlicVersion { get; private set; }
        internal uint FirmwareVersion { get; private set; }

        protected override void ParseInternal(BinaryReader reader)
        {
            BdAddr = reader.ReadBdaddr();

            ReadOnlySpan<byte> uuidBytes = reader.ReadBytes(16);
            if (uuidBytes.Length != 16)
                throw new EndOfStreamException("End of stream while reading Uuid");

            if (uuidBytes.SequenceEqual(EmptyUuid))
                Uuid = null;
            else
                Uuid = Convert.ToHexString(uuidBytes);

            // For old protocol
            if (reader.PeekChar() == -1)
                return;

            int colorLen = reader.ReadByte();
            Color = reader.ReadFlicString(colorLen);
            if (colorLen == 0)
                Color = null;

            int serialNumberLen = reader.ReadByte();
            SerialNumber = reader.ReadFlicString(serialNumberLen);
            if (SerialNumber.Length == 0)
                SerialNumber = null;

            FlicVersion = reader.ReadByte();
            FirmwareVersion = reader.ReadUInt32();
        }
    }

    internal class EvtScanWizardFoundPrivateButton : EventPacket
    {
        internal uint ScanWizardId { get; private set; }

        protected override void ParseInternal(BinaryReader reader)
        {
            ScanWizardId = reader.ReadUInt32();
        }
    }

    internal class EvtScanWizardFoundPublicButton : EventPacket
    {
        internal uint ScanWizardId { get; private set; }
        internal Bdaddr BdAddr { get; private set; }
        internal string? Name { get; private set; }

        protected override void ParseInternal(BinaryReader reader)
        {
            ScanWizardId = reader.ReadUInt32();
            BdAddr = reader.ReadBdaddr();

            int nameLen = reader.ReadByte();
            Name = reader.ReadFlicString(nameLen);
        }
    }

    internal class EvtScanWizardButtonConnected : EventPacket
    {
        internal uint ScanWizardId { get; private set; }

        protected override void ParseInternal(BinaryReader reader)
        {
            ScanWizardId = reader.ReadUInt32();
        }
    }

    internal class EvtScanWizardCompleted : EventPacket
    {
        internal uint ScanWizardId { get; private set; }
        internal ScanWizardResult Result { get; private set; }

        protected override void ParseInternal(BinaryReader reader)
        {
            ScanWizardId = reader.ReadUInt32();
            Result = (ScanWizardResult)reader.ReadByte();
        }
    }

    internal class EvtButtonDeleted : EventPacket
    {
        internal Bdaddr BdAddr { get; private set; }
        internal bool DeletedByThisClient { get; private set; }

        protected override void ParseInternal(BinaryReader reader)
        {
            BdAddr = reader.ReadBdaddr();
            DeletedByThisClient = reader.ReadBoolean();
        }
    }
}
