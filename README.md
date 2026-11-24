# AETHER Core Engine

### An Intelligent AI Desktop Companion — Inspired by J.A.R.V.I.S.

**Project Initiated: June 28, 2026**

---

## About the Project

**AETHER Core Engine** (Advanced Executive Tactical & Holographic Engine Resource) is an intelligent, voice-enabled AI desktop assistant designed to transform the way humans interact with their computers.

Inspired by the fictional **J.A.R.V.I.S. (Just A Rather Very Intelligent System)** from Marvel's Iron Man universe, AETHER explores the idea of bringing a futuristic, context-aware, voice-controlled personal AI companion into the real world.

The inspiration behind J.A.R.V.I.S. goes beyond its voice or holographic interface. It represents a vision of an intelligent system that can understand instructions, assist with complex tasks, interact with its environment, and work alongside its user.

AETHER aims to bring elements of that vision into a practical Windows desktop environment.

Built using **.NET 8.0 and WPF**, AETHER combines intelligent conversational interaction, real-time screen understanding, neural voice synthesis, desktop automation, hardware monitoring, and autonomous background task execution into a unified AI-powered workstation.

The internal project architecture also retains the `JarvisCoreEngine` naming convention as a deliberate tribute to the inspiration behind AETHER.

**AETHER is not a recreation of the fictional J.A.R.V.I.S. It is an independent engineering project that takes inspiration from that concept and explores what a practical desktop AI companion can become.**

---

## The Vision Behind AETHER

The goal is simple:

> Your computer shouldn't just execute commands. It should understand what you need, help you accomplish it, and work alongside you.

AETHER is designed around five fundamental principles:

- **Intelligence:** Understand natural-language instructions and provide contextual assistance.
- **Awareness:** Analyze the active screen and understand what is happening on the workstation.
- **Autonomy:** Execute background missions without interrupting the user's workflow.
- **Interaction:** Provide natural voice and text-based communication.
- **Integration:** Connect AI capabilities directly with Windows desktop operations.

---

## Key Features

### 1. AETHER Vision — Instant Screen Intelligence

- Hotkey: `Alt + S`
- Captures the active display buffer.
- Transmits screen content to Groq vision models.
- Analyzes source code, errors, terminal messages, and application interfaces.
- Streams contextual AI-generated explanations in real time.

AETHER Vision allows the assistant to understand what is currently happening on the user's screen without requiring manual screenshots or copied text.

### 2. Autonomous Ghost Agent — Mission Operative

- Trigger: Click GHOST or use the voice command `Ghost mission: [objective]`.
- Executes asynchronous background tasks.
- Researches, analyzes, and compiles detailed reports.
- Generates publication-grade dossiers directly inside the Workspace folder.
- Allows the user to continue working while missions execute independently.

The Ghost Agent introduces an autonomous task-execution layer inspired by the idea of delegating complex work to an intelligent digital assistant.

### 3. Studio-Quality Neural Voice and Audio Pipeline

- Microsoft Neural Edge-TTS integration.
- British voice: `en-GB-RyanNeural`
- American voice: `en-US-ChristopherNeural`
- Hindi voice: `hi-IN-MadhurNeural`
- Offline SAPI 5 fallback.
- Smart Hinglish-to-Devanagari transliteration.
- Voice feedback suppression to prevent microphone echo loops.

AETHER is designed to communicate naturally rather than behaving like a traditional command-line utility.

### 4. Live System Telemetry HUD

- Real-time CPU utilization.
- RAM utilization monitoring.
- Hardware telemetry using Win32 APIs.
- CPU monitoring through `GetSystemTimes`.
- Memory monitoring through `GlobalMemoryStatusEx`.
- Connection status indicators.
- Audio-reactive equalizer visualization.

The HUD provides a real-time overview of workstation activity through a futuristic Arc Reactor-inspired interface.

### 5. PC Desktop Automation and System Control

