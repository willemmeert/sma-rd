using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace smardWeb
{
    public class smardWCFService : IsmardWCFService
    {

        public List<adrmap_energymeter> GetAddressMap_EnergyMeter()
        {
            using (Entities ctx = new Entities())
            {
                return ctx.adrmap_energymeter.ToList();
            }
        }

        public List<log_energymeter> GetLogRaw_EnergyMeter(int deviceID, int iTypeID, Int32 bStartTime, Int32 bEndTime)
        {
            using (Entities ctx = new Entities())
            { 
                var vlog = from p in ctx.log_energymeter
                           where (p.device == deviceID && p.logtime>= bStartTime && p.logtime <= bEndTime)
                           select p;
                return vlog.ToList();
            }
        }

        public int GetDevice(decimal? dSerialNumber)
        {
            using (Entities ctx = new Entities())
            {
                return (from d in ctx.smadevices
                        where (d.serialnumber == dSerialNumber)
                        select d.deviceID).FirstOrDefault();
            }
        }

        public int AddDevice(int iTypeID, decimal? sSerialNumber, string sHostname, string sIPAddress, string sTargetAddress, string sFirmware)
        {
            int iRet = -1;
            using (Entities ctx = new Entities())
            {
                // check if device already exists
                var q = from d in ctx.smadevices
                        where (d.devicetype == iTypeID && d.serialnumber == sSerialNumber)
                        select d;
                if (q.Count() == 0)         // no device with that serial number found, insert it
                {
                    smadevice dev = new smadevice();
                    dev.devicetype = iTypeID;
                    dev.firmware = sFirmware;
                    dev.hostname = sHostname;
                    dev.ipaddress = sIPAddress;
                    dev.serialnumber = sSerialNumber;
                    dev.targetaddress = sTargetAddress;
                    ctx.smadevices.Add(dev);
                    ctx.SaveChanges();
                    iRet = dev.deviceID;        // get the device ID after the insert has been saved (auto increment column)
                }
                else
                {   // device already found in database, update other columns
                    smadevice dev = q.First();
                    dev.firmware = sFirmware;
                    dev.hostname = sHostname;
                    dev.ipaddress = sIPAddress;
                    dev.targetaddress = sTargetAddress;
                    ctx.SaveChanges();
                    iRet = dev.deviceID;
                }
            }
            return iRet;
        }

        public int AddLogEntry_EnergyMeter(log_energymeter logdata)
        {
            using (Entities ctx = new Entities())
            {
                ctx.log_energymeter.Add(logdata);
                ctx.SaveChanges();
            }
            return 0;
        }

    }
}
