using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FliclibDotNetClient
{
    internal abstract class FlicCommand
    {
        protected abstract CommandOpCode Opcode { get; }

        public FlicPacket ToPacket()
        {
            MemoryStream packetData = new();

            WriteBody(new BinaryWriter(packetData));

            return new FlicPacket((byte)Opcode, packetData.ToArray());
        }

        protected virtual void WriteBody(BinaryWriter writer)
        {
        }
    }

    internal class CmdGetInfo : FlicCommand
    {
        protected override CommandOpCode Opcode => CommandOpCode.CmdGetInfo;
    }

    internal class CmdCreateScanner : FlicCommand
    {
        internal uint ScanId { get; init; }

        protected override CommandOpCode Opcode => CommandOpCode.CmdCreateScanner;

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(ScanId);
        }
    }

    internal class CmdRemoveScanner : FlicCommand
    {
        internal uint ScanId { get; init; }

        protected override CommandOpCode Opcode => CommandOpCode.CmdRemoveScanner;

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(ScanId);
        }
    }

    internal class CmdCreateConnectionChannel : FlicCommand
    {
        internal readonly uint ConnId = FlicIdGenerator<CmdCreateConnectionChannel>.NextId();
        internal BluetoothAddress BdAddr { get; init; }
        internal LatencyMode LatencyMode { get; init; }
        internal short AutoDisconnectTime { get; init; }

        protected override CommandOpCode Opcode => CommandOpCode.CmdCreateConnectionChannel;

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(ConnId);
            writer.Write(BdAddr);
            writer.Write((byte)LatencyMode);
            writer.Write(AutoDisconnectTime);
        }
    }

    internal class CmdRemoveConnectionChannel : FlicCommand
    {
        internal uint ConnId { get; init; }

        protected override CommandOpCode Opcode => CommandOpCode.CmdRemoveConnectionChannel;

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(ConnId);
        }
    }

    internal class CmdForceDisconnect : FlicCommand
    {
        internal BluetoothAddress BdAddr { get; init; }

        protected override CommandOpCode Opcode => CommandOpCode.CmdForceDisconnect;

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(BdAddr);
        }
    }

    internal class CmdChangeModeParameters : FlicCommand
    {
        internal uint ConnId { get; init; }
        internal LatencyMode LatencyMode { get; init; }
        internal short AutoDisconnectTime { get; init; }

        protected override CommandOpCode Opcode => CommandOpCode.CmdChangeModeParameters;

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(ConnId);
            writer.Write((byte)LatencyMode);
            writer.Write(AutoDisconnectTime);
        }
    }

    internal class CmdPing : FlicCommand
    {
        internal readonly uint PingId = FlicIdGenerator<CmdPing>.NextId();

        protected override CommandOpCode Opcode => CommandOpCode.CmdPing;

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(PingId);
        }
    }

    internal class CmdGetButtonInfo : FlicCommand
    {
        internal BluetoothAddress BdAddr { get; init; }

        protected override CommandOpCode Opcode => CommandOpCode.CmdGetButtonInfo;

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(BdAddr);
        }
    }

    internal class CmdCreateScanWizard : FlicCommand
    {
        internal uint ScanWizardId { get; init; }

        protected override CommandOpCode Opcode => CommandOpCode.CmdCreateScanWizard;

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(ScanWizardId);
        }
    }

    internal class CmdCancelScanWizard : FlicCommand
    {
        internal uint ScanWizardId { get; init; }

        protected override CommandOpCode Opcode => CommandOpCode.CmdCancelScanWizard;

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(ScanWizardId);
        }
    }

    internal class CmdDeleteButton : FlicCommand
    {
        internal BluetoothAddress BdAddr { get; init; }

        protected override CommandOpCode Opcode => CommandOpCode.CmdDeleteButton;

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(BdAddr);
        }
    }
}