**Application Launcher**
- Open Chrome
- Open VS Code
- Open Notepad
- Open Calculator
- Open Task Manager
- Open Spotify
- Open YouTube
- Open GitHub

**System Controls**
- Mute / Unmute
- Volume Up / Volume Down
- Lock PC / Lock Workstation

**Diagnostics**
- System Status
- Health Check
- Spoken hardware status reports

**Web Intelligence**
- Search for [topic]
- Google [topic]

### 6. Global Tactical Summon Hotkeys

| Shortcut | Function |
|---|---|
| `Alt + Space` | Summon / Dismiss AETHER |
| `Alt + J` | Secondary summon shortcut |
| `Alt + S` | AETHER Vision screen analysis |
| `Enter` | Transmit command or query |
| GHOST Button | Launch autonomous mission |
| VISION Button | Initiate screen analysis |

AETHER can be summoned from other applications and minimized back to the system tray.

---

## Architecture and Solution Structure

The architecture follows a modular design, separating the core intelligence engine, desktop interface, and API layer.

The `JarvisCoreEngine` naming convention is intentionally retained as a reference to J.A.R.V.I.S., the original conceptual inspiration behind the project.

```text
Aether/
|
|-- Aether.sln
|-- .gitignore
|-- README.md
|
|-- JarvisCoreEngine/
|   |-- Commands/
|   |-- Interfaces/
|   |-- Jobs/
|   |-- Models/
|   |-- Services/
|   |-- Utilities/
|
|-- JarvisCoreEngine.Desktop/
|   |-- App.xaml / App.xaml.cs
|   |-- appsettings.json
|   |-- MainWindow.xaml / MainWindow.xaml.cs
|   |-- Assets/
|   |-- Services/
|   |   |-- EdgeTtsService.cs
|   |   |-- GhostAgentService.cs
|   |   |-- SystemControlService.cs
|   |   |-- TransliterationService.cs
|   |   |-- TrayService.cs
|   |   |-- VisionService.cs
|   |   |-- VoiceService.cs
|   |-- Themes/
|   |-- Views/
|
|-- JarvisCoreEngine.Api/
    |-- Controllers/
    |-- Dtos/
```

---

## Technology Stack

| Component | Technology |
|---|---|
| Core Framework | .NET 8.0 |
| Desktop Interface | WPF |
| Programming Language | C# |
| AI / LLM Integration | Groq API |
| Vision Intelligence | Groq Vision Models |
| Neural Voice | Microsoft Edge-TTS |
| Offline Voice | Windows SAPI 5 |
| System Telemetry | Windows Win32 APIs |
| API Layer | ASP.NET Core |
| Desktop Architecture | Modular Service-Based Architecture |

---

## Getting Started

### Prerequisites

- Windows 10 / Windows 11 (64-bit)
- .NET 8.0 SDK or later
- Visual Studio 2022 / Rider / VS Code with C# Dev Kit

### Configuration

AETHER requires a Groq API key for AI-powered functionality.

**Option A — Environment Variable (Recommended)**

```powershell
setx GROQ_API_KEY "your_groq_api_key_here"
```

**Option B — In-App Settings**

Open the Settings Window through the System Tray or HUD and configure your API key. Settings can be applied without restarting the application.

### Build and Run

```powershell
# Restore and build the solution
dotnet build Aether.sln

# Launch the desktop application
dotnet run --project JarvisCoreEngine.Desktop
```

---

## Project Philosophy

AETHER represents an exploration of the future of personal computing.

Taking inspiration from the J.A.R.V.I.S. concept, the project focuses on bringing together voice interaction, contextual intelligence, computer vision, automation, and autonomous task execution into one cohesive desktop environment.

Rather than building another chatbot inside a window, the objective is to create an AI companion that can interact with the actual operating system and assist with real workstation activities.

**The inspiration is J.A.R.V.I.S. The implementation is AETHER.**

---

## Author

**Pratha**

*Independent AI Desktop Assistant Engineering Project*

**Core Framework:** .NET 8.0 WPF

**Project Name:** AETHER Core Engine

