---
page_type: sample
languages:
- c#
products:
- azure
- azure-communication-services
---

# Call Automation Bugbash
This guide walks through simple call automation scenarios and endpoints.

## Prerequisites
- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F).
- An active Communication Services resource. [Create a Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource).
- Dotnet 7 or 6 SDK. [Download Dotnet](https://dotnet.microsoft.com/en-us/download/dotnet). (dotnet --list-sdks)
- VScode. [Download VScode](https://code.visualstudio.com/).
- ngrok
## Setup empty project
1. Create a folder for our project
2. Run `dotnet new web --language c# --name bugbash-testing` in the folder we created to initialize the project.
3. In the new project folder that was created in step 1 Run `dotnet add package Azure.Communication.CallAutomation -v 1.0.0-alpha.20230526.8`. 
4. Run `dotnet add package Azure.Messaging.EventGrid` in the new project folder we created to install the event grid package 
5. Run `dotnet restore` to ensure we can build the required packages
    - If you have issues building the correct package, create a new nuget.config file in the project directory and add the following to it
    - Run `dotnet restore` and it should now work

        ```
        <?xml version="1.0" encoding="utf-8"?>
        <configuration>
        <packageSources>
            <clear />
            <add key="azure-sdk-for-net" value="https://pkgs.dev.azure.com/azure-sdk/public/_packaging/azure-sdk-for-net/nuget/v3/index.json" />
        </packageSources>
        </configuration>
        ```
## NOTE after every code change make sure you end the server and restart it. 

## Setup variables and imports that will be reused later on in this sample in the program.cs file
Add the following snippets under. This will be used to recieve incoming events and the conenction string will link this with the acs resource we created.

```c#
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Azure.Communication.CallAutomation;
using Azure.Communication;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Azure.Messaging;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

const string ngrokEndpoint = ""; //ngrok endpoint for our service
const string cstring = ""; // Input your connection string here
var client = new CallAutomationClient(connectionString: cstring);
var eventProcessor = client.GetEventProcessor(); //This will be used for the event processor later on
string callConnectionId = "";
string recordingId = "";
string contentLocation = "";
string deleteLocation = "";


app.Run();
```

## Test endpoint to make sure we are okay
1. insert the following code snippets above `app.Run()`, and add your connection string from your acs resource
```c#
app.MapGet("/test", ()=>
    {
        Console.WriteLine("test endpoint");
    }
);
```
2. In the projectFolder/Properties/launchSettings.json update the http.applicationUrl to have port 5000
3. Start ngrok on port 5000. From where ngrok is dowloaded, using the terminal run `./ngrok http 5000`
4. update the ngrok endpoints. example `https://9253-2001-569-5146-9600-755d-996a-d84c-8dbf.ngrok.io`
5. from the terminal run `dotnet run` in our project folder"
6. from cmd run "curl http://localhost:5000/test" and ensure you can see test endpoint being written to the console.  


## This can be used as a dummy callback URL for testing or the eventproccesor 
1. insert the following code snippets above `app.Run()`, rerun the server, and end existing calls. 

```c#
app.MapPost("/callback", (
    [FromBody] CloudEvent[] cloudEvents) =>
{
    eventProcessor.ProcessEvents(cloudEvents);
    return Results.Ok();
});
```


## Create a call to an ACS user 
1. insert the following code snippets above `app.Run()`, rerun the server, and end existing calls. 
```c#
app.MapGet("/startcall", (
    [FromQuery] string acsTarget) =>
    {
        Console.WriteLine("start call endpoint");
        Console.WriteLine($"starting a new call to user:{acsTarget}");
        CommunicationUserIdentifier targetUser = new CommunicationUserIdentifier(acsTarget);
        var invite = new CallInvite(targetUser);
        var createCallOptions = new CreateCallOptions(invite, new Uri(ngrokEndpoint+ "/callback"));
        var call = client.CreateCall(createCallOptions);
        callConnectionId = call.Value.CallConnection.CallConnectionId;
        return Results.Ok();
    }
);
```
2. login with an acs user on this site https://acs-sample-app.azurewebsites.net/ with the connection string of the resource we are testing. 
3. To test this, run the following form a cmd prompt `curl http://localhost:5000/startcall?acstarget=INSERTACSTARGETUSERHERE` using the acs user you created
4. On the ACS Test App, you should see the incoming call. 
5. you can hang up the call now. You can keep this tab and user open for upcoming steps.


## Playback media to a specific user 
1. insert the following code snippets above `app.Run()`, rerun the server, and end existing calls. 
```c#
app.MapGet("/playmedia", (
    [FromQuery] string acsTarget) =>
    {
        Console.WriteLine("play media endpoint");
        Console.WriteLine($"playing media to user:{acsTarget}");
        var callConenction = client.GetCallConnection(callConnectionId);
        var callMedia = callConenction.GetCallMedia();
        FileSource fileSource = new FileSource(new System.Uri("https://acstestapp1.azurewebsites.net/audio/bot-hold-music-1.wav"));
        CommunicationUserIdentifier targetUser = new CommunicationUserIdentifier(acsTarget);
        var playOptions = new PlayOptions(new List<PlaySource> {fileSource},new List<CommunicationIdentifier> {targetUser});
        playOptions.Loop=true;
        callMedia.Play(playOptions);
        return Results.Ok();
    }
);
```
2. redo the previus step, while the call is still active, call this endpoint with `curl http://localhost:5000/playmedia?acstarget=ACSTARGETUSERHERE`
you should notice audio will start to play from the call.


## Cancel media playback
1. insert the following code snippets above `app.Run()`, rerun the server, and end existing calls. 
```c#
app.MapGet("/stopmedia", () =>
    {
        Console.WriteLine("stop media operations endpoint");
        var callConenction = client.GetCallConnection(callConnectionId);
        var callMedia = callConenction.GetCallMedia();
        callMedia.CancelAllMediaOperations();
        return Results.Ok();
    }
);
```
2. while the previous call is still active and playing media, call this endpoint with `curl http://localhost:5000/stopmedia`
you should notice audio will stop playing in the call.


## Create a group call to 2 ACS users 
1. insert the following code snippets above `app.Run()`, rerun the server, and end existing calls. 
```c#
app.MapGet("/startgroupcall", (
    [FromQuery] string acsTarget) =>
    {
        Console.WriteLine("start group call endpoint");
        List<string> targets = acsTarget.Split(',').ToList();
        Console.WriteLine($"starting a new group call to user:{targets[0]} and user:{targets[1]}");
        CommunicationUserIdentifier targetUser = new CommunicationUserIdentifier(targets[0]);
        CommunicationUserIdentifier targetUser2 = new CommunicationUserIdentifier(targets[1]);

        var invite = new CallInvite(targetUser);
        var createGroupCallOptions = new CreateGroupCallOptions(new List<CommunicationIdentifier> {targetUser, targetUser2}, new Uri(ngrokEndpoint+ "/callback"));
        var call =client.CreateGroupCall(createGroupCallOptions);
        callConnectionId = call.Value.CallConnection.CallConnectionId;
        return Results.Ok();
    }
);
```
2. login with an acs user on this site https://acs-sample-app.azurewebsites.net/ with the connection string of the resource we are testing. open a second tab and log in with another user
3. To test this, run the following form a cmd prompt `curl http://localhost:5000/startgroupcall?acstarget=INSERTACSTARGETUSERHERE,INSERTACSTARGETUSE2RHERE` using the acs users you created
4. On the ACS Test App, you should see the incoming call on both tabs. 


## Playback media to all users
1. insert the following code snippets above `app.Run()`, rerun the server, and end existing calls. 
```c#
app.MapGet("/playmediatoall", () =>
    {
        Console.WriteLine("play media to all endpoint");
        Console.WriteLine($"playing media to all users");

        var callConenction = client.GetCallConnection(callConnectionId);
        var callMedia = callConenction.GetCallMedia();
        FileSource fileSource = new FileSource(new System.Uri("https://acstestapp1.azurewebsites.net/audio/bot-hold-music-1.wav"));
        var playToAllOptions = new PlayToAllOptions(new List<PlaySource> {fileSource});
        callMedia.PlayToAll(playToAllOptions);
        return Results.Ok();
    }
);
```
2. To test this, run the following form a cmd prompt `curl http://localhost:5000/playmediatoall`


## Start recording
1. insert the following code snippets above `app.Run()`, rerun the server, and end existing calls. 
```c#
app.MapGet("/startrecording", () =>
    {
        Console.WriteLine("start recording endpoint");

        var callConenction = client.GetCallConnection(callConnectionId);
        var callLocator = new ServerCallLocator(callConenction.GetCallConnectionProperties().Value.ServerCallId);
        var callRecording = client.GetCallRecording();
        var recordingOptions = new StartRecordingOptions(callLocator);
        var recording = callRecording.Start(recordingOptions);
        recordingId=recording.Value.RecordingId;
        return Results.Ok();
    }
);
```
2. To test this, after you have started a call, run the following form a cmd prompt `curl http://localhost:5000/startrecording`


## stop recording
1. insert the following code snippets above `app.Run()`, rerun the server, and end existing calls. 
```c#
app.MapGet("/stoprecording", () =>
    {
        Console.WriteLine("stop recording endpoint");

        var callConenction = client.GetCallConnection(callConnectionId);
        var callLocator = new ServerCallLocator(callConenction.GetCallConnectionProperties().Value.ServerCallId);
        var callRecording = client.GetCallRecording();
        var recordingOptions = new StartRecordingOptions(callLocator);
        callRecording.Stop(recordingId);
        return Results.Ok();
    }
);
```
2. To test this, after you have started a recording, run the following form a cmd prompt `curl http://localhost:5000/stoprecording`

## handle file status updated event (get notified when call recording file is ready for download)
1. insert the following code snippets above `app.Run()`, rerun the server, and end existing calls. 
```c#
app.MapPost("/filestatus", ([FromBody] EventGridEvent[] eventGridEvents) =>
{
    Console.WriteLine("filestatus endpoint");
    foreach (var eventGridEvent in eventGridEvents)
    {
        if (eventGridEvent.TryGetSystemEventData(out object eventData))
        {
            // Handle the webhook subscription validation event.
            if (eventData is Azure.Messaging.EventGrid.SystemEvents.SubscriptionValidationEventData subscriptionValidationEventData)
            {
                var responseData = new Azure.Messaging.EventGrid.SystemEvents.SubscriptionValidationResponse
                {
                    ValidationResponse = subscriptionValidationEventData.ValidationCode
                };
                return Results.Ok(responseData);
            }

            if (eventData is Azure.Messaging.EventGrid.SystemEvents.AcsRecordingFileStatusUpdatedEventData statusUpdated)
            {
                contentLocation = statusUpdated.RecordingStorageInfo.RecordingChunks[0].ContentLocation;
                deleteLocation = statusUpdated.RecordingStorageInfo.RecordingChunks[0].DeleteLocation;
                Console.WriteLine(contentLocation);
                Console.WriteLine(deleteLocation);
            }
        }
    }
    return Results.Ok();
});
```
2. First we need to register an event handler with our acs resource. 
    - go to your acs resource in portal https://portal.azure.com/signin/index/
    - click on events from the left side bar
    - click + event subscription to create a new subscription
    - enter name "filestatus"
    - select recording file status updated as the event to filter
    - add a system topic name, testevent for example
    - under endpoint, select webhook and enter the ngrokurl/filestatus as the endpoint. 
    - make sure when we register this, our app is running as the subscription validation handshake is required. 

3. Now that we have completed the setup, we can stop a recording, or end a call and we will get this filestatus updated event. 

4. after we get this, we are setting the content location and delete location for testing with out other endpoints. 




## Download recording
1. insert the following code snippets above `app.Run()`, rerun the server, and end existing calls. 
```c#
app.MapGet("/download", () =>
    {
        Console.WriteLine("download recording endpoint");

        var callRecording = client.GetCallRecording();
        callRecording.DownloadTo(new Uri(contentLocation),"testfile.wav");
        return Results.Ok();
    }
);
```
1. the previous endpoint has been setup so after we get the filestatus updated event, we update the content location. 
2. to download the file, you only need to call `curl http://localhost:5000/download`


## Delete recording
1. insert the following code snippets above `app.Run()`
```c#
app.MapGet("/delete", () =>
    {
        Console.WriteLine("delete recording endpoint");

        var callRecording = client.GetCallRecording();
        callRecording.Delete(new Uri(deleteLocation));
        return Results.Ok();
    }
);
```
1. the previous endpoint has been setup so after we get the filestatus updated event, we update the delete location. 
2. to download the file, you only need to call `curl http://localhost:5000/delete`



## **inbound pstn call
1. insert the following code snippets above `app.Run()`, rerun the server, and end existing calls. 
```c#
app.MapPost("/incomingcall", async (
    [FromBody] Azure.Messaging.EventGrid.EventGridEvent[] eventGridEvents) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        if (eventGridEvent.TryGetSystemEventData(out object eventData))
        {
            // Handle the webhook subscription validation event.
            if (eventData is Azure.Messaging.EventGrid.SystemEvents.SubscriptionValidationEventData subscriptionValidationEventData)
            {
                var responseData = new Azure.Messaging.EventGrid.SystemEvents.SubscriptionValidationResponse
                {
                    ValidationResponse = subscriptionValidationEventData.ValidationCode
                };
                return Results.Ok(responseData);
            }
            else if (eventData is Azure.Messaging.EventGrid.SystemEvents.AcsIncomingCallEventData acsIncomingCallEventData)
            {
                var incomingCallContext = acsIncomingCallEventData.IncomingCallContext;
                var callbackUri = new Uri(ngrokEndpoint+ "/callback");
                AnswerCallResult answerCallResult = await client.AnswerCallAsync(incomingCallContext, callbackUri);
                callConnectionId = answerCallResult.CallConnectionProperties.CallConnectionId;
            }
        }
    }
    return Results.Ok();
});
```
2. First we need to register an event handler with our acs resource. 
    - go to your acs resource in portal https://portal.azure.com/signin/index/
    - click on events from the left side bar
    - click event subscription to create a new subscription
    - enter name "call"
    - select incoming call as the event to filter
    - under endpoint, seelct webhook and enter the ngrokurl/incomingcall as the endpoint. 
    - make sure when we register this, our app is running as the subscription validation handshake is required. 



## **Dtmf recogntion
1. insert the following code snippets above `app.Run()`, rerun the server, and end existing calls. 
```c#
app.MapGet("/recognize", async () =>
    {
        string pstnNumber = "+11231231234";
        Console.WriteLine("play media to all endpoint");
        Console.WriteLine($"playing media to all users");

        var callConenction = client.GetCallConnection(callConnectionId);
        var callMedia = callConenction.GetCallMedia();
        callConenction.GetParticipants();
        CallMediaRecognizeOptions dmtfRecognizeOptions = new CallMediaRecognizeDtmfOptions(new PhoneNumberIdentifier(pstnNumber), maxTonesToCollect: 3)
        {
            InterruptCallMediaOperation = true,
            InterToneTimeout = TimeSpan.FromSeconds(10),
            StopTones = new DtmfTone[] { DtmfTone.Pound },
            InitialSilenceTimeout = TimeSpan.FromSeconds(5),
            InterruptPrompt = true,
            Prompt = new FileSource(new Uri("https://acstestapp1.azurewebsites.net/audio/bot-hold-music-1.wav"))
        };

        var tone  = await callMedia.StartRecognizingAsync(dmtfRecognizeOptions);

        var results = await tone.Value.WaitForEventProcessorAsync();

        //here we write out what the user has entered into the phone. 
        if(results.IsSuccess)
        {
            Console.WriteLine(((DtmfResult)results.SuccessResult.RecognizeResult).ConvertToString());
        }

        return Results.Ok();
    }
);
```
1. once an inbound pstn call has been established, run `curl http://localhost:5000/recognize`. and ensure you have prepopulated the pstnNumber variable with the calling number.  
2. you will now hear a song play (in a real case this would be an audio file containing options)
3. you can enter 1-3 digits, and hit pound. This server will now print the options you chose to the console. 


## Additional things to test
- TODO
