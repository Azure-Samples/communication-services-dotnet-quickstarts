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
- Dev-tunnel. download from the following [Dev-tunnel download](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows).

## Two groups. One for Europe, one for NOAM. (PLEASE SEE DOC FOR THESE LINKS)
- NOAM acs resource
- NOAM storage account
- NOAM blob Container
- Europe ACS Resource
- Europe storage Account 
- Europe blob container

## Optionally, if you would like to test on your own resource, you can follow this guide to attach a storage account to your own test resoruce.
- You need an acs resource, and a storage account under the same subscription to do this. 
- https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/call-automation/call-recording/bring-your-own-storage?pivots=programming-language-csharp#pre-requisite-setting-up-managed-identity-and-rbac-role-assignments

## Setup dev tunnel (Only needed for testing the Optional Actions section)
- Run `devtunnel user login` and login with your msft account or `devtunnel user login -g` for github.
- Run `devtunnel create --allow-anonymous`.
- Run `devtunnel port create -p 5000`.
- Run `devtunnel host` to begin hosting. Copy the url similar to `https://9ndqr7mn.usw2.devtunnels.ms:5000` that is returned. This will be the hostingEndpoint variable.

## GA3 features/pathways to test BYOS
- Start BYOS recording with groupcall
- Start BYOS recording with servercall
- Pause BYOS recording and resume 
- Same call multiple BYOS Recordings

## Optional Actions to test (included in guide and sample file)
- play media (audio will not be recorded)
- play media to all (audio will be recorded)
- start regular recording
- download recording
- delete recording
- *inbound pstn call
- *dtmf recognition


## How to test.
1. Run the sample bugbash-test project.
    - from the sample/bugbash-test folder run `dotnet restore`.
    - update the hostingEndpoint and acsConnectionString variables.
    - run `dotnet run` and follow the test instructions in the guide.
2. In the projectFolder/Properties/launchSettings.json update the http.applicationUrl to have port 5000.
3. Update the hosting endpoint with our dev tunnel. example `https://9ndqr7mn.usw2.devtunnels.ms:5000`.
5. From the terminal run `dotnet run` in our project folder".
6. From cmd run "curl http://localhost:5000/test" and ensure you can see test endpoint being written to the console.  

## Start BYOS recording with a groupcall
1. Generate a guid for a group call. https://guidgenerator.com/ and note the guid somewhere.
2. Login with the connection string of your test resrource on this site https://acs-sample-app.azurewebsites.net/ and join the group call with the guid we generated (make sure we unmute)
3. Start a BYOS Group call by running the following from a cmd prompt `curl "http://localhost:5000/startrecordingbyosgroup?call={GUID}&blob={container}"`
4. After the recording begins, wait 5-10 seconds. and either stop the recording via this app, or end the call on the websites UI. 
5. Wait another 5-10 seconds after ending the call, check your storage account and the recording should be there. It will be organzined by `date\callid\{last 8 char of recordingID + Unique guid per recording}`

## Start BYOS recording with a servercall
1. Login with an acs user on this site https://acs-sample-app.azurewebsites.net/ with the connection string of the resource we are testing. 
2. Run the following from a cmd prompt `curl http://localhost:5000/startcall?acstarget=INSERTACSTARGETUSERHERE` using the acs user you created
3. On the ACS Test App, you should see the incoming call. (make sure we unmute)
3. Start a BYOS server call recording by running the following from a cmd prompt `curl "http://localhost:5000/startrecordingbyos?blob={container}"`
4. After the recording begins, wait 5-10 seconds. and either stop the recording via this app, or end the call on the websites UI. 
5. Wait another 5-10 seconds after ending the call, check your storage account and the recording should be there. It will be organzined by `date\callid\{last 8 char of recordingID + Unique guid per recording}`

## How to modify settings for start recording
```c#
app.MapGet("/startrecordingbyosgroup", (
    [FromQuery] string call,
    [FromQuery] string blob
    ) =>
    {
        Console.WriteLine("start recording byos endpoint");
        Console.WriteLine(call);
        Console.WriteLine(blob);

        var callLocator = new GroupCallLocator(call);
        var callRecording = client.GetCallRecording();
        var recordingOptions = new StartRecordingOptions(callLocator)
        {
            RecordingStorage = RecordingStorage.CreateAzureBlobContainerRecordingStorage(new Uri(blob))
        };

        // Example of modifying start recording options. Play arround with the other values we have in these enums and other options.
        recordingOptions.RecordingFormat = RecordingFormat.Wav;
        recordingOptions.RecordingChannel = RecordingChannel.Unmixed;


        var recording = callRecording.Start(recordingOptions);
        recordingId=recording.Value.RecordingId;
        return Results.Ok(recordingId);
    }
);
```

