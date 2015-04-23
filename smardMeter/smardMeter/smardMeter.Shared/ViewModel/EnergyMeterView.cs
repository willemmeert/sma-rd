using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

using smardMeter.Data;

namespace smardMeter.ViewModel
{
    public partial class EnergyMeterView : Microsoft.Practices.Prism.Mvvm.BindableBase
    {
        public EnergyMeterView()
        {
            hnMulticast = null;
            socketUDP = null;
            txtIP1 = "239";
            txtIP2 = "12";
            txtIP3 = "255";
            txtIP4 = "254";
            txtUDPPort = "9522";
            txtJoinStatus = "Idle, not joined";
            FGJoinStatus = "Yellow";
            txtProcessStatus = "Need to be joined first";
            FGProcessStatus = "Red";
            lstDevicesFound = new ObservableCollection<String>();
            lstDevicesFound.Add(sNone);
            logEMMessages = new LinkedList<EnergyMeterMessage>();


            PowerPlotModel = new OxyPlot.PlotModel();
            PowerPlotModel.PlotMargins = new OxyPlot.OxyThickness(30, 5, 30, 20);
            PowerPlotModel.LegendOrientation = OxyPlot.LegendOrientation.Horizontal;
            PowerPlotModel.LegendPlacement = OxyPlot.LegendPlacement.Outside;
            PowerPlotModel.LegendPosition = OxyPlot.LegendPosition.BottomRight;

//            PowerPlotModel.Background = OxyPlot.OxyColors.Transparent;

            OxyPlot.Axes.CategoryAxis catAxes1 = new OxyPlot.Axes.CategoryAxis();
            catAxes1.Position = OxyPlot.Axes.AxisPosition.Left;
            catAxes1.Labels.Add("L1");
            catAxes1.Labels.Add("L2");
            catAxes1.Labels.Add("L3");
            PowerPlotModel.Axes.Add(catAxes1);

            OxyPlot.Axes.LinearAxis xAxis1 = new OxyPlot.Axes.LinearAxis();
            xAxis1.Position = OxyPlot.Axes.AxisPosition.Bottom;
            xAxis1.MajorGridlineStyle = OxyPlot.LineStyle.Dash;
            xAxis1.MinorGridlineStyle = OxyPlot.LineStyle.Dot;
            xAxis1.MaximumPadding = 0.05;
            xAxis1.MinimumPadding = 0.05;
            xAxis1.Title = "Power";
            xAxis1.Unit = "W / VAr";
            PowerPlotModel.Axes.Add(xAxis1);

            OxyPlot.Series.BarSeries bsPowerP = new OxyPlot.Series.BarSeries();
            bsPowerP.IsStacked = true;
            bsPowerP.Title = "Active";
            bsPowerP.Items.Add(new OxyPlot.Series.BarItem(0));
            bsPowerP.Items.Add(new OxyPlot.Series.BarItem(0));
            bsPowerP.Items.Add(new OxyPlot.Series.BarItem(0));
            PowerPlotModel.Series.Add(bsPowerP);

            OxyPlot.Series.BarSeries bsPowerQ = new OxyPlot.Series.BarSeries();
            bsPowerQ.IsStacked = true;
            bsPowerQ.Title = "Reactive";
            bsPowerQ.Items.Add(new OxyPlot.Series.BarItem(0));
            bsPowerQ.Items.Add(new OxyPlot.Series.BarItem(0));
            bsPowerQ.Items.Add(new OxyPlot.Series.BarItem(0));
            PowerPlotModel.Series.Add(bsPowerQ);
            PowerPlotModel.InvalidatePlot(true);

            VoltagePlotModel = new OxyPlot.PlotModel();
            VoltagePlotModel.PlotMargins = new OxyPlot.OxyThickness(30, 5, 30, 20);
            VoltagePlotModel.LegendOrientation = OxyPlot.LegendOrientation.Horizontal;
            VoltagePlotModel.LegendPlacement = OxyPlot.LegendPlacement.Outside;
            VoltagePlotModel.LegendPosition = OxyPlot.LegendPosition.BottomRight;
            OxyPlot.Axes.CategoryAxis catAxes2 = new OxyPlot.Axes.CategoryAxis();
            catAxes2.Position = OxyPlot.Axes.AxisPosition.Left;
            catAxes2.Labels.Add("L1");
            catAxes2.Labels.Add("L2");
            catAxes2.Labels.Add("L3");
            VoltagePlotModel.Axes.Add(catAxes2);

            OxyPlot.Axes.LinearAxis xAxis2 = new OxyPlot.Axes.LinearAxis();
            xAxis2.Position = OxyPlot.Axes.AxisPosition.Bottom;
            xAxis2.MajorGridlineStyle = OxyPlot.LineStyle.Dash;
            xAxis2.MinorGridlineStyle = OxyPlot.LineStyle.Dot;
            xAxis2.AbsoluteMinimum = 0;
          //xAxis2.Minimum = 100;
            xAxis2.MaximumPadding = 0.05;
            xAxis2.MinimumPadding = 0.05;
            xAxis2.Title = "Voltage";
            xAxis2.Unit = "V";
            VoltagePlotModel.Axes.Add(xAxis2);

            OxyPlot.Series.BarSeries bsVoltage = new OxyPlot.Series.BarSeries();
            bsVoltage.IsStacked = false;
            bsVoltage.Title = "Line voltage";
            bsVoltage.Items.Add(new OxyPlot.Series.BarItem(0));
            bsVoltage.Items.Add(new OxyPlot.Series.BarItem(0));
            bsVoltage.Items.Add(new OxyPlot.Series.BarItem(0));
            VoltagePlotModel.Series.Add(bsVoltage);

            VoltagePlotModel.InvalidatePlot(true);

            THDPlotModel = new OxyPlot.PlotModel();
            THDPlotModel.PlotMargins = new OxyPlot.OxyThickness(30, 5, 30, 20);
            THDPlotModel.LegendOrientation = OxyPlot.LegendOrientation.Horizontal;
            THDPlotModel.LegendPlacement = OxyPlot.LegendPlacement.Outside;
            THDPlotModel.LegendPosition = OxyPlot.LegendPosition.BottomRight;
            OxyPlot.Axes.CategoryAxis catAxes3 = new OxyPlot.Axes.CategoryAxis();
            catAxes3.Position = OxyPlot.Axes.AxisPosition.Left;
            catAxes3.Labels.Add("L1");
            catAxes3.Labels.Add("L2");
            catAxes3.Labels.Add("L3");
            THDPlotModel.Axes.Add(catAxes3);

            OxyPlot.Axes.LinearAxis xAxis3 = new OxyPlot.Axes.LinearAxis();
            xAxis3.Position = OxyPlot.Axes.AxisPosition.Bottom;
            xAxis3.MajorGridlineStyle = OxyPlot.LineStyle.Dash;
            xAxis3.MinorGridlineStyle = OxyPlot.LineStyle.Dot;
            xAxis3.AbsoluteMinimum = 0;
            xAxis3.Minimum = 0;
            xAxis3.MaximumPadding = 0.05;
            xAxis3.MinimumPadding = 0;
            xAxis3.Title = "THD";
            xAxis3.Unit = "%";
            THDPlotModel.Axes.Add(xAxis3);

            OxyPlot.Series.BarSeries bsTHD = new OxyPlot.Series.BarSeries();
            bsTHD.IsStacked = false;
            bsTHD.Title = "T.H.D.";
            bsTHD.Items.Add(new OxyPlot.Series.BarItem(0));
            bsTHD.Items.Add(new OxyPlot.Series.BarItem(0));
            bsTHD.Items.Add(new OxyPlot.Series.BarItem(0));
            THDPlotModel.Series.Add(bsTHD);
            THDPlotModel.InvalidatePlot(true);
        }

