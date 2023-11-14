using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FliclibDotNetClient
{
    /// <summary>
    /// Information about found button
    /// </summary>
    public class ScanWizardButtonInfoEventArgs : EventArgs
    {
        /// <summary>
        /// Bluetooth device address
        /// </summary>
        public Bdaddr BdAddr { get; internal set; }

        /// <summary>
        /// Advertised name
        /// </summary>
        public string? Name { get; internal set; }
    }

    /// <summary>
    /// Information about result of scan wizard
    /// </summary>
    public class ScanWizardCompletedEventArgs : ScanWizardButtonInfoEventArgs
    {
        /// <summary>
        /// The result.
        /// If WizardSuccess, a new button has been found and the NewVerifiedButton event will be raised on the FlicClient.
        /// </summary>
        public ScanWizardResult Result { get; internal set; }
    }

    /// <summary>
    /// A high level scan wizard.
    /// This class should be used when you want to add a new button.
    /// Register the events and add an instance of this class to a FlicClient with AddScanWizard.
    /// </summary>
    public class ScanWizard
    {
        internal uint ScanWizardId = FlicIdGenerator<ScanWizard>.NextId();

        internal Bdaddr BdAddr;
        internal string? Name;

        internal ScanWizard(FlicClient flicClient)
        {
            FlicClient = flicClient;
        }

        public FlicClient FlicClient { get; }

        /// <summary>
        /// Called at most once when a private button has been found. That means the user should press the Flic button for 7 seconds in order to make it public.
        /// </summary>
        public event EventHandler? FoundPrivateButton;

        /// <summary>
        /// Called at most once when a public button has been found. The server will now attempt to connect to the button.
        /// When this event has been received the FoundPrivateButton event will not be raised.
        /// </summary>
        public event EventHandler<ScanWizardButtonInfoEventArgs>? FoundPublicButton;

        /// <summary>
        /// Called at most once when a public button has connected. The server will now attempt to pair to the button.
        /// When this event has been received the FoundPrivateButton or FoundPublicButton will not be raised.
        /// </summary>
        public event EventHandler<ScanWizardButtonInfoEventArgs>? ButtonConnected;

        /// <summary>
        /// Called when the scan wizard has completed for any reason.
        /// </summary>
        public event EventHandler<ScanWizardCompletedEventArgs>? Completed;

        public ValueTask StartAsync(CancellationToken cancellationToken = default) => FlicClient.StartAsync(this, cancellationToken);

        public ValueTask CancelAsync(CancellationToken cancellationToken = default) => FlicClient.CancelAsync(this, cancellationToken);

        protected internal virtual void OnFoundPrivateButton()
        {
            FoundPrivateButton?.Invoke(this, EventArgs.Empty);
        }

        protected internal virtual void OnFoundPublicButton(ScanWizardButtonInfoEventArgs e)
        {
            FoundPublicButton?.Invoke(this, e);
        }

        protected internal virtual void OnButtonConnected(ScanWizardButtonInfoEventArgs e)
        {
            ButtonConnected?.Invoke(this, e);
        }

        protected internal virtual void OnCompleted(ScanWizardCompletedEventArgs e)
        {
            Completed?.Invoke(this, e);
        }
    }
}
