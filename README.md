# AR/VR Mini Project - Documentation

## Project Overview

This AR/VR Mini Project is a **Unity-based Augmented Reality (AR) application** that demonstrates real-time 3D model manipulation through QR code scanning, color customization, gesture control, and screenshot capture functionality. The application is designed for mobile AR platforms using AR Foundation and the new Input System.

**Unity Version:** 6000.3.10f1  
**Target Platforms:** Mobile (Android/iOS)

---

## System Architecture & Process Flow

### High-Level Process Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    AR APPLICATION STARTUP                        │
└────────────────┬────────────────────────────────────────────────┘
                 │
                 ▼
         ┌──────────────────┐
         │ ARSessionChecker │◄─── Verify AR Support
         │   Initialize AR  │    & Enable AR Foundation
         └────────┬─────────┘
                  │
         ┌────────▼──────────┐
         │   QR Scanner      │◄─── Listen to Camera Frames
         │  Ready to Scan    │    Every 60 Frames
         └────────┬──────────┘
                  │
         ┌────────▼──────────────────────┐
    ┌────┤ QR Code Detected?             │
    │    │ (Text = Model Name)           │
    │    └────────┬─────────────────────┘
    │             │ YES
    │    ┌────────▼──────────────────────┐
    │    │   ARModelLoader               │
    │    │ Load GLB from StreamingAssets │
    │    └────────┬─────────────────────┘
    │             │
    │    ┌────────▼──────────────────────┐
    │    │ Auto-Scale Model to Target    │
    │    │ Measure Bounding Box          │
    │    │ Calculate Scale Factor        │
    │    └────────┬─────────────────────┘
    │             │
    │    ┌────────▼──────────────────────┐
    │    │ Position in Front of Camera   │
    │    │ Face Away from User           │
    │    │ Detach from Tracking          │
    │    └────────┬─────────────────────┘
    │             │
    ├─────────────┼──────────────────────┐
    │             │                      │
NO  │    ┌────────▼─────────┐   ┌────────▼──────────┐
    │    │ ColorPicker      │   │  ModelRotator     │
    │    │ Adjust RGB Color │   │  Gesture Control  │
    │    │ Tint Material    │   │  (Pinch/Drag)    │
    │    └──────────────────┘   └───────────────────┘
    │
    └─────────────────────────────────┐
                                      │
                        ┌─────────────▼────────────┐
                        │  ScreenshotCapture       │
                        │  Take Picture & Save    │
                        │  Trigger Flash Effect   │
                        └──────────────────────────┘
```

---

## Core Components & Functionality

### 1. **ARSessionChecker.cs**
**Purpose:** Initialize and verify AR Foundation support on the device.

**Functionality:**
- Checks device AR compatibility at startup
- Validates AR Foundation is properly initialized
- Waits for AR session to reach "SessionTracking" state
- Enables the ARSession component if not already active
- Provides debugging feedback on AR state transitions

**Key Methods:**
- `Start()` (IEnumerator) - Main initialization coroutine

**Special Features:**
- 10-second timeout to prevent infinite waiting
- Real-time state logging for debugging
- Automatic AR session enablement

**Code Flow:**
```
Start() 
  ├─ Log "Checking AR Support"
  ├─ Check if ARSession is running
  ├─ Call ARSession.CheckAvailability()
  ├─ If Unsupported → Exit with error
  ├─ Enable arSession component
  ├─ Wait until SessionTracking state (max 10 seconds)
  └─ Log success or timeout warning
