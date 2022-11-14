import { CallClient } from "@azure/communication-calling";
import { AzureCommunicationTokenCredential } from '@azure/communication-common';

let call;
let callAgent;
let tokenCredential;
const userToken = document.getElementById("token-input"); 
const calleeInput = document.getElementById("callee-id-input");
const submitToken = document.getElementById("token-submit");
const callButton = document.getElementById("call-button");
const hangUpButton = document.getElementById("hang-up-button");

submitToken.addEventListener("click", async () => {
  const callClient = new CallClient(); 
  const userTokenCredential = userToken.value;
    try {
      tokenCredential = new AzureCommunicationTokenCredential(userTokenCredential);
      callAgent = await callClient.createCallAgent(tokenCredential);
      callButton.disabled = false;
      submitToken.disabled = true;
    } catch(error) {
      window.alert("Please submit a valid token!");
    }
})

callButton.addEventListener("click", () => {
  // start a call
  const userToCall = calleeInput.value;

  // To call an Azure Communication Services communication user, use {communicationUserId: 'ACS_USER_ID'}.
  // To call echo bot, use {id: '8:echo123'}.
  call = callAgent.startCall(
      [{ id: userToCall }],
      {}
  );
  // toggle button states
  hangUpButton.disabled = false;
  callButton.disabled = true;
});

hangUpButton.addEventListener("click", () => {
  // end the current call
  call.hangUp({ forEveryone: true });

  // toggle button states
  hangUpButton.disabled = true;
  callButton.disabled = false;
  submitToken.disabled = false;
});