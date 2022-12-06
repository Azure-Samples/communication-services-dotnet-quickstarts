// © Microsoft Corporation. All rights reserved.
import React, { useState, useEffect } from 'react';
import { Label, Overlay, Stack } from '@fluentui/react';
import Header from '../containers/Header';
import MediaGallery from '../containers/MediaGallery';
import { MediaFullScreen } from './MediaFullScreen';
import { CommandPanel, CommandPanelTypes } from './CommandPanel';
import { Constants } from '../core/constants';
import {
  headerStyles,
  containerStyles,
  paneStyles,
  hiddenContainerClassName,
  activeContainerClassName,
  loadingStyle,
  overlayStyles,
  messageBarStyles
} from './styles/GroupCall.styles';
import {
  Call,
  LocalVideoStream,
  AudioDeviceInfo,
  VideoDeviceInfo,
  RemoteParticipant,
  CallAgent,
  DeviceManager,
  TeamsMeetingLinkLocator,
  GroupCallLocator,
  CallEndReason
} from '@azure/communication-calling';
import { ParticipantStream } from 'core/reducers/index.js';
import ComplianceBanner from '../containers/ComplianceBanner';
import { DialogBox } from './DialogBox';

export interface GroupCallProps {
  userId: string;
  groupId: string;
  call: Call;
  callAgent: CallAgent;
  deviceManager: DeviceManager;
  mic: boolean;
  remoteParticipants: RemoteParticipant[];
  callState: string;
  localVideo: boolean;
  localVideoStream: LocalVideoStream;
  screenShareStreams: ParticipantStream[];
  audioDeviceInfo: AudioDeviceInfo;
  videoDeviceInfo: VideoDeviceInfo;
  audioDeviceList: AudioDeviceInfo[];
  videoDeviceList: VideoDeviceInfo[];
  screenWidth: number;
  shareScreen: boolean;
  callEndReason: CallEndReason;
  locator: GroupCallLocator | TeamsMeetingLinkLocator;
  isBeingRecorded: boolean;
  isBeingTranscribed: boolean;
  setAudioDeviceInfo(deviceInfo: AudioDeviceInfo): void;
  setVideoDeviceInfo(deviceInfo: VideoDeviceInfo): void;
  setLocalVideoStream(stream: LocalVideoStream | undefined): void;
  mute(): void;
  join(locator: GroupCallLocator | TeamsMeetingLinkLocator): void;
  endCallHandler(): void;
  recordingStatus: string;
  recordingError: string;
  dialogBoxVisiblilty(b: boolean): void;
  isDialogBoxVisiblile: boolean;
}

export const GroupCall = (props: GroupCallProps): JSX.Element => {
  const [selectedPane, setSelectedPane] = useState(CommandPanelTypes.None);
  const activeScreenShare = props.screenShareStreams && props.screenShareStreams.length === 1;

  const { callAgent, call, join, locator, isBeingRecorded, isBeingTranscribed, callEndReason } = props;

  useEffect(() => {
    if (callAgent && !call && callEndReason === undefined) {
      join(locator);
    }
  }, [callAgent, call, join, locator, callEndReason]);

  const dismissDialogBox = () => {
    props.dialogBoxVisiblilty(false);
  };

  return (
    <Stack horizontalAlign="center" verticalAlign="center" styles={containerStyles}>
      <Stack.Item styles={headerStyles}>
        <Header
          selectedPane={selectedPane}
          setSelectedPane={setSelectedPane}
          endCallHandler={() => {
            props.endCallHandler();
          }}
          screenWidth={props.screenWidth}
        />
      </Stack.Item>
      <Stack.Item styles={messageBarStyles}>
        <ComplianceBanner callTranscribeState={isBeingTranscribed} callRecordState={isBeingRecorded} />
      </Stack.Item>
      <Stack.Item styles={headerStyles}>
        {props.isDialogBoxVisiblile && props.recordingError !== '' && (
          <DialogBox
            message={props.recordingError}
            isDialogBoxVisiblile={props.isDialogBoxVisiblile}
            dismissDialogBox={dismissDialogBox}
          />
        )}
      </Stack.Item>
      <Stack.Item styles={containerStyles}>
        {props.shareScreen && (
          <div className={loadingStyle}>
            <Label>Your screen is being shared</Label>
          </div>
        )}
        {props.callState === Constants.CONNECTED && (
          <Stack horizontal styles={containerStyles}>
            <Stack.Item grow styles={activeScreenShare ? activeContainerClassName : hiddenContainerClassName}>
              {activeScreenShare && <MediaFullScreen activeScreenShareStream={props.screenShareStreams[0]} />}
            </Stack.Item>
            <Stack.Item grow styles={!activeScreenShare ? activeContainerClassName : hiddenContainerClassName}>
              <MediaGallery />
            </Stack.Item>
            {selectedPane !== CommandPanelTypes.None &&
              (window.innerWidth > Constants.MINI_HEADER_WINDOW_WIDTH ? (
                <Stack.Item disableShrink styles={paneStyles}>
                  <CommandPanel {...props} selectedPane={selectedPane} setSelectedPane={setSelectedPane} />
                </Stack.Item>
              ) : (
                <Overlay styles={overlayStyles}>
                  <CommandPanel {...props} selectedPane={selectedPane} setSelectedPane={setSelectedPane} />
                </Overlay>
              ))}
          </Stack>
        )}
        {props.callState === Constants.LOBBY && (
          <Stack horizontal styles={containerStyles}>
            <div className={loadingStyle} style={{ width: '100%' }}>
              <Label>Waiting to be admitted</Label>
            </div>
          </Stack>
        )}
      </Stack.Item>
    </Stack>
  );
};
