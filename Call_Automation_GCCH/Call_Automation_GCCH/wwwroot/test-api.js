// ============================================================================
// CALL AUTOMATION TEST API - CLEAN ARCHITECTURE
// ============================================================================

// ============================================================================
// CONFIGURATION
// ============================================================================
const CONFIG = {
    API_BASE: '/api',
    API_VERSION: 'v2', // Clean Architecture endpoints
    ENDPOINTS: {
        // V2 Clean Architecture Endpoints - Calls
        CREATE_CALL: '/api/v2/calls/create',
        CREATE_GROUP_CALL: '/api/v2/calls/create-group',
        TRANSFER_CALL: '/api/v2/calls/transfer',
        HANGUP: '/api/v2/calls/hangup',

        // V2 Clean Architecture Endpoints - Media
        PLAY_AUDIO: '/api/v2/media/play',
        PLAY_TO_ALL: '/api/v2/media/play-to-all',
        HOLD: '/api/v2/media/hold',
        UNHOLD: '/api/v2/media/unhold',
        START_STREAMING: '/api/v2/media/start-streaming',
        STOP_STREAMING: '/api/v2/media/stop-streaming',
        CANCEL_MEDIA: '/api/v2/media/cancel-all',
        START_TRANSCRIPTION: '/api/v2/media/start-transcription',
        STOP_TRANSCRIPTION: '/api/v2/media/stop-transcription',

        // V2 Clean Architecture Endpoints - Participants
        ADD_PARTICIPANT: '/api/v2/participants/add',
        REMOVE_PARTICIPANT: '/api/v2/participants/remove',
        MUTE_PARTICIPANT: '/api/v2/participants/mute',
        LIST_PARTICIPANTS: '/api/v2/participants',
        GET_PARTICIPANT: '/api/v2/participants',

        // V2 Clean Architecture Endpoints - Recordings
        START_RECORDING: '/api/v2/recordings/start',
        PAUSE_RECORDING: '/api/v2/recordings/pause',
        RESUME_RECORDING: '/api/v2/recordings/resume',
        STOP_RECORDING: '/api/v2/recordings/stop',
        DOWNLOAD_RECORDING: '/api/v2/recordings/download',

        // Recognition endpoints (may still use legacy paths if not migrated)
        RECOGNIZE_DTMF: '/startRecognizeAsync',
        RECOGNIZE_SPEECH: '/startRecognizeAsync',
        RECOGNIZE_CHOICE: '/startRecognizeAsync'
    }
};

// ============================================================================
// API LAYER - HTTP Communication
// ============================================================================
class ApiService {
    static async call(endpoint, method = 'POST', body = null) {
        const options = {
            method: method,
            headers: {
                'Content-Type': 'application/json'
            }
        };

        if (body && method !== 'GET') {
            options.body = JSON.stringify(body);
        }

        try {
            const response = await fetch(endpoint, options);

            // Try to parse response as JSON first
            let data;
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                data = await response.json();
            } else {
                data = await response.text();
            }

            if (!response.ok) {
                // Extract detailed error message from various response formats
                let errorMessage = `HTTP ${response.status}: ${response.statusText}`;

                if (typeof data === 'object') {
                    // Handle different error response formats
                    if (data.error) {
                        errorMessage = data.error;
                    } else if (data.message) {
                        errorMessage = data.message;
                    } else if (data.title) {
                        errorMessage = data.title;
                        if (data.detail) {
                            errorMessage += ': ' + data.detail;
                        }
                    } else if (data.errors) {
                        // Handle validation errors
                        errorMessage = JSON.stringify(data.errors, null, 2);
                    } else {
                        errorMessage = JSON.stringify(data, null, 2);
                    }
                } else if (typeof data === 'string' && data.length > 0) {
                    errorMessage = data;
                }

                // Log detailed error to console for debugging
                console.error('API Error Details:', {
                    endpoint,
                    status: response.status,
                    statusText: response.statusText,
                    errorData: data,
                    errorMessage
                });

                throw new Error(errorMessage);
            }

            return { success: true, data };
        } catch (error) {
            // Log the full error for debugging
            console.error('API Call Failed:', {
                endpoint,
                method,
                body,
                error: error.message,
                stack: error.stack
            });

            return { success: false, error: error.message };
        }
    }
}

// ============================================================================
// UI LAYER - DOM Manipulation
// ============================================================================
class UIManager {
    static showPanel(panelId, eventObj) {
        document.querySelectorAll('.test-panel').forEach(panel => {
            panel.classList.remove('active');
        });

        document.querySelectorAll('.category-card').forEach(card => {
            card.classList.remove('active');
        });

        const panel = document.getElementById(panelId + '-panel');
        if (panel) panel.classList.add('active');

        // Use passed event object or fallback to global event
        const evt = eventObj || (typeof event !== 'undefined' ? event : null);
        if (evt?.currentTarget) {
            evt.currentTarget.classList.add('active');
        }
    }

    static switchTab(event, tabId) {
        if (!event || !event.target) {
            console.error('switchTab: event or event.target is null');
            return;
        }

        const parent = event.target.closest('.test-panel');
        if (!parent) {
            console.error('switchTab: Could not find parent .test-panel');
            return;
        }

        parent.querySelectorAll('.test-section').forEach(section => {
            section.classList.remove('active');
        });

        parent.querySelectorAll('.tab').forEach(tab => {
            tab.classList.remove('active');
        });

        const section = document.getElementById(tabId);
        if (section) section.classList.add('active');

        // Use currentTarget (the element with the event listener) instead of target
        const tabElement = event.currentTarget || event.target;
        if (tabElement) tabElement.classList.add('active');
    }

    static showLoading(sectionId) {
        const resultDiv = document.getElementById(sectionId + '-result');
        if (resultDiv) {
            resultDiv.style.display = 'block';
            resultDiv.className = 'result-panel';
            resultDiv.innerHTML = '<div class="spinner"></div> Processing request...';
        }
    }

