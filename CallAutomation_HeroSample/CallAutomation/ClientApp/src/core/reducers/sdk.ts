import { CallClient } from '@azure/communication-calling';
import { Reducer } from 'redux';
import { SET_USERID, SdkTypes, SET_CALL_CLIENT, SET_DISPLAY_NAME } from '../actions/sdk';

export interface SdkState {
  userId?: string;
  callClient?: CallClient;
  displayName: string;
}

const initialState: SdkState = {
  displayName: ''
};

export const sdkReducer: Reducer<SdkState, SdkTypes> = (state = initialState, action: SdkTypes): SdkState => {
  switch (action.type) {
    case SET_USERID:
      return { ...state, userId: action.userId };
    case SET_CALL_CLIENT:
      return { ...state, callClient: action.callClient };
    case SET_DISPLAY_NAME:
      return { ...state, displayName: action.displayName };
    default:
      return state;
  }
};
