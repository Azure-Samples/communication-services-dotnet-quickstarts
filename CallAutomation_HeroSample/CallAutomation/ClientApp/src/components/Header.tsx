// © Microsoft Corporation. All rights reserved.
import React, { useEffect, useState } from 'react';
import { Separator, Pivot, PivotItem, Stack, CommandButton } from '@fluentui/react';
import { Call, LocalVideoStream, VideoDeviceInfo } from '@azure/communication-calling';
import { MediaControls } from './MediaControls';
import { CommandPanelTypes } from './CommandPanel';
import { UserFriendsIcon, SettingsIcon } from '@fluentui/react-icons-northstar';
import { Constants } from 'core/constants';
import {
  headerContainer,
  pivotItemStyles,
  separatorContainerStyle,
  separatorStyles,
  pivotItemStyle,
  headerCenteredContainer,
  feedbackContainer,
  recordingIcon,
  recordingSplitter
} from './styles/Header.styles';
import { ParticipantStream } from 'core/reducers';
import CallRecording from '../containers/CallRecording';
import { FeedbackButton } from './FeedbackButton';
import { utils } from 'Utils/Utils';

export interface HeaderProps {
  selectedPane: CommandPanelTypes;
  setSelectedPane: any;
  endCallHandler(): void;
  actionable: boolean;
  localVideo: boolean;
  mic: boolean;
  shareScreen: boolean;
  call: Call;
  endCall(): void;
  screenShareStreams: ParticipantStream[];
  activeScreenShareStream: ParticipantStream | undefined;
  localVideoRendererIsBusy: boolean;
  cameraPermission: string;
  microphonePermission: string;
  screenWidth: number;
  setMic(mic: boolean): void;
  setLocalVideoStream(localVideoStream: LocalVideoStream | undefined): void;
  setScreenShare(screenShare: boolean): void;
  isLocalScreenShareSupportedInBrowser(): boolean;
  localVideoStream: LocalVideoStream | undefined;
  videoDeviceInfo: VideoDeviceInfo | undefined;
  recordingStatus: 'STARTED' | 'STOPPED';
}

export const Header = (props: HeaderProps): JSX.Element => {
  const [isFeedbackEnabled, setIsFeedbackEnabled] = useState(false);
  const [isRecordingEnabled, setIsRecordingEnabled] = useState(false);
  const compressedMode = props.screenWidth <= Constants.MINI_HEADER_WINDOW_WIDTH;
  const isRecordingOn = props.recordingStatus === 'STARTED';

  useEffect(() => {
    (async () => {
      const settings = await utils.getFeedbackSettings();
      setIsFeedbackEnabled(settings.isFeedbackEnabled);

      const recordingSettings = await utils.getRecordingSettings();
      setIsRecordingEnabled(recordingSettings.isRecordingEnabled);
    })();
  }, [])

  const togglePeople = (selectedPane: string, setSelectedPane: (pane: string) => void) => {
    return selectedPane !== CommandPanelTypes.People
      ? setSelectedPane(CommandPanelTypes.People)
      : setSelectedPane(CommandPanelTypes.None);
  };

  const toggleOptions = (selectedPane: string, setSelectedPane: (pane: string) => void) => {
    return selectedPane !== CommandPanelTypes.Settings
      ? setSelectedPane(CommandPanelTypes.Settings)
      : setSelectedPane(CommandPanelTypes.None);
  };

  const handleLocalVideoOnOff = async () => {
    if (props.localVideoStream) {
      await props.call.stopVideo(props.localVideoStream);
      props.setLocalVideoStream(undefined);
    } else {
      if (props.videoDeviceInfo) {
        const localVideoStream = new LocalVideoStream(props.videoDeviceInfo);
        props.setLocalVideoStream(localVideoStream);
        await props.call.startVideo(localVideoStream);
      }
    }
  };

  const handleMuteOnOff = () => {
    props.setMic(!props.mic);
  };

  const handleScreenSharingOnOff = () => {
    props.setScreenShare(!props.shareScreen);
  };

  useEffect(() => {
    if (props.call && props.call.localVideoStreams.length === 0 && props.localVideoStream) {
      props.call.startVideo(props.localVideoStream);
    }
  }, [props.call, props.localVideoStream]);

  return (
    <Stack
      id="header"
      className={props.screenWidth > Constants.MINI_HEADER_WINDOW_WIDTH ? headerContainer : headerCenteredContainer}
    >
      <Stack.Item grow={1} className={feedbackContainer}>
        {isFeedbackEnabled && <FeedbackButton iconOnly={compressedMode} />}
        {isFeedbackEnabled && isRecordingOn && <div className={recordingSplitter}>
          <Separator styles={separatorStyles} vertical={true} />
        </div>}
        {isRecordingOn &&
          <CommandButton
            className={recordingIcon}
            key='RadioBtnOn'
            text={compressedMode ? undefined : 'Recording'}
            ariaLabel={compressedMode ? 'Recording' : undefined}
            iconProps={{ iconName: 'RadioBtnOn' }}
          />}
      </Stack.Item>

      <Pivot
        onKeyDownCapture={(e) => {
          if ((e.target as HTMLElement).id === CommandPanelTypes.People && e.keyCode === 39) e.preventDefault();
        }}
        getTabId={(itemKey: string) => itemKey}
        onLinkClick={(item) => {
          if (!item) return;
          if (item.props.itemKey === CommandPanelTypes.Settings)
            toggleOptions(props.selectedPane, props.setSelectedPane);
          if (item.props.itemKey === CommandPanelTypes.People) togglePeople(props.selectedPane, props.setSelectedPane);
        }}
        styles={pivotItemStyles}
        initialSelectedKey={CommandPanelTypes.None}
        selectedKey={props.selectedPane}
      >
        <PivotItem
          itemKey={CommandPanelTypes.Settings}
          onRenderItemLink={() => (
            <SettingsIcon
              outline={props.selectedPane === CommandPanelTypes.Settings ? false : true}
              size="medium"
              className={pivotItemStyle}
            />
          )}
        />
        <PivotItem
          itemKey={CommandPanelTypes.People}
          onRenderItemLink={() => (
            <UserFriendsIcon
              outline={props.selectedPane === CommandPanelTypes.People ? false : true}
              size="medium"
              className={pivotItemStyle}
            />
          )}
        />
        <PivotItem itemKey={CommandPanelTypes.None} />
      </Pivot>
      {isRecordingEnabled && <Stack>
        <CallRecording />
      </Stack>}
      {props.screenWidth > Constants.MINI_HEADER_WINDOW_WIDTH && (
        <div className={separatorContainerStyle}>
          <Separator styles={separatorStyles} vertical={true} />
        </div>
      )}
      <MediaControls
        micActive={props.mic}
        onMicChange={handleMuteOnOff}
        cameraActive={props.localVideoStream !== undefined}
        onCameraChange={handleLocalVideoOnOff}
        screenShareActive={props.shareScreen}
        activeScreenShareStream={props.screenShareStreams[0] ?? undefined}
        onScreenShareChange={handleScreenSharingOnOff}
        onEndCallClick={() => {
          if (props.localVideo) handleLocalVideoOnOff();
          props.endCall();
        }}
        cameraPermission={props.cameraPermission}
        microphonePermission={props.microphonePermission}
        localVideoRendererIsBusy={props.localVideoRendererIsBusy}
        compressedMode={compressedMode}
        isLocalScreenShareSupportedInBrowser={props.isLocalScreenShareSupportedInBrowser}
      />
    </Stack>
  );
};
