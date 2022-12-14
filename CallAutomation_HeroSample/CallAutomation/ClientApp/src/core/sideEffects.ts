import {
  AudioDeviceInfo,
  Call,
  CommunicationServicesError,
  GroupCallLocator,
  JoinCallOptions,
  DeviceManager,
  DeviceAccess,
  RemoteParticipant,
  VideoDeviceInfo,
  CallAgent,
  CallClient,
  HangUpOptions,
  CallEndReason,
  TeamsMeetingLinkLocator,
  Features
} from '@azure/communication-calling';
import { AzureCommunicationTokenCredential, CommunicationUserKind } from '@azure/communication-common';
import { Dispatch } from 'redux';
import { utils, RecordingApiResponse, RecordingActionResponse, RecordingLinkResponse } from '../Utils/Utils';
import {
  callAdded,
  callRemoved,
  setCallState,
  setParticipants,
  setCallAgent,
  setRecordingActive,
  setTranscribingActive,
  setServerCallId,
  startRecording,
  stopRecording,
  recordingError,
  recordingLink,
  setUserForRoomWithRefreshToken,
  setRoomId,
  setSelectedUserForRoom
} from './actions/calls';
import { setMic, setShareScreen } from './actions/controls';
import {
  setAudioDeviceInfo,
  setAudioDeviceList,
  setCameraPermission,
  setMicrophonePermission,
  setVideoDeviceInfo,
  setVideoDeviceList,
  setDeviceManager
} from './actions/devices';
import { setCallClient } from './actions/sdk';
import { addScreenShareStream, removeScreenShareStream } from './actions/streams';
import { State } from './reducers';
import { setLogLevel } from '@azure/logger';
import RemoteStreamSelector from './RemoteStreamSelector';
import { Constants } from './constants';
import { TokenResponse } from '../components/Configuration';

export const setMicrophone = (mic: boolean) => {
  return async (dispatch: Dispatch, getState: () => State): Promise<void> => {
    const state = getState();

    if (state === undefined || state.calls.call === undefined) {
      console.error('state or state.controls.mic is null');
      return;
    }

    try {
      if (!state.controls.mic) {
        await state.calls.call.unmute();
      } else {
        await state.calls.call.mute();
      }

      dispatch(setMic(mic));
    } catch (e) {
      console.error(e);
    }
  };
};

export const setShareUnshareScreen = (shareScreen: boolean) => {
  return async (dispatch: Dispatch, getState: () => State): Promise<void> => {
    const state = getState();

    if (state === undefined || state.calls.call === undefined) {
      console.error('state or state.controls.shareScreen is null');
      return;
    }

    try {
      if (!state.controls.shareScreen) {
        await state.calls.call.startScreenSharing();
      } else {
        await state.calls.call.stopScreenSharing();
      }

      dispatch(setShareScreen(shareScreen));
    } catch (e) {
      console.error(e);
    }
  };
};

const subscribeToParticipant = (participant: RemoteParticipant, call: Call, dispatch: Dispatch): void => {
  const dominantParticipantCount = utils.isSafari()
    ? Constants.DOMINANT_PARTICIPANTS_COUNT_SAFARI
    : Constants.DOMINANT_PARTICIPANTS_COUNT;
  const remoteStreamSelector = RemoteStreamSelector.getInstance(dominantParticipantCount, dispatch);

  participant.on('stateChanged', () => {
    remoteStreamSelector.participantStateChanged(
      utils.getId(participant.identifier),
      participant.displayName ?? '',
      participant.state,
      !participant.isMuted,
      participant.videoStreams[0].isAvailable
    );
    dispatch(setParticipants([...call.remoteParticipants.values()]));
  });

  participant.on('isMutedChanged', () => {
    remoteStreamSelector.participantAudioChanged(utils.getId(participant.identifier), !participant.isMuted);
  });

  participant.on('isSpeakingChanged', () => {
    dispatch(setParticipants([...call.remoteParticipants.values()]));
  });

  participant.on('videoStreamsUpdated', (e): void => {
    e.added.forEach((addedStream) => {
      if (addedStream.mediaStreamType === 'ScreenSharing') {
        addedStream.on('isAvailableChanged', () => {
          if (addedStream.isAvailable) {
            dispatch(addScreenShareStream(addedStream, participant));
          } else {
            dispatch(removeScreenShareStream(addedStream, participant));
          }
        });

        if (addedStream.isAvailable) {
          dispatch(addScreenShareStream(addedStream, participant));
        }
      } else if (addedStream.mediaStreamType === 'Video') {
        addedStream.on('isAvailableChanged', () => {
          remoteStreamSelector.participantVideoChanged(utils.getId(participant.identifier), addedStream.isAvailable);
        });
      }
    });
    dispatch(setParticipants([...call.remoteParticipants.values()]));
  });
};

