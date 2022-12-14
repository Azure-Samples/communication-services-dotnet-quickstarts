import React, { useEffect, useState, useRef } from 'react';
import { ParticipantStream } from '../core/reducers';
import { hiddenFullScreenStyle, fullScreenStyle, loadingStyle } from './styles/MediaFullScreen.styles';
import { RemoteVideoStream, VideoStreamRenderer, VideoStreamRendererView } from '@azure/communication-calling';
import { Spinner, SpinnerSize } from '@fluentui/react';
import { utils } from 'Utils/Utils';

export interface MediaFullScreenProps {
  activeScreenShareStream: ParticipantStream;
}

export const MediaFullScreen = (props: MediaFullScreenProps): JSX.Element => {
  const [loading, setLoading] = useState(true);
  const fullScreenStreamMediaId = 'fullScreenStreamMediaId';
  const rendererView = useRef<VideoStreamRendererView | undefined>();

  /**
   * Start stream after DOM has rendered
   */

  const activeScreenShareStream = props.activeScreenShareStream;

  useEffect(() => {
    (async () => {
      if (activeScreenShareStream && activeScreenShareStream.stream) {
        const stream: RemoteVideoStream = activeScreenShareStream.stream;
        const renderer: VideoStreamRenderer = new VideoStreamRenderer(stream);
        rendererView.current = await renderer.createView({ scalingMode: 'Fit' });
  
        const container = document.getElementById(fullScreenStreamMediaId);
        if (container && container.childElementCount === 0) {
          setLoading(false);
          container.appendChild(rendererView.current.target);
        }
      } else {
        if (rendererView.current) {
          rendererView.current.dispose();
        }
      }
    })();
  }, [activeScreenShareStream]);

  const displayName =
    props.activeScreenShareStream.user.displayName ?? utils.getId(props.activeScreenShareStream.user.identifier);

  return (
    <>
      {loading && (
        <div className={loadingStyle}>
          <Spinner label={`Loading ${displayName}'s screen`} size={SpinnerSize.xSmall} />
        </div>
      )}
      <div id={fullScreenStreamMediaId} className={loading ? hiddenFullScreenStyle : fullScreenStyle}></div>
    </>
  );
};
