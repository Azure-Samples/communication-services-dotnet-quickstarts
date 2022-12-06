import { BlobServiceClient } from '@azure/storage-blob';
import { utils } from 'Utils/Utils';
import { Feedback } from './Feedback';

const getCanvasBlob = (canvas: HTMLCanvasElement): Promise<Blob | null> => {
  return new Promise((resolve) => {
    canvas.toBlob((blob) => {
      resolve(blob);
    }, 'image/png');
  });
};

export const uploadFeedback = async (
  feedback: Feedback,
  screenShotCanvas: HTMLCanvasElement | undefined
): Promise<void> => {
  try {
    const settings = await utils.getFeedbackSettings();

    const blobServiceClient = new BlobServiceClient(settings.sasUri);
    const containerClient = blobServiceClient.getContainerClient(settings.containerName);
    const blockBlobClient = containerClient.getBlockBlobClient(`logs-${feedback.feedbackId}.json`);
    const serializedFeedback = JSON.stringify(feedback, null, 4);
    const logPromise = blockBlobClient.upload(serializedFeedback, serializedFeedback.length);
    let screenShotPromise = undefined;
    if (screenShotCanvas) {
      const blockBlobClient = containerClient.getBlockBlobClient(`screenShot-${feedback.feedbackId}.png`);
      const screenShotBlob = await getCanvasBlob(screenShotCanvas);
      if (screenShotBlob != null) {
        screenShotPromise = blockBlobClient.uploadData(screenShotBlob);
      }
    }
    await Promise.all([logPromise, screenShotPromise]);
  } catch (error) {
    console.log(error);
  }
};
