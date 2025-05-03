const { EmailClient } = require("@azure/communication-email");
require("dotenv").config();

// This code demonstrates how to fetch your connection string
// from an environment variable.
const connectionString = process.env['https://verizann-media.communication.azure.com/;accesskey=EQHWAYFO9E0NNcj8OZEFHcVtFWUa1EBEV4tsgX1ej53kJjv4v9ZgBLVotnwhKRtjTxdIf2UEq4xoJ5n/on5IYA==
'];
async function main() {
    try {
      var client = new EmailClient(connectionString);
      //send mail
      const emailMessage = {
        sender: "DoNotReply@402eaf99-ad29-431c-94fb-389dd5ab257a.azurecomm.net",
        content: {
          subject: "Welcome to Azure Communication Service Email.",
          plainText: "<This email message is sent from Azure Communication Service Email using JS SDK.>"
        },
        recipients: {
          to: [
            {
              email: "<anthony.robinson@lookinnovative.com>",
            },
          ],
        },
      };
      var response = await client.send(emailMessage);
    } catch (e) {
      console.log(e);
    }
  }
  main();

  const messageId = response.messageId;
  if (messageId === null) {
    console.log("Message Id not found.");
    return;
  }

    // check mail status, wait for 5 seconds, check for 60 seconds.
    let counter = 0;
    const statusInterval = setInterval(async function () {
      counter++;
      try {
        const response = await client.getSendStatus(messageId);
        if (response) {
          console.log(`Email status for ${messageId}: ${response.status}`);
          if (response.status.toLowerCase() !== "queued" || counter > 12) {
            clearInterval(statusInterval);
          }
        }
      } catch (e) {
        console.log(e);
      }
    }, 5000);

    