```

---

### 2. **QRScanner.cs**
**Purpose:** Detect and decode QR codes from the device camera feed to trigger model loading.

**Functionality:**
- Listens to AR camera frames continuously
- Processes frame data every 60 frames (performance optimization)
- Detects QR codes using ZXing library
- Extracts QR code text and passes it to ARModelLoader
- One-time scan prevention (single model load per reset)

**Key Components:**
- `arCameraManager` - Receives raw camera frames
- `reader` - ZXing BarcodeReader for QR decoding
- `frameSkip` - Counter to reduce scanning frequency
- `hasScanned` - Flag to prevent duplicate scans

**Key Methods:**
- `Awake()` - Get ARCameraManager reference
- `OnEnable()` - Register camera frame callback
- `OnDisable()` - Unregister camera frame callback
- `OnFrame()` - Process camera frames for QR detection
- `ResetScanner()` - Reset scan state for next model

**Special Features:**
- Frame skipping every 60 frames reduces CPU load and heat
- Image downsampling (4x reduction) speeds up QR detection
- Device vibration feedback on successful scan
- Efficient use of managed memory with `using` statements

**Code Flow:**
```
OnFrame(ARCameraFrameEventArgs)
  ├─ Check if already scanned → Return if true
  ├─ Increment frameSkip counter
  ├─ Skip processing unless frameSkip % 60 == 0
  ├─ Try to acquire latest CPU image from camera
  │  ├─ Create conversion params (downscale to 1/4 resolution)
  │  ├─ Allocate native buffer for image data
  │  ├─ Convert image to RGBA32 format
  │  ├─ Decode buffer with ZXing reader
  │  ├─ If QR found:
  │  │   ├─ Trigger device vibration
  │  │   ├─ Set hasScanned = true
  │  │   └─ Call loader.LoadModel(qrText)
  │  └─ Clean up native resources
  └─ Exit and wait for next eligible frame
```

---

### 3. **ARModelLoader.cs**
**Purpose:** Load GLB 3D models from StreamingAssets and instantiate them in the AR scene.

**Functionality:**
- Loads GLB (binary GLTF) models asynchronously
- Automatically scales models to a target size
- Positions models in front of the camera
- Manages model lifecycle (destroy old, create new)
- Handles loading screen UI feedback
- Integrates with ColorPicker for material customization

**Key Components:**
- `targetSizeInMeters` - Desired real-world model size (0.3m default)
- `spawnDistance` - Distance from camera (1.5m default)
- `currentModel` - Reference to active 3D model
- `loadingScreen` - UI shown during load
- `scanAgainButton` - Enabled after successful load

**Key Methods:**
- `LoadModel(string modelName)` - Entry point for model loading
- `LoadCoroutine()` (IEnumerator) - Async loading logic
- `AutoScaleModel()` - Calculate and apply scale based on bounds
- `DetachFromTracking()` - Make model stationary in world space

**Special Features:**
- **Smart Auto-Scaling:** Measures model bounding box and calculates scale factor to match target size
- **Dynamic Positioning:** Places model at eye level, slightly below camera, facing away
- **Model Detachment:** Once loaded, model stays in world space (not tracked to camera)
- **Error Handling:** Graceful failures with detailed logging
- **Loading Feedback:** Shows/hides loading screen during async operations

**Code Flow:**
```
LoadModel(modelName)
  └─ StartCoroutine(LoadCoroutine(modelName))
      │
      ├─ Show loading screen
      ├─ Build path: StreamingAssets/{modelName}.glb
      ├─ Create UnityWebRequest to fetch GLB file
      ├─ Wait for download completion
      │  └─ If failed → Log error, hide loading screen, exit
      │
      ├─ Extract byte array from downloaded data
      ├─ Create GltfImport instance
      ├─ Load GLB binary data asynchronously
      ├─ Wait for load task completion
      │  └─ If failed → Log error, hide loading screen, exit
      │
      ├─ Destroy previous model (if exists)
      ├─ Create new GameObject with model name
      │
      ├─ Calculate spawn position:
      │   ├─ Get camera position + forward direction * spawnDistance
      │   ├─ Adjust Y position (0.2m below camera)
      │   └─ Face model away from camera
      │
      ├─ Instantiate GLB scene into GameObject
      ├─ Wait for instantiation completion
      │
      ├─ Call AutoScaleModel() to fit target size
      │  └─ Bounds calculation:
      │      ├─ Get all renderers' bounding boxes
      │      ├─ Find largest dimension
      │      ├─ Calculate scaleFactor = targetSize / largestDimension
      │      └─ Apply uniform scale
      │
      ├─ Call DetachFromTracking() to freeze in place
      ├─ Hide loading screen
      ├─ Pass model to ColorPicker
      ├─ Show "Scan Again" button
      └─ Log success
