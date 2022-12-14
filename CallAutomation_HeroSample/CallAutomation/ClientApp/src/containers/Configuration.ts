import { connect } from 'react-redux';
import { Configuration as ConfigurationScreen, ConfigurationScreenProps } from '../components/Configuration';
import { setVideoDeviceInfo, setAudioDeviceInfo } from '../core/actions/devices';
import { initCallAgent, initCallClient, updateDevices, getUserForRoomWithRefreshToken, setSelectedUsersForRoom, getSetRoomId } from '../core/sideEffects';
import { setMic } from '../core/actions/controls';
import { State } from '../core/reducers';
import { AudioDeviceInfo, VideoDeviceInfo, LocalVideoStream, CallClient } from '@azure/communication-calling';
import { setLocalVideoStream } from '../core/actions/streams';
import { AzureCommunicationTokenCredential } from '@azure/communication-common';

const mapStateToProps = (state: State, props: ConfigurationScreenProps) => ({
  deviceManager: state.devices.deviceManager,
  callClient: state.sdk.callClient,
  callAgent: state.calls.callAgent,
  mic: state.controls.mic,
  screenWidth: props.screenWidth,
  localVideoStream: state.streams.localVideoStream,
  audioDeviceInfo: state.devices.audioDeviceInfo,
  videoDeviceInfo: state.devices.videoDeviceInfo,
  videoDeviceList: state.devices.videoDeviceList,
  audioDeviceList: state.devices.audioDeviceList,
  cameraPermission: state.devices.cameraPermission,
  microphonePermission: state.devices.microphonePermission,
  userForRoomWithRefreshToken: state.calls.userForRoomWithRefreshToken,
  roomId: state.calls.roomId
});

const mapDispatchToProps = (dispatch: any, props: ConfigurationScreenProps) => ({
  setLocalVideoStream: (localVideoStream: LocalVideoStream) => dispatch(setLocalVideoStream(localVideoStream)),
  setMic: (mic: boolean) => dispatch(setMic(mic)),
  setAudioDeviceInfo: (deviceInfo: AudioDeviceInfo) => dispatch(setAudioDeviceInfo(deviceInfo)),
  setVideoDeviceInfo: (deviceInfo: VideoDeviceInfo) => dispatch(setVideoDeviceInfo(deviceInfo)),
    setupCallClient: (unsupportedStateHandler: () => void) => dispatch(initCallClient(unsupportedStateHandler)),
    getUserForRoom: (): Promise<void> => dispatch(getUserForRoomWithRefreshToken()),
    setRoomId: (): Promise<void> => dispatch(getSetRoomId()),
    setupCallAgent: (token: AzureCommunicationTokenCredential, callClient: CallClient, displayName: string) =>
    dispatch(initCallAgent(token, callClient, displayName, props.callEndedHandler)),
    updateDevices: () => dispatch(updateDevices()),
    setSelectedUserToServer: (selectedRoomUser: string) => dispatch(setSelectedUsersForRoom(selectedRoomUser)),
});

const connector: any = connect(mapStateToProps, mapDispatchToProps);
export default connector(ConfigurationScreen);
