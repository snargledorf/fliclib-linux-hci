using System;
using System.Drawing;
using System.Windows.Forms;
using FliclibDotNetClient;

namespace FlicLibTest
{
    public partial class FlicButtonControl : UserControl
    {
        private readonly FlicButton? button;

        private ButtonConnectionChannel? channel;

        public FlicButtonControl(FlicButton button)
            : this()
        {
            this.button = button ?? throw new ArgumentNullException(nameof(button));

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
                chkListen.Checked = value;
                chkListen.CheckedChanged += chkListen_CheckedChanged;

                if (value)
                    chkListen.Text = "Stop";
                else
                    chkListen.Text = "Listen";
            }
        }

        private async void chkListen_CheckedChanged(object? sender, EventArgs e)
        {
            if (button == null)
                return;

            if (chkListen.Checked)
            {
                try
                {
                    channel = await button.OpenConnectionAsync();

                    channel.Removed += (sender1, eventArgs) =>
                    {
                        lblStatus.Text = "Disconnected";

                        Listening = false;
                    };

                    channel.ConnectionStatusChanged += (sender1, eventArgs) =>
                    {
                        lblStatus.Text = eventArgs.ConnectionStatus.ToString();
                    };

                    channel.ButtonUpOrDown += (sender1, eventArgs) =>
                    {
                        pictureBox.BackColor = eventArgs.ClickType == ClickType.ButtonDown ? Color.LimeGreen : Color.Red;
                    };

                    Listening = true;
                }
                catch
                {
                    Listening = false;
                }
            }
            else if (channel != null)
            {
                Listening = false;

                await channel.CloseAsync();
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            if (button != null)
                await button.DeleteAsync();

            this.Parent?.Controls.Remove(this);

            this.Dispose();
        }
    }
}
