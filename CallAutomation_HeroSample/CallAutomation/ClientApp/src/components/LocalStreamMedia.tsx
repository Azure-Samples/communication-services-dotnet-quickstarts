// © Microsoft Corporation. All rights reserved.

import React, { useEffect, useState, useRef } from 'react';
import { Image, ImageFit, Label } from '@fluentui/react';
import { LocalVideoStream, VideoStreamRenderer, VideoStreamRendererView } from '@azure/communication-calling';
import { videoHint, mediaContainer, localVideoContainerStyle } from './styles/StreamMedia.styles';
import { Constants } from '../core/constants';
import staticMediaSVG from '../assets/staticmedia.svg';

export interface LocalStreamMediaProps {
  label: string;
  stream: LocalVideoStream;
}

export const LocalStreamMedia = (props: LocalStreamMediaProps): JSX.Element => {
  const rendererView = useRef<undefined | VideoStreamRendererView>();

  const [activeStreamBeingRendered, setActiveStreamBeingRendered] = useState(false);

  const imageProps = {
    src: staticMediaSVG.toString(),
    imageFit: ImageFit.contain,
    styles: {
      root: {
        width: '100%',
        height: '100%',
        display: activeStreamBeingRendered ? 'none' : 'block'
      }
    }
  };

  const { stream, label } = props;

  useEffect(() => {
    (async () => {
      if (stream) {
        const renderer: VideoStreamRenderer = new VideoStreamRenderer(stream);
        rendererView.current = await renderer.createView({ scalingMode: 'Crop', isMirrored: true  });

        const container = document.getElementById(Constants.LOCAL_VIDEO_PREVIEW_ID);

        if (container && container.childElementCount === 0) {
          container.appendChild(rendererView.current.target);
          setActiveStreamBeingRendered(true);
        }
      } else {
        if (rendererView.current) {
          rendererView.current.dispose();
          setActiveStreamBeingRendered(false);
        }
      }
    })();

    return () => {
      if (rendererView.current) {
        rendererView.current.dispose();
        rendererView.current = undefined;
        setActiveStreamBeingRendered(false);
      }
    };
  }, [stream]);

  return (
    <div className={mediaContainer}>
      <div
        style={{ display: activeStreamBeingRendered ? 'block' : 'none' }}
        className={localVideoContainerStyle}
        id={Constants.LOCAL_VIDEO_PREVIEW_ID}
      />
      <Image {...imageProps}/>
      <Label className={videoHint}>{label}</Label>
    </div>
  );
};