## Pause BYOS recording and resume 
1. Follow either servercall or groupcall byos recording steps, but do not end the call.
2. pause the recording by running the following command `curl http://localhost:5000/pauserecording`
3. resume the recording by running the following command `curl http://localhost:5000/resumerecording`
4. Wait 5-10 seconds. and either stop the recording via this app, or end the call on the websites UI. 
5. Wait another 5-10 seconds after ending the call, check your storage account and the recording should be there. It will be organzined by `date\callid\{last 8 char of recordingID + Unique guid per recording}`

## Same call multiple BYOS Recordings
1. Follow either servercall or groupcall byos recording steps, but do not end the call.
2. stop the recording by running the following command `curl http://localhost:5000/stoprecording`
3. wait 5-10 seconds and start another recording as you did in the previous steps but for the same call (Do not end the call or handup)
4. Wait 5-10 seconds. and either stop the call via this app, or end the call on the websites UI. 
5. Wait another 5-10 seconds after ending the call, check your storage account and the recording should be there. It will be organzined by `date\callid\{last 8 char of recordingID + Unique guid per recording}` in this case, you should see two recording folders under the same callid.

## Create a call to an ACS user 
```c#
app.MapGet("/startcall", (
    [FromQuery] string acsTarget) =>
    {
        Console.WriteLine("start call endpoint");
        Console.WriteLine($"starting a new call to user:{acsTarget}");
        CommunicationUserIdentifier targetUser = new CommunicationUserIdentifier(acsTarget);
        var invite = new CallInvite(targetUser);
        var createCallOptions = new CreateCallOptions(invite, new Uri(hostingEndpoint+ "/callback"));
        var call = client.CreateCall(createCallOptions);
        callConnectionId = call.Value.CallConnection.CallConnectionId;
        return Results.Ok();
    }
);
```
1. Login with an acs user on this site https://acs-sample-app.azurewebsites.net/ with the connection string of the resource we are testing. 
2. To test this, run the following from a cmd prompt `curl http://localhost:5000/startcall?acstarget=INSERTACSTARGETUSERHERE` using the acs user you created
3. On the ACS Test App, you should see the incoming call. 
4. you can hang up the call now. You can keep this tab and user open for upcoming steps.


## Playback media to a specific user 
```c#
app.MapGet("/playmedia", (
    [FromQuery] string acsTarget) =>
    {
        Console.WriteLine("play media endpoint");
        Console.WriteLine($"playing media to user:{acsTarget}");
        var callConnection = client.GetCallConnection(callConnectionId);
        var callMedia = callConnection.GetCallMedia();
        FileSource fileSource = new FileSource(new System.Uri("https://acstestapp1.azurewebsites.net/audio/bot-hold-music-1.wav"));
        CommunicationUserIdentifier targetUser = new CommunicationUserIdentifier(acsTarget);
        var playOptions = new PlayOptions(new List<PlaySource> {fileSource},new List<CommunicationIdentifier> {targetUser});
        playOptions.Loop=true;
        callMedia.Play(playOptions);
        return Results.Ok();
    }
);
```
1. redo the previous step, while the call is still active, call this endpoint with `curl http://localhost:5000/playmedia?acstarget=ACSTARGETUSERHERE`
you should notice audio will start to play from the call.

## Cancel media playback
```c#
app.MapGet("/stopmedia", () =>
    {
        Console.WriteLine("stop media operations endpoint");
        var callConnection = client.GetCallConnection(callConnectionId);
        var callMedia = callConnection.GetCallMedia();
        callMedia.CancelAllMediaOperations();
        return Results.Ok();
    }
);
```
1. while the previous call is still active and playing media, call this endpoint with `curl http://localhost:5000/stopmedia`
you should notice audio will stop playing in the call.

