# 🚗 Automotive Interactive Showcase
## AI-Powered Car Customization Experience | Unity Project

---

## 📋 Overview

An **interactive car showcase** built with Unity that allows users to modify car colors, wheel styles, and aesthetics in real-time. **Generative AI** suggests 3-5 customization configurations based on themes like **"Sporty"**, **"Luxury"**, **"Classic"**, **"Futuristic"**, and **"Offroad"**.

---

## 🚀 SETUP — Just press Play!

### Steps:
1. **Create a new Unity 3D project** in Unity Hub (any version 2021.3+)
2. **Copy the `Assets/Scripts/` folder** into your project's `Assets/` folder
3. When prompted, click **Import TMP Essentials** (TextMeshPro)
4. **Press Play** ▶️

> **That's it.** The `Bootstrap.cs` script uses `[RuntimeInitializeOnLoadMethod]` to automatically build the ENTIRE scene at runtime — car, showroom, camera, lighting, UI, and AI system. No GameObjects to create. No scripts to drag. No inspector to configure. Just press Play.

---

## ✨ Features Implemented

### 🎮 Game Mechanics
- **Real-time color customization** — 10 preset color swatches + RGB sliders
- **Material properties** — Metallic and Smoothness sliders (matte → chrome)
- **Wheel style switching** — 5 wheel types (Standard, Sport, Luxury, Offroad, Racing)
- **Smooth animated transitions** — All color changes lerp smoothly
- **Orbit camera** — Left-click drag to rotate, scroll to zoom
- **Auto-rotation** — Camera auto-orbits when idle
- **Camera presets** — Front, Side, Rear, Top views
- **Reset button** — One-click return to default configuration

### 🎨 Graphics
- **Professional 3-point lighting** — Key (warm), Fill (cool blue), Rim (edge highlight)
- **Overhead spotlights** — 3 dramatic spotlights from ceiling
- **Blue accent floor lights** — 4 point lights around turntable
- **Reflective showroom floor** — High-gloss metallic floor
- **Turntable platform** — Metallic display platform with accent ring
- **Fog & atmosphere** — Exponential fog for depth
- **Real-time reflection probe** — Dynamic reflections on car

### 🤖 Generative AI Integration
- **5 AI themes** — Sporty, Luxury, Classic, Futuristic, Offroad
- **25+ unique configurations** — 5 named configs per theme with descriptions
- **OpenAI API support** (optional) — Set API key for GPT-powered dynamic generation
- **Built-in presets** (default) — Works out of the box, no API key needed
- **AI suggestion cards** — Color preview, name, description, finish type, and APPLY button
- **Real-time application** — Click APPLY and watch the car smoothly transition

---

## 🎯 Controls

| Action | Control |
|--------|---------|
| **Rotate Camera** | Left-click + Drag |
| **Zoom In/Out** | Mouse Scroll Wheel |
| **Change Color** | Click color swatches in the left panel |
| **Fine-tune Color** | Adjust R/G/B sliders |
| **Change Paint Finish** | Adjust Metallic & Smoothness sliders |
| **Change Wheels** | Click wheel style buttons (S/S/L/O/R) |
| **AI Suggestions** | Click theme buttons (Sporty/Luxury/etc.) |
| **Apply AI Suggestion** | Click "APPLY" on a suggestion card |
| **Camera Preset** | Click F/S/R/T buttons on the right |
| **Reset Car** | Click "⟳ RESET" |

---

## 🤖 Generative AI Component

### How It Works

The AI system operates in two modes:

#### Mode 1: Built-in Presets (Default — No API Key Required)
25+ curated car configurations spanning 5 themes. Each config specifies:
- Body color, metallic value, smoothness (matte/satin/gloss/chrome)
- Wheel style index and wheel color
- Brake calliper color, window tint, interior color
- Config name and description explaining the design rationale

#### Mode 2: OpenAI API Integration (Optional)
Set your OpenAI API key in the `AIStyleEngine` component's inspector to enable GPT-powered dynamic suggestions. Falls back to built-in presets if API fails.

### Theme Examples

| Theme | Sample Configs |
|-------|---------------|
| **Sporty** | Track Day Red, Electric Blue Sprint, Neon Green Venom, Sunset Orange GT, Stealth Matte Black |
| **Luxury** | Champagne Prestige, Midnight Sapphire, Pearl White Diamond, British Racing Green, Rose Gold Edition |
| **Classic** | Cherry Red Classic, Cream & Chrome, Navy Blue Heritage, Forest Green Touring, Silver Arrow |
| **Futuristic** | Cyberpunk Neon, Holographic Chrome, Quantum Purple, Arctic White EV, Sunset Gradient |
| **Offroad** | Desert Storm, Jungle Hunter, Arctic Explorer, Volcanic Orange, Rock Crawler Gray |

---

## 📁 Project Structure

```
AutoShowcase/
├── README.md
└── Assets/Scripts/
    ├── Core/
    │   ├── Bootstrap.cs              ← AUTO-STARTS everything (no setup needed)
    │   ├── AutoSetup.cs              ← Builds entire scene at runtime
    │   ├── GameManager.cs            ← Central coordinator
    │   ├── CameraOrbitController.cs  ← Orbit camera
    │   └── QuickSceneSetup.cs        ← Alternative manual setup (optional)
    ├── Customization/
    │   └── CarCustomizer.cs          ← Paint/wheels/material system
    ├── Data/
    │   └── CarConfiguration.cs       ← Configuration data model
    ├── AI/
    │   └── AIStyleEngine.cs          ← Generative AI engine
    ├── Environment/
    │   ├── EnvironmentManager.cs     ← Lighting & atmosphere
    │   ├── ShowroomGenerator.cs      ← Procedural showroom
    │   └── TurntableRotator.cs       ← Platform rotation
    └── UI/
        ├── UIController.cs           ← UI event handling
        ├── UIBuilder.cs              ← Runtime UI constructor
        ├── SuggestionCard.cs         ← AI suggestion card component
        └── UIInteractionFeedback.cs  ← Hover/click animations
```

---

## 🏗️ Building for Distribution

### Windows Build
1. File → Build Settings → PC, Mac & Linux Standalone
2. Add Open Scenes → Build
3. Share the entire output folder (.exe + _Data)

### WebGL Build
1. File → Build Settings → WebGL → Build
2. Host output on any web server

---

## 📝 Technical Notes

- **Zero-setup**: Uses `[RuntimeInitializeOnLoadMethod]` — no scene configuration needed
- **No external dependencies**: Only Unity built-in packages + TextMeshPro
- **All UI built at runtime**: No prefabs, no manual UI creation
- **Car built from primitives**: Works without importing any 3D models
- **Supports real car models**: Optionally import cars from Asset Store
- **Pipeline compatible**: Works with both Standard and URP shaders

---

*Built for the Solutions Engineer Evaluation Assignment — Automotive Theme*
