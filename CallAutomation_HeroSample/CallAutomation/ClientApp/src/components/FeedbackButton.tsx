import {
  Checkbox,
  CommandButton,
  DefaultButton,
  Dialog,
  DialogFooter,
  DialogType,
  Dropdown,
  IDropdownOption,
  Label,
  PrimaryButton,
  TextField
} from '@fluentui/react';
import { uploadFeedback } from 'feedbacks/submitFeedback';
import React, { useCallback, useState } from 'react';
import { isScreenShotAvailable, captureScreenshot } from 'Utils/captureScreenshot';
import { createFeedback } from 'Utils/createFeedback';

type FeedbackButtonProps = {
  iconOnly?: boolean;
}

export const FeedbackButton = ({ iconOnly }: FeedbackButtonProps): JSX.Element => {
  const [hidden, setHidden] = useState(true);
  const togglePopup = useCallback(() => {
    setHidden(!hidden);
  }, [hidden]);

  return (
    <>
      <CommandButton
        key='Feedback'
        text={iconOnly ? undefined : 'Report a bug'}
        ariaLabel={iconOnly ? 'Report a bug': undefined}
        iconProps={{ iconName: 'Feedback' }}
        onClick={togglePopup}
      />
      {!hidden && <FeedbackPopup toggleHidden={togglePopup} />}
    </>
  );
};

type FeedbackPopupProps = {
  toggleHidden: () => void;
};

const options: IDropdownOption[] = [
  { key: 'Calling - Audio', text: 'Calling - Audio' },
  { key: 'Calling - Call drops', text: 'Calling - Call drops' },
  { key: 'Calling - Other', text: 'Calling - Other' },
  { key: 'Calling - Video', text: 'Calling - Video' },
  { key: 'Calling - Screen sharing', text: 'Calling - Screen sharing' },
  { key: 'Participants list', text: 'Participants list' },
  { key: 'Join experience', text: 'Join experience' },
  { key: 'App Crashing', text: 'App Crashing' },
  { key: 'Others', text: 'Others' }
];

const modalProps = {
  isBlocking: false,
  styles: { main: { maxWidth: 650, minWidth: 450 } }
};

const screenShotStyle = {
  width: '16.75rem',
  objectFit: 'cover' as const
};

const dialogContentProps = {
  type: DialogType.largeHeader,
  title: 'Feedback',
  subText: 'Something went wrong? Please provide feedback here!'
};

const FeedbackPopup = (props: FeedbackPopupProps): JSX.Element => {
  const [feedbackType, setFeedbackType] = useState<string>('');
  const [comment, setComment] = useState<string>('');
  const [guid, setGuid] = useState<string>('');
  const [isScreenShotDialogOn, setIsScreenShotDialogOn] = useState<boolean>(false);
  const [screenShot, setScreenShot] = useState<HTMLCanvasElement>();
  const [isHidden, setIsHidden] = useState<boolean>(false);

  const takeScreenShot = useCallback(async () => {
    const screenShot = await captureScreenshot();
    setScreenShot(screenShot);
  }, []);

  const submitFeedback = useCallback(async () => {
    const feedback = createFeedback(feedbackType, comment);
    await uploadFeedback(feedback, screenShot);
    setGuid(feedback.feedbackId);
  }, [feedbackType, comment, screenShot]);

  const feedbackInput = (
    <>
      <Label>What is this related to? (Required)</Label>
      <Dropdown
        defaultSelectedKey={'Calling - Audio'}
        options={options}
        onChange={(_, item): void => {
          setFeedbackType(item?.key as string);
        }}
      />
      <Label>What are you seeing? Has it always been that way? Any step to repro?</Label>
      <TextField
        onChange={(_, value): void => {
          setComment(value ?? '');
        }}
        value={comment}
        multiline
        rows={6}
      />
      <Checkbox
        label="Include screen shot"
        checked={!!screenShot}
        onChange={(_, value): void => (value ? setIsScreenShotDialogOn(value ?? false) : setScreenShot(undefined))}
      />
      {isScreenShotDialogOn && (
        <ScreenShotInstruction
          onDismiss={(): void => {
            setIsScreenShotDialogOn(false);
          }}
          onTakeScreenShot={takeScreenShot}
          setIsParentHidden={setIsHidden}
        />
      )}
      {screenShot && <img src={screenShot?.toDataURL()} style={screenShotStyle} alt='screen shot' />}
    </>
  );

  const feedbackSubmitted = (
    <>
      <Label>Please provide this guid to developers for targeting the case:</Label>
      <TextField value={guid} readOnly />
    </>
  );

  return (
    <Dialog
      dialogContentProps={dialogContentProps}
      modalProps={modalProps}
      hidden={isHidden}
      onDismiss={props.toggleHidden}
    >
      {!guid && feedbackInput}
      {guid && feedbackSubmitted}
      <DialogFooter>
        {!guid && <PrimaryButton onClick={submitFeedback} text="Send" />}
        <DefaultButton onClick={props.toggleHidden} text="Close" />
      </DialogFooter>
    </Dialog>
  );
};

const screenShotDialogContentProps = {
  type: DialogType.largeHeader,
  title: 'Take a screen shot',
  subText: isScreenShotAvailable()
    ? 'Ready to take a screen shot? Please click [Microsoft Edge tab]/[Chrome tab] in next popup and choose the current tab!'
    : 'Sorry, screen shot is not available for your browser.'
};

type ScreenShotPopupProps = {
  onTakeScreenShot: () => Promise<void>;
  onDismiss: () => void;
  setIsParentHidden: (value: boolean) => void;
};

const ScreenShotInstruction = ({ onTakeScreenShot, onDismiss, setIsParentHidden }: ScreenShotPopupProps): JSX.Element => {
  const [isHidden, setIsHidden] = useState<boolean>(false);
  const takeScreenShot = async (): Promise<void> => {
    setIsParentHidden(true);
    setIsHidden(true);
    try {
      await onTakeScreenShot();
    } catch { }

    setIsParentHidden(false);
    onDismiss();
  };
  return (
    <Dialog
      dialogContentProps={screenShotDialogContentProps}
      modalProps={modalProps}
      hidden={isHidden}
      onDismiss={onDismiss}
    >
      <DialogFooter>
        {isScreenShotAvailable() && <PrimaryButton onClick={takeScreenShot} text="I am ready!" />}
        <DefaultButton onClick={onDismiss} text="Never mind" />
      </DialogFooter>
    </Dialog>
  );
};