## Create a group call to 2 ACS users 
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
        var createGroupCallOptions = new CreateGroupCallOptions(new List<CommunicationIdentifier> {targetUser, targetUser2}, new Uri(hostingEndpoint+ "/callback"));
        var call =client.CreateGroupCall(createGroupCallOptions);
        callConnectionId = call.Value.CallConnection.CallConnectionId;
        return Results.Ok();
    }
);
```
1. login with an acs user on this site https://acs-sample-app.azurewebsites.net/ with the connection string of the resource we are testing. open a second tab and log in with another user
2. To test this, run the following form a cmd prompt `curl http://localhost:5000/startgroupcall?acstarget=INSERTACSTARGETUSERHERE,INSERTACSTARGETUSE2RHERE` using the acs users you created
3. On the ACS Test App, you should see the incoming call on both tabs. 


## Playback media to all users
```c#
app.MapGet("/playmediatoall", () =>
    {
        Console.WriteLine("play media to all endpoint");
        Console.WriteLine($"playing media to all users");

        var callConnection = client.GetCallConnection(callConnectionId);
        var callMedia = callConnection.GetCallMedia();
        FileSource fileSource = new FileSource(new System.Uri("https://acstestapp1.azurewebsites.net/audio/bot-hold-music-1.wav"));
        var playToAllOptions = new PlayToAllOptions(new List<PlaySource> {fileSource});
        callMedia.PlayToAll(playToAllOptions);
        return Results.Ok();
    }
);
```
1. To test this, run the following form a cmd prompt `curl http://localhost:5000/playmediatoall`


## Start recording
```c#
app.MapGet("/startrecording", () =>
    {
        Console.WriteLine("start recording endpoint");

        var callConnection = client.GetCallConnection(callConnectionId);
        var callLocator = new ServerCallLocator(callConnection.GetCallConnectionProperties().Value.ServerCallId);
        var callRecording = client.GetCallRecording();
        var recordingOptions = new StartRecordingOptions(callLocator);
        var recording = callRecording.Start(recordingOptions);
        recordingId=recording.Value.RecordingId;
        return Results.Ok();
    }
);
```
1. To test this, after you have started a call, run the following form a cmd prompt `curl http://localhost:5000/startrecording`


## stop recording
```c#
app.MapGet("/stoprecording", () =>
    {
        Console.WriteLine("stop recording endpoint");
        var callRecording = client.GetCallRecording();
        callRecording.Stop(recordingId);
        return Results.Ok();
    }
);
```
1. To test this, after you have started a recording, run the following form a cmd prompt `curl http://localhost:5000/stoprecording`

## handle file status updated event (get notified when call recording file is ready for download)
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
1. First we need to register an event handler with our acs resource. 
    - go to your acs resource in portal https://portal.azure.com/signin/index/
    - click on events from the left side bar
    - click + event subscription to create a new subscription
    - enter name "filestatus"
    - select recording file status updated as the event to filter
    - add a system topic name, testevent for example
    - under endpoint, select webhook and enter the hostingEndpoint/filestatus as the endpoint. 
    - make sure when we register this, our app is running as the subscription validation handshake is required. 

3. Now that we have completed the setup, we can stop a recording, or end a call and we will get this filestatus updated event. 

4. after we get this, we are setting the content location and delete location for testing with out other endpoints. 

## Download recording
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
2. to delete the file, you only need to call `curl http://localhost:5000/delete`

## **Inbound pstn call
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
                var callbackUri = new Uri(hostingEndpoint+ "/callback");
                AnswerCallResult answerCallResult = await client.AnswerCallAsync(incomingCallContext, callbackUri);
                callConnectionId = answerCallResult.CallConnectionProperties.CallConnectionId;
            }
        }
    }
    return Results.Ok();
});
```
1. First we need to register an event handler with our acs resource. 
    - go to your acs resource in portal https://portal.azure.com/signin/index/
    - click on events from the left side bar
    - click event subscription to create a new subscription
    - enter name "call"
    - select incoming call as the event to filter
    - under endpoint, seelct webhook and enter the hostingEndpoint/incomingcall as the endpoint. 
    - make sure when we register this, our app is running as the subscription validation handshake is required. 



## **Dtmf recogntion
```c#
app.MapGet("/recognize", async () =>
    {
        string pstnNumber = "+11231231234";
        Console.WriteLine("recognize endpoint");

        var callConnection = client.GetCallConnection(callConnectionId);
        var callMedia = callConnection.GetCallMedia();
        callConnection.GetParticipants();
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

# Api view
- https://apiview.dev/Assemblies/Review/e6e07bd411d646d5a6b0fe5b6760c976