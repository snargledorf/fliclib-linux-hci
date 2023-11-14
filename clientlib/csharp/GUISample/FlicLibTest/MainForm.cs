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
        private FlicClient? _flicClient;
        private CancellationTokenSource? scanWizardCancellationSource;

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
            try
            {
                if (_flicClient == null)
                {
                    btnConnectDisconnect.Enabled = false;

                    try
                    {
                        if (int.TryParse(txtPort.Text, out int port))
                            _flicClient = new FlicClient(txtServer.Text, port);
                        else
                            _flicClient = new FlicClient(txtServer.Text);

                        _flicClient.BluetoothControllerStateChange += (o, args) => lblBluetoothStatus.Text = "Bluetooth controller status: " + args.State.ToString();

                        _flicClient.NewVerifiedButton += async (o, args) => await GotButton(args.Button);

                        _flicClient.OnDisconnect += (_, _) => OnClientDisconnected();

                        _flicClient.OnException += (_, ex) => MessageBox.Show("Unexpected exception from client: " + ex.Message);

                        await _flicClient.ConnectAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Connect failed: " + ex.Message);
                        _flicClient = null;
                        return;
                    }

                    lblConnectionStatus.Text = "Connection status: Connected";
                    btnConnectDisconnect.Text = "Disconnect";

                    btnAddNewFlic.Text = "Add new Flic";
                    btnAddNewFlic.Enabled = true;
                    btnPing.Enabled = true;

                    GetInfoResponse getInfoResponse = await _flicClient.GetInfoAsync();

                    lblBluetoothStatus.Text = "Bluetooth controller status: " + getInfoResponse.BluetoothControllerState.ToString();

                    foreach (var bdAddr in getInfoResponse.VerifiedButtons)
                    {
                        await GotButton(bdAddr);
                    }
                }
                else
                {
                    _flicClient.Disconnect();
                }
            }
            finally
            {
                btnConnectDisconnect.Enabled = true;
            }
        }

        private void OnClientDisconnected()
        {
            _flicClient = null;

            scanWizardCancellationSource?.Cancel();
            scanWizardCancellationSource = null;

            buttonsList.Controls.Clear();
            btnAddNewFlic.Enabled = false;
            btnPing.Enabled = false;

            lblConnectionStatus.Text = "Connection status: Disconnected";
            lblBluetoothStatus.Text = "Bluetooth controller status:";
            btnConnectDisconnect.Text = "Connect";

            lblScanWizardStatus.Text = "";

            btnConnectDisconnect.Enabled = true;
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

        private async void btnPing_Click(object sender, EventArgs e)
        {
            btnPing.Enabled = false;

            while (_flicClient != null)
            {
                try
                {
                    var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await _flicClient.PingAsync(timeoutCts.Token);

                    MessageBox.Show("Success!");

                    break;
                }
                catch(Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
                {
                    var result = MessageBox.Show(
                        "Ping timed out",
                        "Ping failed",
                        MessageBoxButtons.RetryCancel,
                        MessageBoxIcon.Warning);

                    if(result != DialogResult.Retry)
                        break;
                }
                catch(Exception ex)
                {
                    var result = MessageBox.Show(
                        ex.Message,
                        "Ping failed",
                        _flicClient != null ? MessageBoxButtons.RetryCancel : MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    if (result != DialogResult.Retry)
                        break;
                }
            }

            btnPing.Enabled = _flicClient != null;
        }

        private async void btnAddNewFlic_Click(object sender, EventArgs e)
        {
            if (_flicClient == null)
                return;

            if (scanWizardCancellationSource == null)
            {
                var currentScanWizard = _flicClient.CreateScanWizard();

                currentScanWizard.FoundPrivateButton += (o, args) =>
                {
                    lblScanWizardStatus.Text = "Hold down your Flic button for 7 seconds";
                };

                currentScanWizard.FoundPublicButton += (o, args) =>
                {
                    lblScanWizardStatus.Text = "Found button " + args.BdAddr.ToString() + ", now connecting...";
                };

                currentScanWizard.ButtonConnected += (o, args) =>
                {
                    lblScanWizardStatus.Text = "Connected to " + args.BdAddr.ToString() + ", now pairing...";
                };

                btnAddNewFlic.Text = "Cancel";
                lblScanWizardStatus.Text = "Press and hold down your Flic button until it connects";

                try
                {
                    scanWizardCancellationSource = new CancellationTokenSource();
                    var results = await currentScanWizard.RunAsync(scanWizardCancellationSource.Token);
                    lblScanWizardStatus.Text = "Result: " + results.Result;
                }
                catch (Exception ex)
                {
                    lblScanWizardStatus.Text = "Error: " + ex.Message;
                }
                finally
                {
                    btnAddNewFlic.Text = "Add new Flic";
                    scanWizardCancellationSource = null;
                }
            }
            else
            {
                scanWizardCancellationSource.Cancel();
            }
        }
    }
}
