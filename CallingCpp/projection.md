# C# to C++/WinRT Projection Mapping (Azure Communication Services Calling SDK)

This document outlines the necessary classes and concepts from the C# Calling SDK used for basic **voice calls** and their anticipated equivalents in a future C++/WinRT projection. Stubs are currently used in the `Calling-Cpp` project.

| C# Class/Concept (Azure.Communication.Calling) | C++/WinRT Stub (`stub_calling_sdk.h`) | Notes |
|---|---|---|
| `CommunicationIdentifier` (base) | `stub_CommunicationIdentifier` | Base class for various identifiers. |
| `CommunicationUserIdentifier` | `stub_CommunicationUserIdentifier` | Represents an ACS user. |
| `PhoneNumberIdentifier` | `stub_PhoneNumberIdentifier` | Represents a PSTN phone number. |
| `MicrosoftTeamsUserIdentifier` | `stub_MicrosoftTeamsUserIdentifier` | Represents a Teams user (relevant for joining Teams meetings). |
| `UnknownIdentifier` | `stub_UnknownIdentifier` | Represents an unknown identifier type. |
| `CommunicationTokenCredential` (Azure.Core) | `stub_CommunicationTokenCredential` | Used for authentication. Likely from a core C++ library. |
| `CallClient` | `stub_CallClient` | Entry point for creating agents and accessing device manager. |
| `CallAgent` | `stub_CallAgent` | Represents the local endpoint, manages calls and devices. Handles `IncomingCall` events. |
| `DeviceManager` | `stub_DeviceManager` | Enumerates and manages media devices (microphones, speakers). |
| `AudioDeviceInfo` | `stub_AudioDeviceInfo` | Represents an audio device (microphone or speaker). *(Added)* |
| `Call` | `stub_Call` | Represents an active call session. Provides methods like `MuteAsync`, `UnmuteAsync`, `HangUpAsync`. Handles `StateChanged` and `RemoteParticipantsUpdated` events. |
| `IncomingCall` | `stub_IncomingCall` | Represents an incoming call notification. Provides `AcceptAsync`. |
| `RemoteParticipant` | `stub_RemoteParticipant` | Represents a participant in the call. |
| `LocalOutgoingAudioStream` | `stub_LocalOutgoingAudioStream` | Represents the local microphone's audio stream. *(Added)* |
| `StartCallOptions` | `stub_StartCallOptions` | Options for starting an outgoing call (e.g., target identifiers, audio options). |
| `JoinCallOptions` | `stub_JoinCallOptions` | Options for joining a call (e.g., meeting link, group ID, audio options). |
| `AcceptCallOptions` | `stub_AcceptCallOptions` | Options for accepting an incoming call (e.g., audio options). |
| `HangUpOptions` | `stub_HangUpOptions` | Options for hanging up a call. |
| `CallState` (Enum) | `stub_CallState` (Enum) | Represents the different states of a call (e.g., Connecting, Connected, Disconnected). *(Added)* |
| `PushNotificationInfo` | `stub_PushNotificationInfo` | Information needed for push notification registration (likely part of `CallAgent` methods). |

**Note:** This table focuses on **basic voice call functionality**. Video-related classes (`VideoDeviceInfo`, `LocalVideoStream`, `RemoteVideoStream`, `VideoStreamRenderer`, etc.) and video effects have been omitted. The exact C++/WinRT class names, namespaces, method signatures, and parameter types may differ in the official SDK projection. This mapping is based on the C# SDK's structure. Async operations in C# (`Task<T>`, `Task`) are mapped to C++/WinRT's `IAsyncOperation<T>` and `IAsyncAction`. Events are mapped to standard C++/WinRT events (`event_token`, `event`).
