import { CallClient, CallAgent } from "@azure/communication-calling";
import { AzureCommunicationTokenCredential } from '@azure/communication-common';

let call;
let callAgent;

const calleePhoneInput = document.getElementById("callee-phone-input");
const callPhoneButton = document.getElementById("call-phone-button");
const hangUpPhoneButton = document.getElementById("hang-up-phone-button");

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

async function init() {
    const callClient = new CallClient();
    const tokenCredential = new AzureCommunicationTokenCredential('eyJhbGciOiJSUzI1NiIsImtpZCI6IjEwNiIsIng1dCI6Im9QMWFxQnlfR3hZU3pSaXhuQ25zdE5PU2p2cyIsInR5cCI6IkpXVCJ9.eyJza3lwZWlkIjoiYWNzOjUyOWM3YjcyLTdjMzQtNGRkYi05ZTc4LTEzMThiZWJjMWU0ZF8wMDAwMDAxMi0zYWJjLTkwYmUtM2VmMC04YjNhMGQwMDc2ZDgiLCJzY3AiOjE3OTIsImNzaSI6IjE2NTYxNjA0NDciLCJleHAiOjE2NTYyNDY4NDcsImFjc1Njb3BlIjoiY2hhdCx2b2lwIiwicmVzb3VyY2VJZCI6IjUyOWM3YjcyLTdjMzQtNGRkYi05ZTc4LTEzMThiZWJjMWU0ZCIsImlhdCI6MTY1NjE2MDQ0N30.S-oJwAFsI_0R4Eg6ynZrRyOEqLRZeh8tWjHQF94ZnwoYTCsd7-D6F-Ic1a4UfF7rTP_EWkZnpiGwvEpp7dw-xyTqN5OAR9i8XHVEANEgib-6WWJt_iQ_yHuSSzPNArTPYavN8wOyLE4TLagrT0ZZDQGLc-iNtX1MHrFHVVLK2ZU-Ux8WHnLr1pvbJGTlC2nMU7fGxqQwgnsEdh_HVe3qIAZOtzWiXnaEp9qHXw7B288-hRxTG5HXphEY_qvz3v6Uvg8bhPU4zSe2SgmV22DsPqvcAQAW5q3tBR0pUUNLGzKFr22IxW1taas2kHRxOhuVxPFCRfJBKOmrjRToLBoaoA');
    callAgent = await callClient.createCallAgent(tokenCredential);
    //callPhoneButton.disabled = false;
}

init();

callPhoneButton.addEventListener("click", () => {
    // start a call to phone
    const phoneToCall = calleePhoneInput.value;
    call = callAgent.startCall(
      [{phoneNumber: phoneToCall}], { alternateCallerId: {phoneNumber: '+18772178780'}
    });
    // toggle button states
    hangUpPhoneButton.disabled = false;
    callPhoneButton.disabled = true;
  });

  hangUpButton.addEventListener("click", () => {
    // end the current call
    call.hangUp({ forEveryone: true });
  
    // toggle button states
    hangUpButton.disabled = true;
    callButton.disabled = false;
    submitToken.disabled = false;
  });