```

---

### 4. **ColorPicker.cs**
**Purpose:** Provide RGB color customization for loaded 3D models through UI sliders.

**Functionality:**
- Three RGB sliders (Red, Green, Blue) with range 0-1
- Real-time color preview in UI
- Applies tint color to all materials on target model
- Saves original material states for reference
- Resets colors to white
- Supports multiple material property types

**Key Components:**
- `sliderR`, `sliderG`, `sliderB` - RGB value controls (0-1)
- `colorPreview` - UI Image showing current color
- `target` - Reference to model being customized
- `savedMaterials` - List storing original material states

**MaterialState Structure:**
```csharp
struct MaterialState {
    Color originalColor;        // Original color before modification
    Texture originalTexture;    // Original texture (if any)
    string colorProperty;       // Name of color property (_BaseColor, _Color, etc.)
    string textureProperty;     // Name of texture property
}
```

**Key Methods:**
- `Start()` - Initialize sliders and attach listeners
- `SetTarget(GameObject newTarget)` - Set model for customization
- `SaveOriginalMaterials()` - Store material states
- `ApplyColor()` - Update material colors based on sliders
- `ResetToWhite()` - Reset sliders to white (1,1,1)

**Special Features:**
- **Multi-Shader Support:** Handles different material property names:
  - `_BaseColor` (URP/modern)
  - `_Color` (standard)
  - `baseColorFactor` (glTF)
- **Original Material Preservation:** Saves initial state for accurate tinting
- **Tint Blending:** Multiplies original color by slider color for natural results
- **Comprehensive Logging:** Debug output for troubleshooting material properties

**Code Flow:**
```
SetTarget(newTarget)
  ├─ Set target = newTarget
  ├─ Remove all slider listeners (prevent recursive calls)
  ├─ Reset sliders to white (1, 1, 1)
  ├─ Re-attach slider listeners
  └─ Call SaveOriginalMaterials()
      │
      ├─ Clear savedMaterials list
      ├─ For each Renderer in target (including children):
      │   └─ For each Material in renderer:
      │       ├─ Create MaterialState
      │       ├─ Detect color property name:
      │       │   ├─ Check _BaseColor (URP default)
      │       │   ├─ Check _Color (legacy)
      │       │   ├─ Check baseColorFactor (glTF)
      │       │   └─ Set to null if none found
      │       ├─ Store original color value
      │       ├─ Store original texture reference
      │       ├─ Add (material, state) to savedMaterials list
      │       └─ Log material info for debugging
      └─ Done

ApplyColor() [Called when any slider changes]
  ├─ Check if target exists
  ├─ Read current slider values (0-1)
  ├─ Create color: Color(sliderR, sliderG, sliderB, 1)
  ├─ Update colorPreview Image color
  ├─ For each (material, state) in savedMaterials:
  │   ├─ Skip if material null or no color property
  │   ├─ Calculate tinted color:
  │   │   └─ tinted = originalColor × sliderColor
  │   ├─ Preserve alpha from original
  │   └─ Apply to material property
  └─ Changes visible immediately in AR view

ResetToWhite()
  ├─ Set all sliders to 1.0
  └─ ApplyColor() triggers automatically → All materials reset
