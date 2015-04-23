// Header file for mysql database back-end

#pragma once

#include "pch.h"
#include "HubPage.g.h"

/* #include <mysql++.h>
#include <ssqls.h>

sql_create_3(SMADeviceTypes, 1, 3,
	mysqlpp::sql_int, ID,
	mysqlpp::sql_varchar, name,
	mysqlpp::sql_varchar, deviceclass)

sql_create_7(SMADevices, 1, 7,
	mysqlpp::sql_int, ID,
	mysqlpp::sql_int, type,
	mysqlpp::sql_decimal_null, serialnumber,
	mysqlpp::sql_varchar_null, hostname,
	mysqlpp::sql_char_null, ipaddress,
	mysqlpp::sql_char_null, targetaddress,
	mysqlpp::sql_varchar_null, firmware)

sql_create_4(adr_energymeter, 2, 4,
	mysqlpp::sql_int, address,
	mysqlpp::sql_int, type,
	mysqlpp::sql_varchar_null, description,
	mysqlpp::sql_varchar_null, log_column)
*/

namespace SMAEnergyMeter
{
	namespace db
	{
		[Windows::UI::Xaml::Data::Bindable]
		public ref class EnergyMeterDatabaseView sealed : public Windows::UI::Xaml::Data::INotifyPropertyChanged
		{
		public:
			virtual event Windows::UI::Xaml::Data::PropertyChangedEventHandler^ PropertyChanged;

		private:
			void OnPropertyChanged(Platform::String^ propertyName);
		};
	}
}