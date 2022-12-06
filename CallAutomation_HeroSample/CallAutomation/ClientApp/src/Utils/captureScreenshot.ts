declare class ImageCapture {
  constructor(track: MediaStreamTrack);
  grabFrame: () => Promise<ImageBitmap>;
}

declare class MediaDevicesWithDisplayMedia extends MediaDevices {
  getDisplayMedia: (constraints?: MediaStreamConstraints | undefined) => Promise<MediaStream>;
}

export const captureScreenshot = async (): Promise<HTMLCanvasElement> => {
  const canvas = document.createElement('canvas');
  const context = canvas.getContext('2d');
  const captureStream = await (navigator.mediaDevices as MediaDevicesWithDisplayMedia).getDisplayMedia({
    video: true
  });
  const track = captureStream.getVideoTracks()[0];
  const capture = new ImageCapture(track);
  const bitmap = await capture.grabFrame();
  canvas.width = bitmap.width;
  canvas.height = bitmap.height;
  track.stop();
  context?.drawImage(bitmap, 0, 0, bitmap.width, bitmap.height);
  captureStream.getTracks().forEach((track): void => track.stop());
  return canvas;
};

export const isScreenShotAvailable = (): boolean => {
  return (
    !!(window?.navigator?.mediaDevices as MediaDevicesWithDisplayMedia)?.getDisplayMedia &&
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    !!(window as any).ImageCapture
  );
};
