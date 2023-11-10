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
                btnConnectDisconnect.Enabled = false;
                try
                {
                    _flicClient = await FlicClient.CreateAsync(txtServer.Text);
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

                _flicClient.NewVerifiedButton += async (o, args) => await GotButton(args.Button);

                GetInfoResponse getInfoResponse = await _flicClient.GetInfoAsync();

                lblBluetoothStatus.Text = "Bluetooth controller status: " + getInfoResponse.bluetoothControllerState.ToString();

                foreach (var bdAddr in getInfoResponse.verifiedButtons)
                {
                    await GotButton(bdAddr);
                }
            }
            else
            {
                _flicClient.Disconnect();

                btnConnectDisconnect.Enabled = false;

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
        }

        private async Task GotButton(FlicButton button)
        {
            var control = new FlicButtonControl(button);

            var bi = await button.GetButtonInfoAsync();

            switch (bi.Color)
            {
                case "white":
                    control.BackColor = Color.White;
                    break;
            }

            buttonsList.Controls.Add(control);
        }

        private async void btnAddNewFlic_Click(object sender, EventArgs e)
        {
            if (_currentScanWizard == null)
            {
                lblScanWizardStatus.Text = "Press and hold down your Flic button until it connects";

                _currentScanWizard = _flicClient.CreateScanWizard();

                _currentScanWizard.FoundPrivateButton += (o, args) =>
                {
                    lblScanWizardStatus.Text = "Hold down your Flic button for 7 seconds";
                };

                _currentScanWizard.FoundPublicButton += (o, args) =>
                {
                    lblScanWizardStatus.Text = "Found button " + args.BdAddr.ToString() + ", now connecting...";
                };

                _currentScanWizard.ButtonConnected += (o, args) =>
                {
                    lblScanWizardStatus.Text = "Connected to " + args.BdAddr.ToString() + ", now pairing...";
                };

                _currentScanWizard.Completed += (o, args) =>
                {
                    lblScanWizardStatus.Text = "Result: " + args.Result;
                    _currentScanWizard = null;
                    btnAddNewFlic.Text = "Add new Flic";
                };

                await _currentScanWizard.StartAsync();

                btnAddNewFlic.Text = "Cancel";
            }
            else
            {
                await _currentScanWizard.CancelAsync();

                btnAddNewFlic.Text = "Add new Flic";

                _currentScanWizard = null;
            }
        }
    }
}
