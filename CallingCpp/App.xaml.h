#pragma once

#include "App.xaml.g.h"

namespace winrt::CallingCpp::implementation
{
    struct App : AppT<App>
    {
        App();

        void OnLaunched(winrt::Windows::ApplicationModel::Activation::LaunchActivatedEventArgs const&);
        void OnSuspending(winrt::Windows::Foundation::IInspectable const&, winrt::Windows::ApplicationModel::SuspendingEventArgs const&);
        void OnNavigationFailed(winrt::Windows::Foundation::IInspectable const&, winrt::Windows::UI::Xaml::Navigation::NavigationFailedEventArgs const&);

      private:
        using super = AppT<App>;
    };
} // namespace winrt::CallingCpp::implementation

namespace winrt::CallingCpp::factory_implementation
{
    struct App : AppT<App, implementation::App>
    {
    };
} // namespace winrt::CallingCpp::factory_implementation
