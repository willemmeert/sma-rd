#include "pch.h"


using namespace SMAEnergyMeter::db;
namespace WNS = Windows::Networking::Sockets;
using namespace Concurrency;
namespace WSS = Windows::Storage::Streams;
namespace WFC = Windows::Foundation::Collections;

void EnergyMeterDatabaseView::OnPropertyChanged(Platform::String^ propertyName)
{
	PropertyChanged(this, ref new Windows::UI::Xaml::Data::PropertyChangedEventArgs(propertyName));
}