    static showResult(sectionId, data, isError = false) {
        const resultDiv = document.getElementById(sectionId + '-result');
        if (resultDiv) {
            resultDiv.style.display = 'block';
            resultDiv.className = 'result-panel ' + (isError ? 'error' : 'success');

            // Enhanced error display
            if (isError) {
                let displayContent = '';

                // If data is a string (error message), format it nicely
                if (typeof data === 'string') {
                    displayContent = `<div class="error-header">❌ Error</div>\n<div class="error-message">${data}</div>`;
                } 
                // If data is an object, show it as formatted JSON
                else if (typeof data === 'object') {
                    displayContent = `<div class="error-header">❌ Error Details</div>\n<pre>${JSON.stringify(data, null, 2)}</pre>`;
                }

                resultDiv.innerHTML = displayContent;
            } else {
                // Success response
                resultDiv.innerHTML = '<div class="success-header">✅ Success</div>\n<pre>' + JSON.stringify(data, null, 2) + '</pre>';
            }
        }
    }

    static clearResult(sectionId) {
        const resultDiv = document.getElementById(sectionId + '-result');
        if (resultDiv) {
            resultDiv.style.display = 'none';
            resultDiv.innerHTML = '';
            resultDiv.className = 'result-panel';
        }
    }

    static toggleSection(sectionId, isVisible) {
        const section = document.getElementById(sectionId);
        if (section) {
            section.style.display = isVisible ? 'block' : 'none';
        }
    }

    static getInputValue(elementId) {
        const element = document.getElementById(elementId);
        return element ? element.value.trim() : '';
    }

    static getCheckboxValue(elementId) {
        const element = document.getElementById(elementId);
        return element ? element.checked : false;
    }

    static showAlert(message) {
        alert(message);
    }
}

// ============================================================================
// FORM DATA HANDLERS - Data Collection & Validation
// ============================================================================
class FormDataHandler {
    static getCallOptions(prefix) {
        const options = {};

        // Transcription options
        if (UIManager.getCheckboxValue(`${prefix}CallEnableTranscription`)) {
            options.transcriptionOptions = {
                locale: UIManager.getInputValue(`${prefix}CallTransLocale`),
                startTranscription: UIManager.getCheckboxValue(`${prefix}CallTransStartImmediate`),
                enableIntermediateResults: UIManager.getCheckboxValue(`${prefix}CallTransIntermediate`)
            };
        }

        // Media streaming options
        if (UIManager.getCheckboxValue(`${prefix}CallEnableStreaming`)) {
            options.mediaStreamingOptions = {
                startMediaStreaming: UIManager.getCheckboxValue(`${prefix}CallStreamStartImmediate`),
                mediaStreamingAudioChannel: UIManager.getInputValue(`${prefix}CallStreamAudioChannel`),
                enableBidirectional: UIManager.getCheckboxValue(`${prefix}CallStreamBidirectional`),
                audioFormat: UIManager.getInputValue(`${prefix}CallStreamAudioFormat`)
            };
        }

        // Call intelligence options
        if (UIManager.getCheckboxValue(`${prefix}CallEnableIntelligence`)) {
            const endpoint = UIManager.getInputValue(`${prefix}CallCognitiveEndpoint`);
            if (!endpoint) {
                throw new Error('Cognitive Services Endpoint is required when Call Intelligence is enabled');
            }
            options.callIntelligenceOptions = {
                cognitiveServicesEndpoint: endpoint
            };
        }

        return options;
    }

    static validateRequired(value, fieldName) {
        if (!value) {
            throw new Error(`${fieldName} is required`);
        }
    }
}

// ============================================================================
// UTILITIES
// ============================================================================
class Utilities {
    static parseMultilineInput(text) {
        return text.split('\n')
            .map(line => line.trim())
            .filter(line => line.length > 0);
    }

    static parseCommaSeparated(text) {
        return text.split(',')
            .map(item => item.trim())
            .filter(item => item.length > 0);
    }
}

// ============================================================================
// LEGACY FUNCTION WRAPPERS (for backward compatibility)
// ============================================================================
function showTestPanel(panelId) {
    UIManager.showPanel(panelId);
}

function switchSubTab(event, tabId) {
    UIManager.switchTab(event, tabId);
}

function clearResults(sectionId) {
    UIManager.clearResult(sectionId);
}

function showResult(sectionId, data, isError = false) {
    UIManager.showResult(sectionId, data, isError);
}

function showLoading(sectionId) {
    UIManager.showLoading(sectionId);
}

async function callAPI(endpoint, method = 'POST', body = null) {
    return ApiService.call(endpoint, method, body);
}

