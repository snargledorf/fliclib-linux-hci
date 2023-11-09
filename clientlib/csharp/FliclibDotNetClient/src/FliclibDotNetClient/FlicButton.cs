using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FliclibDotNetClient
{
    public record FlicButtonInfo(string? Uuid, string? Color, string? SerialNumber, int FlicVersion, uint FirmwareVersion);

    public class FlicButton
    {
        internal readonly FlicClient flicClient;
        private FlicButtonInfo? buttonInfo;

        internal FlicButton(FlicClient flicClient, Bdaddr bdAddr, FlicButtonInfo buttonInfo)
            : this(flicClient, bdAddr)
        {
            this.buttonInfo = buttonInfo;
        }

        internal FlicButton(FlicClient flicClient, Bdaddr bdAddr)
        {
            this.flicClient = flicClient;
            Bdaddr = bdAddr;
        }

        public Bdaddr Bdaddr { get; }

        public Task<ButtonConnectionChannel> OpenConnectionAsync(LatencyMode latencyMode = LatencyMode.NormalLatency, short autoDisconnectTime = ButtonConnectionChannel.DefaultAutoDisconnectTime, CancellationToken cancellationToken = default)
        {
            return flicClient.OpenButtonConnectionChannelAsync(this, latencyMode, autoDisconnectTime, cancellationToken: cancellationToken);
        }

        public Task CloseConnectionAsync(ButtonConnectionChannel channel, CancellationToken cancellationToken = default)
        {
            return flicClient.CloseButtonConnectionChannelAsync(channel, cancellationToken);
        }

        public async ValueTask<FlicButtonInfo> GetButtonInfoAsync(CancellationToken cancellationToken = default)
        {
            if (buttonInfo != null)
                return buttonInfo;

            GetButtonInfoResponse buttonInfoResponse = await flicClient.GetButtonInfoAsync(Bdaddr, cancellationToken).ConfigureAwait(false);
            return buttonInfo = buttonInfoResponse.ButtonInfo;
        }
    }
}
