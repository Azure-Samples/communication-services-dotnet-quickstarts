import { CallEndReason, Call, RemoteParticipant, CallAgent } from '@azure/communication-calling';
import { TokenResponse } from '../../components/Configuration';
import { SelectionState } from '../RemoteStreamSelector';

const SET_CALL_AGENT = 'SET_CALL_AGENT';
const CALL_ADDED = 'CALL_ADDED';
const CALL_REMOVED = 'CALL_REMOVED';
const SET_CALL_STATE = 'SET_CALL_STATE';
const SET_PARTICIPANTS = 'SET_PARTICIPANTS';
const SET_RECORDING_ACTIVE = 'SET_RECORDING_ACTIVE';
const SET_TRANSCRIBING_ACTIVE = 'SET_TRANSCRIBING_ACTIVE';
const SET_DOMINANT_PARTICIPANTS = 'SET_DOMINANT_PARTICIPANTS';
const START_RECORDING = 'START_RECORDING';
const STOP_RECORDING = 'STOP_RECORDING';
const SET_SERVER_CALL_ID = 'SET_SERVER_CALL_ID';
const DIALOGBOX_VISIBLE = 'DIALOGBOX_VISIBLE';
const RECORDING_ERROR = 'RECORDING_ERROR';
const SET_RECORDING_LINK = 'SET_RECORDING_LINK';
const SET_USER_FOR_ROOM_WITH_REFRESH_TOKEN = 'SET_USER_FOR_ROOM_WITH_REFRESH_TOKEN';
const SET_SELECTED_USER_FOR_ROOM = 'SET_SELECTED_USER_FOR_ROOM';
const SET_ROOM_ID = 'SET_ROOM_ID'

interface SetCallAgentAction {
    type: typeof SET_CALL_AGENT;
    callAgent: CallAgent;
}

interface CallAddedAction {
  type: typeof CALL_ADDED;
  call: Call;
}

interface CallRemovedAction {
  type: typeof CALL_REMOVED;
  call: Call | undefined;
  incomingCallEndReason: CallEndReason | undefined;
  callEndReason: CallEndReason | undefined;
}

interface SetCallStateAction {
  type: typeof SET_CALL_STATE;
  callState: string;
}

interface SetParticipantsAction {
  type: typeof SET_PARTICIPANTS;
  remoteParticipants: RemoteParticipant[];
}

interface SetRecordingActiveAction {
  type: typeof SET_RECORDING_ACTIVE;
  active: boolean;
}

interface SetTranscribingActiveAction {
  type: typeof SET_TRANSCRIBING_ACTIVE;
  active: boolean;
}

interface SetDominantParticipantsAction {
  type: typeof SET_DOMINANT_PARTICIPANTS;
  dominantParticipants: SelectionState[];
}

interface StartRecordingAction {
  type: typeof START_RECORDING;
  status: 'STARTED';
}

interface StopRecordingAction {
  type: typeof STOP_RECORDING;
  status: 'STOPPED';
}

interface SetServerCallIdAction {
  type: typeof SET_SERVER_CALL_ID;
  serverCallId: string;
}

interface DialogBoxVisibleAction {
  type: typeof DIALOGBOX_VISIBLE;
  dialogBoxVisible: boolean;
}

interface RecordingErrorAction {
  type: typeof RECORDING_ERROR;
  recordingError: string;
}

interface RecordingLinkAction {
  type: typeof SET_RECORDING_LINK;
  recordingLink: string;
}

interface SetUserForRoomWithRefreshTokenAction {
    type: typeof SET_USER_FOR_ROOM_WITH_REFRESH_TOKEN;
    userForRoomWithRefreshToken: TokenResponse[]
}

interface SetSelectedUserForRoomAction {
    type: typeof SET_SELECTED_USER_FOR_ROOM;
    selectedUserForRoom: string
}

interface SetRoomIdAction {
    type: typeof SET_ROOM_ID;
    roomId: string
}

export const setCallAgent = (callAgent: CallAgent): SetCallAgentAction => {
  return {
    type: SET_CALL_AGENT,
    callAgent
  };
};

export const callAdded = (addedCall: Call): CallAddedAction => {
  return {
    type: CALL_ADDED,
    call: addedCall
  };
};

