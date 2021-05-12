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
            CommunicationTokenCredential token_credential = new CommunicationTokenCredential("eyJhbGciOiJSUzI1NiIsImtpZCI6IjEwMiIsIng1dCI6IjNNSnZRYzhrWVNLd1hqbEIySmx6NTRQVzNBYyIsInR5cCI6IkpXVCJ9.eyJza3lwZWlkIjoiYWNzOjAyNjY1YzU2LTI3N2UtNGM1OS1iYWI0LWM0NzVjYWEzZWU4MF8wMDAwMDAwYS0wMjYzLTI5ZWMtNTcwYy0xMTNhMGQwMDZiNDciLCJzY3AiOjE3OTIsImNzaSI6IjE2MjA4NTUzMjciLCJpYXQiOjE2MjA4NTUzMjcsImV4cCI6MTYyMDk0MTcyNywiYWNzU2NvcGUiOiJ2b2lwIiwicmVzb3VyY2VJZCI6IjAyNjY1YzU2LTI3N2UtNGM1OS1iYWI0LWM0NzVjYWEzZWU4MCJ9.uAcnm1LvcjllX0VRXpf7zzfKKQzG7w4DYkvTrljbn9PmeiCFzYkV38U_yEP_TlEYo9ESJhkRi7kNAnI_xh0Dq26lERzUcUHvspjHmXRULvMffd3aDGcP5HfrKlhRvhsfVuN_r730R7z1DSOzhL0Nw8x3yymx12EW7nPJ-d0HAJWiSum0ededBKt5FkBFdflbu1E9B-89_43Eh6z_kl6urWNg6MCrXiGI4kyLbeRcEn5oMfGu6goWijH1-z2E_IS6G3WrSgkRtxdISVIkfMlnZpKhUGadNIN8nPH3amLO_YQfXcMn8RYLCjVyZpVjpdomb10uVqWTlck0h_E0h25KLw ");
            call_client_ = new CallClient();

            CallAgentOptions callAgentOptions = new CallAgentOptions()
            {
                DisplayName = "Xu Mo"
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