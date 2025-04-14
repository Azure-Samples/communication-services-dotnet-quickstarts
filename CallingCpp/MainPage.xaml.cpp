#include "pch.h"
#include "MainPage.xaml.h"
#if __has_include("MainPage.g.cpp")
#include "MainPage.g.cpp"
#endif

#include <winrt/Windows.UI.Xaml.Media.h> // For SolidColorBrush
#include <winrt/Windows.UI.Popups.h> // For MessageDialog
#include <winrt/Windows.Devices.Enumeration.h> // For device access checks

using namespace winrt;
using namespace winrt::Windows::UI::Xaml;
using namespace winrt::Windows::UI::Xaml::Controls;
using namespace winrt::Windows::Foundation;
using namespace winrt::Windows::Foundation::Collections;

namespace winrt::CallingCpp::implementation
{
    MainPage::MainPage()
    {
        InitializeComponent();
        UpdateStatus(L"Status: Please Initialize Client & Agent.");
        // Basic UI setup if needed
    }

    // --- Event Handlers ---

    fire_and_forget MainPage::InitAuthenticationButton_Click(IInspectable const&, RoutedEventArgs const&)
    {
        auto strong_this{get_strong()}; // Keep instance alive during async
        InitAuthenticationButton().IsEnabled(false);
        AccessTokenTextBox().IsEnabled(false);
        UpdateStatus(L"Status: Initializing...");

        try
        {
            // --- STUB ---
            // In real implementation:
            // 1. Get token from AccessTokenTextBox()
            // 2. Create stub_CommunicationTokenCredential
            // 3. Create stub_CallClient
            // 4. Create stub_CallAgent using client and credential
            // 5. Get stub_DeviceManager from agent
            // 6. Enumerate cameras using device manager
            // 7. Populate CameraComboBox
            // 8. Register for IncomingCall event

            m_stub_callClient = std::make_unique<stub_CallClient>();
            // Simulate async creation
            co_await InitializeAgentAsync();

            if (m_stub_callAgent)
            {
                 m_stub_deviceManager = std::make_unique<stub_DeviceManager>(m_stub_callAgent->DeviceManager());
                 co_await InitVideoAsync(); // Populate cameras
                 UpdateStatus(L"Status: Ready to call.");
                 CallButton().IsEnabled(true);
                 // Register for incoming calls (stub)
                 // m_incomingCallToken = m_stub_callAgent->IncomingCall({ get_strong(), &MainPage::OnIncomingCall });
            }
            else
            {
                 UpdateStatus(L"Status: Agent creation failed (Stub).");
                 InitAuthenticationButton().IsEnabled(true);
                 AccessTokenTextBox().IsEnabled(true);
            }
        }
        catch (winrt::hresult_error const& ex)
        {
            UpdateStatus(L"Status: Initialization Error: " + ex.message());
            InitAuthenticationButton().IsEnabled(true);
            AccessTokenTextBox().IsEnabled(true);
        }
    }

    fire_and_forget MainPage::CallButton_Click(IInspectable const&, RoutedEventArgs const&)
    {
        auto strong_this{get_strong()};
        CallButton().IsEnabled(false);
        HangupButton().IsEnabled(true); // Enable hangup immediately
        UpdateStatus(L"Status: Starting/Joining call...");

        try
        {
            // --- STUB ---
            // Determine if it's a join or start based on CalleeTextBox().Text()
            // Create options (stub_StartCallOptions or stub_JoinCallOptions)
            // Call m_stub_callAgent->StartCallAsync or JoinAsync
            // Store the resulting m_stub_call
            // Register for call events (StateChanged, RemoteParticipantsUpdated)
            // Update UI

            co_await StartCallAsync(); // Simulate starting a call
            if(m_stub_call)
            {
                UpdateStatus(L"Status: Call Connected (Stub).");
                RegisterCallEvents();
                UpdateUiState(); // Enable media buttons etc.
            }
            else
            {
                 UpdateStatus(L"Status: Call failed to start (Stub).");
                 CallButton().IsEnabled(true);
                 HangupButton().IsEnabled(false);
            }
        }
        catch (winrt::hresult_error const& ex)
        {
            UpdateStatus(L"Status: Call Error: " + ex.message());
            CallButton().IsEnabled(true); // Re-enable call button on error
            HangupButton().IsEnabled(false);
        }
    }