const updateAudioDevices = async (
  deviceManager: DeviceManager,
  dispatch: Dispatch,
  getState: () => State
): Promise<void> => {
  const microphoneList: AudioDeviceInfo[] = await deviceManager.getMicrophones();
  dispatch(setAudioDeviceList(microphoneList));

  const state = getState();
  if (state.devices.audioDeviceInfo === undefined && microphoneList.length > 0) {
    dispatch(setAudioDeviceInfo(microphoneList[0]));
    deviceManager.selectMicrophone(microphoneList[0]);
  } else if (
    state.devices.audioDeviceInfo &&
    !utils.isSelectedAudioDeviceInList(state.devices.audioDeviceInfo, microphoneList)
  ) {
    deviceManager.selectMicrophone(state.devices.audioDeviceInfo);
  }
};

const updateVideoDevices = async (
  deviceManager: DeviceManager,
  dispatch: Dispatch,
  getState: () => State
): Promise<void> => {
  const cameraList: VideoDeviceInfo[] = await deviceManager.getCameras();
  dispatch(setVideoDeviceList(cameraList));

  const state = getState();
  if (state.devices.videoDeviceInfo === undefined) {
    dispatch(setVideoDeviceInfo(cameraList[0]));
  } else if (
    state.devices.videoDeviceInfo &&
    !utils.isSelectedVideoDeviceInList(state.devices.videoDeviceInfo, cameraList)
  ) {
    dispatch(setVideoDeviceInfo(state.devices.videoDeviceInfo));
  }
};

const subscribeToDeviceManager = async (
  deviceManager: DeviceManager,
  dispatch: Dispatch,
  getState: () => State
): Promise<void> => {
  // listen for any new events
  deviceManager.on('videoDevicesUpdated', async () => {
    updateVideoDevices(deviceManager, dispatch, getState);
  });

  deviceManager.on('audioDevicesUpdated', async () => {
    updateAudioDevices(deviceManager, dispatch, getState);
  });

  deviceManager.askDevicePermission({ audio: true, video: true }).then((e: DeviceAccess) => {
    if (e.audio !== undefined) {
      if (e.audio) {
        dispatch(setMicrophonePermission('Granted'));

        updateAudioDevices(deviceManager, dispatch, getState);
      } else {
        dispatch(setMicrophonePermission('Denied'));
      }
    }

    if (e.video !== undefined) {
      if (e.video) {
        dispatch(setCameraPermission('Granted'));
        updateVideoDevices(deviceManager, dispatch, getState);
      } else {
        dispatch(setCameraPermission('Denied'));
      }
    }
  });
};

export const updateDevices = () => {
  return async (dispatch: Dispatch, getState: () => State): Promise<void> => {
    const state = getState();
    const deviceManager = state.devices.deviceManager;

    if (deviceManager == null) {
      console.error('no device manager available');
      return;
    }

    const cameraList: VideoDeviceInfo[] = await deviceManager.getCameras();

    dispatch(setVideoDeviceList(cameraList));

    const microphoneList: AudioDeviceInfo[] = await deviceManager.getMicrophones();

    dispatch(setAudioDeviceList(microphoneList));
  };
};

export const startRecord = () => {
  return async (dispatch: Dispatch, getState: () => State): Promise<void> => {
    const state = getState();
    if (state.calls !== undefined && state.calls.serverCallId) {
      const response: RecordingApiResponse = await utils.startRecording(state.calls.serverCallId);
      if (response && !response.message) {
        dispatch(startRecording());
      } else {
        dispatch(recordingError(response.message));
        console.error(response.message);
      }
    } else {
      console.error('serverCallId not available');
      return;
    }
  };
};

export const startAudioRecord = (recordingFormat: string) => {
  return async (dispatch: Dispatch, getState: () => State): Promise<void> => {
    const state = getState();
    if (state.calls !== undefined && state.calls.serverCallId) {
      const response: RecordingApiResponse = await utils.startAudioRecording(state.calls.serverCallId, recordingFormat);
      if (response && !response.message) {
        dispatch(startRecording());
      } else {
        dispatch(recordingError(response.message));
        console.error(response.message);
      }
    } else {
      console.error('serverCallId not available');
      return;
    }
  };
};

