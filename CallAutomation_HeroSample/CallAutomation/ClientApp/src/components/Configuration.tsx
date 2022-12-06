// © Microsoft Corporation. All rights reserved.

import React, { useCallback, useEffect, useState } from 'react';
import { Stack, Spinner, PrimaryButton, Dropdown, Label } from '@fluentui/react';
import { LocalPreview } from './LocalPreview';
import { LocalSettings } from './LocalSettings';
import { DisplayNameField } from './DisplayNameField';
import { dropDownStyles, labelStyle } from './styles/LocalSettings.styles';
import {
  VideoDeviceInfo,
  AudioDeviceInfo,
  LocalVideoStream,
  DeviceManager,
  CallAgent,
  CallEndReason,
  CallClient
} from '@azure/communication-calling';
import { VideoCameraEmphasisIcon } from '@fluentui/react-icons-northstar';
import {
  videoCameraIconStyle,
  configurationStackTokens,
  buttonStyle,
  localSettingsContainerStyle,
  mainContainerStyle,
  fullScreenStyle,
  verticalStackStyle
} from './styles/Configuration.styles';
import { AzureCommunicationTokenCredential } from '@azure/communication-common';

export type TokenResponse = {
    tokenCredential: AzureCommunicationTokenCredential;
    userId: string;
};
export type TokenResponseDropDown = {
    key: string;
    text: string;
    value: string;
};

export interface ConfigurationScreenProps {
  userId: string;
  groupId: string;
  callClient: CallClient;
  callAgent: CallAgent;
  deviceManager: DeviceManager;
  setupCallClient(unsupportedStateHandler: () => void): void;
  setupCallAgent(token: AzureCommunicationTokenCredential, callClient: CallClient, displayName: string): void;
  startCallHandler(): void;
  unsupportedStateHandler: () => void;
  callEndedHandler: (reason: CallEndReason) => void;
  videoDeviceList: VideoDeviceInfo[];
  audioDeviceList: AudioDeviceInfo[];
  setVideoDeviceInfo(device: VideoDeviceInfo): void;
  setAudioDeviceInfo(device: AudioDeviceInfo): void;
  mic: boolean;
  setMic(mic: boolean): void;
  setLocalVideoStream(stream: LocalVideoStream | undefined): void;
  localVideoRendererIsBusy: boolean;
  videoDeviceInfo: VideoDeviceInfo;
  audioDeviceInfo: AudioDeviceInfo;
  localVideoStream: LocalVideoStream;
  screenWidth: number;
  getUserForRoom(): Promise<TokenResponse[]>
  setSelectedUserToServer(selectedRoomUser: string): Promise<void>
  userForRoomWithRefreshToken: TokenResponse[]
  roomId: string
  setRoomId(): Promise<void>
}

