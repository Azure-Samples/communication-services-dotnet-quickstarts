import { Link, MessageBar } from '@fluentui/react';
import React, { useEffect, useState } from 'react';
import { Constants } from 'core/constants';

export interface ComplianceBannerProps {
  callTranscribeState: boolean;
  callRecordState: boolean;
  getRecordingLink(): Promise<string>;
  setRecordingLink(): void;
  recordingLink: string;
}
export const ComplianceBanner = (props: ComplianceBannerProps): JSX.Element => {
  const [previousCallTranscribeState, setPreviousCallTranscribeState] = useState(false);
  const [previousCallRecordState, setPreviousCallRecordState] = useState(false);
  const [variant, setVariant] = useState(0);
  const { callTranscribeState, callRecordState } = props;

  const onDownloadClick = (): void => {
    if (props.recordingLink && props.recordingLink.trim()) {
      window.open(props.recordingLink, '_blank', 'noreferrer');
    } else {
      props.getRecordingLink().then((downloadLink) => {
        if (downloadLink && downloadLink.trim()) {
          window.open(downloadLink, '_blank', 'noreferrer');
        }
      });
    }
  };

  useEffect(() => {
    if (previousCallRecordState && previousCallTranscribeState) {
      if (callRecordState && !callTranscribeState) {
        setVariant(Constants.TRANSCRIPTION_STOPPED_STILL_RECORDING);
      } else if (!callRecordState && callTranscribeState) {
        setVariant(Constants.RECORDING_STOPPED_STILL_TRANSCRIBING);
      } else if (!callRecordState && !callTranscribeState) {
        setVariant(Constants.RECORDING_AND_TRANSCRIPTION_STOPPED);
      } else {
        setVariant(Constants.NO_STATE);
      }
    } else if (previousCallRecordState && !previousCallTranscribeState) {
      if (callRecordState && callTranscribeState) {
        setVariant(Constants.RECORDING_AND_TRANSCRIPTION_STARTED);
      } else if (!callRecordState && callTranscribeState) {
        setVariant(Constants.TRANSCRIPTION_STARTED);
      } else if (!callRecordState && !callTranscribeState) {
        setVariant(Constants.RECORDING_STOPPED);
      } else {
        setVariant(Constants.NO_STATE);
      }
    } else if (!previousCallRecordState && previousCallTranscribeState) {
      if (callRecordState && callTranscribeState) {
        setVariant(Constants.RECORDING_AND_TRANSCRIPTION_STARTED);
      } else if (!callRecordState && callTranscribeState) {
        setVariant(Constants.RECORDING_STARTED);
      } else if (!callRecordState && !callTranscribeState) {
        setVariant(Constants.RECORDING_STOPPED_AND_SAVED);
      } else {
        setVariant(Constants.NO_STATE);
      }
    } else if (!previousCallRecordState && !previousCallTranscribeState) {
      if (callRecordState && callTranscribeState) {
        setVariant(Constants.RECORDING_AND_TRANSCRIPTION_STARTED);
      } else if (callRecordState && !callTranscribeState) {
        setVariant(Constants.RECORDING_STARTED);
      } else if (!callRecordState && callTranscribeState) {
        setVariant(Constants.TRANSCRIPTION_STARTED);
      } else {
        setVariant(Constants.NO_STATE);
      }
    }

    setPreviousCallTranscribeState(callTranscribeState);
    setPreviousCallRecordState(callRecordState);
    props.setRecordingLink();
  }, [callTranscribeState, callRecordState]);

  function PrivacyPolicy(): JSX.Element {
    return (
      <Link
        href="https://privacy.microsoft.com/en-US/privacystatement#mainnoticetoendusersmodule"
        target="_blank"
        underline
      >
        Privacy policy
      </Link>
    );
  }

  function LearnMore(): JSX.Element {
    return (
      <Link
        href="https://support.microsoft.com/en-us/office/record-a-meeting-in-teams-34dfbe7f-b07d-4a27-b4c6-de62f1348c24"
        target="_blank"
        underline
      >
        Learn more
      </Link>
    );
  }

  function DownloadRecording(): JSX.Element {
    return (
      //Get recording link from controller
      <Link onClick={onDownloadClick} underline href="#">
        Link
      </Link>
    );
  }

  function getBannerMessage(variant: number): JSX.Element {
    switch (variant) {
      case Constants.TRANSCRIPTION_STOPPED_STILL_RECORDING:
        return (
          <>
            <b>Transcription has stopped.</b> You are now only recording this meeting.
            <PrivacyPolicy />
          </>
        );
      case Constants.RECORDING_STOPPED_STILL_TRANSCRIBING:
        return (
          <>
            <b>Recording has stopped.</b> You are now only transcribing this meeting.
            <PrivacyPolicy />
          </>
        );
      case Constants.RECORDING_AND_TRANSCRIPTION_STOPPED:
        return (
          <>
            <b>Recording and transcription are being saved. </b> Recording and transcription have stopped.
            <LearnMore />
          </>
        );
      case Constants.RECORDING_AND_TRANSCRIPTION_STARTED:
        return (
          <>
            <b>Recording and transcription have started.</b> By joining, you are giving consent for this meeting to be
            transcribed.
            <PrivacyPolicy />
          </>
        );
      case Constants.TRANSCRIPTION_STARTED:
        return (
          <>
            <b>Transcription has started.</b> By joining, you are giving consent for this meeting to be transcribed.
            <PrivacyPolicy />
          </>
        );
      case Constants.RECORDING_STOPPED:
        return (
          <>
            <b>Recording is being saved.</b> Recording has stopped. You can download the recording from
            <DownloadRecording />.
          </>
        );
      case Constants.RECORDING_STARTED:
        return (
          <>
            <b>Recording has started.</b> By joining, you are giving consent for this meeting to be transcribed.
            <PrivacyPolicy />
          </>
        );
      case Constants.RECORDING_STOPPED_AND_SAVED:
        return (
          <>
            <b>Transcription is being saved.</b> Transcription has stopped.
            <LearnMore />
          </>
        );
    }
    return <></>;
  }

  return variant === Constants.NO_STATE ? (
    <></>
  ) : (
    <MessageBar
      onDismiss={(): void => {
        setVariant(Constants.NO_STATE);
      }}
      dismissButtonAriaLabel="Close"
    >
      {getBannerMessage(variant)}
    </MessageBar>
  );
};
