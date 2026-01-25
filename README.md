# Unity Nano Banana 

A Unity Editor tool that creates AI-generated images from your Unity Scene View using the Google Nano Banana Image API.

[![Unity Nano Banana Demo](https://img.youtube.com/vi/kVZgRQq9lZc/0.jpg)](https://www.youtube.com/watch?v=kVZgRQq9lZc)
*Click the image above to watch the demo video used for this presentation.*

## Features

- **Scene View Capture**: Instantly captures your current Unity Scene View as the base composition for generation.
- **Dual Models**:
  - **Nano Banana**: Faster generation.
  - **Nano Banana Pro**: High-fidelity, thinking model for professional results.
- **Reference Images**:
  - **Character Reference**: Maintain character identity across generations.
  - **Style Reference**: Enforce specific art styles.
  - **Object/Human References**: Add specific elements or poses (Pro model supports up to 14 references).
- **Templates**: Built-in style templates with support for custom user-defined templates.

## Installation

### Option 1: Unity Package (Recommended)
1. Download the latest `.unitypackage` from the **[Releases Page](https://github.com/roundyyy/UnityNanoBanana/releases)**.
2. Open your Unity project.
3. Double-click the downloaded `.unitypackage` file, or go to **Assets > Import Package > Custom Package...** in the Unity Editor.
4. Click **Import** to add the tool to your project.

### Option 2: Clone Repository
1. Navigate to your Unity project's `Assets` folder in your terminal.
2. Clone this repository into a folder (e.g., `UnityBanana`):
   ```bash
   git clone https://github.com/roundyyy/UnityNanoBanana.git
   ```

## Prerequisites

This tool requires a Google Gemini API Key.
**Note**: This API requires a paid plan.

[Get your API Key here](https://ai.google.dev/gemini-api/docs/api-key)

## Usage

1. **Open the Tool**: Go to `Tools > UnityBanana > Open Generator`.
2. **Setup API Key**:
   - Go to the **Settings** tab.
   - Paste your Google API Key and click **Save**.
3. **Capture Scene**:
   - Position your Scene View camera.
   - In the **Generation** tab, click **Capture Scene View**.
4. **Configure Generation**:
   - Select a **Model** (Standard or Pro).
   - Choose a **Template** or write a **Prompt**.
   - (Optional) Add **Reference Images** for style or character consistency.
5. **Generate**: Click **Generate Image**. The result will be saved to your output folder.

## Use Cases

- **Visual Storytelling**: Create comic stories with consistent places and characters using your Unity scene as the stage.
- **Video Generation Assets**:
    - Generate consistent scenes for video AI tools.
    - Create stylized "First Frame" and "Last Frame" keyframes for video generators (e.g., Veo).
    - **Workflow Example**: Pose a Unity skinned mesh character for frame 1, snapshot & generate. Change pose for frame 2, snapshot & generate. Use these as keyframes for interpolation.
