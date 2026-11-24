# AETHER Core Engine

## Project Initiated: June 28, 2026

## Overview

AETHER Core Engine (Advanced Executive Tactical & Holographic Engine Resource) is an intelligent, voice-enabled AI desktop system. Built on .NET 8.0 WPF, AETHER combines global hotkey access, an Arc Reactor HUD visualizer, real-time token streaming, neural voice synthesis, live hardware telemetry, computer vision screen analysis, and autonomous background mission execution.

---

## Key Features

### AETHER Vision (Instant Screen Intelligence)
* Hotkey: Alt + S (or click VISION on the HUD, or voice: "Analyze screen").
* Captures the active display buffer, transmits it to Groq's llama-3.2-11b-vision-preview model, and streams real-time tactical breakdowns of code, errors, terminal messages, and UI layouts without manual copying.

### Autonomous Ghost Agent (Mission Operative)
* Trigger: Click GHOST (or voice: "Ghost mission: [objective]").
* Dispatches an asynchronous background worker to research, analyze, and compile publication-grade dossiers directly into your Workspace folder while you continue working uninterrupted.

### Studio-Quality Neural Voice and Audio Pipeline
* Microsoft Neural Edge-TTS: Fluent British voice (en-GB-RyanNeural), American voice (en-US-ChristopherNeural), and Hindi voices (hi-IN-MadhurNeural).
* Offline SAPI 5 Fallback: Zero-downtime voice synthesis when disconnected from network.
* Smart Hinglish Transliteration: Automatically converts Romanized Hindi speech into Devanagari script for natural pronunciation.
* Voice Feedback Suppression: Automatically pauses microphone listener while AETHER is actively speaking to prevent echo loops.

### Live System Telemetry HUD
* Meters on HUD: Real-time CPU Load % and RAM Utilization % with memory tooltips via zero-overhead Win32 APIs (GetSystemTimes, GlobalMemoryStatusEx).
* Status Bar: Connection indicators and responsive audio equalizer waves driven by microphone input and TTS output.

### PC Desktop Automation and System Control
* App Launcher: "Open Chrome", "Open VS Code", "Open Notepad", "Open Calculator", "Open Task Manager", "Open Spotify", "Open YouTube", "Open GitHub".
* Volume Controls: "Mute", "Unmute", "Volume up", "Volume down".
* Workstation Security: "Lock PC", "Lock workstation" (instant Windows lock).
* Diagnostic Report: "System status", "Health check" (spoken hardware brief).
* Web Search: "Search for [topic]", "Google [topic]".

### Global Tactical Summon Hotkeys
* Alt + Space (or Alt + J): Instantly summons AETHER to the foreground from any full-screen application. Pressing it again minimizes it back to the system tray.
* Alt + S: Triggers immediate AETHER Vision screen capture and analysis.

---

## Tactical Shortcuts Reference

| Shortcut | Function | Description |
| :--- | :--- | :--- |
| Alt + Space | Summon / Dismiss | Toggles HUD overlay over active applications |
| Alt + S | AETHER Vision | Snaps screen and streams AI visual analysis |
| Alt + J | Summon Fallback | Secondary hotkey if Alt + Space is intercepted |
| Enter | Transmit | Sends command or query in chat bar |
| Click Ghost Button | Ghost Agent | Dispatches background research mission |
| Click Vision Button | Vision Scan | Manually initiates screen reconnaissance |

---

## Architecture and Project Structure

```
JarvisCoreEngine.Desktop/
├── App.xaml / App.xaml.cs          # Application bootstrapper and DI container
├── appsettings.json                # Configuration and LLM endpoint settings
├── MainWindow.xaml / .cs           # Hologram HUD, Arc Reactor and chat panel
├── Assets/
│   └── jarvis-icon.ico             # Application icon
├── Services/
│   ├── EdgeTtsService.cs           # Microsoft Edge Neural TTS client
│   ├── GhostAgentService.cs        # Autonomous background research agent
│   ├── SystemControlService.cs     # Windows PC automation and hardware telemetry
│   ├── TransliterationService.cs   # Hinglish-to-Devanagari text pipeline
│   ├── TrayService.cs              # System tray residency and global hotkeys
│   ├── VisionService.cs            # Screen capture and Llama 3.2 Vision engine
│   └── VoiceService.cs             # Speech recognition (STT) and playback (TTS)
├── Themes/
│   └── JarvisTheme.xaml            # Cyberpunk neon palette and gradient brushes
└── Views/
    └── SettingsWindow.xaml / .cs   # Graphical settings manager
```

---

## Getting Started

### Prerequisites
* Windows 10 / 11 (64-bit)
* .NET 8.0 SDK or later
* Visual Studio 2022 / Rider / VS Code with C# Dev Kit

### Configuration
AETHER accepts API credentials through environment variables or configuration files:

1. Option A (Recommended - Environment Variable):
   ```powershell
   setx GROQ_API_KEY "your_groq_api_key_here"
   ```
2. Option B (In-App Settings):
   Open the Settings Window (Settings via System Tray or HUD) and enter your API key directly. Settings apply live without restarting.

### Build and Run
```powershell
# Restore and Build
dotnet build

# Launch AETHER
dotnet run
```

---

## Author
* Pratha
* Core Framework: .NET 8.0 WPF