```

---

### 5. **ModelRotator.cs**
**Purpose:** Enable multi-touch gesture control for model manipulation (rotation and scaling).

**Functionality:**
- Single-finger drag to rotate model around Y-axis (and optional X-axis)
- Two-finger pinch to zoom in/out with scale clamping
- Touch input via Enhanced Touch Support API
- Prevents interaction with UI elements
- Automatic sensitivity adjustment

**Key Components:**
- `rotationSpeed` - Rotation sensitivity (degrees per pixel)
- `allowVerticalRotation` - Toggle for pitch rotation
- `allowPinchScale` - Toggle for zoom functionality
- `minScale`, `maxScale` - Zoom boundaries (0.05 - 5.0)
- `scaleSpeed` - Pinch sensitivity
- `modelLoader` - Reference to ARModelLoader for current model

**Touch State Variables:**
```csharp
float prevPinchDistance;        // Distance between two fingers at last frame
bool isPinching;                // Currently in pinch gesture
Vector2 prevTouchPos;           // Position of single touch at last frame
bool wasSingleTouch;            // Was in single-touch state
```

**Key Methods:**
- `Start()` - Auto-find ARModelLoader if not assigned
- `OnEnable()` - Enable Enhanced Touch Support
- `OnDisable()` - Disable Enhanced Touch Support
- `Update()` - Handle all touch inputs and gestures
- Property `Target` - Returns current model from loader

**Special Features:**
- **Multi-Touch Priority:** Two-finger pinch takes priority over single-touch rotation
- **UI Avoidance:** Checks if touch is over UI before processing (uses EventSystem)
- **Smooth Gestures:** Calculates delta between frames for fluid movement
- **Performance Optimized:** Uses Enhanced Touch System instead of legacy Input API
- **Smart Clamping:** Scale constrained between min/max to prevent extreme sizes

**Code Flow:**
```
Update()
  ├─ Get reference to current model (Target)
  ├─ If no target → return (wait for model)
  │
  ├─ Get active touches array
  │
  ├─ If touches.Count == 2 AND allowPinchScale:
  │   ├─ Set wasSingleTouch = false
  │   ├─ Calculate distance between two touch points
  │   ├─ If first pinch frame:
  │   │   ├─ Store prevPinchDistance
  │   │   └─ Set isPinching = true
  │   ├─ Else (continuing pinch):
  │   │   ├─ Calculate delta = current distance - previous distance
  │   │   ├─ Calculate newScale = currentScale + (delta × scaleSpeed)
  │   │   ├─ Clamp newScale to [minScale, maxScale]
  │   │   ├─ Apply uniform scale to Target
  │   │   └─ Update prevPinchDistance
  │   └─ Return (skip single-touch processing)
  │
  ├─ Set isPinching = false (pinch released)
  │
  ├─ If touches.Count == 1:
  │   ├─ Get single touch
  │   ├─ Check if touch is over UI → Skip if true
  │   │
  │   ├─ If touch.phase == Began:
  │   │   ├─ Store prevTouchPos = touch position
  │   │   ├─ Set wasSingleTouch = true
  │   │   └─ Return (wait for movement)
  │   │
  │   ├─ If wasSingleTouch AND touch.phase == Moved:
  │   │   ├─ Calculate delta = current position - prevTouchPos
  │   │   ├─ Rotate around Y-axis: Target.Rotate(Y, -delta.x × rotationSpeed)
  │   │   ├─ If allowVerticalRotation:
  │   │   │   └─ Rotate around X-axis: Target.Rotate(X, delta.y × rotationSpeed)
  │   │   └─ Update prevTouchPos
  │   │
  │   ├─ If touch.phase == Ended OR Canceled:
  │   │   └─ Set wasSingleTouch = false
  │   │
  │   └─ Update prevTouchPos
  │
  └─ Else (no touches):
      └─ Set wasSingleTouch = false
```

---

### 6. **ScreenshotCapture.cs**
**Purpose:** Capture and save screenshots of the AR scene to device storage with visual feedback.

**Functionality:**
- Captures complete AR scene including model and UI
- Saves screenshots to appropriate device storage location
- Platform-specific folder handling (Android DCIM/iOS Documents)
- Visual flash effect feedback
- "Saved!" notification display
- Automatic media gallery registration (Android only)

**Key Components:**
- `captureButton` - UI button to trigger screenshot
- `flashOverlay` - Full-screen white Image for flash effect
- `savedNotification` - "Saved!" text display
- `flashDuration` - Flash animation duration (0.15s)
- `notificationDuration` - Notification display time (2s)

**Key Methods:**
- `Start()` - Initialize UI button listeners
- `TakeScreenshot()` - Entry point to start capture
- `CaptureRoutine()` (IEnumerator) - Main capture logic

**Special Features:**
- **Platform Detection:** Custom folder paths per platform:
  - Android: `/storage/emulated/0/DCIM/ARCaptures/`
  - iOS: `Application.persistentDataPath/`
  - Editor: `Application.persistentDataPath/Screenshots/`
- **Timestamp Naming:** Format `ARCapture_YYYY-MM-DD_HH-mm-ss.png`
- **Flash Effect:** White overlay simulates camera flash
- **Android Gallery Integration:** Scans media after save so image appears in gallery
- **Automatic Directory Creation:** Creates folder structure if missing

**Code Flow:**
```
TakeScreenshot()
  └─ StartCoroutine(CaptureRoutine())
      │
      ├─ Wait until end of current frame (rendering complete)
      │
      ├─ Generate timestamp string (YYYY-MM-DD_HH-mm-ss format)
      ├─ Create filename: ARCapture_{timestamp}.png
      │
      ├─ Determine platform-specific folder:
      │   ├─ Android → /storage/emulated/0/DCIM/ARCaptures/
      │   ├─ iOS → persistentDataPath/
      │   └─ Editor → persistentDataPath/Screenshots/
      │
      ├─ Create directory if it doesn't exist
      │
      ├─ Build full file path: folder + filename
      │
      ├─ Wait until end of frame (ensure rendering is complete)
      │
      ├─ Create Texture2D (Screen.width × Screen.height, RGB24)
      ├─ Read pixels from screen into texture
      ├─ Apply texture (finalize pixel data)
      │
      ├─ Encode texture to PNG bytes
      ├─ Write PNG bytes to file
      ├─ Destroy texture (free memory)
      ├─ Log file path
      │
      ├─ Show flash effect:
      │   ├─ Activate flashOverlay
      │   ├─ Wait flashDuration seconds
      │   └─ Deactivate flashOverlay
      │
      ├─ [Android only] Trigger media scanner:
      │   ├─ Get AndroidJavaClass for MediaScannerConnection
      │   ├─ Get current Activity
      │   └─ Call scanFile() to register image with gallery
      │
      └─ Show and hide "Saved!" notification:
          ├─ Set notification text
          ├─ Activate notification
          ├─ Wait notificationDuration seconds
          └─ Deactivate notification