export const stopRecord = () => {
  return async (dispatch: Dispatch, getState: () => State): Promise<void> => {
    const state = getState();
    if (state.calls !== undefined && state.calls.serverCallId) {
      const response: RecordingActionResponse = await utils.stopRecording(state.calls.serverCallId);
      if (response && !response.message) {
        dispatch(stopRecording());
      } else {
        console.error(response.message);
      }
    } else return;
  };
};

export const getRecordLink = () => {
  return async (dispatch: Dispatch, getState: () => State): Promise<string> => {
    const state = getState();
    if (state.calls !== undefined && state.calls.serverCallId) {
      const response: RecordingLinkResponse = await utils.getRecordingLink(state.calls.serverCallId);

      if (response.uri !== null && response.uri !== '') {
        dispatch(recordingLink(response.uri));
        return response.uri;
      } else {
        dispatch(recordingError(response.message));
        console.error(response.message);
        dispatch(recordingLink(''));
        return '';
      }
    } else {
      dispatch(recordingLink(''));
      return '';
    }
  };
};
export const getUserForRoomWithRefreshToken = () => {
    return async (dispatch: Dispatch): Promise<void> => {
        let usersForRoom: TokenResponse[] = [];
        const tokenResponse = await utils.getUsersForRoom();
        for (let i = 0; i < tokenResponse.length; i++) {
            let userToken = tokenResponse[i].token;
            let userId = tokenResponse[i].user;

            let tokenCredential = new AzureCommunicationTokenCredential({
                tokenRefresher: (): Promise<string> => {
                    return utils.getRefreshedTokenForUser(userId);
                },
                refreshProactively: true,
                token: userToken
            });

            usersForRoom[i] = { tokenCredential, userId }
        }

        dispatch(setUserForRoomWithRefreshToken(usersForRoom));
    };
};

export const getSetRoomId = () => {
    return async (dispatch: Dispatch): Promise<void> => {
        const roomId = await utils.getRoomId();
        dispatch(setRoomId(roomId));
    };
};

export const setSelectedUsersForRoom = (selectedUser: string) => {
    return async (dispatch: Dispatch): Promise<void> => {
        try {
            await fetch('/setSelectedUsersForRoom/' + selectedUser);
        }
        catch (e) {
            throw new Error('Invalid user setSelectedUsersForRoom response');
        }
        dispatch(setSelectedUserForRoom(selectedUser));
    };
};

