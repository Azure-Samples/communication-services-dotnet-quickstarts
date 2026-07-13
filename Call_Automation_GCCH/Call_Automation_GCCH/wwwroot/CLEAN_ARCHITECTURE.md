# Clean Architecture Documentation - Test API

## Overview
The `test-api.js` file has been refactored following **Clean Architecture** principles to improve maintainability, testability, and scalability.

## Architecture Layers

### 1. **Configuration Layer**
```javascript
const CONFIG = {
	API_BASE: '/api',
	ENDPOINTS: { ... }
};
```
- **Purpose**: Centralized configuration management
- **Benefits**: Easy endpoint updates, no hardcoded values scattered across code
- **Usage**: All API calls reference `CONFIG.ENDPOINTS.*`

### 2. **API Service Layer**
```javascript
class ApiService {
	static async call(endpoint, method, body)
}
```
- **Purpose**: HTTP communication abstraction
- **Responsibilities**:
  - Handle all fetch requests
  - Manage HTTP headers
  - Error handling and response parsing
  - Return standardized response format `{ success, data/error }`
- **Benefits**: Single point for HTTP logic changes

### 3. **UI Manager Layer**
```javascript
class UIManager {
	static showPanel()
	static switchTab()
	static showLoading()
	static showResult()
	static clearResult()
	static toggleSection()
	static getInputValue()
	static getCheckboxValue()
}
```
- **Purpose**: DOM manipulation and UI state management
- **Responsibilities**:
  - All direct DOM operations
  - UI state changes
  - Form value extraction
  - Result display
- **Benefits**: Separation of UI concerns from business logic

### 4. **Form Data Handler Layer**
```javascript
class FormDataHandler {
	static getCallOptions(prefix)
	static validateRequired(value, fieldName)
}
```
- **Purpose**: Form data collection and validation
- **Responsibilities**:
  - Collect and structure form data
  - Validate user inputs
  - Build request payloads
- **Benefits**: Reusable validation logic, DRY principle

### 5. **Business Logic Layer - Services**

#### CallManagementService
```javascript
class CallManagementService {
	static async createCall()
	static async createGroupCall()
	static async transferCall()
	static async hangup()
}
```
- **Purpose**: Call-related business operations
- **Responsibilities**:
  - Orchestrate call creation/management workflows
  - Coordinate between UI, validation, and API layers
  - Error handling specific to call operations

#### MediaService
```javascript
class MediaService {
	static updatePlaySourceFields()
}
```
- **Purpose**: Media playback business logic
- **Responsibilities**:
  - Manage media source type switching
  - Handle media-related UI updates

#### ToggleHandlers
```javascript
class ToggleHandlers {
	static toggleTranscriptionOptions(callType)
	static toggleStreamingOptions(callType)
	static toggleIntelligenceOptions(callType)
}
```
- **Purpose**: Dynamic UI section visibility
- **Responsibilities**:
  - Show/hide optional configuration sections
  - Manage conditional UI rendering

### 6. **Utilities Layer**
```javascript
class Utilities {
	static parseMultilineInput(text)
	static parseCommaSeparated(text)
}
```
- **Purpose**: Reusable helper functions
- **Benefits**: Shared utilities across services

### 7. **Legacy Wrapper Layer**
```javascript
async function testCreateCall() {
	await CallManagementService.createCall();
}

function toggleTranscriptionOptions(callType) {
	ToggleHandlers.toggleTranscriptionOptions(callType);
}
```
- **Purpose**: Backward compatibility with HTML `onclick` handlers
- **Benefits**: No HTML changes required, clean migration path

## Design Principles Applied

### ✅ **Single Responsibility Principle (SRP)**
- Each class has one clear purpose
- `ApiService` → HTTP only
- `UIManager` → DOM only
- `CallManagementService` → Call operations only

### ✅ **Open/Closed Principle**
- Easy to add new services (e.g., `RecordingService`, `TranscriptionService`)
- No need to modify existing classes

### ✅ **Dependency Inversion Principle**
- Services depend on abstractions (`ApiService`, `UIManager`)
- Low-level details (fetch, DOM) are abstracted away

### ✅ **DRY (Don't Repeat Yourself)**
- `FormDataHandler.getCallOptions()` used by both create and group call
- Shared validation logic
- Reusable UI methods

### ✅ **Separation of Concerns**
- UI logic separated from business logic
- API communication isolated
- Form handling decoupled from API calls

## Benefits of This Architecture

### 🎯 **Testability**
- Each class can be unit tested independently
- Mock `ApiService` for testing business logic
- Mock `UIManager` for testing services

### 🔧 **Maintainability**
- Easy to locate and fix bugs (clear responsibility boundaries)
- Changes are isolated (e.g., API endpoint changes only affect `CONFIG`)

### 📈 **Scalability**
- Easy to add new features:
  ```javascript
  class RecordingService {
	  static async startRecording() { ... }
	  static async stopRecording() { ... }
  }
  ```

### 🔄 **Reusability**
- Components can be reused across different features
- `FormDataHandler.validateRequired()` works everywhere

### 🛡️ **Error Handling**
- Centralized error handling in `ApiService`
- Consistent error display through `UIManager`

## Migration Path

### Before (Procedural)
```javascript
async function testCreateCall() {
	const target = document.getElementById('createCallTarget').value;
	if (!target) {
		alert('Please enter target');
		return;
	}

	const response = await fetch('/api/calls/createCall', {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify({ target })
	});

	const data = await response.json();
	document.getElementById('result').innerHTML = JSON.stringify(data);
}
```

### After (Clean Architecture)
```javascript
async function testCreateCall() {
	await CallManagementService.createCall();
}

class CallManagementService {
	static async createCall() {
		const target = UIManager.getInputValue('createCallTarget');
		FormDataHandler.validateRequired(target, 'Target');

		const result = await ApiService.call(
			CONFIG.ENDPOINTS.CREATE_CALL,
			'POST',
			{ target }
		);

		UIManager.showResult('create-call', result.data, !result.success);
	}
}
```

## Future Enhancements

### Planned Additions
1. **RecordingService** - Handle all recording operations
2. **TranscriptionService** - Manage transcription features
3. **ParticipantService** - Participant management
4. **MediaStreamingService** - Streaming operations

### Possible Improvements
- Add TypeScript for type safety
- Implement dependency injection container
- Add state management (e.g., Redux pattern)
- Create observable pattern for real-time updates
- Add unit tests using Jest

## Code Organization

```
test-api.js
├── Configuration Layer (CONFIG)
├── Core Infrastructure
│   ├── ApiService (HTTP)
│   ├── UIManager (DOM)
│   ├── FormDataHandler (Validation)
│   └── Utilities (Helpers)
├── Business Services
│   ├── CallManagementService
│   ├── MediaService
│   └── ToggleHandlers
└── Legacy Wrappers (Backward Compatibility)
```

## Best Practices

1. ✅ Always use `UIManager` for DOM operations
2. ✅ Always use `ApiService` for HTTP calls
3. ✅ Always use `FormDataHandler` for validation
4. ✅ Keep business logic in service classes
5. ✅ Use `CONFIG` for all endpoints
6. ✅ Add legacy wrappers for HTML compatibility
7. ✅ Document new services and methods

---

**Refactored by**: Clean Architecture Implementation
**Date**: January 2026
**Version**: 2.0