```

---

### 7. **ColorExtractor.cs** (Utility/Reference)
**Purpose:** Extract dominant color from screen and apply to model materials (currently unused).

**Functionality:**
- Captures entire screen as texture
- Calculates average color from all pixels
- Applies extracted color to model materials
- Supports multiple material property types

**Note:** This is a utility class referenced in older code but not actively used in current implementation. ColorPicker provides more control.

**Code Flow:**
```
CaptureAndApply(model) [Static Coroutine]
  ├─ Wait until end of frame
  ├─ Create Texture2D (Screen.width × Screen.height)
  ├─ Read screen pixels into texture
  ├─ Calculate average color:
  │   ├─ Sum all R, G, B channels across all pixels
  │   ├─ Divide by total pixel count
  │   └─ Return average as Color
  ├─ For each Renderer in model:
  │   └─ For each Material:
  │       ├─ Check for _BaseColor property → Set color
  │       ├─ Check for _Color property → Set color
  │       └─ Fallback to other property if found
  ├─ Clean up texture
  └─ Return
```

---

## Complete Application Workflow

### Scenario: Loading and Customizing a Model

1. **Application Launch**
   - ARSessionChecker verifies device AR support
   - AR Foundation initializes and begins tracking

2. **QR Scanning Phase**
   - User points camera at QR code
   - QRScanner processes frames every 60 updates
   - QR code detected (e.g., text = "car_model")

3. **Model Loading Phase**
   - ARModelLoader.LoadModel("car_model") triggered
   - Fetches `StreamingAssets/car_model.glb`
   - GLTFast library parses binary GLB data
   - Model instantiated with calculated scale
   - Positioned 1.5m in front of camera
   - ColorPicker enabled, "Scan Again" button shown

4. **User Interaction Phase**
   - **Rotation:** User drags finger → ModelRotator rotates model
   - **Zoom:** User pinches with two fingers → ModelRotator scales model
   - **Color:** User adjusts RGB sliders → ColorPicker tints all materials

5. **Capture Phase**
   - User taps "Capture" button
   - ScreenshotCapture saves frame to gallery
   - Flash effect and "Saved!" notification shown

6. **Next Model (Optional)**
   - User taps "Scan Again"
   - QRScanner resets (hasScanned = false)
   - Process repeats from step 2

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── ARModelLoader.cs          # Model loading & scaling
│   ├── ARSessionChecker.cs       # AR initialization
│   ├── ColorExtractor.cs         # Color sampling (utility)
│   ├── ColorPicker.cs            # RGB color customization
│   ├── Modelrotator.cs          # Touch gesture control
│   ├── QRScanner.cs             # QR detection
│   └── Screenshotcapture.cs     # Screenshot saving
├── StreamingAssets/             # GLB model files go here
├── Scenes/
│   └── SampleScene.unity        # Main AR scene
├── Resources/                   # UI resources, materials
├── Plugins/                     # External libraries (ZXing, GLTFast)
└── Settings/
    └── ProjectSettings.asset
```

---

## Dependencies & Libraries

- **Unity AR Foundation** - AR device tracking and camera access
- **GLTFast** - Loading binary GLTF/GLB 3D models
- **ZXing** - QR code detection and decoding
- **Enhanced Touch Support** - Modern multi-touch input handling
- **TextMesh Pro** - UI text rendering