        public OxyPlot.PlotModel PowerPlotModel
        {
            get { return _PowerPlotModel; }
            set { SetProperty(ref _PowerPlotModel, value); }
        }
        public OxyPlot.PlotModel VoltagePlotModel
        {
            get { return _VoltagePlotModel; }
            set { SetProperty(ref _VoltagePlotModel, value); }
        }
        public OxyPlot.PlotModel THDPlotModel
        {
            get { return _THDPlotModel; }
            set { SetProperty(ref _THDPlotModel, value); }
        }
        public String txtIP1
        {
            get { return _txtIP1; }
            set { SetProperty(ref _txtIP1, value); }
        }
        public String txtIP2
        {
            get { return _txtIP2; }
            set { SetProperty(ref _txtIP2, value); }
        }
        public String txtIP3
        {
            get { return _txtIP3; }
            set { SetProperty(ref _txtIP3, value); }
        }
        public String txtIP4
        {
            get { return _txtIP4; }
            set { SetProperty(ref _txtIP4, value); }
        }
        public String txtUDPPort
        {
            get { return _txtUDPPort; }
            set { SetProperty(ref _txtUDPPort, value); }
        }
        public String txtJoinStatus
        {
            get { return _txtJoinStatus; }
            set { SetProperty(ref _txtJoinStatus, value); }
        }
        public String txtProcessStatus
        {
            get { return _txtProcessStatus; }
            set { SetProperty(ref _txtProcessStatus, value); }
        }
        public String FGJoinStatus
        {
            get { return _FGJoinStatus; }
            set { SetProperty(ref _FGJoinStatus, value); }
        }
        public String FGProcessStatus
        {
            get { return _FGProcessStatus;}
            set { SetProperty(ref _FGProcessStatus, value); }
        }
        public String lstDevicesFoundSelectedItem
        {
            get { return _SelectedDevice; }
            set { SetProperty(ref _SelectedDevice, value); }
        }
        public ObservableCollection<String> lstDevicesFound
        {
            get;
            set;
        }

