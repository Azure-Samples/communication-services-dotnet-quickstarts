# Unmixed Audio Recording - Sentiment Analysis Sample

## Overview
This sample demonstrates a sample end-to-end flow using Azure Communication Services Call Automation SDK 
to answer an incoming call, start recording the unmixed audio, split the channels into separate file
and perform some basic sentiment analysis on the result using Azure Cognitive Services.

## Prerequisites
- [.NET 7](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
- [Azure Communication Services resource](https://azure.microsoft.com/en-gb/products/communication-services/)
- [Azure Speech resource](https://azure.microsoft.com/en-us/products/cognitive-services/speech-services/)
- [Azure Cognitive Services resource](https://azure.microsoft.com/en-us/products/cognitive-services/)
- [Ngrok](https://ngrok.com/download)
- [FFMPeg](https://ffmpeg.org/download.html)

## How to run locally
1. Start Ngrok using `ngrok http 5000`
1. Replace the following strings with your own values:
  - ACS_CONNECTION_STRING your ACS connection string
  - NGROK_URI your ngrok Uri to expose
  - WELCOME_WAV_FILE_URI an audio wav file to play when call is connected
  - COGNITIVE_SERVICE_URI endpoint uri of your cognitive services
  - COGNITIVE_SERVICE_KEY secret of your cognitive services
  - SPEECH_KEY key of your speech services
  - SPEECH_REGION region of your speech services
1. Start your service
1. Setup the following Event Grid subscriptions for your ACS resource in the Azure Portal
  - Incoming Call with Webhook Uri `<NGROK_URI>/api/incomingCall`
  - Recording File Status Updated with Webhook Uri `<NGROK_URI>/api/recordingDone`
1. Run the following ffmpeg commands:
  - Find number of channels: `ffprobe -v error -show_entries stream=channels,channel_layout -of default=nw=1 unmixed_recording.wav`
  - Split into 2 mono files: `ffmpeg -i unmixed_recording.wav -filter_complex "[0:a]channelsplit=channel_layout=stereo[left][right]" -map "[left]" channel0.wav -map "[right]" channel1.wav`
1. Get the sentiment analysis by opening following URI in a browser: https://localhost:5001/api/sentimentAnalysis?filePath=channel0.wav