    fire_and_forget MainPage::HangupButton_Click(IInspectable const&, RoutedEventArgs const&)
    {
        auto strong_this{get_strong()};
        HangupButton().IsEnabled(false);
        UpdateStatus(L"Status: Hanging up...");

        try
        {
            // --- STUB ---
            // If m_stub_call exists:
            // 1. Unregister call events
            // 2. Call m_stub_call->HangUpAsync()
            // 3. Set m_stub_call = nullptr
            // 4. Update UI (disable media buttons, clear video)
            // 5. Reset state variables

            co_await HangupCallAsync(); // Simulate hangup
            UpdateStatus(L"Status: Call Ended.");
            CallButton().IsEnabled(true); // Ready for new call
            UpdateUiState(); // Disable media buttons etc.
        }
        catch (winrt::hresult_error const& ex)
        {
            UpdateStatus(L"Status: Hangup Error: " + ex.message());
            // Might need to force UI update depending on state
            HangupButton().IsEnabled(true); // Allow retry? Or disable?
        }
    }

    fire_and_forget MainPage::CameraComboBox_SelectionChanged(IInspectable const&, SelectionChangedEventArgs const&)
    {
         auto strong_this{get_strong()};
         int selectedIndex = CameraComboBox().SelectedIndex();
         if (selectedIndex >= 0 && selectedIndex < m_stub_cameras.size())
         {
             // --- STUB ---
             // 1. Get the selected stub_VideoDeviceInfo
             // 2. Create a new stub_LocalVideoStream with this device info
             // 3. If video is already on, potentially stop the old stream and start the new one
             UpdateStatus(L"Status: Camera selected (Stub).");
             // For now, just store it if needed, actual stream creation happens on video start
             // m_selectedCamera = m_stub_cameras[selectedIndex];
             VideoButton().IsEnabled(m_stub_call != nullptr); // Enable video button if in a call
         }
    }

    fire_and_forget MainPage::VideoButton_Click(IInspectable const&, RoutedEventArgs const&)
    {
        auto strong_this{get_strong()};
        VideoButton().IsEnabled(false); // Disable during operation

        try
        {
            if (m_isVideoOn)
            {
                UpdateStatus(L"Status: Stopping video...");
                // --- STUB ---
                // Call StopVideoAsync()
                co_await StopVideoAsync();
                UpdateStatus(L"Status: Video Off.");
                VideoButton().Label(L"Turn Video On");
                m_isVideoOn = false;
                // Clear local video preview (stub)
                // LocalVideo().Source(nullptr);
            }
            else
            {
                 UpdateStatus(L"Status: Starting video...");
                 // --- STUB ---
                 // Ensure m_stub_localVideoStream is created with selected camera
                 // Call StartVideoAsync()
                 co_await StartVideoAsync();
                 UpdateStatus(L"Status: Video On.");
                 VideoButton().Label(L"Turn Video Off");
                 m_isVideoOn = true;
                 // Render local video preview (stub)
                 // auto renderer = winrt::make<stub_VideoStreamRenderer>(m_stub_localVideoStream);
                 // auto view = co_await renderer.CreateViewAsync();
                 // LocalVideo().SetMediaPlayer(/* create media player source from view */);
            }
            VideoButton().IsEnabled(true); // Re-enable after operation
            BlurButton().IsEnabled(m_isVideoOn); // Enable/disable blur based on video state
        }
        catch (winrt::hresult_error const& ex)
        {
             UpdateStatus(L"Status: Video Error: " + ex.message());
             VideoButton().IsEnabled(true); // Re-enable on error
        }
    }

    fire_and_forget MainPage::MicButton_Click(IInspectable const&, RoutedEventArgs const&)
    {
        auto strong_this{get_strong()};
        MicButton().IsEnabled(false); // Disable during operation

        try
        {
            if (m_isMicMuted)
            {
                UpdateStatus(L"Status: Unmuting...");
                // --- STUB ---
                // Call UnmuteAsync()
                co_await UnmuteAsync();
                UpdateStatus(L"Status: Mic Unmuted.");
                MicButton().Label(L"Mute");
                m_isMicMuted = false;
            }
            else
            {
                 UpdateStatus(L"Status: Muting...");
                 // --- STUB ---
                 // Call MuteAsync()
                 co_await MuteAsync();
                 UpdateStatus(L"Status: Mic Muted.");
                 MicButton().Label(L"Unmute");
                 m_isMicMuted = true;
            }
             MicButton().IsEnabled(true); // Re-enable after operation
        }
        catch (winrt::hresult_error const& ex)
        {
             UpdateStatus(L"Status: Mic Error: " + ex.message());
             MicButton().IsEnabled(true); // Re-enable on error
        }
    }

    fire_and_forget MainPage::ScreenShareButton_Click(IInspectable const&, RoutedEventArgs const&)
    {
        // --- STUB --- Screen sharing not fully implemented in stub
        UpdateStatus(L"Status: Screen Sharing (Stub - Not Implemented).");
        // Toggle state and button label if needed
    }

