#include "pch.h"
#include <boost/lexical_cast.hpp>
#include "sma-energymeter-data.h"


using namespace SMAEnergyMeter::Data;
namespace WNS = Windows::Networking::Sockets;
using namespace Concurrency;
namespace WSS = Windows::Storage::Streams;
namespace WFC = Windows::Foundation::Collections;

EnergyMeterMessage::EnergyMeterMessage()
{
	serial = 0;
	timestamp = 0;
}

EnergyMeterMessage::~EnergyMeterMessage()
{
	// no pointers to be freed at the moment
}

// memorize an address when reading the raw message
void EnergyMeterMessage::SetAddress(int16 address, AddressType type, int64 value)		// memorize an address when reading the raw message, version for the "summed" addresses containing 8 byte data
{
	umData.insert({ (int16)(address * 10 + (short)type), value } );
}

int EnergyMeterMessage::GetAddressActual(int16 address)									// retrieve the memorized address ('actual' type)
{
	EnergyMeterDataMap::iterator it = umData.find(address * 10 + (short)AddressType::actual);
	if (it != umData.end())
		return (int16)(it->second);
	else
		return 0;				// assume zero if we do not find the data
}

int64 EnergyMeterMessage::GetAddressSummed(int16 address)									// retrieve the memorized address ('summed' type)
{
	EnergyMeterDataMap::iterator it = umData.find(address * 10 + (short)AddressType::summed);
	if (it != umData.end())
		return it->second;
	else
		return 0;				// assume zero if we do not find the data
}

task<Status> EnergyMeterMessage::ParseRawMessageAsync(WSS::DataReader^ dr)
{
	dr->ByteOrder = WSS::ByteOrder::BigEndian;									// byte order of the SMA stream

	auto loadMsgTask = create_task(dr->LoadAsync(RAW_MESSAGE_LENGTH));			// load the buffer with the expected data

	return loadMsgTask.then([this, dr](task<unsigned int> bytesLoaded)
	{
		bytesLoaded.get();			// catch any exceptions from async load
		if (dr->UnconsumedBufferLength < RAW_MESSAGE_LENGTH) {					// confirm we have correct message length
			Platform::String^ debugMsg = Platform::String::Concat(L"UDP Message ignored: only ", dr->UnconsumedBufferLength.ToString());
			debugMsg += L" bytes received";
			OutputDebugString(debugMsg->Data());
			return Status::parseMsg_notenoughdata;
		}
		// enough data present to try to parse
		Platform::Array<byte>^ arHeader = ref new Platform::Array<byte>(20);		// Header should have 20 bytes
		dr->ReadBytes(arHeader);
		if (arHeader[0] != 'S' || arHeader[1] != 'M' || arHeader[2] != 'A') {		// Header should start with 'SMA' identification
			return Status::parseMsg_invaliddata;
		}
		// ignoring the other 17 bytes for now
		this->serial = dr->ReadInt32();												// serial number of the device (4 bytes)
		this->timestamp = dr->ReadInt32();											// utc timestamp (4 bytes)
		unsigned int iCountFields = 0;												// keep track of how many fields we have parsed
		int16 iAddress = 0;
		AddressType atField;
		Platform::Array<byte>^ arFieldHeader = ref new Platform::Array<byte>(4);
		while (dr->UnconsumedBufferLength >= 4)
		{
			dr->ReadBytes(arFieldHeader);											// field header: 00 xx yy 00   xx = address, yy = type (04 or 08)
			if (arFieldHeader[0] != 0 || arFieldHeader[3] != 0) {
				if (iCountFields >= RAW_MESSAGE_NUMBER_OF_FIELDS) {
					break;
				}
				else {
					return Status::parseMsg_invaliddataformat;
				}
			}
			if (arFieldHeader[2] != static_cast<byte>(AddressType::actual) || arFieldHeader[2] != static_cast<byte>(AddressType::summed)) {
				if (iCountFields >= RAW_MESSAGE_NUMBER_OF_FIELDS) {
					break;
				}
				else {
					return Status::parseMsg_invaliddata;
				}
			}
			atField = static_cast<AddressType>(arFieldHeader[2]);
			if (atField == AddressType::actual) {
				this->SetAddress(static_cast<short>(arFieldHeader[1]), atField, dr->ReadInt32());	// data value is 4 bytes long
			}
			else {
				this->SetAddress(static_cast<short>(arFieldHeader[1]), atField, dr->ReadInt64());  // data value is 8 bytes long
			}
			iCountFields++;
		}
		return Status::ok;
	});

}


// EnergyMeterData^ EnergyMeterData::_energyMeterData = nullptr;

/* concurrency::task<void> EnergyMeterData::Init()
{
	if (_energyMeterData == nullptr)
	{
		_energyMeterData = ref new EnergyMeterData();
	}
	return concurrency::create_task(_energyMeterData->_loadCompletionEvent);
}
*/

