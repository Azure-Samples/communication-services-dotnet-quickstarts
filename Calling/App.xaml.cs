using Azure.Communication.Calling.WindowsClient;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.WindowsAzure.Messaging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Store;
using Windows.Networking.PushNotifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using static System.Collections.Specialized.BitVector32;

namespace CallingQuickstart
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        public Uri PNHChannelUri { get; set; }

        private string AZURE_PNH_HUB_NAME = "<AZURE_PNH_HUB_NAME>";
        private string AZURE_PNH_HUB_CONNECTION_STRING = "<AZURE_PNH_HUB_CONNECTION_STRING>";

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        // Handle protocol activations.
        /// </summary>
        protected override async void OnActivated(IActivatedEventArgs e)
        {
            if (e.Kind == ActivationKind.Protocol || e is ToastNotificationActivatedEventArgs)
            {
                // Ensure the current window is active
                Window.Current.Activate();

                // Handle notification activation
                if (e is ToastNotificationActivatedEventArgs toastActivationArgs)
                {
                    ToastArguments args = ToastArguments.Parse(toastActivationArgs.Argument);
                    string action = args?.Get("action");

                    if (!string.IsNullOrEmpty(action))
                    {
                        var frame = Window.Current.Content as Frame;
                        if (frame.Content is MainPage)
                        {
                            var mainPage = frame.Content as MainPage;
                            await mainPage.AnswerIncomingCall(action);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            await InitNotificationsAsync();

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        // Follow https://learn.microsoft.com/en-us/azure/notification-hubs/notification-hubs-windows-store-dotnet-get-started-wns-push-notification
        // to setup and obtain AZURE_PNH_HUB_NAME and AZURE_PNH_HUB_CONNECTION_STRING
        private async Task InitNotificationsAsync()
        {
            if (AZURE_PNH_HUB_NAME != "<AZURE_PNH_HUB_NAME>" && AZURE_PNH_HUB_CONNECTION_STRING != "<AZURE_PNH_HUB_CONNECTION_STRING>")
            {
                var channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                channel.PushNotificationReceived += Channel_PushNotificationReceived;

                var hub = new NotificationHub(AZURE_PNH_HUB_NAME, AZURE_PNH_HUB_CONNECTION_STRING);
                var result = await hub.RegisterNativeAsync(channel.Uri);

                if (result.ChannelUri != null)
                {
                    PNHChannelUri = new Uri(result.ChannelUri);
                }
                else
                {
                    Debug.WriteLine("Cannot register WNS channel");
                }
            }
        }

        private async void Channel_PushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
        {
            args.Cancel = true;

            switch (args.NotificationType)
            {
                case PushNotificationType.Toast:
                case PushNotificationType.Tile:
                case PushNotificationType.TileFlyout:
                case PushNotificationType.Badge:
                    break;
                case PushNotificationType.Raw:
                    var frame = Window.Current.Content as Frame;
                    if (frame.Content is MainPage)
                    {
                        var mainPage = frame.Content as MainPage;
                        await mainPage.HandlePushNotificationIncomingCallAsync(args.RawNotification.Content);
                    }
                    break;
            }
        }

        public void ShowIncomingCallNotification(IncomingCall incomingCall)
        {
            string incomingCallType = incomingCall.IsVideoEnabled ? "Video" : "Audio";
            string caller = incomingCall.CallerDetails.DisplayName != "" ? incomingCall.CallerDetails.DisplayName : incomingCall.CallerDetails.Identifier.RawId;
            new ToastContentBuilder()
            .SetToastScenario(ToastScenario.IncomingCall)
            .AddText(caller + " is calling you.")
            .AddText("New Incoming " + incomingCallType + " Call")
                .AddButton(new ToastButton()
                    .SetContent("Decline")
                    .AddArgument("action", "decline"))
                .AddButton(new ToastButton()
                    .SetContent("Accept")
                    .AddArgument("action", "accept"))
                .Show();
        }
    }
}