---

## Data Flow Diagram

```
┌──────────────┐
│   Device    │
│   Camera    │
└──────┬───────┘
       │
       ├──────────────────┬────────────────────┐
       │                  │                    │
       ▼                  ▼                    ▼
   ┌────────────┐  ┌────────────┐      ┌──────────────┐
   │ QRScanner  │  │ColorExtract│      │ ScreenCapture│
   │(Decode QR)│  │(Sample BG) │      │(Frame Buffer)│
   └────┬───────┘  └────────────┘      └──────┬───────┘
        │                                      │
        │                                      ▼
        │                              ┌──────────────┐
        │                              │  File System │
        │                              │(Screenshots) │
        │                              └──────────────┘
        ▼
┌──────────────────────┐
│  ARModelLoader       │
│ (Fetch GLB model)    │
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐     ┌──────────────┐
│  Auto-Scale Model    │────▶│ ColorPicker  │
│  Render in AR Scene  │     │(Material Tint)
└──────┬───────────────┘     └──────────────┘
       │
       ▼
┌──────────────────────┐     ┌──────────────┐
│  ModelRotator        │────▶│ User's Screen│
│ (Touch Gestures)     │     │(AR Display)  │
└──────────────────────┘     └──────────────┘
```

---

## Key Technical Features

### 1. **Asynchronous Loading**
- All I/O operations (network, file, parsing) are non-blocking
- Uses coroutines for sequential async operations
- Prevents UI freezing during model load

### 2. **Performance Optimization**
- QR scanning every 60 frames reduces CPU load
- Image downsampling (4x) accelerates QR detection
- Efficient memory management with `using` statements

### 3. **Cross-Platform Support**
- Platform-specific code paths for Android/iOS
- Adaptive folder structure based on device
- Proper permission handling

### 4. **Material System Flexibility**
- Supports multiple shader types automatically
- Preserves original colors for accurate tinting
- Handles textured and non-textured materials

### 5. **Robust Error Handling**
- Graceful fallbacks for missing resources
- Comprehensive debug logging
- Timeout mechanisms to prevent hangs

---

## How to Use

1. **Setup AR Environment:**
   - Create an AR Foundation setup with ARSession, ARCameraManager
   - Assign managers to respective scripts (QRScanner, ARSessionChecker)

2. **Prepare 3D Models:**
   - Convert models to GLB format
   - Place in `Assets/StreamingAssets/` folder
   - Name them appropriately (e.g., `car_model.glb`)

3. **Create QR Codes:**
   - Generate QR codes containing model names
   - Print or display on screen
   - Point camera at QR code to load model

4. **Configure UI:**
   - Set up Canvas with Color Picker sliders
   - Add Screenshot button
   - Add loading screen prefab
   - Add "Scan Again" button

5. **Build & Deploy:**
   - Build for Android/iOS
   - Test on device with AR-capable hardware

---

## Debugging Tips

| Issue | Cause | Solution |
|-------|-------|----------|
| AR not initializing | Device doesn't support AR | Check device specs, enable in settings |
| QR code not scanning | Poor lighting, distance | Move closer, ensure good light |
| Model doesn't appear | Wrong filename, GLB format | Check StreamingAssets path, validate GLB |
| Scale is wrong | Model has extreme proportions | Adjust targetSizeInMeters parameter |
| Colors look off | Different shader system | Check material property names in logs |
| Touch not working | AR session not tracking | Wait for ARSessionState.SessionTracking |
| Screenshot location | Wrong platform target | Verify build platform selection |

---

## Notes

- **GLB Format:** Export 3D models as GLB (binary GLTF) for best compatibility
- **Model Size:** Target size is in meters; 0.3m = 30cm in real world
- **Color Space:** RGB values are normalized (0-1), not 0-255
- **Texture Preservation:** Original textures are maintained underneath color tints
- **Memory:** Destroy old models before loading new ones to prevent leaks

---

## Future Enhancements

- [ ] Multiple simultaneous models
- [ ] Physics-based model interactions
- [ ] Advanced lighting effects
- [ ] Animation support for GLB models
- [ ] Model import preview in Editor
- [ ] Local QR code database
- [ ] Cloud model storage integration

---

**Last Updated:** April 2026  
**Project Version:** 1.0  
**Developed for:** AR/VR Mini Project Course
