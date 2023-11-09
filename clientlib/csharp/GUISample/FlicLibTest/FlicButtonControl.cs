using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FliclibDotNetClient;

namespace FlicLibTest
{
    public partial class FlicButtonControl : UserControl
    {
        private readonly Bdaddr bdAddr;
        private readonly FlicClient flicClient;

        public FlicButtonControl(Bdaddr bdAddr, FlicClient flicClient)
            : this()
        {
            this.bdAddr = bdAddr;
            this.flicClient = flicClient;

            lblBdAddr.Text = bdAddr.ToString();
        }

        public FlicButtonControl()
        {
            InitializeComponent();
        }

        public bool Listening
        {
            get
            {
                return chkListen.Checked;
            }

            set
            {
                chkListen.CheckedChanged -= chkListen_CheckedChanged;
                chkListen.Checked = false;
                chkListen.CheckedChanged += chkListen_CheckedChanged;
            }
        }

        public ButtonConnectionChannel Channel
        {
            get;
            set;
        }

        private async void chkListen_CheckedChanged(object sender, EventArgs e)
        {
            if (chkListen.Checked)
            {
                Channel = new ButtonConnectionChannel(bdAddr);

                Channel.CreateConnectionChannelResponse += (sender1, eventArgs) =>
                {
                    if (eventArgs.Error != CreateConnectionChannelError.NoError)
                    {
                        Listening = false;
                    }
                    else
                    {
                        lblStatus.Text = eventArgs.ConnectionStatus.ToString();
                    }
                };

                Channel.Removed += (sender1, eventArgs) =>
                {
                    lblStatus.Text = "Disconnected";

                    Listening = false;
                };

                Channel.ConnectionStatusChanged += (sender1, eventArgs) =>
                {
                    lblStatus.Text = eventArgs.ConnectionStatus.ToString();
                };

                Channel.ButtonUpOrDown += (sender1, eventArgs) =>
                {
                    pictureBox.BackColor = eventArgs.ClickType == ClickType.ButtonDown ? Color.LimeGreen : Color.Red;
                };

                chkListen.Text = "Stop";

                await flicClient.AddConnectionChannelAsync(Channel);
            }
            else if (Channel != null)
            {
                chkListen.Text = "Listen";

                await flicClient.RemoveConnectionChannelAsync(Channel);
            }
        }
    }
}