EnergyMeterDataView::EnergyMeterDataView()
{
	hnMulticast = nullptr;
	socketUDP = nullptr;
	iUDPPort = 0;
	
	// UI controls
	sIP1 = L"239";
	sIP2 = L"12";
	sIP3 = L"255";
	sIP4 = L"254";
	sUDPPort = L"9522";
	sJoinStatus = L"Idle, not joined";
	sJoinStatusFG = L"Yellow";
	sProcessStatus = L"Need to be joined first";
	sProcessStatusFG = L"Red";
	vDevicesFound = ref new Platform::Collections::Vector<Platform::String^>();
	vDevicesFound->Append(L"<None>");
	logEMMessages.clear();
	
	// Signal load completion event
	// _loadCompletionEvent.set();
}

EnergyMeterDataView::EnergyMeterDataView(Platform::String^ tIP1, Platform::String^ tIP2, Platform::String^ tIP3, Platform::String^ tIP4, Platform::String^ tUDP):
sIP1(tIP1), sIP2(tIP2), sIP3(tIP3), sIP4(tIP4), sUDPPort(tUDP)
{
}

Platform::String^ EnergyMeterDataView::txtIP1::get()
{ return sIP1;
}

void EnergyMeterDataView::txtIP1::set(Platform::String^ value)
{ 
	if (sIP1 != value) {
		sIP1 = value;
		OnPropertyChanged("txtIP1"); }
}

Platform::String^ EnergyMeterDataView::txtIP2::get()
{
	return sIP2;
}

void EnergyMeterDataView::txtIP2::set(Platform::String^ value)
{
	if (sIP2 != value) {
		sIP2 = value;
		OnPropertyChanged("txtIP2");
	}
}

Platform::String^ EnergyMeterDataView::txtIP3::get()
{
	return sIP3;
}

void EnergyMeterDataView::txtIP3::set(Platform::String^ value)
{
	if (sIP3 != value) {
		sIP3 = value;
		OnPropertyChanged("txtIP3");
	}
}

Platform::String^ EnergyMeterDataView::txtIP4::get()
{
	return sIP4;
}

void EnergyMeterDataView::txtIP4::set(Platform::String^ value)
{
	if (sIP4 != value) {
		sIP4 = value;
		OnPropertyChanged("txtIP4");
	}
}

Platform::String^ EnergyMeterDataView::txtUDPPort::get()
{
	return sUDPPort;
}

void EnergyMeterDataView::txtUDPPort::set(Platform::String^ value)
{
	if (sUDPPort != value) {
		sUDPPort = value;
		OnPropertyChanged("txtUDPPort");
	}
}

Platform::String^ EnergyMeterDataView::txtJoinStatus::get()
{
	return sJoinStatus;
}

void EnergyMeterDataView::txtJoinStatus::set(Platform::String^ value)
{
	if (sJoinStatus != value) {
		sJoinStatus = value;
		OnPropertyChanged("txtJoinStatus");
	}
}

Platform::String^ EnergyMeterDataView::txtProcessStatus::get()
{
	return sProcessStatus;
}

void EnergyMeterDataView::txtProcessStatus::set(Platform::String^ value)
{
	if (sProcessStatus != value) {
		sProcessStatus = value;
		OnPropertyChanged("txtProcessStatus");
	}
}

Platform::String^ EnergyMeterDataView::FGJoinStatus::get()
{
	return sJoinStatusFG;
}

void EnergyMeterDataView::FGJoinStatus::set(Platform::String^ value)
{
	if (sJoinStatusFG != value) {
		sJoinStatusFG = value;
		OnPropertyChanged("FGJoinStatus");
	}
}

Platform::String^ EnergyMeterDataView::FGProcessStatus::get()
{
	return sProcessStatusFG;
}

void EnergyMeterDataView::FGProcessStatus::set(Platform::String^ value)
{
	if (sProcessStatusFG != value) {
		sProcessStatusFG = value;
		OnPropertyChanged("FGProcessStatus");
	}
}

WFC::IVector<Platform::String^>^ EnergyMeterDataView::lstDevicesFound::get()
{
	return vDevicesFound;
}

void EnergyMeterDataView::lstDevicesFound::set(WFC::IVector<Platform::String^>^ value)
{
	if (vDevicesFound->Equals(value))
		return;
	vDevicesFound = nullptr;
	vDevicesFound = safe_cast<Platform::Collections::Vector<Platform::String ^>^>(value);
	OnPropertyChanged("lstDevicesFound");
}

Windows::Networking::HostName^ EnergyMeterDataView::MulticastHostName::get()
{
	return hnMulticast;
}

void EnergyMeterDataView::OnPropertyChanged(Platform::String^ propertyName)
{
	PropertyChanged(this, ref new Windows::UI::Xaml::Data::PropertyChangedEventArgs(propertyName));
}

