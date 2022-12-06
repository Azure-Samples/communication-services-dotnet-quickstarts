export class Constants {
  static INCOMING = 'Incoming';
  static RINGING = 'Ringing';
  static CONNECTING = 'Connecting';
  static CONNECTED = 'Connected';
  static LOBBY = 'InLobby';
  static DEFAULT_IMG_WIDTH = 200;
  static CROP_MEDIA = 'Crop';
  static CONFIGURATION_LOCAL_VIDEO_PREVIEW_ID = 'ConfigurationLocalVideoPreview';
  static LOCAL_VIDEO_PREVIEW_ID = 'LocalVideoPreview';
  static MINI_HEADER_WINDOW_WIDTH = 680;
  // For safari browsers we want to set the default to 1 remote participant that can show their video based on quality constraints
  static DOMINANT_PARTICIPANTS_COUNT_SAFARI = 1;
  // For non-safari browsers we want to set the default to 4 remote participant that can show their video based on quality constraints
  static DOMINANT_PARTICIPANTS_COUNT = 4;

  //Recording states
  static NO_STATE = 0;
  static TRANSCRIPTION_STOPPED_STILL_RECORDING = 1;
  static RECORDING_STOPPED_STILL_TRANSCRIBING = 2;
  static RECORDING_AND_TRANSCRIPTION_STOPPED = 3;
  static RECORDING_AND_TRANSCRIPTION_STARTED = 4;
  static TRANSCRIPTION_STARTED = 5;
  static RECORDING_STOPPED = 6;
  static RECORDING_STARTED = 7;
  static RECORDING_STOPPED_AND_SAVED = 8;

  static START_RECORDING = 'Start recording';
  static START_AUDIO_RECORDING = 'Start audio-only recording';
  static STOP_RECORDING = 'Stop recording';
  static SELECT_RECORDING_FORMAT = 'Select Recording Format';
  static START = 'Start';
  static STARTED = 'Started';
  static STOPPED = 'Stopped';
}

export enum RecordingKeys {
  StartRecording = 'StartRecording',
  StartAudioRecording = 'StartAudioRecording',
  RecordingFormat = 'RecordingFormat',
  StopRecording = 'StopRecording'
}

export enum RecordingOptions {
  Mp3 = 'Mp3',
  Wav = 'Wav'
}
