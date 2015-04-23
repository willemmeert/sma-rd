using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace smardWeb
{
    [ServiceContract]
    public interface IsmardWCFService
    {
        [OperationContract]
        List<adrmap_energymeter> GetAddressMap_EnergyMeter();

        [OperationContract]
        List<log_energymeter> GetLogRaw_EnergyMeter(int deviceID, int iTypeID, Int32 bStartTime, Int32 bEndTime);

        // AddDevice operation:
        //   Adds or updates the device to our database
        //   Returns:
        //      >0:  ID used in the database for this device
        //      -1:  unknown error
        //      -2:  database not available
        //      -3:  access denied
        [OperationContract]
        int AddDevice(int iTypeID, decimal? dSerialNumber, string sHostname, string sIPAddress, string sTargetAddress, string sFirmware);
        [OperationContract]
        int GetDevice(decimal? dSerialNumber);

        [OperationContract]
        int AddLogEntry_EnergyMeter(log_energymeter logdata);
    }
}
