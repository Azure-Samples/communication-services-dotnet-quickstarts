const getLineBreak = () => document.createElement("br");

const getLabel = (textContent) => {
  const label = document.createElement("label");
  label.textContent = textContent;

  return label;
};

const getOption = (value, textContent) => {
  const option = document.createElement("option");
  option.value = value;
  option.textContent = textContent;

  return option;
};

const getSelect = (id, options, ariaLabel) => {
  const select = document.createElement("select");

  options.forEach((option) => {
    select.appendChild(getOption(option.value, option.textContent));
  });
  select.id = id;
  select.ariaLabel = ariaLabel;

  return select;
};

const getButton = (textContent) => {
  const button = document.createElement("button");
  button.textContent = textContent;

  return button;
};

const showLibraryVersion = () => {
  fetch("/version")
    .then((response) => response.json())
    .then((data) => {
      const libraryVersion = document.createElement("h4");
      libraryVersion.textContent = `${data.library}: ${data.version}`;

      document.querySelector(".info").appendChild(libraryVersion);
    })
    .catch((error) => {
      console.error("Error fetching version:", error);
    });
};

const isUriValid = (uri) => {
  try {
    const url = new URL(uri);

    // Check if the URL has a valid scheme (http or https)
    if (url.protocol !== "http:" && url.protocol !== "https:") {
      return false;
    }

    return true;
  } catch (_) {
    return false;
  }
};

const communicationConfig = {
  acsConnectionString: {
    label: "ACS Connection String",
    type: "password",
    isValid: (acsConnectionString) => {
      if (acsConnectionString === "") {
        return true;
      }

      return /^endpoint=https:\/\/[^;]+;accesskey=[^;]+$/.test(
        acsConnectionString
      );
    },
  },
  acsPhoneNumber: {
    label: "ACS Phone Number",
    type: "password",
    isValid: (acsPhoneNumber) => {
      if (acsPhoneNumber === "") {
        return true;
      }

      return /^\+\d{10,15}$/.test(acsPhoneNumber);
    },
  },
  callbackUriHost: {
    label: "Callback URI Host",
    type: "password",
    isValid: (callbackUriHost) => {
      if (callbackUriHost === "") {
        return true;
      }

      return isUriValid(callbackUriHost);
    },
  },
  pmaEndpoint: {
    label: "PMA Endpoint",
    type: "password",
    isValid: (pmaEndpoint) => {
      if (pmaEndpoint === "") {
        return true;
      }

      return isUriValid(pmaEndpoint);
    },
  },
};

const showCommunicationConfig = () => {
  // Load saved config from local storage
  const savedCommunicationConfig = JSON.parse(
    localStorage.getItem("communicationConfig")
  ) || {
    acsConnectionString: "",
    acsPhoneNumber: "",
    callbackUriHost: "",
    pmaEndpoint: "",
  };

  const communicationConfigTitle = document.createElement("div");
  communicationConfigTitle.classList.add("communication-config-title");

  const communicationLabel = document.createElement("h3");
  communicationLabel.textContent = "Configuration";
  const communicationIcon = document.createElement("span");
  communicationIcon.title =
    "Secrets will be stored locally in your browser and sent in HTTP headers over HTTPS. Use only on trusted devices. They may be visible to other users on the same device and accessible to browser extensions. Do not use in production environments.";
  communicationIcon.textContent = "ⓘ";

  communicationConfigTitle.appendChild(communicationLabel);
  communicationConfigTitle.appendChild(communicationIcon);

  const communicationConfigContainer = document.createElement("div");
  communicationConfigContainer.classList.add("communication-config-container");

  Object.keys(communicationConfig).forEach((key) => {
    const label = getLabel(communicationConfig[key].label);
    label.classList.add("communication-config-parameter-name");
    communicationConfigContainer.appendChild(label);

    communicationConfigContainer.appendChild(getLineBreak());

    const input = document.createElement("input");
    input.id = key;
    input.placeholder = "Default";
    input.type = communicationConfig[key].type;

    input.ariaLabel = communicationConfig[key].label;

    // Set saved value if exists
    if (savedCommunicationConfig[key] != null) {
      input.value = savedCommunicationConfig[key];
    }

    input.classList.add("communication-config-parameter-input");

    // Save to local storage on change
    input.addEventListener("change", () => {
      // Validate the input value
      if (communicationConfig[key].isValid(input.value)) {
        savedCommunicationConfig[key] = input.value;
        localStorage.setItem(
          "communicationConfig",
          JSON.stringify(savedCommunicationConfig)
        );

        input.classList.remove("input-error");
      } else {
        savedCommunicationConfig[key] = "";
        localStorage.setItem(
          "communicationConfig",
          JSON.stringify(savedCommunicationConfig)
        );

        input.classList.add("input-error");
      }
    });

    communicationConfigContainer.appendChild(input);

    communicationConfigContainer.appendChild(getLineBreak());
  });

  let elapsed = 0;
  const maxWait = 10000;
  const appendCommunicationConfig = setInterval(() => {
    const topbar = document.querySelector(".information-container");
    if (topbar) {
      topbar.appendChild(communicationConfigTitle);
      topbar.appendChild(communicationConfigContainer);

      showLibraryVersion();

      clearInterval(appendCommunicationConfig);
    }

    elapsed += 300;

    if (elapsed >= maxWait) {
      clearInterval(appendCommunicationConfig);
    }
  }, 300);
};

