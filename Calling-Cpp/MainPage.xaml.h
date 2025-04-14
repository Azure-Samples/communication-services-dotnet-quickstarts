#pragma once

#include "MainPage.g.h"
#include "stub_calling_sdk.h" // Include the stub header
#include <winrt/Windows.UI.Xaml.Controls.h> // For ComboBox etc.
#include <winrt/Windows.Media.Playback.h> // For MediaPlayerElement

namespace winrt::CallingCpp::implementation
{
    struct MainPage : MainPageT<MainPage>
    {
        MainPage();

        // Event Handlers (stubs for now)
        void InitAuthenticationButton_Click(winrt::Windows::Foundation::IInspectable const& sender, winrt::Windows::UI::Xaml::RoutedEventArgs const& e);
        void CallButton_Click(winrt::Windows::Foundation::IInspectable const& sender, winrt::Windows::UI::Xaml::RoutedEventArgs const& e);
        void HangupButton_Click(winrt::Windows::Foundation::IInspectable const& sender, winrt::Windows::UI::Xaml::RoutedEventArgs const& e);
        void CameraComboBox_SelectionChanged(winrt::Windows::Foundation::IInspectable const& sender, winrt::Windows::UI::Xaml::Controls::SelectionChangedEventArgs const& e);
        void VideoButton_Click(winrt::Windows::Foundation::IInspectable const& sender, winrt::Windows::UI::Xaml::RoutedEventArgs const& e);
        void MicButton_Click(winrt::Windows::Foundation::IInspectable const& sender, winrt::Windows::UI::Xaml::RoutedEventArgs const& e);
        void ScreenShareButton_Click(winrt::Windows::Foundation::IInspectable const& sender, winrt::Windows::UI::Xaml::RoutedEventArgs const& e);
        void BlurButton_Click(winrt::Windows::Foundation::IInspectable const& sender, winrt::Windows::UI::Xaml::RoutedEventArgs const& e);

    private:
        // Stub SDK objects
        std::unique_ptr<stub_CallClient> m_stub_callClient;
        std::unique_ptr<stub_CallAgent> m_stub_callAgent;
        std::unique_ptr<stub_Call> m_stub_call;
        std::unique_ptr<stub_DeviceManager> m_stub_deviceManager;
        std::unique_ptr<stub_LocalVideoStream> m_stub_localVideoStream;
        std::vector<stub_VideoDeviceInfo> m_stub_cameras; // Store available cameras

        // State variables
        bool m_isMicMuted{false};
        bool m_isVideoOn{false};
        bool m_isScreenSharing{false};
        bool m_isBackgroundBlurred{false};

        // Private methods (stubs)
        winrt::Windows::Foundation::IAsyncAction InitializeAgentAsync();
        winrt::Windows::Foundation::IAsyncAction InitVideoAsync();
        winrt::Windows::Foundation::IAsyncAction StartCallAsync();
        winrt::Windows::Foundation::IAsyncAction JoinCallAsync();
        winrt::Windows::Foundation::IAsyncAction HangupCallAsync();
        winrt::Windows::Foundation::IAsyncAction StartVideoAsync();
        winrt::Windows::Foundation::IAsyncAction StopVideoAsync();
        winrt::Windows::Foundation::IAsyncAction MuteAsync();
        winrt::Windows::Foundation::IAsyncAction UnmuteAsync();
        winrt::Windows::Foundation::IAsyncAction StartScreenShareAsync();
        winrt::Windows::Foundation::IAsyncAction StopScreenShareAsync();
        winrt::Windows::Foundation::IAsyncAction StartBackgroundBlurAsync();
        winrt::Windows::Foundation::IAsyncAction StopBackgroundBlurAsync();
        void UpdateUiState();
        void UpdateStatus(winrt::hstring status);

        // Event handler registrations (stubs)
        void RegisterCallEvents();
        void UnregisterCallEvents();

        // Event callbacks (stubs)
        void OnCallStateChanged(winrt::Windows::Foundation::IInspectable const& sender, winrt::hstring const& args);
        void OnRemoteParticipantsUpdated(winrt::Windows::Foundation::IInspectable const& sender, winrt::Windows::Foundation::Collections::IVectorView<stub_RemoteParticipant> const& args);
        void OnIncomingCall(winrt::Windows::Foundation::IInspectable const& sender, stub_IncomingCall const& args);

        // Event tokens
        winrt::event_token m_callStateChangedToken{};
        winrt::event_token m_remoteParticipantsUpdatedToken{};
        winrt::event_token m_incomingCallToken{};
    };
}

namespace winrt::CallingCpp::factory_implementation
{
    struct MainPage : MainPageT<MainPage, implementation::MainPage>
    {
    };
}
