// Header file defining SMA Energy Meter data
#pragma once
#include "pch.h"
#include "HubPage.g.h"

namespace WNS = Windows::Networking::Sockets;
namespace WSS = Windows::Storage::Streams;
namespace WFC = Windows::Foundation::Collections;

namespace SMAEnergyMeter
{
	namespace Data
	{
		typedef std::map<int16, int64>  EnergyMeterDataMap;
		public enum class Status
		{
			ok,				// no error occured
			invalidMulticastAddress,
			invalidUDPPort,
			joinFailed,
			parseMsg_notenoughdata,
			parseMsg_wrongheader,
			parseMsg_invaliddataformat,
			parseMsg_invaliddata
		};
		public enum class AddressType
		{
			actual = 4,
			summed = 8
		};

		// EnergyMeterMessage class
		//   Will contain the parsed data of a raw Message from the SMA EnergyMeter
		private class EnergyMeterMessage sealed
		{
		public:
			EnergyMeterMessage();									// constructor
			~EnergyMeterMessage();									// destructor

			int32 serial;											// serial number of the EnergyMeter
			int32 timestamp;										// UTC timestamp of the message

			void SetAddress(int16 address, AddressType type, int64 value);	// memorize an address when reading the raw message, version for the "summed" addresses containing 8 byte data

			int  GetAddressActual(int16 address);							// retrieve the memorized address ('actual' type)
			int64 GetAddressSummed(int16 address);							// retrieve the memorized address ('summed' type)

			Concurrency::task<Status> ParseRawMessageAsync(WSS::DataReader^ dr);			// read raw message from the supplied datareader (async)

		private:
			const unsigned int RAW_MESSAGE_LENGTH = 600;				// one UDP packet has 600 bytes of data
			const unsigned int RAW_MESSAGE_NUMBER_OF_FIELDS = 58;		// expected (minimum) number of data fields in the message
			EnergyMeterDataMap umData;									// collection of data: key = address(P,Q,S,...) + type (4 or 8, actual or sum), value = value from meter
		};

		[Windows::UI::Xaml::Data::Bindable]
		public ref class EnergyMeterDataView sealed : public Windows::UI::Xaml::Data::INotifyPropertyChanged
		{
		public:
			EnergyMeterDataView();

			property Platform::String^ txtIP1 {
				Platform::String^ get();
				void set(Platform::String^ value);
			}
			property Platform::String^ txtIP2 {
				Platform::String^ get();
				void set(Platform::String^ value);
			}
			property Platform::String^ txtIP3 {
				Platform::String^ get();
				void set(Platform::String^ value);
			}
			property Platform::String^ txtIP4 {
				Platform::String^ get();
				void set(Platform::String^ value);
			}
			property Platform::String^ txtUDPPort {
				Platform::String^ get();
				void set(Platform::String^ value);
			}
			property Platform::String^ txtJoinStatus {
				Platform::String^ get();
				void set(Platform::String^ value);
			}
			property Platform::String^ txtProcessStatus {
				Platform::String^ get();
				void set(Platform::String^ value);
			}
			property Platform::String^ FGJoinStatus {
				Platform::String^ get();
				void set(Platform::String^ value);
			}
			property Platform::String^ FGProcessStatus {
				Platform::String^ get();
				void set(Platform::String^ value);
			}

			property WFC::IVector<Platform::String^>^ lstDevicesFound {
				WFC::IVector<Platform::String^>^ get();
				void set(WFC::IVector<Platform::String^>^ value);
			}

			property Windows::Networking::HostName ^ MulticastHostName {
				Windows::Networking::HostName^ get();
			}

			property int UDPPort {
				int get() { return iUDPPort; }
				void set(int value) { iUDPPort = value; }
			}

//			property SMAEnergyMeter::HubPage^ rootPage;				// saves the Hubpage control for later access inside our class

// Implementation of ICustomPropertyProvider provides a string representation for the object to be used as automation name for accessibility
//			virtual Windows::UI::Xaml::Data::ICustomProperty^ GetCustomProperty(Platform::String^ name);
//			virtual Windows::UI::Xaml::Data::ICustomProperty^ GetIndexedProperty(Platform::String^ name, Windows::UI::Xaml::Interop::TypeName type);
//			virtual Platform::String^ GetStringRepresentation();
//			property Windows::UI::Xaml::Interop::TypeName Type { virtual Windows::UI::Xaml::Interop::TypeName get(); }

			virtual event Windows::UI::Xaml::Data::PropertyChangedEventHandler^ PropertyChanged;
			
			Status JoinMulticast(/* Windows::UI::Xaml::Controls::ToggleSwitch^ ts */);

		internal:
			EnergyMeterDataView(Platform::String^ tIP1, Platform::String^ tIP2, Platform::String^ tIP3, Platform::String^ tIP4, Platform::String^ tUDP);

		private:
			// const data
			const unsigned int MAX_LOG_SIZE = 188;								// keep 3 minutes of log + some extra seconds
			// data bindings to UI controls in Hub page
			Platform::String^ sIP1, ^ sIP2, ^ sIP3, ^ sIP4;						// 4 textboxes to hold parts of the IPv4 address
			Platform::String^ sUDPPort;											// UDP Port receiving data

			Platform::String^ sJoinStatus, ^ sProcessStatus;					// status fields of toggle switches for feedback to user
			Platform::String^ sJoinStatusFG, ^ sProcessStatusFG;				// foreground colors of above status fields
			Platform::Collections::Vector<Platform::String^>^ vDevicesFound;	// list of devices found when parsing received messages

			// variables to work with:
			Windows::Networking::HostName ^hnMulticast;
			int iUDPPort;
			WNS::DatagramSocket ^socketUDP;
			std::deque<EnergyMeterMessage*> logEMMessages;						// log of messages implemented as a double ended queue of pointers to the actual messages

//			concurrency::task_completion_event<void> _loadCompletionEvent;
//			static concurrency::task<void> Init();

			void OnPropertyChanged(Platform::String^ propertyName);

			bool SetMulticastAddress();
			bool SetUDPPort();

			void OnUDPMessageReceived(WNS::DatagramSocket ^sender, WNS::DatagramSocketMessageReceivedEventArgs ^args);
			void CheckLogMessages();											// check if log needs backup to database and/or should be truncated

		};

	}

}