import React, { useMemo, useState } from 'react';
import {
  mediaGalleryGridStyle,
  mediaGalleryStyle,
  mediaGallerySubstageStyle,
  substageMediaGalleryStyle
} from './styles/MediaGallery.styles';
import { RemoteParticipant, LocalVideoStream } from '@azure/communication-calling';
import { utils } from '../Utils/Utils';
import { LocalStreamMedia } from './LocalStreamMedia';
import { RemoteStreamMedia } from './RemoteStreamMedia';
import { SelectionState } from 'core/RemoteStreamSelector';
import { Stack } from '@fluentui/react';
import { Constants } from '../core/constants';

export interface MediaGalleryProps {
  userId: string;
  displayName: string;
  remoteParticipants: RemoteParticipant[];
  localVideoStream: LocalVideoStream;
  dominantParticipants: SelectionState[];
}

export const MediaGallery = (props: MediaGalleryProps): JSX.Element => {
  const [gridCol, setGridCol] = useState(1);
  const [gridRow, setGridRow] = useState(1);


  // For now we are only going to support up to a 4x3 grid or 10 participants in a call
  // Since this is a sample, we will just hard-code how we want the grid to scale
  // the rows and columns for the number of users in the call
  const rows          = [1, 1, 2, 2, 2, 2, 3, 3, 3, 3];
  const cols          = [1, 2, 2, 2, 3, 3, 3, 3, 3, 4];

  const dominantParticipantCount = utils.isSafari() ? Constants.DOMINANT_PARTICIPANTS_COUNT_SAFARI : Constants.DOMINANT_PARTICIPANTS_COUNT;

  if (dominantParticipantCount < 0 || dominantParticipantCount > rows.length - 1) {
    console.warn(`Please use a value for dominant participants between 0 <= x <= ${rows.length - 1}`);
  }

  const numRemoteParticipantsToRender = Math.min(dominantParticipantCount, rows.length - 1);

  const clamp = (num: number, min: number, max: number) => Math.min(Math.max(num, min), max);

  const getMediaGalleryTilesForParticipants = (
    participants: RemoteParticipant[],
    displayName: string
  ): JSX.Element[] => {
    const remoteParticipantsMediaGalleryItems = participants.map((participant) => (
      <div key={`${utils.getId(participant.identifier)}-tile`} className={mediaGalleryStyle}>
        <RemoteStreamMedia
          key={utils.getId(participant.identifier)}
          stream={participant.videoStreams[0]}
          isParticipantStreamSelected={
            props.dominantParticipants.filter((p) => p.participantId === utils.getId(participant.identifier)).length > 0
          }
          label={participant.displayName ?? utils.getId(participant.identifier)}
        />
      </div>
    ));

    // for now we will always add the local user to the main stage
    const localParticipantMediaGalleryItem = (
      <div key="localParticipantTile" className={mediaGalleryStyle}>
        <LocalStreamMedia label={displayName} stream={props.localVideoStream} />
      </div>
    );

    remoteParticipantsMediaGalleryItems.unshift(localParticipantMediaGalleryItem);

    return remoteParticipantsMediaGalleryItems;
  };

  const getSubstageMediaGalleryTilesForParticipants = (participants: RemoteParticipant[]): JSX.Element[] => {
    const remoteParticipantsMediaGalleryItems = participants.map((participant) => (
      <div key={`${utils.getId(participant.identifier)}-tile`} className={substageMediaGalleryStyle}>
        <RemoteStreamMedia
          key={utils.getId(participant.identifier)}
          stream={participant.videoStreams[0]}
          isParticipantStreamSelected={false}
          label={participant.displayName ?? utils.getId(participant.identifier)}
        />
      </div>
    ));

    return remoteParticipantsMediaGalleryItems;
  };

  // determine number of rows/columns to add to the grid
  const numberStreamsToRender = useMemo(() => clamp(props.remoteParticipants.length, 0, numRemoteParticipantsToRender), [props.remoteParticipants.length])
  if (cols[numberStreamsToRender] !== gridCol) {
    if (numberStreamsToRender > cols.length - 1) {
      throw `attempting to set up a number of columns in the gallery for an unexpected number of participants ${numberStreamsToRender}`
    }
    setGridCol(cols[numberStreamsToRender]);
  }

  if (rows[numberStreamsToRender] !== gridRow) {
    if (numberStreamsToRender > rows.length - 1) {
      throw `attempting to set up a number of rows in the gallery for an expected unnumber of participants ${numberStreamsToRender}`
    }
    setGridRow(rows[numberStreamsToRender]);
  } 

  // sort by dominance
  const participantsToLayout = props.remoteParticipants.sort((a, b) => {
    const isParticipantADominant =
      props.dominantParticipants.filter((p) => p.participantId === utils.getId(a.identifier)).length > 0;
    const isParticipantBDominant =
      props.dominantParticipants.filter((p) => p.participantId === utils.getId(b.identifier)).length > 0;
    if (isParticipantADominant && !isParticipantBDominant) {
      return -1;
    } else if (!isParticipantADominant && isParticipantBDominant) {
      return 1;
    }
    return 0;
  });

  const mainStageParticipants = participantsToLayout.slice(0, numRemoteParticipantsToRender);
  const substageParticipants = participantsToLayout.slice(numRemoteParticipantsToRender);
  // don't show the substage if its not necessary
  const isSubstageVisible = substageParticipants.length > 0;
  return (
    <Stack style={{ height: '100%' }}>
      <div
        id="video-gallery"
        className={mediaGalleryGridStyle}
        style={{
          gridTemplateRows: `repeat(${gridRow}, minmax(0, 1fr))`,
          gridTemplateColumns: `repeat(${gridCol}, 1fr)`
        }}
      >
        {getMediaGalleryTilesForParticipants(mainStageParticipants, props.displayName)}
      </div>
      {isSubstageVisible && (
        <Stack horizontal className={mediaGallerySubstageStyle}>
          {getSubstageMediaGalleryTilesForParticipants(substageParticipants)}
        </Stack>
      )}
    </Stack>
  );
};