     fire_and_forget MainPage::BlurButton_Click(IInspectable const&, RoutedEventArgs const&)
    {
        auto strong_this{get_strong()};
        if (!m_stub_localVideoStream || !m_isVideoOn) return; // Need active video stream

        BlurButton().IsEnabled(false); // Disable during operation

        try
        {
            if (m_isBackgroundBlurred)
            {
                UpdateStatus(L"Status: Stopping background blur...");
                // --- STUB ---
                // Call m_stub_localVideoStream->StopEffectsAsync()
                co_await StopBackgroundBlurAsync();
                UpdateStatus(L"Status: Background blur off.");
                BlurButton().Label(L"Blur Background");
                m_isBackgroundBlurred = false;
            }
            else
            {
                 UpdateStatus(L"Status: Starting background blur...");
                 // --- STUB ---
                 // Call m_stub_localVideoStream->StartEffectsAsync() with blur effect
                 co_await StartBackgroundBlurAsync();
                 UpdateStatus(L"Status: Background blur on.");
                 BlurButton().Label(L"Unblur Background");
                 m_isBackgroundBlurred = true;
            }
             BlurButton().IsEnabled(true); // Re-enable after operation
        }
        catch (winrt::hresult_error const& ex)
        {
             UpdateStatus(L"Status: Blur Effect Error: " + ex.message());
             BlurButton().IsEnabled(true); // Re-enable on error
        }
    }

    // --- Private Helper Methods (Stubs/Simulations) ---

    IAsyncAction MainPage::InitializeAgentAsync()
    {
        // Simulate async operation
        co_await winrt::resume_after(std::chrono::milliseconds(50)); // Simulate delay
        // --- STUB --- In real code, this uses m_stub_callClient
        m_stub_callAgent = std::make_unique<stub_CallAgent>();
        co_return;
    }

    IAsyncAction MainPage::InitVideoAsync()
    {
        // Simulate async operation
        co_await winrt::resume_after(std::chrono::milliseconds(30)); // Simulate delay
        // --- STUB ---
        // 1. Check for camera permissions
        // 2. Get cameras from m_stub_deviceManager
        // 3. Populate m_stub_cameras vector
        // 4. Populate CameraComboBox items
        // 5. Enable ComboBox if cameras found

        m_stub_cameras.clear();
        CameraComboBox().Items().Clear();
        if (m_stub_deviceManager)
        {
            auto cameras = m_stub_deviceManager->Cameras();
            if (cameras.Size() > 0)
            {
                for (auto const& cam : cameras)
                {
                    m_stub_cameras.push_back(cam);
                    CameraComboBox().Items().Append(box_value(cam.Name()));
                }
                CameraComboBox().IsEnabled(true);
                CameraComboBox().SelectedIndex(0); // Select first camera by default
            }
            else
            {
                 CameraComboBox().PlaceholderText(L"No cameras found");
                 CameraComboBox().IsEnabled(false);
            }
        }
        co_return;
    }


    IAsyncAction MainPage::StartCallAsync()
    {
        // Simulate async operation
        co_await winrt::resume_after(std::chrono::milliseconds(100)); // Simulate delay
        // --- STUB ---
        m_stub_call = std::make_unique<stub_Call>();
        co_return;
    }

     IAsyncAction MainPage::JoinCallAsync()
    {
        // Simulate async operation
        co_await winrt::resume_after(std::chrono::milliseconds(100)); // Simulate delay
        // --- STUB ---
        m_stub_call = std::make_unique<stub_Call>();
        co_return;
    }

    IAsyncAction MainPage::HangupCallAsync()
    {
        // Simulate async operation
        co_await winrt::resume_after(std::chrono::milliseconds(50)); // Simulate delay
        // --- STUB ---
        UnregisterCallEvents();
        m_stub_call = nullptr; // Release the call object
        m_isVideoOn = false;
        m_isMicMuted = false;
        m_isScreenSharing = false;
        m_isBackgroundBlurred = false;
        // Clear video elements (stub)
        // LocalVideo().Source(nullptr);
        // RemoteVideo().Source(nullptr);
        co_return;
    }

     IAsyncAction MainPage::StartVideoAsync()
    {
        co_await winrt::resume_after(std::chrono::milliseconds(50));
        // --- STUB --- Create/Use m_stub_localVideoStream
        if (!m_stub_localVideoStream) {
             // Create based on selected camera (stub)
             m_stub_localVideoStream = std::make_unique<stub_LocalVideoStream>();
        }
        // Call m_stub_call->StartVideoAsync(*m_stub_localVideoStream);
        co_return;
    }

    IAsyncAction MainPage::StopVideoAsync()
    {
        co_await winrt::resume_after(std::chrono::milliseconds(50));
        // --- STUB ---
        if (m_stub_localVideoStream && m_stub_call) {
             // Call m_stub_call->StopVideoAsync(*m_stub_localVideoStream);
        }
        // Maybe release m_stub_localVideoStream here or keep it? Depends on SDK lifetime.
        // m_stub_localVideoStream = nullptr;
        co_return;
    }

