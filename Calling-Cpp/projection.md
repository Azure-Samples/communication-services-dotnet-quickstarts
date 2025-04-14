# C# to C++/WinRT Projection Mapping (Azure Communication Services Calling SDK)

This document outlines the necessary classes and concepts from the C# Calling SDK used in the `CallingQuickstart` sample and their anticipated equivalents in a future C++/WinRT projection. Stubs are currently used in the `Calling-Cpp` project.

| C# Class/Concept (Azure.Communication.Calling) | C++/WinRT Stub (`stub_calling_sdk.h`) | Notes |
|---|---|---|
| `CommunicationIdentifier` (base) | `stub_CommunicationIdentifier` | Base class for various identifiers. |
| `CommunicationUserIdentifier` | `stub_CommunicationUserIdentifier` | Represents an ACS user. |
| `PhoneNumberIdentifier` | `stub_PhoneNumberIdentifier` | Represents a PSTN phone number. |
| `MicrosoftTeamsUserIdentifier` | `stub_MicrosoftTeamsUserIdentifier` | Represents a Teams user. |
| `UnknownIdentifier` | `stub_UnknownIdentifier` | Represents an unknown identifier type. |
| `CommunicationTokenCredential` (Azure.Core) | `stub_CommunicationTokenCredential` | Used for authentication. Likely from a core C++ library. |
| `CallClient` | `stub_CallClient` | Entry point for creating agents and accessing device manager. |
| `CallAgent` | `stub_CallAgent` | Represents the local endpoint, manages calls and devices. |
| `DeviceManager` | `stub_DeviceManager` | Enumerates and manages media devices (cameras, mics, speakers). |
| `VideoDeviceInfo` | `stub_VideoDeviceInfo` | Represents a camera device. |
| `Call` | `stub_Call` | Represents an active call session. |
| `IncomingCall` | `stub_IncomingCall` | Represents an incoming call notification. |
| `RemoteParticipant` | `stub_RemoteParticipant` | Represents a participant in the call. |
| `LocalVideoStream` | `stub_LocalVideoStream` | Represents the local camera's video stream. |
| `RemoteVideoStream` | `stub_RemoteVideoStream` | Represents a remote participant's video stream. |
| `VideoStreamRenderer` | `stub_VideoStreamRenderer` | Renders video streams to UI elements. |
| `VideoStreamRendererView` | `stub_VideoStreamRendererView` | The view created by the renderer. |
| `StartCallOptions` | `stub_StartCallOptions` | Options for starting a call. |
| `JoinCallOptions` | `stub_JoinCallOptions` | Options for joining a call (e.g., meeting link, ID/passcode). |
| `AcceptCallOptions` | `stub_AcceptCallOptions` | Options for accepting an incoming call. |
| `HangUpOptions` | `stub_HangUpOptions` | Options for hanging up a call. |
| `VideoEffects` (Property/Methods on `LocalVideoStream`) | `StartEffectsAsync`, `StopEffectsAsync` methods on `stub_LocalVideoStream` | Functionality to apply video effects like background blur. |
| `PushNotificationInfo` | `stub_PushNotificationInfo` | Information needed for push notification registration (likely part of `CallAgent` methods). |

**Note:** The exact C++/WinRT class names, namespaces, method signatures, and parameter types may differ in the official SDK projection. This mapping is based on the C# SDK's structure and the functionality used in the sample. Async operations in C# (`Task<T>`, `Task`) are mapped to C++/WinRT's `IAsyncOperation<T>` and `IAsyncAction`. Events are mapped to standard C++/WinRT events (`event_token`, `event`).
