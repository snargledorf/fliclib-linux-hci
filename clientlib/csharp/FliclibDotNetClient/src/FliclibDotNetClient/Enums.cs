using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FliclibDotNetClient
{
    public enum CreateConnectionChannelError : byte
    {
        NoError,
        MaxPendingConnectionsReached
    };

    public enum ConnectionStatus : byte
    {
        Disconnected,
        Connected,
        Ready
    };

    public enum DisconnectReason : byte
    {
        Unspecified,
        ConnectionEstablishmentFailed,
        TimedOut,
        BondingKeysMismatch
    };

    public enum RemovedReason : byte
    {
        RemovedByThisClient,
        ForceDisconnectedByThisClient,
        ForceDisconnectedByOtherClient,

        ButtonIsPrivate,
        VerifyTimeout,
        InternetBackendError,
        InvalidData,

        CouldntLoadDevice,

        DeletedByThisClient,
        DeletedByOtherClient,
        ButtonBelongsToOtherPartner,
        DeletedFromButton
    };

    public enum ClickType : byte
    {
        ButtonDown,
        ButtonUp,
        ButtonClick,
        ButtonSingleClick,
        ButtonDoubleClick,
        ButtonHold
    };

    public enum BdAddrType : byte
    {
        PublicBdAddrType,
        RandomBdAddrType
    };

    public enum LatencyMode : byte
    {
        NormalLatency,
        LowLatency,
        HighLatency
    };

    public enum ScanWizardResult : byte
    {
        WizardSuccess,
        WizardCancelledByUser,
        WizardFailedTimeout,
        WizardButtonIsPrivate,
        WizardBluetoothUnavailable,
        WizardInternetBackendError,
        WizardInvalidData,
        WizardButtonBelongsToOtherPartner,
        WizardButtonAlreadyConnectedToOtherDevice
    };

    public enum BluetoothControllerState : byte
    {
        Detached,
        Resetting,
        Attached
    };

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

    internal enum CommandOpCode : byte
    {
        CmdGetInfo = 0,
        CmdCreateScanner = 1,
        CmdRemoveScanner = 2,
        CmdCreateConnectionChannel = 3,
        CmdRemoveConnectionChannel = 4,
        CmdForceDisconnect = 5,
        CmdChangeModeParameters = 6,
        CmdPing = 7,
        CmdGetButtonInfo = 8,
        CmdCreateScanWizard = 9,
        CmdCancelScanWizard = 10,
        CmdDeleteButton = 11,
    }
}
