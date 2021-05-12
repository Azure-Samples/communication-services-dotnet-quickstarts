using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Azure.Communication;
using Azure.Communication.Calling;

namespace CallingQuickstart
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void CallButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            CommunicationTokenCredential token_credential = new CommunicationTokenCredential("<USER_ACCESS_TOKEN>");
            call_client_ = new CallClient();

            CallAgentOptions callAgentOptions = new CallAgentOptions()
            {
                DisplayName = "<YOUR_DISPLAY_NAME>"
            };
            call_agent_ = await call_client_.CreateCallAgent(token_credential, callAgentOptions);

            StartCallOptions startCallOptions = new StartCallOptions();
            ICommunicationIdentifier[] callees = new ICommunicationIdentifier[1]
            {
                new CommunicationUserIdentifier(CalleeTextBox.Text)
            };

            call_ = await call_agent_.StartCallAsync(callees, startCallOptions);
        }

        private async void HangupButton_Click(object sender, RoutedEventArgs e)
        {
            await call_.HangUp(new HangUpOptions());
        }

        CallClient call_client_;
        CallAgent call_agent_;
        Call call_;
    }
}
