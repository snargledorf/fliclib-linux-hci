using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FliclibDotNetClient;

/*
 * Example client of FlicLib.
 * 
 * Consists of a GUI where a user can scan for new buttons as well as connect and see button up/down events.
 * 
 * Note that the Invoke((MethodInvoker) delegate { ... }) calls are made in order to run code on the UI thread which is needed to update the GUI.
 */

namespace FlicLibTest
{
    public partial class MainForm : Form
    {
        private FlicClient _flicClient;
        private ScanWizard _currentScanWizard;
        private CancellationTokenSource connectCancellationSource;

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            lblScanWizardStatus.Text = "";
        }

        private async void btnConnectDisconnect_Click(object sender, EventArgs e)
        {
            if (_flicClient == null)
            {
                connectCancellationSource = new CancellationTokenSource();

                btnConnectDisconnect.Enabled = false;
                try
                {
                    _flicClient = await FlicClient.CreateAsync(txtServer.Text, connectCancellationSource.Token);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Connect failed: " + ex.Message);
                    btnConnectDisconnect.Enabled = true;
                    return;
                }

                lblConnectionStatus.Text = "Connection status: Connected";
                btnConnectDisconnect.Text = "Disconnect";
                btnConnectDisconnect.Enabled = true;

                btnAddNewFlic.Text = "Add new Flic";
                btnAddNewFlic.Enabled = true;

                _flicClient.BluetoothControllerStateChange += (o, args) => lblBluetoothStatus.Text = "Bluetooth controller status: " + args.State.ToString();

                _flicClient.NewVerifiedButton += (o, args) => GotButton(args.BdAddr);

                Task getInfoTask = DisplayFlicInfoAsync(connectCancellationSource.Token);

                Task handleEventsTask = _flicClient.HandleEventsAsync(connectCancellationSource.Token);

                await Task.WhenAll(handleEventsTask, getInfoTask);

                // HandleEvents returns when the socket has disconnected for any reason

                buttonsList.Controls.Clear();
                btnAddNewFlic.Enabled = false;

                _flicClient = null;
                lblConnectionStatus.Text = "Connection status: Disconnected";
                lblBluetoothStatus.Text = "Bluetooth controller status:";
                btnConnectDisconnect.Text = "Connect";
                btnConnectDisconnect.Enabled = true;

                _currentScanWizard = null;
                lblScanWizardStatus.Text = "";
            }
            else
            {
                connectCancellationSource?.Cancel();
                _flicClient.Disconnect();
                btnConnectDisconnect.Enabled = false;
            }
        }

        private async Task DisplayFlicInfoAsync(CancellationToken cancellationToken)
        {
            GetInfoResponse getInfoResponse = await _flicClient.GetInfoAsync(cancellationToken);

            lblBluetoothStatus.Text = "Bluetooth controller status: " + getInfoResponse.bluetoothControllerState.ToString();

            foreach (var bdAddr in getInfoResponse.verifiedButtons)
            {
                GotButton(bdAddr);
            }
        }

        private void GotButton(Bdaddr bdAddr)
        {
            var control = new FlicButtonControl();
            control.lblBdAddr.Text = bdAddr.ToString();
            control.btnListen.Click += async (o, args) =>
            {
                if (!control.Listens)
                {
                    control.Listens = true;
                    control.btnListen.Text = "Stop";

                    control.Channel = new ButtonConnectionChannel(bdAddr);
                    control.Channel.CreateConnectionChannelResponse += (sender1, eventArgs) => 
                    {
                        if (eventArgs.Error != CreateConnectionChannelError.NoError)
                        {
                            control.Listens = false;
                            control.btnListen.Text = "Listen";
                        }
                        else
                        {
                            control.lblStatus.Text = eventArgs.ConnectionStatus.ToString();
                        }
                    };

                    control.Channel.Removed += (sender1, eventArgs) => 
                    {
                        control.lblStatus.Text = "Disconnected";
                        control.Listens = false;
                        control.btnListen.Text = "Listen";
                    };

                    control.Channel.ConnectionStatusChanged += (sender1, eventArgs) => 
                    {
                        control.lblStatus.Text = eventArgs.ConnectionStatus.ToString();
                    };

                    control.Channel.ButtonUpOrDown += (sender1, eventArgs) =>
                    {
                        control.pictureBox.BackColor = eventArgs.ClickType == ClickType.ButtonDown ? Color.LimeGreen : Color.Red;
                    };

                    await _flicClient.AddConnectionChannelAsync(control.Channel);
                }
                else
                {
                    await _flicClient.RemoveConnectionChannelAsync(control.Channel);
                }
            };
            buttonsList.Controls.Add(control);
        }

        private async void btnAddNewFlic_Click(object sender, EventArgs e)
        {
            if (_currentScanWizard == null)
            {
                lblScanWizardStatus.Text = "Press and hold down your Flic button until it connects";

                var scanWizard = new ScanWizard();
                scanWizard.FoundPrivateButton += (o, args) => 
                {
                    lblScanWizardStatus.Text = "Hold down your Flic button for 7 seconds";
                };

                scanWizard.FoundPublicButton += (o, args) => 
                {
                    lblScanWizardStatus.Text = "Found button " + args.BdAddr.ToString() + ", now connecting...";
                };

                scanWizard.ButtonConnected += (o, args) =>
                {
                    lblScanWizardStatus.Text = "Connected to " + args.BdAddr.ToString() + ", now pairing...";
                };

                scanWizard.Completed += (o, args) => 
                {
                    lblScanWizardStatus.Text = "Result: " + args.Result;
                    _currentScanWizard = null;
                    btnAddNewFlic.Text = "Add new Flic";
                };

                await _flicClient.AddScanWizardAsync(scanWizard);

                _currentScanWizard = scanWizard;
                btnAddNewFlic.Text = "Cancel";
            }
            else
            {
                await _flicClient.CancelScanWizardAsync(_currentScanWizard);
            }
        }
    }
}
