import { getTheme, mergeStyles } from '@fluentui/react';

const palette = getTheme().palette;
export const IconColor = mergeStyles({
  color: palette.neutralSecondary,
  selectors: {
    ':hover': { color: palette.blue }
  }
});

export const recordingItemStyle = mergeStyles({
  paddingLeft: '0.8125rem'
});

export const recordingIconStyle = mergeStyles({
  color: 'red',
  selectors: {
    ':hover': { color: 'red' }
  }
});

export const recordingIconDivStyle = mergeStyles({
  position: 'absolute',
  left: '10px',
  top: '16px'
});

export const recordingTextDivStyle = mergeStyles({
  position: 'relative',
  left: '10px',
  bottom: '5px',
  fontSize: '90%'
});
