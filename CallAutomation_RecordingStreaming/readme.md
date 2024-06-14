|page_type|languages|products
|---|---|---|
|sample|<table><tr><td>csharp</tr></td></table>|<table><tr><td>azure</td><td>azure-communication-services</td><td>azure-cognitive-services</td></tr></table>|

# Unmixed Audio Recording - Sentiment Analysis Sample

## Overview
This sample demonstrates a sample end-to-end flow using Azure Communication Services Call Automation SDK 
to answer an incoming call, start recording the unmixed audio, split the channels into separate file
and perform some basic sentiment analysis on the result using Azure Cognitive Services.

## Flow
![Flow](./images/unmixed_demo.png)

## Prerequisites
- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/)
- [Visual Studio (2022 17.4)](https://visualstudio.microsoft.com/vs/) and above
- Enable (Visual Studio Web Tunnels) option in Visual Studio [https://devblogs.microsoft.com/visualstudio/public-preview-of-dev-tunnels-in-visual-studio-for-asp-net-core-projects/]
- [.NET 7](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) and above
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You'll need to record your resource connection string for this sample.
- Create an [Azure Cognitive Services resource](https://azure.microsoft.com/en-us/products/cognitive-services/)
- Create an [Azure Speech resource](https://azure.microsoft.com/en-us/products/cognitive-services/speech-services/)
- [FFMPeg](https://ffmpeg.org/download.html)

## Before running the sample for the first time
1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. git clone https://github.com/williamzhao87/Communication-Services-dotnet-quickstarts.git.

## How to run locally
1. Go to CallAutomation_UnmixedSentimentAnalysis folder and open `CallAutomation_UnmixedSentimentAnalysis.sln` solution in Visual Studio.
2. Replace the following strings with your own values:
  - ACS_CONNECTION_STRING your ACS connection string
  - COGNITIVE_SERVICE_URI endpoint uri of your cognitive services
  - COGNITIVE_SERVICE_KEY secret of your cognitive services
  - SPEECH_KEY key of your speech services
  - SPEECH_REGION region of your speech services
3. Start your service and copy down the CALLBACK URI which will be printed out to the console log (this is using Visual Studio Web Tunnel)
4. Setup the following [Event Grid subscriptions](https://learn.microsoft.com/en-us/azure/event-grid/event-schema-communication-services) for your ACS resource in the Azure Portal
  - Incoming Call with Webhook Uri `<CALLBACK URI>/api/incomingCall`
  - Recording File Status Updated with Webhook Uri `<CALLBACK URI>/api/recordingDone`
5. Run the following ffmpeg commands:
  - Find number of channels: `ffprobe -v error -show_entries stream=channels,channel_layout -of default=nw=1 unmixed_recording.wav`
  - Split into 2 mono files: `ffmpeg -i unmixed_recording.wav -filter_complex "[0:a]channelsplit=channel_layout=stereo[left][right]" -map "[left]" channel0.wav -map "[right]" channel1.wav`
6. Get the sentiment analysis by opening following URI in a browser: https://localhost:5001/api/sentimentAnalysis?filePath=channel0.wav
7. (Optional) Get the text transcript by opening following URI in a browser: https://localhost:5001/api/speechToText?filePath=channel0.wav