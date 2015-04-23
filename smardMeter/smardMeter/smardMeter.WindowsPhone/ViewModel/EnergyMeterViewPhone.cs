using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
                this.logEMMessages.AddLast(emm);                                                            // save message to our log (in memory)
                this.UpdateDeviceFound(emm.serial.ToString());                                              // update devices found list for UI
                EnergyMeterMessage emFirst = this.logEMMessages.First(em1 => em1.serial == emm.serial);     // first message of corresponding device
                if (emFirst.timestamp < emm.timestamp - 180)
                {
                    this.logEMMessages.RemoveAll(em2 => em2.serial == emm.serial && em2.timestamp >= emFirst.timestamp && em2.timestamp < (emFirst.timestamp - emFirst.timestamp % 60 + 60));
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Parsemessage failed ! Status was " + s.ToString());
            }
        }

    }
}