     IAsyncAction MainPage::MuteAsync()
    {
        co_await winrt::resume_after(std::chrono::milliseconds(20));
        // --- STUB --- Call m_stub_call->MuteAsync();
        co_return;
    }

    IAsyncAction MainPage::UnmuteAsync()
    {
        co_await winrt::resume_after(std::chrono::milliseconds(20));
        // --- STUB --- Call m_stub_call->UnmuteAsync();
        co_return;
    }

     IAsyncAction MainPage::StartScreenShareAsync()
    {
        co_await winrt::resume_after(std::chrono::milliseconds(50));
        // --- STUB --- Not implemented
        co_return;
    }

    IAsyncAction MainPage::StopScreenShareAsync()
    {
        co_await winrt::resume_after(std::chrono::milliseconds(50));
        // --- STUB --- Not implemented
        co_return;
    }

     IAsyncAction MainPage::StartBackgroundBlurAsync()
    {
        co_await winrt::resume_after(std::chrono::milliseconds(80));
        // --- STUB --- Call m_stub_localVideoStream->StartEffectsAsync()
        co_return;
    }

    IAsyncAction MainPage::StopBackgroundBlurAsync()
    {
        co_await winrt::resume_after(std::chrono::milliseconds(80));
        // --- STUB --- Call m_stub_localVideoStream->StopEffectsAsync()
        co_return;
    }


    void MainPage::UpdateUiState()
    {
        bool inCall = (m_stub_call != nullptr);

        CallButton().IsEnabled(!inCall && m_stub_callAgent != nullptr);
        HangupButton().IsEnabled(inCall);
        MicButton().IsEnabled(inCall);
        ScreenShareButton().IsEnabled(inCall); // Stub - always enabled if in call for now
        VideoButton().IsEnabled(inCall && CameraComboBox().SelectedIndex() >= 0);
        BlurButton().IsEnabled(inCall && m_isVideoOn); // Only enable blur if video is on

        // Reset button labels/states if not in call
        if (!inCall)
        {
            MicButton().Label(L"Mute");
            VideoButton().Label(L"Turn Video On");
            BlurButton().Label(L"Blur Background");
        }
    }

    void MainPage::UpdateStatus(winrt::hstring status)
    {
        // Ensure UI update happens on the UI thread
        DispatcherQueue().TryEnqueue([this, status]() {
            StatusTextBlock().Text(status);
        });
    }

    // --- Event Registration/Callback Stubs ---

    void MainPage::RegisterCallEvents()
    {
        if (!m_stub_call) return;
        // --- STUB ---
        // m_callStateChangedToken = m_stub_call->StateChanged({ get_strong(), &MainPage::OnCallStateChanged });
        // m_remoteParticipantsUpdatedToken = m_stub_call->RemoteParticipantsUpdated({ get_strong(), &MainPage::OnRemoteParticipantsUpdated });
    }

    void MainPage::UnregisterCallEvents()
    {
         if (!m_stub_call) return; // Or check tokens?
        // --- STUB ---
        // m_stub_call->StateChanged(m_callStateChangedToken);
        // m_stub_call->RemoteParticipantsUpdated(m_remoteParticipantsUpdatedToken);
    }

    void MainPage::OnCallStateChanged(IInspectable const&, winrt::hstring const& args)
    {
        // --- STUB ---
        // UpdateStatus(L"Status: Call State - " + args);
        // Handle state changes (e.g., Disconnected -> HangupCallAsync)
    }

    void MainPage::OnRemoteParticipantsUpdated(IInspectable const&, IVectorView<stub_RemoteParticipant> const& args)
    {
        // --- STUB ---
        // UpdateStatus(L"Status: Participants updated.");
        // Handle participant join/leave, update remote video etc.
        // Check args.Size()
        // Iterate through args.GetAt(i)
        // Look for new video streams args.GetAt(i).VideoStreams()
        // Render remote video (stub)
        // RemoteVideo().SetMediaPlayer(...)
    }

     void MainPage::OnIncomingCall(IInspectable const&, stub_IncomingCall const& args)
    {
        // --- STUB ---
        // Show a dialog asking to accept/reject
        // If accept:
        //   Create stub_AcceptCallOptions
        //   auto call = co_await args.AcceptAsync(options);
        //   m_stub_call = std::make_unique<stub_Call>(call);
        //   RegisterCallEvents();
        //   UpdateUiState();
        // If reject:
        //   co_await args.RejectAsync();
    }

} // namespace winrt::CallingCpp::implementation
