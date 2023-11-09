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
        private readonly FlicButton button;
        private readonly FlicClient flicClient;

        public FlicButtonControl(FlicButton button, FlicClient flicClient)
            : this()
        {
            this.button = button;
            this.flicClient = flicClient;

            lblBdAddr.Text = button.Bdaddr.ToString();
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
                try
                {
                    Channel = await button.OpenConnectionAsync();

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
                }
                catch
                {
                    Listening = false;
                }
            }
            else if (Channel != null)
            {
                chkListen.Text = "Listen";

                await Channel.CloseAsync();
            }
        }
    }
}
