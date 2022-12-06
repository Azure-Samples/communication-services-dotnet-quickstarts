import { CallClient } from '@azure/communication-calling';

const SET_USERID = 'SET_USERID';
const SET_CALL_CLIENT = 'SET_CALL_CLIENT';
const SET_DISPLAY_NAME = 'SET_DISPLAY_NAME';

interface SetUserIdAction {
  type: typeof SET_USERID;
  userId: string;
}

interface SetCallClient {
  type: typeof SET_CALL_CLIENT;
  callClient: CallClient;
}

interface SetDisplayNameAction {
  type: typeof SET_DISPLAY_NAME;
  displayName: string;
}

export const setUserId = (userId: string): SetUserIdAction => {
  return {
    type: SET_USERID,
    userId
  };
};

export const setCallClient = (callClient: CallClient): SetCallClient => {
  return {
    type: SET_CALL_CLIENT,
    callClient
  };
};

export const setDisplayName = (displayName: string): SetDisplayNameAction => {
  return {
    type: SET_DISPLAY_NAME,
    displayName
  };
};

export { SET_USERID, SET_CALL_CLIENT, SET_DISPLAY_NAME };

export type SdkTypes = SetUserIdAction | SetCallClient | SetDisplayNameAction;
