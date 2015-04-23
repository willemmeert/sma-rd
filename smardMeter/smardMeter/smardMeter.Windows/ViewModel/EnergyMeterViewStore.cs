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
        async void socketUDP_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            DataReader dr = args.GetDataReader();
            EnergyMeterMessage emm = new EnergyMeterMessage();
            Status s = await emm.ParseMessageAsync(dr);
            dr.Dispose();
            if (s == Status.ok)
            {
                System.Diagnostics.Debug.WriteLine("Incoming message sucessfully parsed at timestamp" + emm.timestamp.ToString());
                this.logEMMessages.AddLast(emm);                    // save message to our log (in memory)
                this.UpdateDeviceFound(emm.serial.ToString());              // update devices found list for UI
                this.UpdateLiveData(emm);
                EnergyMeter2db.CheckLogForDBUpdate(this.logEMMessages, EnergyMeterView.MAX_LOG_SIZE, 60);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Parsemessage failed ! Status was " + s.ToString());
            }
        }

    }
}
