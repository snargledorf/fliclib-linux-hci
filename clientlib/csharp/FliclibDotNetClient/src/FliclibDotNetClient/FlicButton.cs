using System;
using System.Threading;
using System.Threading.Tasks;

namespace FliclibDotNetClient
{
    public record FlicButtonInfo(string? Uuid, string? Color, string? SerialNumber, int FlicVersion, uint FirmwareVersion);

    public class FlicButton
    {
        private FlicButtonInfo? buttonInfo;

        internal FlicButton(FlicClient flicClient, Bdaddr bdAddr, FlicButtonInfo buttonInfo)
            : this(flicClient, bdAddr)
        {
            this.buttonInfo = buttonInfo;
        }

        internal FlicButton(FlicClient flicClient, Bdaddr bdAddr)
        {
            FlicClient = flicClient;
            Bdaddr = bdAddr;
        }

        public Bdaddr Bdaddr { get; }

        public FlicClient FlicClient { get; }

        public Task<ButtonConnectionChannel> OpenConnectionAsync(
            LatencyMode latencyMode = LatencyMode.NormalLatency,
            short autoDisconnectTime = ButtonConnectionChannel.DefaultAutoDisconnectTime,
            CancellationToken cancellationToken = default)
        {
            return FlicClient.OpenButtonConnectionChannelAsync(this, latencyMode, autoDisconnectTime, cancellationToken: cancellationToken);
        }

        public Task CloseConnectionAsync(ButtonConnectionChannel channel, CancellationToken cancellationToken = default)
        {
            return FlicClient.CloseButtonConnectionChannelAsync(channel, cancellationToken);
        }

        public async ValueTask<FlicButtonInfo> GetButtonInfoAsync(CancellationToken cancellationToken = default)
        {
            return buttonInfo ??= await FlicClient.GetButtonInfoAsync(this, cancellationToken).ConfigureAwait(false);
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default) => FlicClient.DisconnectAsync(this, cancellationToken);

        public Task DeleteAsync(CancellationToken cancellationToken = default) => FlicClient.DeleteAsync(this, cancellationToken);
    }
}
