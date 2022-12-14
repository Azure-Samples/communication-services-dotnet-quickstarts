import { connect } from 'react-redux';
import { CallRecording } from '../components/CallRecording';
import { State } from '../core/reducers';
import { startAudioRecord, startRecord, stopRecord } from '../core/sideEffects';

const mapStateToProps = (state: State) => ({
  conversationId: state.calls.serverCallId,
  recordingStatus: state.calls.recordingStatus
});

const mapDispatchToProps = (dispatch: any) => ({
  startRecording: (): void => dispatch(startRecord()),
  startAudioRecording: (recordingFormat: string): void => dispatch(startAudioRecord(recordingFormat)),
  stopRecording: (): void => dispatch(stopRecord())
});

const connector = connect(mapStateToProps, mapDispatchToProps);
export default connector(CallRecording);