export const initCallAgent = (
  token: AzureCommunicationTokenCredential,
  callClient: CallClient,
  name: string,
  callEndedHandler: (reason: CallEndReason) => void
) => {
    return async (dispatch: Dispatch, getState: () => State): Promise<void> => {

        const callAgent: CallAgent = await callClient.createCallAgent(token, { displayName: name });

    if (callAgent === undefined) {
      return;
    }

    dispatch(setCallAgent(callAgent));

    callAgent.on('callsUpdated', (e: { added: Call[]; removed: Call[] }): void => {
      e.added.forEach((addedCall) => {
        console.log(`Call added : Call Id = ${addedCall.id}`);

        const state = getState();
        if (state.calls.call && addedCall.direction === 'Incoming') {
          addedCall.hangUp();
          return;
        }

        dispatch(callAdded(addedCall));

        addedCall.on('stateChanged', (): void => {
          dispatch(setCallState(addedCall.state));

          if (addedCall.state === 'Connected') {
            addedCall.info
              .getServerCallId()
              .then((result) => {
                console.log('Conversation URL is - ' + result);
                dispatch(setServerCallId(result));
              })
              .catch((err) => {
                console.log(err);
              });
          }
        });

        dispatch(setCallState(addedCall.state));

        addedCall.on('isScreenSharingOnChanged', (): void => {
          dispatch(setShareScreen(addedCall.isScreenSharingOn));
        });

        dispatch(setShareScreen(addedCall.isScreenSharingOn));

        addedCall.api(Features.Recording).on('isRecordingActiveChanged', (): void => {
          const callRecordingActive = addedCall.api(Features.Recording).isRecordingActive;
          if (callRecordingActive) {
            dispatch(startRecording());
          } else if (!callRecordingActive) {
            dispatch(stopRecording());
          }
          dispatch(setRecordingActive(callRecordingActive));
        });

        // if you are not in a teams meeting call you will just get false
        dispatch(setRecordingActive(addedCall.api(Features.Recording).isRecordingActive));

        addedCall.api(Features.Transcription).on('isTranscriptionActiveChanged', (): void => {
          dispatch(setTranscribingActive(addedCall.api(Features.Transcription).isTranscriptionActive));
        });

        dispatch(setTranscribingActive(addedCall.api(Features.Transcription).isTranscriptionActive));

        // if remote participants have changed, subscribe to the added remote participants
        addedCall.on('remoteParticipantsUpdated', (ev): void => {
          // for each of the added remote participants, subscribe to events and then just update as well in case the update has already happened
          ev.added.forEach((addedRemoteParticipant) => {
            subscribeToParticipant(addedRemoteParticipant, addedCall, dispatch);
            dispatch(setParticipants([...state.calls.remoteParticipants, addedRemoteParticipant]));
          });

          // We don't use the actual value we are just going to reset the remoteParticipants based on the call
          if (ev.removed.length > 0) {
            dispatch(setParticipants([...addedCall.remoteParticipants.values()]));
          }
        });

        dispatch(setParticipants([...state.calls.remoteParticipants]));
      });
      e.removed.forEach((removedCall) => {
        const state = getState();
        if (state.calls.call && state.calls.call === removedCall) {
          dispatch(callRemoved(removedCall));

          // if we were not allowed into invited into a Teams call
          if (
            removedCall.callEndReason &&
            removedCall.callEndReason.code === 0 &&
            removedCall.callEndReason.subCode === 5854
          ) {
            removedCall.callEndReason && callEndedHandler(removedCall.callEndReason);
            return;
          }

          if (removedCall.callEndReason && removedCall.callEndReason.code !== 0) {
            removedCall.callEndReason && callEndedHandler(removedCall.callEndReason);
            return;
          }
        }
      });
    });
  };
};

export const initCallClient = (unsupportedStateHandler: () => void) => {
  return async (dispatch: Dispatch, getState: () => State): Promise<void> => {
    let callClient;

    // check if chrome on ios OR firefox browser
    if (utils.isOnIphoneAndNotSafari() || utils.isUnsupportedBrowser()) {
      unsupportedStateHandler();
      return;
    }

    try {
      setLogLevel('verbose');
      callClient = new CallClient();
    } catch (e) {
      unsupportedStateHandler();
      return;
    }

    if (!callClient) {
      return;
    }

    const deviceManager: DeviceManager = await callClient.getDeviceManager();

    dispatch(setCallClient(callClient));
    dispatch(setDeviceManager(deviceManager));
    subscribeToDeviceManager(deviceManager, dispatch, getState);
  };
};

// what does the forEveryone parameter really mean?
export const endCall = async (call: Call, options: HangUpOptions): Promise<void> => {
  await call.hangUp(options).catch((e: CommunicationServicesError) => console.error(e));
};

const joinGroup = async (
  callAgent: CallAgent,
  context: GroupCallLocator,
  callOptions: JoinCallOptions
): Promise<void> => {
  try {
    await callAgent.join(context, callOptions);
  } catch (e) {
    console.log('Failed to join a call', e);
    return;
  }
};

const joinTeamsMeeting = async (
  callAgent: CallAgent,
  context: TeamsMeetingLinkLocator,
  callOptions: JoinCallOptions
): Promise<void> => {
  try {
    await callAgent.join(context, callOptions);
  } catch (e) {
    console.log('Failed to join a call', e);
    return;
  }
};

export const join = async (
  callAgent: CallAgent,
  locator: GroupCallLocator | TeamsMeetingLinkLocator,
  callOptions: JoinCallOptions
): Promise<void> => {
  const isGroupCallLocator = (locator: GroupCallLocator | TeamsMeetingLinkLocator): locator is GroupCallLocator => {
    return true;
  };

  if (isGroupCallLocator(locator)) {
    return joinGroup(callAgent, locator, callOptions);
  } else {
    return joinTeamsMeeting(callAgent, locator, callOptions);
  }
};

export const addParticipant = async (call: Call, user: CommunicationUserKind): Promise<void> => {
  await call.addParticipant(user);
};

export const removeParticipant = async (call: Call, user: CommunicationUserKind): Promise<void> => {
  await call.removeParticipant(user).catch((e: CommunicationServicesError) => console.error(e));
};