        private const uint MAX_LOG_SIZE = 188;              // 3 minuts of log, + some extra seconds as buffer
        private const String sNone = "<None>";

        // data binding private fields
        private String _txtIP1;
        private String _txtIP2;
        private String _txtIP3;
        private String _txtIP4;
        private String _txtUDPPort;
        private String _txtJoinStatus;
        private String _txtProcessStatus;
        private String _FGJoinStatus;
        private String _FGProcessStatus;
        private String _SelectedDevice;
        private OxyPlot.PlotModel _PowerPlotModel;
        private OxyPlot.PlotModel _VoltagePlotModel;
        private OxyPlot.PlotModel _THDPlotModel;

        private HostName hnMulticast;
        private int iUDPPort;
        private DatagramSocket socketUDP;
        private LinkedList<EnergyMeterMessage> logEMMessages;       // log of messages, need double ended queue to be able to remove from beginning, add to end

        private bool SetMulticastAddress()
        {
            hnMulticast = null;
            Byte ip1, ip2, ip3, ip4;
            if (Byte.TryParse(txtIP1, out ip1) && Byte.TryParse(txtIP2, out ip2) && Byte.TryParse(txtIP3, out ip3) && Byte.TryParse(txtIP4, out ip4) )
            {
                hnMulticast = new HostName(ip1.ToString() + "." + ip2.ToString() + "." + ip3.ToString() + "." + ip4.ToString());
                return true;
            }
            return false;
        }
        private bool SetUDPPort()
        {
            if (int.TryParse(txtUDPPort, out iUDPPort))
            {
                if (iUDPPort > 0 || iUDPPort <= 65535)
                    return true;
                else
                    iUDPPort = 0;           // illegal range for UDP port
            }
            return false;
        }

