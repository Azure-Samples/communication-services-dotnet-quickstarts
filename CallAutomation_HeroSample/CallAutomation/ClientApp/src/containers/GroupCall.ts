import { connect } from 'react-redux';
import { GroupCall, GroupCallProps } from '../components/GroupCall';
import { join, setMicrophone } from '../core/sideEffects';
import { setLocalVideoStream } from '../core/actions/streams';
import { setVideoDeviceInfo, setAudioDeviceInfo } from '../core/actions/devices';
import {
  AudioDeviceInfo,
  VideoDeviceInfo,
  LocalVideoStream,
  TeamsMeetingLinkLocator,
  GroupCallLocator
} from '@azure/communication-calling';
import { State } from '../core/reducers';
import { dialogBoxVisible } from 'core/actions/calls';

const mapStateToProps = (state: State, props: GroupCallProps) => ({
  userId: state.sdk.userId,
  callAgent: state.calls.callAgent,
  deviceManager: state.devices.deviceManager,
  screenWidth: props.screenWidth,
  call: state.calls.call,
  shareScreen: state.controls.shareScreen,
  mic: state.controls.mic,
  callEndReason: state.calls.callEndReason,
  join: async (locator: GroupCallLocator | TeamsMeetingLinkLocator) => {
    state.calls.callAgent &&
      (await join(state.calls.callAgent, locator, {
        videoOptions: {
          localVideoStreams: state.streams.localVideoStream ? [state.streams.localVideoStream] : undefined
        },
        audioOptions: { muted: !state.controls.mic }
      }));
  },
  remoteParticipants: state.calls.remoteParticipants,
  callState: state.calls.callState,
  localVideo: state.controls.localVideo,
  localVideoStream: state.streams.localVideoStream,
  screenShareStreams: state.streams.screenShareStreams,
  videoDeviceInfo: state.devices.videoDeviceInfo,
  audioDeviceInfo: state.devices.audioDeviceInfo,
  videoDeviceList: state.devices.videoDeviceList,
  audioDeviceList: state.devices.audioDeviceList,
  cameraPermission: state.devices.cameraPermission,
  microphonePermission: state.devices.microphonePermission,
  recordingStatus: state.calls.recordingStatus,
  recordingError: state.calls.recordingError,
  isDialogBoxVisiblile: state.calls.dialogBoxVisible,
  isBeingRecorded: state.calls.isBeingRecorded,
  isBeingTranscribed: state.calls.isBeingTranscribed
});

const mapDispatchToProps = (dispatch: any) => ({
  mute: () => dispatch(setMicrophone(false)),
  setAudioDeviceInfo: (deviceInfo: AudioDeviceInfo) => {
    dispatch(setAudioDeviceInfo(deviceInfo));
  },
  setVideoDeviceInfo: (deviceInfo: VideoDeviceInfo) => {
    dispatch(setVideoDeviceInfo(deviceInfo));
  },
  dialogBoxVisiblilty: (dialogBoxVisibilty: boolean) => dispatch(dialogBoxVisible(dialogBoxVisibilty)),
  setLocalVideoStream: (localVideoStream: LocalVideoStream) => dispatch(setLocalVideoStream(localVideoStream))
});

const connector: any = connect(mapStateToProps, mapDispatchToProps);
export default connector(GroupCall);
