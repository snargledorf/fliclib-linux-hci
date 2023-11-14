using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FliclibDotNetClient
{
    public record ScanWizardButtonInfo(Bdaddr BdAddr, string? Name);

    public record ScanWizardResults(
        ScanWizardResult Result,
        IEnumerable<ScanWizardButtonInfo> ConnectedButtons);

    /// <summary>
    /// A high level scan wizard.
    /// This class should be used when you want to add a new button.
    /// Register the events and add an instance of this class to a FlicClient with AddScanWizard.
    /// </summary>
    public class ScanWizard
    {
        private const int ScanTimeoutSeconds = 20;

        internal uint ScanWizardId = FlicIdGenerator<ScanWizard>.NextId();

        private readonly TaskCompletionSource<ScanWizardResults> completionSource = new();

        private ScanWizardButtonInfo? publicButton;

        private readonly Dictionary<Bdaddr, ScanWizardButtonInfo> connectedButtons = new();

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
        public event EventHandler<ScanWizardButtonInfo>? FoundPublicButton;

        /// <summary>
        /// Called at most once when a public button has connected. The server will now attempt to pair to the button.
        /// When this event has been received the FoundPrivateButton or FoundPublicButton will not be raised.
        /// </summary>
        public event EventHandler<ScanWizardButtonInfo>? ButtonConnected;

        public async Task<ScanWizardResults> RunAsync(CancellationToken cancellationToken = default)
        {
            await FlicClient.StartAsync(this, cancellationToken).ConfigureAwait(false);

            // Create a cancellation source to try and force cancellation if the clean method takes too long
            var forceCancelTokenSource = new CancellationTokenSource();
            var forceCancelReg = forceCancelTokenSource.Token
                .Register(
                    () =>
                    {
                        // Forced cancel timeout, try to cancel the task
                        completionSource.TrySetCanceled(cancellationToken);
                    });

            // Register against the methods cancellation token in order to issue a cancellation request
            var cancelReg = cancellationToken.Register(
                async () =>
                {
                    // Trigger forced cancellation after a timeout period
                    forceCancelTokenSource.CancelAfter(TimeSpan.FromSeconds(ScanTimeoutSeconds));

                    try
                    {
                        await FlicClient.CancelAsync(this, forceCancelTokenSource.Token).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
                    {
                        // Forced timeout elapsed, try cancelling the task
                        completionSource.TrySetCanceled(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        // An unexpected exception occurred, fail the task
                        completionSource.TrySetException(ex);
                    }
                });

            var result = await completionSource.Task.ConfigureAwait(false);

            // Unregister so that we don't trigger cancellations after we have received the result
            forceCancelReg.Unregister();
            cancelReg.Unregister();

            return result;
        }

        protected internal virtual void OnFoundPrivateButton()
        {
            FoundPrivateButton?.Invoke(this, EventArgs.Empty);
        }

        protected internal virtual void OnFoundPublicButton(ScanWizardButtonInfo e)
        {
            publicButton = e;
            FoundPublicButton?.Invoke(this, e);
        }

        protected internal virtual void OnButtonConnected()
        {
            if (publicButton == null)
                return;

            connectedButtons[publicButton.BdAddr] = publicButton;

            ButtonConnected?.Invoke(this, publicButton);

            publicButton = null;
        }

        protected internal virtual void OnCompleted(ScanWizardResult e)
        {
            publicButton = null;
            completionSource.SetResult(new(e, connectedButtons.Values.ToArray()));
        }
    }
}
