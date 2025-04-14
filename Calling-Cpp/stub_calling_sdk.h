#pragma once
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.UI.Xaml.Controls.h> // For UIElement

// Forward declarations
namespace winrt::Windows::Media::Capture { struct MediaStreamType; }
namespace winrt::Windows::UI::Xaml { struct UIElement; }
namespace winrt::Windows::ApplicationModel::Activation { struct LaunchActivatedEventArgs; }

namespace winrt::CallingCpp::implementation
{
    // --- Identifiers ---
    struct stub_CommunicationIdentifier {};
    struct stub_CommunicationUserIdentifier : stub_CommunicationIdentifier {};
    struct stub_PhoneNumberIdentifier : stub_CommunicationIdentifier {};
    struct stub_MicrosoftTeamsUserIdentifier : stub_CommunicationIdentifier {};
    struct stub_UnknownIdentifier : stub_CommunicationIdentifier {};

    // --- Core Classes ---
    struct stub_CommunicationTokenCredential {};

    struct stub_VideoDeviceInfo
    {
        winrt::hstring Name() { return L"Stub Camera"; }
        winrt::hstring Id() { return L"stub_camera_id"; }
    };

    struct stub_DeviceManager
    {
        winrt::Windows::Foundation::Collections::IVectorView<stub_VideoDeviceInfo> Cameras()
        {
            // Return a dummy list for now
            auto cameras = winrt::single_threaded_vector<stub_VideoDeviceInfo>();
            cameras.Append(stub_VideoDeviceInfo{});
            return cameras.GetView();
        }
    };

    struct stub_VideoStreamRendererView {}; // Placeholder

    struct stub_VideoStreamRenderer
    {
        winrt::Windows::Foundation::IAsyncOperation<stub_VideoStreamRendererView> CreateViewAsync()
        {
            co_return stub_VideoStreamRendererView{}; // Placeholder async return
        }
        void Dispose() {}
    };

    struct stub_LocalVideoStream
    {
        winrt::Windows::Media::Capture::MediaStreamType Source() { return {}; } // Placeholder
        winrt::Windows::Foundation::IAsyncAction StartEffectsAsync() { co_return; } // Placeholder async return
        winrt::Windows::Foundation::IAsyncAction StopEffectsAsync() { co_return; } // Placeholder async return
        bool IsSending() { return false; }
    };

    struct stub_RemoteVideoStream
    {
        int Id() { return 0; }
        bool IsAvailable() { return false; }
        winrt::event_token IsAvailableChanged(winrt::Windows::Foundation::EventHandler<bool> const& handler) { return {}; }
        void IsAvailableChanged(winrt::event_token const& token) noexcept {}
    };

    struct stub_RemoteParticipant
    {
        stub_CommunicationIdentifier Identifier() { return {}; }
        winrt::hstring DisplayName() { return L"Stub Participant"; }
        winrt::hstring State() { return L"Connected"; } // Placeholder state
        winrt::Windows::Foundation::Collections::IVectorView<stub_RemoteVideoStream> VideoStreams()
        {
             // Return a dummy list for now
            auto streams = winrt::single_threaded_vector<stub_RemoteVideoStream>();
            return streams.GetView();
        }
        winrt::event_token StateChanged(winrt::Windows::Foundation::EventHandler<winrt::hstring> const& handler) { return {}; }
        void StateChanged(winrt::event_token const& token) noexcept {}
        winrt::event_token VideoStreamsUpdated(winrt::Windows::Foundation::EventHandler<winrt::Windows::Foundation::Collections::IVectorView<stub_RemoteVideoStream>> const& handler) { return {}; }
        void VideoStreamsUpdated(winrt::event_token const& token) noexcept {}
    };

    struct stub_StartCallOptions {};
    struct stub_JoinCallOptions {};
    struct stub_AcceptCallOptions {};
    struct stub_HangUpOptions {};

    struct stub_Call
    {
        winrt::hstring Id() { return L"stub_call_id"; }
        winrt::hstring State() { return L"None"; } // Placeholder state
        winrt::Windows::Foundation::Collections::IVectorView<stub_RemoteParticipant> RemoteParticipants()
        {
            // Return a dummy list for now
            auto participants = winrt::single_threaded_vector<stub_RemoteParticipant>();
            return participants.GetView();
        }
        winrt::Windows::Foundation::IAsyncAction HangUpAsync(stub_HangUpOptions const& options) { co_return; }
        winrt::Windows::Foundation::IAsyncAction MuteAsync() { co_return; }
        winrt::Windows::Foundation::IAsyncAction UnmuteAsync() { co_return; }
        winrt::Windows::Foundation::IAsyncAction StartVideoAsync(stub_LocalVideoStream const& stream) { co_return; }
        winrt::Windows::Foundation::IAsyncAction StopVideoAsync(stub_LocalVideoStream const& stream) { co_return; }

        winrt::event_token StateChanged(winrt::Windows::Foundation::EventHandler<winrt::hstring> const& handler) { return {}; }
        void StateChanged(winrt::event_token const& token) noexcept {}
        winrt::event_token RemoteParticipantsUpdated(winrt::Windows::Foundation::EventHandler<winrt::Windows::Foundation::Collections::IVectorView<stub_RemoteParticipant>> const& handler) { return {}; }
        void RemoteParticipantsUpdated(winrt::event_token const& token) noexcept {}
    };

    struct stub_IncomingCall
    {
        winrt::hstring Id() { return L"stub_incoming_call_id"; }
        stub_CommunicationIdentifier CallerInfo() { return {}; }
        winrt::Windows::Foundation::IAsyncOperation<stub_Call> AcceptAsync(stub_AcceptCallOptions const& options)
        {
             co_return stub_Call{}; // Placeholder async return
        }
        winrt::Windows::Foundation::IAsyncAction RejectAsync() { co_return; }
    };

    struct stub_PushNotificationInfo {}; // Placeholder

    struct stub_CallAgent
    {
        stub_DeviceManager DeviceManager() { return {}; }
        winrt::Windows::Foundation::IAsyncOperation<stub_Call> StartCallAsync(
            winrt::Windows::Foundation::Collections::IVectorView<stub_CommunicationIdentifier> const& participants,
            stub_StartCallOptions const& options)
        {
            co_return stub_Call{}; // Placeholder async return
        }
         winrt::Windows::Foundation::IAsyncOperation<stub_Call> JoinAsync(stub_JoinCallOptions const& options)
        {
             co_return stub_Call{}; // Placeholder async return
        }
        winrt::Windows::Foundation::IAsyncAction RegisterForPushNotificationAsync(winrt::hstring const& deviceToken) { co_return; }

        winrt::event_token IncomingCall(winrt::Windows::Foundation::EventHandler<stub_IncomingCall> const& handler) { return {}; }
        void IncomingCall(winrt::event_token const& token) noexcept {}
    };

    struct stub_CallClient
    {
        winrt::Windows::Foundation::IAsyncOperation<stub_CallAgent> CreateCallAgentAsync(
            stub_CommunicationTokenCredential const& credential,
            winrt::Windows::Foundation::IInspectable const& options) // Options type might vary
        {
            co_return stub_CallAgent{}; // Placeholder async return
        }
        stub_DeviceManager GetDeviceManager() { return {}; }
    };
}
