import { connect } from 'react-redux';
import { ComplianceBanner } from '../components/ComplianceBanner';
import { recordingLink } from '../core/actions/calls';
import { State } from '../core/reducers';
import { getRecordLink } from '../core/sideEffects';

const mapStateToProps = (state: State) => ({
  conversationId: state.calls.serverCallId,
  recordingLink: state.calls.recordingLink
});

const mapDispatchToProps = (dispatch: any) => ({
  getRecordingLink: (): Promise<string> => dispatch(getRecordLink()),
  setRecordingLink: (): void => dispatch(recordingLink(''))
});

const connector: any = connect(mapStateToProps, mapDispatchToProps);
export default connector(ComplianceBanner);
