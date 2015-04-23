using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage.Streams;

namespace smardMeter.Data
{
    using EnergyMeterDataDictionary = Dictionary<short, long>;     // definition of our "Map": key = address * 10 + type (4 or 8, for actual or sum)

    public enum Status
    {
        ok,
        invalidMulticastAddress,
        invalidUDPPort,
        joinFailed,
        parseMsg_notenoughdata,
        parseMsg_wrongheader,
        parseMsg_invaliddataformat,
        parseMsg_invaliddata
    }

    public enum AddressType : short
	{
		actual = 4,
		summed = 8
	}

    public enum AddressMap : short
    {
        p_in_all = 1,
        p_out_all,
        q_out_all,
        q_in_all,
        s_in_all = 9,
        s_out_all,
        cosphi_all = 13,
        p_in_l1 = 21,
        p_out_l1,
        q_out_l1,
        q_in_l1,
        s_in_l1 = 29,
        s_out_l1,
        thd_l1,
        v_l1,
        cosphi_l1,
        p_in_l2 = 41,
        p_out_l2,
        q_out_l2,
        q_in_l2,
        s_in_l2 = 49,
        s_out_l2,
        thd_l2,
        v_l2,
        cosphi_l2,
        p_in_l3 = 61,
        p_out_l3,
        q_out_l3,
        q_in_l3,
        s_in_l3 = 69,
        s_out_l3,
        thd_l3,
        v_l3,
        cosphi_l3
    }
    public static class Extension
    {
        // extension for linked list
        public static int RemoveAll<T>(this LinkedList<T> list, Predicate<T> match)
        {
            if (list == null)
            {
                throw new ArgumentNullException("list");
            }
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }
            var count = 0;
            var node = list.First;
            while (node != null)
            {
                var next = node.Next;
                if (match(node.Value))
                {
                    list.Remove(node);
                    count++;
                }
                node = next;
            }
            return count;
        }

    }

    /// <summary>
    /// Message from an SMA EnergyMeter device
    /// </summary>
    public class EnergyMeterMessage
    {
        public const uint RAW_MESSAGE_LENGTH = 600;             // expected (minimum) number of bytes in one UDP packet
        public const uint RAW_MESSAGE_NUMBER_OF_FIELDS = 58;    // expected (minimum) number of data fields in a UDP packet

        public int serial { get; set; }                 // serial number of the device
        public int timestamp { get; set; }              // UTC timestamp of the message

        public EnergyMeterMessage()                     // public default constructor
        {
            serial = 0;
            timestamp = 0;
            msgData = new EnergyMeterDataDictionary();
        }

        public async Task<Status> ParseMessageAsync(DataReader dr)      // parse raw message from a datareader (should have a complete message waiting)
        {
            dr.ByteOrder = ByteOrder.BigEndian;                         // SMA stream has BigEndian type numbers !
            await dr.LoadAsync(RAW_MESSAGE_LENGTH);                     // load message into buffer
            if (dr.UnconsumedBufferLength < RAW_MESSAGE_LENGTH)
            {
                System.Diagnostics.Debug.WriteLine("UDP Message ignored; only " + dr.UnconsumedBufferLength.ToString() + " bytes received");
                return Status.parseMsg_notenoughdata;
            }
            // enough data are loaded, try to parse
            byte[] arHeader = new byte[20];                             // header is 20 bytes
            dr.ReadBytes(arHeader);
            if (arHeader[0] != 'S' || arHeader[1] != 'M' || arHeader[2] != 'A')
            {
                return Status.parseMsg_wrongheader;                     // header should start with 'SMA' string
            }
            // ignoring other 17 bytes for now
            // following 4 bytes are serial number
            this.serial = dr.ReadInt32();
            // following 4 bytes are UTC timestamp
            this.timestamp = dr.ReadInt32();
            uint iCountFields = 0;                                      // count each field we read to see if we get the minimum number of fields
            AddressType atField;
            byte[] arFieldHeader = new byte[4];
            while (dr.UnconsumedBufferLength >=4)
            {
                dr.ReadBytes(arFieldHeader);
                if (arFieldHeader[0] != 0 || arFieldHeader[3] != 0)
                {
                    if (iCountFields >= RAW_MESSAGE_NUMBER_OF_FIELDS)
                        break;                                              // no valid field header any more, but enough data read, just exit while
                    else
                        return Status.parseMsg_invaliddataformat;
                }
                if (!Enum.IsDefined(typeof(AddressType), arFieldHeader[2]))
                {
                    if (iCountFields >= RAW_MESSAGE_NUMBER_OF_FIELDS)
                        break;                                              // no valid field type, but enough data read, just exit while
                    else
                        return Status.parseMsg_invaliddata;
                }
                atField = (AddressType)Enum.Parse(typeof(AddressType), arFieldHeader[2].ToString());
                if (atField == AddressType.actual)
                    SetAddress((short)arFieldHeader[1], atField, (long)dr.ReadInt32());         // read 4 bytes
                else
                    SetAddress((short)arFieldHeader[1], atField, dr.ReadInt64());               // read 8 bytes
                iCountFields++;
            }
            return Status.ok;
        }

        public void SetAddress(short sAddress, AddressType adType, long value)
        {
            int iKey = sAddress * 10 + (int)adType;
            if (iKey <= 32767)
            {
                msgData[(short)iKey]=value;
            }          
        }

        public int GetAddressActual(AddressMap sAddress)
        {
            long lvalue = 0;
            int iKey = (short)sAddress * 10 + (int)AddressType.actual;
            if (iKey <= 32767)
            {
                bool b = msgData.TryGetValue((short)iKey, out lvalue);
                if (b)
                    return (int)lvalue;
                else
                    return 0;
            }
            return 0;
        }
        // Get the value associated with an "actual" type address, returns true if address is present in the message
        public bool GetAddressActual(short sAddress, ref int value)
        {
            long lvalue = 0;
            int iKey = sAddress * 10 + (int)AddressType.actual;
            if (iKey <=32767)
            {
                bool b = msgData.TryGetValue((short)iKey, out lvalue);
                if (b)
                    value = (int)lvalue;
                else
                    value = 0;
                return b;
            }
            return false;
        }

        // Get the value associated with an "summed" type address, returns true if address is present in the message
        public bool GetAddressSummed(short sAddress, ref long value)
        {
            int iKey = sAddress * 10 + (int)AddressType.summed;
            if (iKey <= 32767)
                return msgData.TryGetValue((short)iKey, out value);
            else
                return false;
        }

        private EnergyMeterDataDictionary msgData;      // actual data of the message, saved in Dictionary type
    }
  
}
