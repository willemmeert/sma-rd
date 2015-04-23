//
// HubPage.xaml.h
// Declaration of the HubPage class
//

#pragma once
#include "pch.h"
#include "HubPage.g.h"
#include "sma-energymeter-data.h"

namespace SMAEnergyMeter
{
	/// <summary>
	/// A page that displays a grouped collection of items.
	/// </summary>
	public ref class HubPage sealed
	{
	public:
		HubPage();
		/// <summary>
		/// This can be changed to a strongly typed view model.
		/// </summary>
		property Windows::Foundation::Collections::IObservableMap<Platform::String^, Platform::Object^>^ DefaultViewModel
		{
			Windows::Foundation::Collections::IObservableMap<Platform::String^, Platform::Object^>^  get();
		}
		/// <summary>
		/// NavigationHelper is used on each page to aid in navigation and 
		/// process lifetime management
		/// </summary>
		property SMAEnergyMeter::Common::NavigationHelper^ NavigationHelper
		{
			SMAEnergyMeter::Common::NavigationHelper^ get();
		}
		property SMAEnergyMeter::Data::EnergyMeterDataView^ HubSMAData
		{
			SMAEnergyMeter::Data::EnergyMeterDataView^ get() { return emData; }
			void set(SMAEnergyMeter::Data::EnergyMeterDataView^ value) { emData = value; }
		}

	protected:
		virtual void OnNavigatedTo(Windows::UI::Xaml::Navigation::NavigationEventArgs^ e) override;
		virtual void OnNavigatedFrom(Windows::UI::Xaml::Navigation::NavigationEventArgs^ e) override;

	private:
		SMAEnergyMeter::Data::EnergyMeterDataView^ emData;
		bool bJoinedMulticast;


		void LoadState(Platform::Object^ sender, SMAEnergyMeter::Common::LoadStateEventArgs^ e);
		void Hub_SectionHeaderClick(Platform::Object^ sender, Windows::UI::Xaml::Controls::HubSectionHeaderClickEventArgs^ e);
		void ItemView_ItemClick(Platform::Object^ sender, Windows::UI::Xaml::Controls::ItemClickEventArgs^ e);

		static Windows::UI::Xaml::DependencyProperty^ _defaultViewModelProperty;
		static Windows::UI::Xaml::DependencyProperty^ _navigationHelperProperty;
		void JoinMulticast_Toggled(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e);
	};
}
