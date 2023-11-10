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
    internal abstract class CommandPacket
    {
        protected abstract int Opcode { get; }

        public byte[] Construct()
        {
            MemoryStream stream = new();

            Write(new BinaryWriter(stream));

            var res = new byte[3 + stream.Length];

            res[0] = (byte)(1 + stream.Length);
            res[1] = (byte)((1 + stream.Length) >> 8);
            res[2] = (byte)Opcode;

            stream.Position = 0;
            stream.Read(res.AsSpan()[3..]);

            return res;
        }

        protected abstract void Write(BinaryWriter writer);
    }

    internal class CmdGetInfo : CommandPacket
    {
        protected override int Opcode => 0;

        protected override void Write(BinaryWriter writer)
        {
        }
    }

    internal class CmdCreateScanner : CommandPacket
    {
        internal uint ScanId;
        protected override int Opcode => 1;

        protected override void Write(BinaryWriter writer)
        {
            writer.Write(ScanId);
        }
    }

    internal class CmdRemoveScanner : CommandPacket
    {
        internal uint ScanId;

        protected override int Opcode => 2;

        protected override void Write(BinaryWriter writer)
        {
            writer.Write(ScanId);
        }
    }

    internal class CmdCreateConnectionChannel : CommandPacket
    {
        private static int _nextId;

        internal uint ConnId = (uint)Interlocked.Increment(ref _nextId);
        internal Bdaddr BdAddr;
        internal LatencyMode LatencyMode;
        internal short AutoDisconnectTime;

        protected override int Opcode => 3;

        protected override void Write(BinaryWriter writer)
        {
            writer.Write(ConnId);
            writer.Write(BdAddr);
            writer.Write((byte)LatencyMode);
            writer.Write(AutoDisconnectTime);
        }
    }

    internal class CmdRemoveConnectionChannel : CommandPacket
    {
        internal uint ConnId;

        protected override int Opcode => 4;

        protected override void Write(BinaryWriter writer)
        {
            writer.Write(ConnId);
        }
    }

    internal class CmdForceDisconnect : CommandPacket
    {
        internal Bdaddr BdAddr;

        protected override int Opcode => 5;

        protected override void Write(BinaryWriter writer)
        {
            writer.Write(BdAddr);
        }
    }

    internal class CmdChangeModeParameters : CommandPacket
    {
        internal uint ConnId;
        internal LatencyMode LatencyMode;
        internal short AutoDisconnectTime;

        protected override int Opcode => 6;

        protected override void Write(BinaryWriter writer)
        {
            writer.Write(ConnId);
            writer.Write((byte)LatencyMode);
            writer.Write(AutoDisconnectTime);
        }
    }

    internal class CmdPing : CommandPacket
    {
        internal uint PingId;

        protected override int Opcode => 7;

        protected override void Write(BinaryWriter writer)
        {
            writer.Write(PingId);
        }
    }

    internal class CmdGetButtonInfo : CommandPacket
    {
        internal Bdaddr BdAddr;

        protected override int Opcode => 8;

        protected override void Write(BinaryWriter writer)
        {
            writer.Write(BdAddr);
        }
    }

    internal class CmdCreateScanWizard : CommandPacket
    {
        private static int _nextId = 0;
        internal uint ScanWizardId = (uint)Interlocked.Increment(ref _nextId);

        protected override int Opcode => 9;

        protected override void Write(BinaryWriter writer)
        {
            writer.Write(ScanWizardId);
        }
    }

    internal class CmdCancelScanWizard : CommandPacket
    {
        internal uint ScanWizardId;

        protected override int Opcode => 10;

        protected override void Write(BinaryWriter writer)
        {
            writer.Write(ScanWizardId);
        }
    }

    internal class CmdDeleteButton : CommandPacket
    {
        internal Bdaddr BdAddr;

        protected override int Opcode => 11;

        protected override void Write(BinaryWriter writer)
        {
            writer.Write(BdAddr);
        }
    }

    internal enum EventPacketOpCode
    {
        EVT_ADVERTISEMENT_PACKET_OPCODE = 0,
        EVT_CREATE_CONNECTION_CHANNEL_RESPONSE_OPCODE = 1,
        EVT_CONNECTION_STATUS_CHANGED_OPCODE = 2,
        EVT_CONNECTION_CHANNEL_REMOVED_OPCODE = 3,
        EVT_BUTTON_UP_OR_DOWN_OPCODE = 4,
        EVT_BUTTON_CLICK_OR_HOLD_OPCODE = 5,
        EVT_BUTTON_SINGLE_OR_DOUBLE_CLICK_OPCODE = 6,
        EVT_BUTTON_SINGLE_OR_DOUBLE_CLICK_OR_HOLD_OPCODE = 7,
        EVT_NEW_VERIFIED_BUTTON_OPCODE = 8,
        EVT_GET_INFO_RESPONSE_OPCODE = 9,
        EVT_NO_SPACE_FOR_NEW_CONNECTION_OPCODE = 10,
        EVT_GOT_SPACE_FOR_NEW_CONNECTION_OPCODE = 11,
        EVT_BLUETOOTH_CONTROLLER_STATE_CHANGE_OPCODE = 12,
        EVT_PING_RESPONSE_OPCODE = 13, // TODO Implement Ping Event
        EVT_GET_BUTTON_INFO_RESPONSE_OPCODE = 14,
        EVT_SCAN_WIZARD_FOUND_PRIVATE_BUTTON_OPCODE = 15,
        EVT_SCAN_WIZARD_FOUND_PUBLIC_BUTTON_OPCODE = 16,
        EVT_SCAN_WIZARD_BUTTON_CONNECTED_OPCODE = 17,
        EVT_SCAN_WIZARD_COMPLETED_OPCODE = 18,
        EVT_BUTTON_DELETED_OPCODE = 19,
    }

    internal abstract class EventPacket
    {
                
        internal void Parse(ReadOnlyMemory<byte> arr)
        {
            var stream = new BufferStream(arr);
            ParseInternal(new BinaryReader(stream));
        }

        protected abstract void ParseInternal(BinaryReader reader);
    }

    internal class EvtAdvertisementPacket : EventPacket
    {
        internal uint ScanId;
        internal Bdaddr BdAddr;
        internal string? Name;
        internal int Rssi;
        internal bool IsPrivate;
        internal bool AlreadyVerified;
        internal bool AlreadyConnectedToThisDevice;
        internal bool AlreadyConnectedToOtherDevice;

        protected override void ParseInternal(BinaryReader reader)
        {
            ScanId = reader.ReadUInt32();
            BdAddr = reader.ReadBdaddr();
            int nameLen = reader.ReadByte();
            var bytes = new byte[nameLen];
            for (var i = 0; i < nameLen; i++)
            {
                bytes[i] = reader.ReadByte();
            }
            for (var i = nameLen; i < 16; i++)
            {
                reader.ReadByte();
            }
            Name = Encoding.UTF8.GetString(bytes);
            Rssi = reader.ReadSByte();
            IsPrivate = reader.ReadBoolean();
            AlreadyVerified = reader.ReadBoolean();
            AlreadyConnectedToThisDevice = reader.ReadBoolean();
            AlreadyConnectedToOtherDevice = reader.ReadBoolean();
        }
    }

    internal class EvtCreateConnectionChannelResponse : EventPacket
    {
        internal uint ConnId;
        internal CreateConnectionChannelError Error;
        internal ConnectionStatus ConnectionStatus;

        protected override void ParseInternal(BinaryReader reader)
        {
            ConnId = reader.ReadUInt32();
            Error = (CreateConnectionChannelError)reader.ReadByte();
            ConnectionStatus = (ConnectionStatus)reader.ReadByte();
        }
    }

    internal class EvtConnectionStatusChanged : EventPacket
    {
        internal uint ConnId;
        internal ConnectionStatus ConnectionStatus;
        internal DisconnectReason DisconnectReason;

        protected override void ParseInternal(BinaryReader reader)
        {
            ConnId = reader.ReadUInt32();
            ConnectionStatus = (ConnectionStatus)reader.ReadByte();
            DisconnectReason = (DisconnectReason)reader.ReadByte();
        }
    }

    internal class EvtConnectionChannelRemoved : EventPacket
    {
        internal uint ConnId;
        internal RemovedReason RemovedReason;

        protected override void ParseInternal(BinaryReader reader)
        {
            ConnId = reader.ReadUInt32();
            RemovedReason = (RemovedReason)reader.ReadByte();
        }
    }

    internal class EvtButtonEvent : EventPacket
    {
        internal uint ConnId;
        internal ClickType ClickType;
        internal bool WasQueued;
        internal uint TimeDiff;

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
        internal Bdaddr BdAddr;

        protected override void ParseInternal(BinaryReader reader)
        {
            BdAddr = reader.ReadBdaddr();
        }
    }

    internal class EvtGetInfoResponse : EventPacket
    {
        internal BluetoothControllerState BluetoothControllerState;
        internal Bdaddr MyBdAddr;
        internal BdAddrType MyBdAddrType;
        internal byte MaxPendingConnections;
        internal short MaxConcurrentlyConnectedButtons;
        internal byte CurrentPendingConnections;
        internal bool CurrentlyNoSpaceForNewConnection;
        internal Bdaddr[]? BdAddrOfVerifiedButtons;

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
        internal byte MaxConcurrentlyConnectedButtons;

        protected override void ParseInternal(BinaryReader reader)
        {
            MaxConcurrentlyConnectedButtons = reader.ReadByte();
        }
    }

    internal class EvtGotSpaceForNewConnection : EventPacket
    {
        internal byte MaxConcurrentlyConnectedButtons;

        protected override void ParseInternal(BinaryReader reader)
        {
            MaxConcurrentlyConnectedButtons = reader.ReadByte();
        }
    }

    internal class EvtBluetoothControllerStateChange : EventPacket
    {
        internal BluetoothControllerState State;

        protected override void ParseInternal(BinaryReader reader)
        {
            State = (BluetoothControllerState)reader.ReadByte();
        }
    }

    internal class EvtGetButtonInfoResponse : EventPacket
    {
        internal Bdaddr BdAddr;
        internal string? Uuid;
        internal string? Color;
        internal string? SerialNumber;
        internal int FlicVersion;
        internal uint FirmwareVersion;

        protected override void ParseInternal(BinaryReader reader)
        {
            BdAddr = reader.ReadBdaddr();
            var uuidBytes = reader.ReadBytes(16);
            if (uuidBytes.Length != 16)
            {
                throw new EndOfStreamException();
            }
            var sb = new StringBuilder(32);
            for (var i = 0; i < 16; i++)
            {
                sb.Append(string.Format("{0:x2}", uuidBytes[i]));
            }
            Uuid = sb.ToString();
            if (Uuid == "00000000000000000000000000000000")
            {
                Uuid = null;
            }

            if (reader.PeekChar() == -1)
            {
                // For old protocol
                return;
            }
            int colorLen = reader.ReadByte();
            var colorBytes = new byte[colorLen];
            for (var i = 0; i < colorLen; i++)
            {
                colorBytes[i] = reader.ReadByte();
            }
            for (var i = colorLen; i < 16; i++)
            {
                reader.ReadByte();
            }
            Color = colorLen == 0 ? null : Encoding.UTF8.GetString(colorBytes);

            int serialNumberLen = reader.ReadByte();
            var serialNumberBytes = new byte[serialNumberLen];
            for (var i = 0; i < serialNumberLen; i++)
            {
                serialNumberBytes[i] = reader.ReadByte();
            }
            for (var i = serialNumberLen; i < 16; i++)
            {
                reader.ReadByte();
            }
            SerialNumber = serialNumberLen == 0 ? null : Encoding.UTF8.GetString(serialNumberBytes);
            FlicVersion = reader.ReadByte();
            FirmwareVersion = reader.ReadUInt32();
        }
    }

    internal class EvtScanWizardFoundPrivateButton : EventPacket
    {
        internal uint ScanWizardId;

        protected override void ParseInternal(BinaryReader reader)
        {
            ScanWizardId = reader.ReadUInt32();
        }
    }

    internal class EvtScanWizardFoundPublicButton : EventPacket
    {
        internal uint ScanWizardId;
        internal Bdaddr BdAddr;
        internal string? Name;

        protected override void ParseInternal(BinaryReader reader)
        {
            ScanWizardId = reader.ReadUInt32();
            BdAddr = reader.ReadBdaddr();
            int nameLen = reader.ReadByte();
            var bytes = new byte[nameLen];
            for (var i = 0; i < nameLen; i++)
            {
                bytes[i] = reader.ReadByte();
            }
            for (var i = nameLen; i < 16; i++)
            {
                reader.ReadByte();
            }
            Name = Encoding.UTF8.GetString(bytes);
        }
    }

    internal class EvtScanWizardButtonConnected : EventPacket
    {
        internal uint ScanWizardId;

        protected override void ParseInternal(BinaryReader reader)
        {
            ScanWizardId = reader.ReadUInt32();
        }
    }

    internal class EvtScanWizardCompleted : EventPacket
    {
        internal uint ScanWizardId;
        internal ScanWizardResult Result;

        protected override void ParseInternal(BinaryReader reader)
        {
            ScanWizardId = reader.ReadUInt32();
            Result = (ScanWizardResult)reader.ReadByte();
        }
    }

    internal class EvtButtonDeleted : EventPacket
    {
        internal Bdaddr BdAddr;
        internal bool DeletedByThisClient;

        protected override void ParseInternal(BinaryReader reader)
        {
            BdAddr = reader.ReadBdaddr();
            DeletedByThisClient = reader.ReadBoolean();
        }
    }
}