export const Configuration = (props: ConfigurationScreenProps): JSX.Element => {
  const spinnerLabel = 'Initializing call client...';
  const buttonText = 'Start call';

  const createUserId = () => 'user' + Math.ceil(Math.random() * 1000);

  const [name, setName] = useState(createUserId());
  const [emptyWarning, setEmptyWarning] = useState(false);

  const {setupCallClient, setupCallAgent, unsupportedStateHandler, callClient} = props;

  const memoizedSetupCallClient = useCallback(() => setupCallClient(unsupportedStateHandler), [unsupportedStateHandler]);
    const RoomsPlaceholder = 'Select a user';
    const RoomLabel = 'Select a user from the room';
    const setSelectedUser = async () => {
        let roomUsers = await props.setSelectedUserToServer(selectedRoomUser);
        console.log(roomUsers);
    }
    const [roomUsers, setRoomUsers] = useState<TokenResponse[]>([]);
    const [roomUserdropdownItem, setRoomUserdropdownItem] = useState<TokenResponseDropDown[]>([]);
    const [roomDropdownDisable, setRoomDropdownDisable] = useState(true);
    const [startupButtonDisabled, setStartupButtonDisabled] = useState(true);
    const [selectedRoomUser, setSelectedRoomUser] = useState('');
    const [selectedUserToken, setSelectedUserToken] = useState<AzureCommunicationTokenCredential>();

    const createJoinRoom = async (): Promise<void> => {
        let roomUsers = await props.getUserForRoom();
        console.log(roomUsers);

        let users = props.userForRoomWithRefreshToken;
        setRoomUsers(users);

        await props.setRoomId();
        var dropdownItems: TokenResponseDropDown[] = []
        users.forEach((user, count) => {
            dropdownItems.push({ key: user.userId, text: "User-" + user.userId.split('-')[8], value: user.userId })
        })
        setRoomUserdropdownItem(dropdownItems)
        setRoomDropdownDisable(false)
    }

  useEffect(() => {
      memoizedSetupCallClient();
      createJoinRoom();
  }, [memoizedSetupCallClient, props.userForRoomWithRefreshToken]);

  return (
    <Stack className={mainContainerStyle} horizontalAlign="center" verticalAlign="center">
      {props.deviceManager ? (
        <Stack
          className={props.screenWidth > 750 ? fullScreenStyle : verticalStackStyle}
          horizontal={props.screenWidth > 750}
          horizontalAlign="center"
          verticalAlign="center"
          tokens={props.screenWidth > 750 ? configurationStackTokens : undefined}
        >
          <LocalPreview
            mic={props.mic}
            setMic={props.setMic}
            setLocalVideoStream={props.setLocalVideoStream}
            videoDeviceInfo={props.videoDeviceInfo}
            audioDeviceInfo={props.audioDeviceInfo}
            localVideoStream={props.localVideoStream}
            videoDeviceList={props.videoDeviceList}
            audioDeviceList={props.audioDeviceList}
          />
          <Stack className={localSettingsContainerStyle}>
          <DisplayNameField setName={setName} name={name} setEmptyWarning={setEmptyWarning} isEmpty={emptyWarning} />
          <Label style={labelStyle}>Room Id : {props.roomId}</Label>
            <Dropdown
                disabled={roomDropdownDisable}
                placeholder={RoomsPlaceholder}
                label={RoomLabel}
                          options={roomUserdropdownItem}
                styles={dropDownStyles}
                onChange={(...args): void => {
                    if (args[1] && args[1].key) {
                        let selectedValue = args[1].key.toString();
                        setSelectedRoomUser(selectedValue)
                        let token: AzureCommunicationTokenCredential = roomUsers.find(a => a.userId == selectedValue)!.tokenCredential
                        setSelectedUserToken(token)
                        setStartupButtonDisabled(false);
                    }
                }}
            />
            <div>
              <LocalSettings
                videoDeviceList={props.videoDeviceList}
                audioDeviceList={props.audioDeviceList}
                audioDeviceInfo={props.audioDeviceInfo}
                videoDeviceInfo={props.videoDeviceInfo}
                setVideoDeviceInfo={props.setVideoDeviceInfo}
                setAudioDeviceInfo={props.setAudioDeviceInfo}
                deviceManager={props.deviceManager}
              />
            </div>
            <div>
            <PrimaryButton
                disabled={startupButtonDisabled}
                className={buttonStyle}
                onClick={async () => {
                  if (!name) {
                    setEmptyWarning(true);
                  } else {
                      setEmptyWarning(false);
                      await setSelectedUser();

                      await setupCallAgent(selectedUserToken!, callClient, name);
                    props.startCallHandler();
                  }
                }}
              >
                <VideoCameraEmphasisIcon className={videoCameraIconStyle} size="medium" />
                {buttonText}
              </PrimaryButton>
            </div>
          </Stack>
        </Stack>
      ) : (
        <Spinner label={spinnerLabel} ariaLive="assertive" labelPosition="top" />
      )}
    </Stack>
  );
};