bool EnergyMeterDataView::SetMulticastAddress()
{
	hnMulticast = nullptr;
	if (sIP1->IsEmpty() || sIP2->IsEmpty() || sIP3->IsEmpty() || sIP4->IsEmpty()) {
		return false;
	}
	std::wstring wsIP1(sIP1->Data());
	int ip1;
	std::wstring wsIP2(sIP2->Data());
	int ip2;
	std::wstring wsIP3(sIP3->Data());
	int ip3;
	std::wstring wsIP4(sIP4->Data());
	int ip4;
	try {
		ip1 = boost::lexical_cast<int>(wsIP1);
		ip2 = boost::lexical_cast<int>(wsIP2);
		ip3 = boost::lexical_cast<int>(wsIP3);
		ip4 = boost::lexical_cast<int>(wsIP4);
	}
	catch (const boost::bad_lexical_cast& e) {  // impossible to convert individual octets to unsigned __int8
		(void)e;
		return false;
	}
	if (ip1 < 0 || ip2 < 0 || ip3 < 0 || ip4 < 0 || ip1>255 || ip2>255 || ip3>255 || ip4>255) {
		return false;
	}
	Platform::String^ plIP = ip1.ToString() + "." + ip2.ToString() + "." + ip3.ToString() + "." + ip4.ToString();
	hnMulticast = ref new Windows::Networking::HostName(plIP);
	return true;
}

bool EnergyMeterDataView::SetUDPPort()
{
	iUDPPort = 0;
	std::wstring wsPort(sUDPPort->Data());
	try {
		iUDPPort = boost::lexical_cast<int>(wsPort);
	}
	catch (const boost::bad_lexical_cast& e) {
		(void) e;
		return false;
	}
	if (iUDPPort<0 || iUDPPort>65535) { // illegal range for UDP ports
		iUDPPort = 0;
		return false;
	}
	return true;
}

Status EnergyMeterDataView::JoinMulticast(/* Windows::UI::Xaml::Controls::ToggleSwitch^ ts */)
{
	if (this->SetMulticastAddress()) {		// set multicastaddress from UI
		if (this->SetUDPPort()) {			// set UDP port from UI
			if (this->socketUDP != nullptr)
				delete this->socketUDP;
			this->socketUDP = ref new WNS::DatagramSocket();
			this->socketUDP->MessageReceived += ref new Windows::Foundation::TypedEventHandler<WNS::DatagramSocket ^, WNS::DatagramSocketMessageReceivedEventArgs ^>(this, &SMAEnergyMeter::Data::EnergyMeterDataView::OnUDPMessageReceived);

			// try bind to our UDP Port
			create_task(this->socketUDP->BindServiceNameAsync(iUDPPort.ToString())).then([this /*, ts */ ](task<void> previousTask)
			{
				try
				{
					previousTask.get();		// try getting an exception
					txtJoinStatus = L"Bind succesfull, join started...";
					FGJoinStatus = L"Yellow";
					socketUDP->JoinMulticastGroup(MulticastHostName);
					txtJoinStatus = L"Joined succesfully";
					FGJoinStatus = L"Green";
				}
				catch (Platform::Exception^ e)
				{
					/* ts->IsOn = false;   // reset to off state, as joined did not work */
					OutputDebugString(e->Message->Data());
					txtJoinStatus = L"Bind not succesfull !";
					FGJoinStatus = L"Red";
				}
			});
		}
		else  // invalid UDPPort
		{
			return Status::invalidUDPPort;
		}

	}
	else
	{
		return Status::invalidMulticastAddress;
	}
	return Status::ok;
}

void EnergyMeterDataView::OnUDPMessageReceived(WNS::DatagramSocket ^sender, WNS::DatagramSocketMessageReceivedEventArgs ^args)
{
	WSS::DataReader^ dr = args->GetDataReader();
	EnergyMeterMessage *emm = new EnergyMeterMessage();
	create_task(emm->ParseRawMessageAsync(dr)).then([this, emm](Status parsed)
	{
		if (parsed == Status::ok) {   // emm has meaningfull data --> store it in our memory log
			OutputDebugString(L"Message succesfully parsed");
			this->logEMMessages.push_back(emm);
		}
		else {
			OutputDebugString(L"ParseMessage failed");
		}
	});
}

void EnergyMeterDataView::CheckLogMessages()
{
	if (this->logEMMessages.size() >= MAX_LOG_SIZE) {			// maximum size of log reached
		// TODO: if database link is passed, save the oldest minute (should be about 60 messages) as one database entry for archive and/or later data mining
		while (this->logEMMessages.size() >= MAX_LOG_SIZE) {
			delete this->logEMMessages.front();				// free storage of oldest Message
			this->logEMMessages.pop_front();				// and remove it from our log
		}
	}
}