export const callRemoved = (removedCall: Call): CallRemovedAction => {
  return {
    type: CALL_REMOVED,
    call: undefined,
    incomingCallEndReason: removedCall.direction === 'Incoming' ? removedCall.callEndReason : undefined,
    callEndReason: removedCall.callEndReason
  };
};

export const setCallState = (callState: string): SetCallStateAction => {
  return {
    type: SET_CALL_STATE,
    callState
  };
};

export const setParticipants = (participants: RemoteParticipant[]): SetParticipantsAction => {
  return {
    type: SET_PARTICIPANTS,
    remoteParticipants: participants
  };
};

export const setRecordingActive = (isActive: boolean): SetRecordingActiveAction => {
  return {
    type: SET_RECORDING_ACTIVE,
    active: isActive
  };
};

export const setTranscribingActive = (isActive: boolean): SetTranscribingActiveAction => {
  return {
    type: SET_TRANSCRIBING_ACTIVE,
    active: isActive
  };
};

export const setDominantParticipants = (selected: SelectionState[]): SetDominantParticipantsAction => {
  return {
    type: SET_DOMINANT_PARTICIPANTS,
    dominantParticipants: selected
  };
};

export const startRecording = (): StartRecordingAction => {
  return {
    type: 'START_RECORDING',
    status: 'STARTED'
  };
};

export const stopRecording = (): StopRecordingAction => {
  return {
    type: 'STOP_RECORDING',
    status: 'STOPPED'
  };
};

export const setServerCallId = (serverCallId: string): SetServerCallIdAction => {
  return {
    type: 'SET_SERVER_CALL_ID',
    serverCallId
  };
};

export const dialogBoxVisible = (dialogBoxVisible: boolean): DialogBoxVisibleAction => {
  return {
    type: 'DIALOGBOX_VISIBLE',
    dialogBoxVisible
  };
};

export const recordingError = (recordingError: string): RecordingErrorAction => {
  return {
    type: 'RECORDING_ERROR',
    recordingError
  };
};

export const recordingLink = (recordingLink: string): RecordingLinkAction => {
  return {
    type: 'SET_RECORDING_LINK',
    recordingLink
  };
};

export const setUserForRoomWithRefreshToken = (userForRoomWithRefreshToken: TokenResponse[]): SetUserForRoomWithRefreshTokenAction => {
    return {
        type: 'SET_USER_FOR_ROOM_WITH_REFRESH_TOKEN',
        userForRoomWithRefreshToken
    };
};

export const setSelectedUserForRoom = (selectedUserForRoom: string): SetSelectedUserForRoomAction => {
    return {
        type: 'SET_SELECTED_USER_FOR_ROOM',
        selectedUserForRoom
    };
};
export const setRoomId = (roomId: string): SetRoomIdAction => {
    return {
        type: 'SET_ROOM_ID',
        roomId
    };
};

export {
  SET_CALL_AGENT,
  CALL_ADDED,
  CALL_REMOVED,
  SET_CALL_STATE,
  SET_PARTICIPANTS,
  SET_RECORDING_ACTIVE,
  SET_TRANSCRIBING_ACTIVE,
  SET_DOMINANT_PARTICIPANTS,
  START_RECORDING,
  STOP_RECORDING,
  SET_SERVER_CALL_ID,
  RECORDING_ERROR,
  DIALOGBOX_VISIBLE,
  SET_RECORDING_LINK,
  SET_USER_FOR_ROOM_WITH_REFRESH_TOKEN,
  SET_SELECTED_USER_FOR_ROOM,
  SET_ROOM_ID
};

export type CallTypes =
  | SetCallAgentAction
  | SetParticipantsAction
  | SetDominantParticipantsAction
  | SetCallStateAction
  | CallAddedAction
  | CallRemovedAction
  | SetRecordingActiveAction
  | SetTranscribingActiveAction
  | StartRecordingAction
  | StopRecordingAction
  | SetServerCallIdAction
  | RecordingErrorAction
  | DialogBoxVisibleAction
  | RecordingLinkAction
  | SetUserForRoomWithRefreshTokenAction
  | SetSelectedUserForRoomAction
  | SetRoomIdAction

