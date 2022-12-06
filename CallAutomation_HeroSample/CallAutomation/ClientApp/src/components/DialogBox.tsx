// © Microsoft Corporation. All rights reserved.
import React from 'react';
import { Stack, MessageBar, MessageBarType, DefaultButton } from '@fluentui/react';
import { Dialog, DialogType, DialogFooter } from 'office-ui-fabric-react/lib/Dialog';
import { OkButtonStyles } from './styles/MediaControls.styles';

export interface DialogBoxProps {
  message: string;
  dismissDialogBox(): void;
  isDialogBoxVisiblile: boolean;
}
const modalPropsStyles = { main: { width: 340 } };
const dialogContentProps = {
  type: DialogType.normal,
  title: 'Error !'
};
export const DialogBox = (props: DialogBoxProps): JSX.Element => {
  const modalProps = React.useMemo(
    () => ({
      isBlocking: true,
      styles: modalPropsStyles
    }),
    []
  );

  return (
    <Stack>
      <Dialog
        hidden={!props.isDialogBoxVisiblile}
        onDismiss={props.dismissDialogBox}
        dialogContentProps={dialogContentProps}
        modalProps={modalProps}
      >
        <MessageBar messageBarType={MessageBarType.error} dismissButtonAriaLabel="Close">
          {props.message}
        </MessageBar>
        <DialogFooter className={OkButtonStyles}>
          <DefaultButton text="OK" onClick={props.dismissDialogBox} />
        </DialogFooter>
      </Dialog>
    </Stack>
  );
};