// ============================================================================
// CALL MANAGEMENT SERVICE
// ============================================================================
class CallManagementService {
    static async createCall() {
        try {
            const target = UIManager.getInputValue('createCallTarget');
            FormDataHandler.validateRequired(target, 'Target phone number');

            const requestBody = {
                target: target,
                isPstn: UIManager.getCheckboxValue('createCallIsPstn'),
                operationContext: UIManager.getInputValue('createCallContext') || null,
                ...FormDataHandler.getCallOptions('create')
            };

            UIManager.showLoading('create-call');
            const result = await ApiService.call(CONFIG.ENDPOINTS.CREATE_CALL, 'POST', requestBody);

            if (result.success) {
                UIManager.showResult('create-call', result.data);
            } else {
                UIManager.showResult('create-call', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('create-call');
            UIManager.showResult('create-call', { error: error.message }, true);
        }
    }

    static async createGroupCall() {
        try {
            const targetsText = UIManager.getInputValue('groupCallTargets');
            FormDataHandler.validateRequired(targetsText, 'Target participants');

            const targets = Utilities.parseMultilineInput(targetsText);

            const requestBody = {
                targets: targets,
                operationContext: UIManager.getInputValue('groupCallContext') || null,
                sourceDisplayName: UIManager.getInputValue('groupCallDisplayName') || null,
                ...FormDataHandler.getCallOptions('group')
            };

            UIManager.showLoading('group-call');
            const result = await ApiService.call(CONFIG.ENDPOINTS.CREATE_GROUP_CALL, 'POST', requestBody);

            if (result.success) {
                UIManager.showResult('group-call', result.data);
            } else {
                UIManager.showResult('group-call', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('group-call');
            UIManager.showResult('group-call', { error: error.message }, true);
        }
    }

    static async transferCall() {
        try {
            const callId = UIManager.getInputValue('transferCallId');
            const targetType = UIManager.getInputValue('transferTargetType');
            const transfereeType = UIManager.getInputValue('transfereeType');

            FormDataHandler.validateRequired(callId, 'Call Connection ID');

            // Get transfer target based on type
            let transferTarget = '';
            let isPstn = false;

            if (targetType === 'pstn') {
                transferTarget = UIManager.getInputValue('transferTargetPstn');
                FormDataHandler.validateRequired(transferTarget, 'Transfer Target Phone Number');
                isPstn = true;
            } else if (targetType === 'acs') {
                transferTarget = UIManager.getInputValue('transferTargetAcs');
                FormDataHandler.validateRequired(transferTarget, 'Transfer Target ACS User ID');
                isPstn = false;
            }

            // Get transferee (optional) based on type
            let transferee = null;
            if (transfereeType === 'pstn') {
                transferee = UIManager.getInputValue('transfereePstn');
            } else if (transfereeType === 'acs') {
                transferee = UIManager.getInputValue('transfereeAcs');
            }

            const requestBody = {
                callConnectionId: callId,
                transferTarget: transferTarget,
                isPstn: isPstn,
                transferee: transferee
            };

            UIManager.showLoading('transfer');
            const result = await ApiService.call(CONFIG.ENDPOINTS.TRANSFER_CALL, 'POST', requestBody);

            if (result.success) {
                UIManager.showResult('transfer', result.data);
            } else {
                UIManager.showResult('transfer', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('transfer');
            UIManager.showResult('transfer', { error: error.message }, true);
        }
    }

    static async hangup() {
        try {
            const callId = UIManager.getInputValue('hangupCallId');
            const forEveryone = UIManager.getCheckboxValue('hangupForEveryone');

            FormDataHandler.validateRequired(callId, 'Call Connection ID');

            UIManager.showLoading('hangup');
            const result = await ApiService.call(CONFIG.ENDPOINTS.HANGUP, 'POST', {
                callConnectionId: callId,
                forEveryone: forEveryone
            });

            if (result.success) {
                UIManager.showResult('hangup', result.data);
            } else {
                UIManager.showResult('hangup', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('hangup');
            UIManager.showResult('hangup', { error: error.message }, true);
        }
    }
}

// ============================================================================
// UI TOGGLE HANDLERS
// ============================================================================
class ToggleHandlers {
    static toggleTranscriptionOptions(callType) {
        if (!callType) {
            console.error('toggleTranscriptionOptions: callType is required');
            return;
        }
        const checkboxId = `${callType}CallEnableTranscription`;
        const fieldsId = `${callType}CallTranscriptionFields`;
        const isEnabled = UIManager.getCheckboxValue(checkboxId);
        UIManager.toggleSection(fieldsId, isEnabled);
    }

    static toggleStreamingOptions(callType) {
        if (!callType) {
            console.error('toggleStreamingOptions: callType is required');
            return;
        }
        const checkboxId = `${callType}CallEnableStreaming`;
        const fieldsId = `${callType}CallStreamingFields`;
        const isEnabled = UIManager.getCheckboxValue(checkboxId);
        UIManager.toggleSection(fieldsId, isEnabled);
    }

    static toggleIntelligenceOptions(callType) {
        if (!callType) {
            console.error('toggleIntelligenceOptions: callType is required');
            return;
        }
        const checkboxId = `${callType}CallEnableIntelligence`;
        const fieldsId = `${callType}CallIntelligenceFields`;
        const isEnabled = UIManager.getCheckboxValue(checkboxId);
        UIManager.toggleSection(fieldsId, isEnabled);
    }
}

// ============================================================================
// LEGACY FUNCTION WRAPPERS - Call Management
// ============================================================================
async function testCreateCall() {
    await CallManagementService.createCall();
}

async function testGroupCall() {
    await CallManagementService.createGroupCall();
}

async function testTransfer() {
    await CallManagementService.transferCall();
}

async function testHangup() {
    await CallManagementService.hangup();
}

// ============================================================================
// LEGACY FUNCTION WRAPPERS - UI Toggles
// ============================================================================
function toggleTranscriptionOptions(callType) {
    ToggleHandlers.toggleTranscriptionOptions(callType);
}

function toggleStreamingOptions(callType) {
    ToggleHandlers.toggleStreamingOptions(callType);
}

function toggleIntelligenceOptions(callType) {
    ToggleHandlers.toggleIntelligenceOptions(callType);
}

function updatePlaySourceFields() {
    MediaService.updatePlaySourceFields();
}

// ============================================================================
// MEDIA PLAYBACK SERVICE
// ============================================================================
class MediaService {
    static updatePlaySourceFields() {
        const sourceType = UIManager.getInputValue('playSourceType');

        UIManager.toggleSection('playFileGroup', sourceType === 'file');
        UIManager.toggleSection('playTextGroup', sourceType === 'text');
        UIManager.toggleSection('playSsmlGroup', sourceType === 'ssml');
    }
}

// ============================================================================
// MEDIA TESTS - Clean Architecture
// ============================================================================
class MediaTests {
    static async playAudio() {
        try {
            const callId = UIManager.getInputValue('playCallId');
            const sourceType = UIManager.getInputValue('playSourceType');
            const loop = UIManager.getCheckboxValue('playLoop');
            const interruptible = UIManager.getCheckboxValue('playInterruptPrompt');
            const targetType = UIManager.getInputValue('playTargetType');

            FormDataHandler.validateRequired(callId, 'Call Connection ID');

            // Get target participant based on type
            let targetParticipant = '';
            let isPstn = false;

            if (targetType === 'pstn') {
                targetParticipant = UIManager.getInputValue('playTargetPstn');
                FormDataHandler.validateRequired(targetParticipant, 'Target Phone Number');
                isPstn = true;
            } else if (targetType === 'acs') {
                targetParticipant = UIManager.getInputValue('playTargetAcs');
                FormDataHandler.validateRequired(targetParticipant, 'Target ACS User ID');
                isPstn = false;
            }

            // Get media source based on type
            let audioFileUrl = null;
            let textToPlay = null;

            if (sourceType === 'file') {
                audioFileUrl = UIManager.getInputValue('playFileUrl');
                FormDataHandler.validateRequired(audioFileUrl, 'File URL');
            } else if (sourceType === 'text') {
                textToPlay = UIManager.getInputValue('playText');
                FormDataHandler.validateRequired(textToPlay, 'Text to speak');
            } else if (sourceType === 'ssml') {
                textToPlay = UIManager.getInputValue('playSsml');
                FormDataHandler.validateRequired(textToPlay, 'SSML content');
            }

            const requestBody = {
                callConnectionId: callId,
                targetParticipant: targetParticipant,
                isPstn: isPstn,
                audioFileUrl: audioFileUrl,
                textToPlay: textToPlay,
                loop: loop,
                interruptCallMediaOperation: interruptible
            };

            UIManager.showLoading('play-audio');
            const result = await ApiService.call(CONFIG.ENDPOINTS.PLAY_AUDIO, 'POST', requestBody);

            if (result.success) {
                UIManager.showResult('play-audio', result.data);
            } else {
                UIManager.showResult('play-audio', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('play-audio');
            UIManager.showResult('play-audio', { error: error.message }, true);
        }
    }

    static async playToAll() {
        try {
            const callId = UIManager.getInputValue('playCallId');
            const sourceType = UIManager.getInputValue('playSourceType');
            const loop = UIManager.getCheckboxValue('playLoop');
            const interruptible = UIManager.getCheckboxValue('playInterruptPrompt');

            FormDataHandler.validateRequired(callId, 'Call Connection ID');

            // Get media source based on type
            let audioFileUrl = null;
            let textToPlay = null;

            if (sourceType === 'file') {
                audioFileUrl = UIManager.getInputValue('playFileUrl');
                FormDataHandler.validateRequired(audioFileUrl, 'File URL');
            } else if (sourceType === 'text') {
                textToPlay = UIManager.getInputValue('playText');
                FormDataHandler.validateRequired(textToPlay, 'Text to speak');
            } else if (sourceType === 'ssml') {
                textToPlay = UIManager.getInputValue('playSsml');
                FormDataHandler.validateRequired(textToPlay, 'SSML content');
            }

            const requestBody = {
                callConnectionId: callId,
                audioFileUrl: audioFileUrl,
                textToPlay: textToPlay,
                loop: loop,
                interruptCallMediaOperation: interruptible
            };

            UIManager.showLoading('play-audio');
            const result = await ApiService.call(CONFIG.ENDPOINTS.PLAY_TO_ALL, 'POST', requestBody);

            if (result.success) {
                UIManager.showResult('play-audio', result.data);
            } else {
                UIManager.showResult('play-audio', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('play-audio');
            UIManager.showResult('play-audio', { error: error.message }, true);
        }
    }

    static async hold() {
        try {
            const callId = UIManager.getInputValue('holdCallId');
            const participantType = UIManager.getInputValue('holdParticipantType');
            const musicUrl = UIManager.getInputValue('holdMusicUrl');

            FormDataHandler.validateRequired(callId, 'Call Connection ID');

            // Get participant based on type
            let participantId = '';
            let isPstn = false;

            if (participantType === 'pstn') {
                participantId = UIManager.getInputValue('holdParticipantPstn');
                FormDataHandler.validateRequired(participantId, 'Participant Phone Number');
                isPstn = true;
            } else if (participantType === 'acs') {
                participantId = UIManager.getInputValue('holdParticipantAcs');
                FormDataHandler.validateRequired(participantId, 'Participant ACS User ID');
                isPstn = false;
            }

            const requestBody = {
                callConnectionId: callId,
                participantId: participantId,
                isPstn: isPstn,
                playSourceId: musicUrl || null
            };

            UIManager.showLoading('hold-unhold');
            const result = await ApiService.call(CONFIG.ENDPOINTS.HOLD, 'POST', requestBody);

            if (result.success) {
                UIManager.showResult('hold-unhold', result.data);
            } else {
                UIManager.showResult('hold-unhold', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('hold-unhold');
            UIManager.showResult('hold-unhold', { error: error.message }, true);
        }
    }

    static async unhold() {
        try {
            const callId = UIManager.getInputValue('holdCallId');
            const participantType = UIManager.getInputValue('holdParticipantType');

            FormDataHandler.validateRequired(callId, 'Call Connection ID');

            // Get participant based on type
            let participantId = '';
            let isPstn = false;

            if (participantType === 'pstn') {
                participantId = UIManager.getInputValue('holdParticipantPstn');
                FormDataHandler.validateRequired(participantId, 'Participant Phone Number');
                isPstn = true;
            } else if (participantType === 'acs') {
                participantId = UIManager.getInputValue('holdParticipantAcs');
                FormDataHandler.validateRequired(participantId, 'Participant ACS User ID');
                isPstn = false;
            }

            const requestBody = {
                callConnectionId: callId,
                participantId: participantId,
                isPstn: isPstn
            };

            UIManager.showLoading('hold-unhold');
            const result = await ApiService.call(CONFIG.ENDPOINTS.UNHOLD, 'POST', requestBody);

            if (result.success) {
                UIManager.showResult('hold-unhold', result.data);
            } else {
                UIManager.showResult('hold-unhold', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('hold-unhold');
            UIManager.showResult('hold-unhold', { error: error.message }, true);
        }
    }

    static async startStreaming() {
        try {
            const callId = UIManager.getInputValue('streamCallId');
            const wsUrl = UIManager.getInputValue('streamWsUrl');

            FormDataHandler.validateRequired(callId, 'Call Connection ID');

            const requestBody = {
                callConnectionId: callId,
                websocketUrl: wsUrl || null
            };

            UIManager.showLoading('streaming');
            const result = await ApiService.call(CONFIG.ENDPOINTS.START_STREAMING, 'POST', requestBody);

            if (result.success) {
                UIManager.showResult('streaming', result.data);
            } else {
                UIManager.showResult('streaming', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('streaming');
            UIManager.showResult('streaming', { error: error.message }, true);
        }
    }

    static async stopStreaming() {
        try {
            const callId = UIManager.getInputValue('streamCallId');

            FormDataHandler.validateRequired(callId, 'Call Connection ID');

            const requestBody = {
                callConnectionId: callId
            };

            UIManager.showLoading('streaming');
            const result = await ApiService.call(CONFIG.ENDPOINTS.STOP_STREAMING, 'POST', requestBody);

            if (result.success) {
                UIManager.showResult('streaming', result.data);
            } else {
                UIManager.showResult('streaming', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('streaming');
            UIManager.showResult('streaming', { error: error.message }, true);
        }
    }

    static async cancelMedia() {
        try {
            const callId = UIManager.getInputValue('cancelCallId');

            FormDataHandler.validateRequired(callId, 'Call Connection ID');

            const requestBody = {
                callConnectionId: callId
            };

            UIManager.showLoading('cancel-media');
            const result = await ApiService.call(CONFIG.ENDPOINTS.CANCEL_MEDIA, 'POST', requestBody);

            if (result.success) {
                UIManager.showResult('cancel-media', result.data);
            } else {
                UIManager.showResult('cancel-media', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('cancel-media');
            UIManager.showResult('cancel-media', { error: error.message }, true);
        }
    }

    static async startTranscription() {
        try {
            const callId = UIManager.getInputValue('startTransCallId');
            const locale = UIManager.getInputValue('startTransLocale');

            FormDataHandler.validateRequired(callId, 'Call Connection ID');

            const requestBody = {
                callConnectionId: callId,
                locale: locale || 'en-US'
            };

            UIManager.showLoading('start-transcription');
            const result = await ApiService.call(CONFIG.ENDPOINTS.START_TRANSCRIPTION, 'POST', requestBody);

            if (result.success) {
                UIManager.showResult('start-transcription', result.data);
            } else {
                UIManager.showResult('start-transcription', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('start-transcription');
            UIManager.showResult('start-transcription', { error: error.message }, true);
        }
    }

    static async stopTranscription() {
        try {
            const callId = UIManager.getInputValue('stopTransCallId');

            FormDataHandler.validateRequired(callId, 'Call Connection ID');

            const requestBody = {
                callConnectionId: callId
            };

            UIManager.showLoading('stop-transcription');
            const result = await ApiService.call(CONFIG.ENDPOINTS.STOP_TRANSCRIPTION, 'POST', requestBody);

            if (result.success) {
                UIManager.showResult('stop-transcription', result.data);
            } else {
                UIManager.showResult('stop-transcription', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('stop-transcription');
            UIManager.showResult('stop-transcription', { error: error.message }, true);
        }
    }
}

// ============================================================================
// RECORDING TESTS - Clean Architecture
// ============================================================================
class RecordingTests {
    static async startRecording() {
        try {
            const callId = UIManager.getInputValue('startRecCallId');
            const storageUrl = UIManager.getInputValue('startRecStorageUrl');

            FormDataHandler.validateRequired(callId, 'Call Connection ID');

            const requestBody = {
                callConnectionId: callId
            };

            if (storageUrl) {
                requestBody.recordingStorageUrl = storageUrl;
            }

            UIManager.showLoading('start-recording');
            const result = await ApiService.call(CONFIG.ENDPOINTS.START_RECORDING, 'POST', requestBody);

            if (result.success) {
                UIManager.showResult('start-recording', result.data);
            } else {
                UIManager.showResult('start-recording', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('start-recording');
            UIManager.showResult('start-recording', { error: error.message }, true);
        }
    }

    static async pauseRecording() {
        try {
            const callId = UIManager.getInputValue('pauseRecCallId');
            const recId = UIManager.getInputValue('pauseRecId');

            FormDataHandler.validateRequired(callId, 'Call Connection ID');
            FormDataHandler.validateRequired(recId, 'Recording ID');

            const requestBody = {
                callConnectionId: callId,
                recordingId: recId
            };

            UIManager.showLoading('pause-recording');
            const result = await ApiService.call(CONFIG.ENDPOINTS.PAUSE_RECORDING, 'POST', requestBody);

            if (result.success) {
                UIManager.showResult('pause-recording', result.data);
            } else {
                UIManager.showResult('pause-recording', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('pause-recording');
            UIManager.showResult('pause-recording', { error: error.message }, true);
        }
    }

    static async resumeRecording() {
        try {
            const callId = UIManager.getInputValue('resumeRecCallId');
            const recId = UIManager.getInputValue('resumeRecId');

            FormDataHandler.validateRequired(callId, 'Call Connection ID');
            FormDataHandler.validateRequired(recId, 'Recording ID');

            const requestBody = {
                callConnectionId: callId,
                recordingId: recId
            };

            UIManager.showLoading('resume-recording');
            const result = await ApiService.call(CONFIG.ENDPOINTS.RESUME_RECORDING, 'POST', requestBody);

            if (result.success) {
                UIManager.showResult('resume-recording', result.data);
            } else {
                UIManager.showResult('resume-recording', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('resume-recording');
            UIManager.showResult('resume-recording', { error: error.message }, true);
        }
    }

    static async stopRecording() {
        try {
            const callId = UIManager.getInputValue('stopRecCallId');
            const recId = UIManager.getInputValue('stopRecId');

            FormDataHandler.validateRequired(callId, 'Call Connection ID');
            FormDataHandler.validateRequired(recId, 'Recording ID');

            const requestBody = {
                callConnectionId: callId,
                recordingId: recId
            };

            UIManager.showLoading('stop-recording');
            const result = await ApiService.call(CONFIG.ENDPOINTS.STOP_RECORDING, 'POST', requestBody);

            if (result.success) {
                UIManager.showResult('stop-recording', result.data);
            } else {
                UIManager.showResult('stop-recording', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('stop-recording');
            UIManager.showResult('stop-recording', { error: error.message }, true);
        }
    }

    static async downloadRecording() {
        try {
            const callId = UIManager.getInputValue('downloadRecCallId');
            const recId = UIManager.getInputValue('downloadRecId');

            FormDataHandler.validateRequired(callId, 'Call Connection ID');
            FormDataHandler.validateRequired(recId, 'Recording ID');

            UIManager.showLoading('download-recording');
            const result = await ApiService.call(`${CONFIG.ENDPOINTS.DOWNLOAD_RECORDING}/${recId}`, 'GET');

            if (result.success) {
                UIManager.showResult('download-recording', result.data);
            } else {
                UIManager.showResult('download-recording', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('download-recording');
            UIManager.showResult('download-recording', { error: error.message }, true);
        }
    }
}

// ============================================================================
// PARTICIPANT TESTS - Clean Architecture
// ============================================================================
class ParticipantTests {
    static async addParticipant() {
        try {
            const callId = UIManager.getInputValue('addPartCallId');
            const participantType = UIManager.getInputValue('addPartType');

            FormDataHandler.validateRequired(callId, 'Call Connection ID');

            let participantId, isPstn;

            if (participantType === 'pstn') {
                participantId = UIManager.getInputValue('addPartPhone');
                FormDataHandler.validateRequired(participantId, 'Participant Phone');
                isPstn = true;
            } else {
                participantId = UIManager.getInputValue('addPartAcsId');
                FormDataHandler.validateRequired(participantId, 'ACS User ID');
                isPstn = false;
            }

            const requestBody = {
                callConnectionId: callId,
                participantId: participantId,
                isPstn: isPstn,
                operationContext: UIManager.getInputValue('addPartContext') || null
            };

            const timeout = UIManager.getInputValue('addPartTimeout');
            if (timeout) {
                requestBody.invitationTimeout = parseInt(timeout);
            }

            UIManager.showLoading('add-participant');
            const result = await ApiService.call(CONFIG.ENDPOINTS.ADD_PARTICIPANT, 'POST', requestBody);

            if (result.success) {
                UIManager.showResult('add-participant', result.data);
            } else {
                UIManager.showResult('add-participant', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('add-participant');
            UIManager.showResult('add-participant', { error: error.message }, true);
        }
    }

    static async removeParticipant() {
        try {
            const callId = UIManager.getInputValue('removePartCallId');
            const participantType = UIManager.getInputValue('removePartType');

            FormDataHandler.validateRequired(callId, 'Call Connection ID');

            let participantId, isPstn;

            if (participantType === 'pstn') {
                participantId = UIManager.getInputValue('removePartPhone');
                FormDataHandler.validateRequired(participantId, 'Participant Phone');
                isPstn = true;
            } else {
                participantId = UIManager.getInputValue('removePartId');
                FormDataHandler.validateRequired(participantId, 'ACS User ID');
                isPstn = false;
            }

            const requestBody = {
                callConnectionId: callId,
                participantId: participantId,
                isPstn: isPstn,
                operationContext: UIManager.getInputValue('removePartContext') || null
            };

            UIManager.showLoading('remove-participant');
            const result = await ApiService.call(CONFIG.ENDPOINTS.REMOVE_PARTICIPANT, 'POST', requestBody);

            if (result.success) {
                UIManager.showResult('remove-participant', result.data);
            } else {
                UIManager.showResult('remove-participant', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('remove-participant');
            UIManager.showResult('remove-participant', { error: error.message }, true);
        }
    }

    static async muteParticipant() {
        try {
            const callId = UIManager.getInputValue('mutePartCallId');
            const participantType = UIManager.getInputValue('mutePartType');

            FormDataHandler.validateRequired(callId, 'Call Connection ID');

            let participantId, isPstn;

            if (participantType === 'pstn') {
                participantId = UIManager.getInputValue('mutePartPhone');
                FormDataHandler.validateRequired(participantId, 'Participant Phone');
                isPstn = true;
            } else {
                participantId = UIManager.getInputValue('mutePartId');
                FormDataHandler.validateRequired(participantId, 'ACS User ID');
                isPstn = false;
            }

            const requestBody = {
                callConnectionId: callId,
                participantId: participantId,
                isPstn: isPstn
            };

            UIManager.showLoading('mute-participant');
            const result = await ApiService.call(CONFIG.ENDPOINTS.MUTE_PARTICIPANT, 'POST', requestBody);

            if (result.success) {
                UIManager.showResult('mute-participant', result.data);
            } else {
                UIManager.showResult('mute-participant', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('mute-participant');
            UIManager.showResult('mute-participant', { error: error.message }, true);
        }
    }

    static async listParticipants() {
        try {
            const callId = UIManager.getInputValue('listPartCallId');
            FormDataHandler.validateRequired(callId, 'Call Connection ID');

            UIManager.showLoading('list-participants');
            const result = await ApiService.call(`${CONFIG.ENDPOINTS.LIST_PARTICIPANTS}/${callId}/all`, 'GET');

            if (result.success) {
                UIManager.showResult('list-participants', result.data);
            } else {
                UIManager.showResult('list-participants', { error: result.error }, true);
            }
        } catch (error) {
            UIManager.clearResult('list-participants');
            UIManager.showResult('list-participants', { error: error.message }, true);
        }
    }
}

// Recording Functions - OLD STYLE (kept for backwards compatibility)
async function testStartRecording() {
    RecordingTests.startRecording();
}

async function testPauseRecording() {
    RecordingTests.pauseRecording();
}

async function testResumeRecording() {
    RecordingTests.resumeRecording();
}

async function testStopRecording() {
    RecordingTests.stopRecording();
}

async function testDownloadRecording() {
    RecordingTests.downloadRecording();
}

async function testStopRecording() {
    const callId = document.getElementById('stopRecCallId').value;
    const recId = document.getElementById('stopRecId').value;

    if (!callId || !recId) {
        alert('Please enter call ID and recording ID');
        return;
    }

    showLoading('stop-recording');

    try {
        const result = await callAPI('/api/recordings/stopRecording', 'POST', {
            callConnectionId: callId,
            recordingId: recId
        });
        showResult('stop-recording', result);
    } catch (error) {
        showResult('stop-recording', { error: error.message }, true);
    }
}

async function testDownloadRecording() {
    RecordingTests.downloadRecording();
}

// Transcription Functions - OLD STYLE (kept for backwards compatibility)
async function testStartTranscription() {
    MediaTests.startTranscription();
}

async function testStopTranscription() {
    MediaTests.stopTranscription();
}

// Recognition Functions
async function testDtmfRecognition() {
    const callId = document.getElementById('dtmfCallId').value;
    const maxTones = document.getElementById('dtmfMaxTones').value;
    const prompt = document.getElementById('dtmfPrompt').value;

    if (!callId) {
        alert('Please enter a call connection ID');
        return;
    }

    showLoading('dtmf-recognition');

    try {
        const result = await callAPI('/startRecognizeAsync', 'POST', {
            callConnectionId: callId,
            recognizeType: 'dtmf',
            maxTonesToCollect: parseInt(maxTones),
            promptText: prompt || null
        });
        showResult('dtmf-recognition', result);
    } catch (error) {
        showResult('dtmf-recognition', { error: error.message }, true);
    }
}

async function testSpeechRecognition() {
    const callId = document.getElementById('speechCallId').value;
    const locale = document.getElementById('speechLocale').value;
    const prompt = document.getElementById('speechPrompt').value;

    if (!callId) {
        alert('Please enter a call connection ID');
        return;
    }

    showLoading('speech-recognition');

    try {
        const result = await callAPI('/startRecognizeAsync', 'POST', {
            callConnectionId: callId,
            recognizeType: 'speech',
            locale: locale,
            promptText: prompt || null
        });
        showResult('speech-recognition', result);
    } catch (error) {
        showResult('speech-recognition', { error: error.message }, true);
    }
}

async function testChoiceRecognition() {
    const callId = document.getElementById('choiceCallId').value;
    const choicesText = document.getElementById('choiceOptions').value;
    const prompt = document.getElementById('choicePrompt').value;

    if (!callId || !choicesText) {
        alert('Please enter call ID and choices');
        return;
    }

    const choices = choicesText.split(',').map(s => s.trim()).filter(s => s);

    showLoading('choice-recognition');

    try {
        const result = await callAPI('/startRecognizeAsync', 'POST', {
            callConnectionId: callId,
            recognizeType: 'choice',
            choices: choices,
            promptText: prompt || null
        });
        showResult('choice-recognition', result);
    } catch (error) {
        showResult('choice-recognition', { error: error.message }, true);
    }
}

// ============================================================================
// UI HELPER FUNCTIONS
// ============================================================================

/**
 * Toggle between phone and ACS user input fields for participant operations
 */
function toggleParticipantInputType(operation) {
    if (!operation) {
        console.error('toggleParticipantInputType: operation is required');
        return;
    }
    const typeElement = document.getElementById(`${operation}PartType`);
    const phoneGroup = document.getElementById(`${operation}PartPhoneGroup`);
    const acsGroup = document.getElementById(`${operation}PartAcsGroup`);

    if (!typeElement) {
        console.error(`toggleParticipantInputType: Element ${operation}PartType not found`);
        return;
    }

    const type = typeElement.value;

    if (type === 'pstn') {
        if (phoneGroup) phoneGroup.style.display = 'block';
        if (acsGroup) acsGroup.style.display = 'none';
    } else {
        if (phoneGroup) phoneGroup.style.display = 'none';
        if (acsGroup) acsGroup.style.display = 'block';
    }
}

/**
 * Toggle target participant fields for Play Audio
 */
function updatePlayTargetFields() {
    const targetTypeElement = document.getElementById('playTargetType');
    const pstnGroup = document.getElementById('playTargetPstnGroup');
    const acsGroup = document.getElementById('playTargetAcsGroup');

    if (!targetTypeElement) {
        console.error('updatePlayTargetFields: playTargetType element not found');
        return;
    }

    const targetType = targetTypeElement.value;

    if (targetType === 'pstn') {
        if (pstnGroup) pstnGroup.style.display = 'block';
        if (acsGroup) acsGroup.style.display = 'none';
    } else {
        if (pstnGroup) pstnGroup.style.display = 'none';
        if (acsGroup) acsGroup.style.display = 'block';
    }
}

/**
 * Toggle transfer target fields
 */
function updateTransferTargetFields() {
    const targetTypeElement = document.getElementById('transferTargetType');
    const pstnGroup = document.getElementById('transferTargetPstnGroup');
    const acsGroup = document.getElementById('transferTargetAcsGroup');

    if (!targetTypeElement) {
        console.error('updateTransferTargetFields: transferTargetType element not found');
        return;
    }

    const targetType = targetTypeElement.value;

    if (targetType === 'pstn') {
        if (pstnGroup) pstnGroup.style.display = 'block';
        if (acsGroup) acsGroup.style.display = 'none';
    } else {
        if (pstnGroup) pstnGroup.style.display = 'none';
        if (acsGroup) acsGroup.style.display = 'block';
    }
}

/**
 * Toggle transferee fields
 */
function updateTransfereeFields() {
    const transfereeTypeElement = document.getElementById('transfereeType');
    const pstnGroup = document.getElementById('transfereePstnGroup');
    const acsGroup = document.getElementById('transfereeAcsGroup');

    if (!transfereeTypeElement) {
        console.error('updateTransfereeFields: transfereeType element not found');
        return;
    }

    const transfereeType = transfereeTypeElement.value;

    if (transfereeType === 'pstn') {
        if (pstnGroup) pstnGroup.style.display = 'block';
        if (acsGroup) acsGroup.style.display = 'none';
    } else if (transfereeType === 'acs') {
        if (pstnGroup) pstnGroup.style.display = 'none';
        if (acsGroup) acsGroup.style.display = 'block';
    } else {
        if (pstnGroup) pstnGroup.style.display = 'none';
        if (acsGroup) acsGroup.style.display = 'none';
    }
}

/**
 * Toggle hold participant fields
 */
function updateHoldParticipantFields() {
    const participantTypeElement = document.getElementById('holdParticipantType');
    const pstnGroup = document.getElementById('holdParticipantPstnGroup');
    const acsGroup = document.getElementById('holdParticipantAcsGroup');

    if (!participantTypeElement) {
        console.error('updateHoldParticipantFields: holdParticipantType element not found');
        return;
    }

    const participantType = participantTypeElement.value;

    if (participantType === 'pstn') {
        if (pstnGroup) pstnGroup.style.display = 'block';
        if (acsGroup) acsGroup.style.display = 'none';
    } else {
        if (pstnGroup) pstnGroup.style.display = 'none';
        if (acsGroup) acsGroup.style.display = 'block';
    }
}

/**
 * Toggle media source fields for Play Audio
 */
function updatePlaySourceFields() {
    const sourceTypeElement = document.getElementById('playSourceType');
    const fileGroup = document.getElementById('playFileGroup');
    const textGroup = document.getElementById('playTextGroup');
    const ssmlGroup = document.getElementById('playSsmlGroup');

    if (!sourceTypeElement) {
        console.error('updatePlaySourceFields: playSourceType element not found');
        return;
    }

    const sourceType = sourceTypeElement.value;

    if (fileGroup) fileGroup.style.display = 'none';
    if (textGroup) textGroup.style.display = 'none';
    if (ssmlGroup) ssmlGroup.style.display = 'none';

    if (sourceType === 'file') {
        if (fileGroup) fileGroup.style.display = 'block';
    } else if (sourceType === 'text') {
        if (textGroup) textGroup.style.display = 'block';
    } else if (sourceType === 'ssml') {
        if (ssmlGroup) ssmlGroup.style.display = 'block';
    }
}

// ============================================================================
// WRAPPER FUNCTIONS FOR HTML ONCLICK HANDLERS
// ============================================================================

// Media wrapper functions
function testPlayAudio() {
    MediaTests.playAudio();
}

function testPlayToAll() {
    MediaTests.playToAll();
}

function testHold() {
    MediaTests.hold();
}

function testUnhold() {
    MediaTests.unhold();
}

function testStartStreaming() {
    MediaTests.startStreaming();
}

function testStopStreaming() {
    MediaTests.stopStreaming();
}

function testCancelMedia() {
    MediaTests.cancelMedia();
}

function testStartTranscription() {
    MediaTests.startTranscription();
}

function testStopTranscription() {
    MediaTests.stopTranscription();
}

// Participant wrapper functions
function testAddParticipant() {
    ParticipantTests.addParticipant();
}

function testRemoveParticipant() {
    ParticipantTests.removeParticipant();
}

function testMuteParticipant() {
    ParticipantTests.muteParticipant();
}

function testListParticipants() {
    ParticipantTests.listParticipants();
}

// Recording wrapper functions
function testStartRecording() {
    RecordingTests.startRecording();
}

function testPauseRecording() {
    RecordingTests.pauseRecording();
}

function testResumeRecording() {
    RecordingTests.resumeRecording();
}

function testStopRecording() {
    RecordingTests.stopRecording();
}

function testDownloadRecording() {
    RecordingTests.downloadRecording();
}

// ============================================================================
// END OF CLEAN ARCHITECTURE IMPLEMENTATION
// ============================================================================
// The code above follows clean architecture principles with:
// - Separation of Concerns: API, UI, Business Logic, and Data Handling
// - Single Responsibility: Each class has one clear purpose
// - Dependency Inversion: Services depend on abstractions (API layer)
// - Open/Closed: Easy to extend with new services without modifying existing code
// - DRY: Reusable utilities and form handlers
// - Legacy wrapper functions maintain backward compatibility with HTML onclick handlers
// ============================================================================

// The code above follows clean architecture principles with:
// - Separation of Concerns: API, UI, Business Logic, and Data Handling
// - Single Responsibility: Each class has one clear purpose
// - Dependency Inversion: Services depend on abstractions (API layer)
// - Open/Closed: Easy to extend with new services without modifying existing code
// - DRY: Reusable utilities and form handlers
// - Legacy wrapper functions maintain backward compatibility with HTML onclick handlers
// ============================================================================

