using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using smardMeter.smardWCFServiceReference;

namespace smardMeter.Data
{
    public class EnergyMeter2db
    {
        
        // Converts the messages of the SMA EnergyMeter device to a database entry
        // Inputs: * linked list of messages  (converted messages will be removed from the list)
        //         * start timestamp          (first time stamp to consider, will also be timestamp of database entry)
        //         * end timestamp            (last message to consider, will not be removed from list)
        // Output: writes the summary of the messages to a single database entry as follows:
        //            * actual data addresses: average is taken;
        //            * summed data addresses: last value is taken;
        //         removes the used messages from the queue
        public static async void ConvertMsgLogToDbEntry(LinkedList<EnergyMeterMessage> llemm , int tsStart, int tsEnd, decimal? dSerial)
        {
            var q = llemm.Where(lem1 => (lem1.serial == (int)dSerial && lem1.timestamp >= tsStart && lem1.timestamp < tsEnd));        // filter out the timestamps for the specified device
            if (q.Count() <= 0)
                return;                         // no timestamps to process, exit

            log_energymeter lem = new log_energymeter();                                                            // our log record for the database

            ObservableCollection<adrmap_energymeter> lAdrMap = await dbClient.GetAddressMap_EnergyMeterAsync();     // map address+type --> column name

            lem.device = await dbClient.GetDeviceAsync(dSerial);                                                    // get the database ID for our serial number
            lem.logtime = tsStart - (tsStart % 60);                                                                 // make timestamps rounded off to nearest minute

            Type tlem = typeof(log_energymeter);
            foreach (adrmap_energymeter mapping in lAdrMap)                                                         // now map each address+type to a database field
            {   
                if (mapping.type == (int)AddressType.actual)
                {
                    tlem.GetRuntimeProperty(mapping.log_column).SetValue(lem, q.Average(q1 => q1.GetAddressActual((AddressMap)mapping.address)));    // db should log average value
                }
                else
                    if (mapping.type == (int)AddressType.summed)
                    {
                        tlem.GetRuntimeProperty(mapping.log_column).SetValue(lem, q.Last());                        // last of the readings has correct sum to store
                    }
            }

            int iStatus = await dbClient.AddLogEntry_EnergyMeterAsync(lem);                                         // add summary log entry to the database
            if (iStatus >= 0)
            {
                llemm.RemoveAll(lem1 => (lem1.serial == (int)dSerial && lem1.timestamp >= tsStart && lem1.timestamp < tsEnd));  // remove the logged items from our queue
            }
        }

        // Checks the log to see if it gets too long (>3 minutes)
        // if so, messages of the oldest minute are updated to the database (and removed from memory)
        public static void CheckLogForDBUpdate(LinkedList<EnergyMeterMessage> llemm, uint iMaxLogSize, uint iSaveLogSize)
        {
            EnergyMeterMessage emFirst = llemm.First(em1 => em1.serial == llemm.Last().serial);                 // first message of device that last added a message
            if (emFirst.timestamp < llemm.Last().timestamp - iMaxLogSize)
            {
                ConvertMsgLogToDbEntry(llemm, emFirst.timestamp, (int)(emFirst.timestamp - emFirst.timestamp%iSaveLogSize + iSaveLogSize), emFirst.serial);
            }
            
        }

        private static smardWCFServiceReference.IsmardWCFServiceClient dbClient = new smardWCFServiceReference.IsmardWCFServiceClient();
    }
}