showCommunicationConfig();

const inboundCallConfig = {
  audioChannelMixed: {
    label: "Audio Channel Mixed",
    type: "bool",
  },
  audioFormat16k: {
    label: "Audio Format 16k",
    type: "bool",
  },
  mediaStreaming: {
    label: "Media Streaming",
    type: "bool",
  },
  bidirectionalStreaming: {
    label: "Bidirectional Streaming",
    type: "bool",
  },
};

const modal = document.querySelector("#modal");
const modalContent = document.querySelector("#modal-content");
window.addEventListener("click", (event) => {
  if (event.target === modal) {
    // Clear modal content
    modalContent.innerHTML = "";

    modal.style.display = "none";
  }
});

const showInboundCallConfig = (eventData) => {
  // Load saved config from local storage
  const savedInboundCallConfig = JSON.parse(
    localStorage.getItem("inboundCallConfig")
  ) || {
    audioChannelMixed: true,
    audioFormat16k: true,
    mediaStreaming: true,
    bidirectionalStreaming: true,
  };

  const inboundCallConfigTitle = document.createElement("div");
  inboundCallConfigTitle.classList.add("inbound-call-config-title");

  const inboundCallLabel = document.createElement("h3");
  inboundCallLabel.textContent = "Incoming Call";
  const inboundCallIcon = document.createElement("h3");
  inboundCallIcon.classList.add("phone");
  inboundCallIcon.textContent = "📞";

  inboundCallConfigTitle.appendChild(inboundCallLabel);
  inboundCallConfigTitle.appendChild(inboundCallIcon);

  const inboundCallConfigContainer = document.createElement("div");
  inboundCallConfigContainer.classList.add("inbound-call-config-container");

  Object.keys(inboundCallConfig).forEach((key) => {
    const label = getLabel(inboundCallConfig[key].label);
    label.classList.add("inbound-call-config-parameter-name");
    inboundCallConfigContainer.appendChild(label);

    inboundCallConfigContainer.appendChild(getLineBreak());

    const select = getSelect(
      key,
      [
        { value: "true", textContent: "true" },
        { value: "false", textContent: "false" },
      ],
      inboundCallConfig[key].label
    );

    // Set saved value if exists
    if (savedInboundCallConfig[key] != null) {
      select.value = savedInboundCallConfig[key].toString();
    }

    select.classList.add("inbound-call-config-parameter-option");

    // Save to local storage on change
    select.addEventListener("change", () => {
      // Parse the value to boolean
      savedInboundCallConfig[key] = select.value === "true";
      localStorage.setItem(
        "inboundCallConfig",
        JSON.stringify(savedInboundCallConfig)
      );
    });

    inboundCallConfigContainer.appendChild(select);

    inboundCallConfigContainer.appendChild(getLineBreak());
  });

  const acceptButton = getButton("ACCEPT");
  acceptButton.classList.add("inbound-call-config-accept-button");

  acceptButton.addEventListener("click", () => {
    const {
      audioChannelMixed,
      audioFormat16k,
      mediaStreaming,
      bidirectionalStreaming,
    } = savedInboundCallConfig;

    // Call API to accept the call
    const acceptCallUrl = `/api/events/incomingcall?audioChannelMixed=${audioChannelMixed}&audioFormat16k=${audioFormat16k}&mediaStreaming=${mediaStreaming}&bidirectionalStreaming=${bidirectionalStreaming}`;

    fetch(acceptCallUrl, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: eventData,
    });

    modal.style.display = "none";
  });

  const inboundCallConfigActions = document.createElement("div");
  inboundCallConfigActions.classList.add("inbound-call-config-actions");

  inboundCallConfigActions.appendChild(acceptButton);

  modalContent.appendChild(inboundCallConfigTitle);
  modalContent.appendChild(inboundCallConfigContainer);
  modalContent.appendChild(inboundCallConfigActions);

  modal.style.display = "block";
};

const evtSource = new EventSource("/sse/connect");
evtSource.onmessage = (event) => {
  const eventData = event.data;

  console.log("Incoming call event received:", JSON.parse(eventData));

  showInboundCallConfig(eventData);

  setTimeout(() => {
    modal.style.display = "none";
  }, 45000);
};

evtSource.onerror = (err) => {
  console.error("SSE error:", err);
  evtSource.close();
};