        private void UpdateDeviceFound(String sDeviceName)          // check if this device name is already in lstDevicesFound
        {
            if (lstDevicesFound.Count == 1 && lstDevicesFound[0].Equals(sNone))
            {
                // special case: actually no device found up to now
                lstDevicesFound.Clear();                            // remove "<None>" item
                lstDevicesFound.Add(sDeviceName);
                lstDevicesFoundSelectedItem = sDeviceName;          // add and select the first device found
            }
            else
            {
                if (!lstDevicesFound.Contains(sDeviceName))
                {
                    lstDevicesFound.Add(sDeviceName);
                }
            }
        }
        private void UpdateLiveData(EnergyMeterMessage emm)           // new data arrived from the device, if it is selected update the live data plots
        {
            if (emm.serial.ToString() == lstDevicesFoundSelectedItem)
            {
                OxyPlot.Series.BarSeries bs = (OxyPlot.Series.BarSeries)(PowerPlotModel.Series[0]);     // get Power P series
                int i = emm.GetAddressActual(AddressMap.p_in_l1);
                if (i > 0)
                    bs.Items[0].Value = i;
                else
                    bs.Items[0].Value = -emm.GetAddressActual(AddressMap.p_out_l1);
                i = emm.GetAddressActual(AddressMap.p_in_l2);
                if (i > 0)
                    bs.Items[1].Value = i;
                else
                    bs.Items[1].Value = -emm.GetAddressActual(AddressMap.p_out_l2);
                i = emm.GetAddressActual(AddressMap.p_in_l3);
                if (i > 0)
                    bs.Items[2].Value = i;
                else
                    bs.Items[2].Value = -emm.GetAddressActual(AddressMap.p_out_l3);

                bs = (OxyPlot.Series.BarSeries)(PowerPlotModel.Series[1]);                          // get Power Q series
                i = emm.GetAddressActual(AddressMap.q_in_l1);
                if (i > 0)
                    bs.Items[0].Value = i;
                else
                    bs.Items[0].Value = -emm.GetAddressActual(AddressMap.q_out_l1);
                i = emm.GetAddressActual(AddressMap.q_in_l2);
                if (i > 0)
                    bs.Items[1].Value = i;
                else
                    bs.Items[1].Value = -emm.GetAddressActual(AddressMap.q_out_l2);
                i = emm.GetAddressActual(AddressMap.q_in_l3);
                if (i > 0)
                    bs.Items[2].Value = i;
                else
                    bs.Items[2].Value = -emm.GetAddressActual(AddressMap.q_out_l3);

                PowerPlotModel.InvalidatePlot(true);

            }
        }

        public async Task<Status> JoinMulticast()
        {
            if (this.SetMulticastAddress())                     // get multicast from Hubpage
            {
                if (this.SetUDPPort())                        // get UDP port from Hubpage
                {
                    if (this.socketUDP != null)
                        this.socketUDP.Dispose();            // clean up memory of old socket
                    this.socketUDP = new DatagramSocket();
                    this.socketUDP.MessageReceived += socketUDP_MessageReceived;

                    // start listen operation
                    try
                    {
                        await this.socketUDP.BindServiceNameAsync(iUDPPort.ToString());
                        txtJoinStatus = "Bind successfull, join started...";
                        FGJoinStatus = "Yellow";
                        this.socketUDP.JoinMulticastGroup(hnMulticast);
                        txtJoinStatus = "Joined succesfully";
                        FGJoinStatus = "Green";
                    }
                    catch (Exception e)
                    {
                        if (SocketError.GetStatus(e.HResult) == SocketErrorStatus.Unknown)
                        {
                            throw;
                        }
                        System.Diagnostics.Debug.WriteLine("Join failed with error: " + e.Message);
                        txtJoinStatus = "Failed with error: " + e.Message;
                        FGJoinStatus = "Red";
                        return Status.joinFailed;
                    }
                }
                else
                {
                    return Status.invalidUDPPort;
                }
            }
            else
            {
                return Status.invalidMulticastAddress;
            }
            return Status.ok;
        }

    }
}
