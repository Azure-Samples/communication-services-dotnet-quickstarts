import { store } from 'core/store';
import { Feedback } from 'feedbacks/Feedback';
import { getLogs } from 'feedbacks/logger';
import { v1 as createGUID } from 'uuid';

export const createFeedback = (feedbackType: string, comment: string): Feedback => {
  const callId = store.getState().calls.call?.id ?? 'Not Found';
  return {
    logs: getLogs(),
    callId: callId,
    comments: comment,
    feedbackId: createGUID(),
    type: feedbackType
  };
};
