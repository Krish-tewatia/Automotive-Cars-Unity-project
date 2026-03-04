# Automotive Interactive Showcase
## AI-Powered Car Customization Experience | Unity Project

---

## Overview

An interactive car showcase built with Unity that allows users to modify car colors, wheel styles, and aesthetics in real-time. The project now supports a dynamic multi-car showroom (primitive + imported car assets) with Generative AI suggesting 3-5 customization configurations for themes like Sporty, Luxury, Classic, Futuristic, and Offroad.

---

## SETUP - Just press Play!

### Steps:
1. Open this repository as a Unity project (Unity 2021.3+ recommended)
2. If prompted, import required TMP/UI essentials
3. Press Play

> That is it. The `Bootstrap.cs` script uses `[RuntimeInitializeOnLoadMethod]` to automatically build the entire scene at runtime - cars, showroom, camera, lighting, UI, and AI system. No manual scene wiring required.

---

## Features Implemented

### Game Mechanics
- Real-time color customization - 10 preset color swatches + RGB sliders
- Material properties - Metallic and Smoothness sliders (matte to chrome)
- Wheel style switching - Runtime wheel swapping with multiple wheel prefabs
- Adaptive wheel fitting for imported cars - Wheel size auto-fits imported models
- Primitive car wheel stability - Primitive car keeps fixed/non-adaptive wheel behavior
- Improved wheel alignment - Correct outward-facing wheel orientation on both sides
- Smooth animated transitions - Color/config changes lerp smoothly
- Orbit camera - Left-click drag to rotate, scroll to zoom
- Auto-rotation - Camera auto-orbits when idle
- Camera presets - Front, Side, Rear, Top views
- Reset button - One-click return to default configuration for active car

### Multi-Car Showroom
- Built-in base cars - Classic Sedan (primitive) + Sport GT
- Auto-loaded imported cars - Additional prefabs are discovered and added automatically
- Dynamic car registration - All discovered cars are registered to `GameManager`
- Dynamic car selector UI - Bottom selector builds buttons for every available car
- Showroom arrangement system - Cars are auto-placed in a clean staggered layout
- Per-car material instancing - Customizing one car does not recolor other cars
- Auto renderer classification - Body/windows/tires/wheels/lights/interior are inferred from names/materials

### Graphics
- Professional 3-point lighting - Key (warm), Fill (cool blue), Rim (edge highlight)
- Accent floor lights - Point-light accents around the display area
- Reflective showroom floor + turntable - Glossy platform styling
- Fog and atmosphere - Exponential fog for depth
- Real-time reflection probe - Dynamic reflections on cars
- Large modular showroom - Runtime-built modular space (walls, floor, roof, decor)

### Generative AI Integration
- 5 AI themes - Sporty, Luxury, Classic, Futuristic, Offroad
- 25+ curated fallback configurations - Theme-based presets for reliable offline/demo use
- Hugging Face chat-completions API support (optional)
- Built-in fallback mode - Works without API key and also handles API failures safely
- AI suggestion cards - Color preview, name, description, finish type, and Apply button
- Real-time application - Click Apply and watch the selected car transition

---

## Controls

| Action | Control |
|--------|---------|
| Rotate Camera | Left-click + Drag |
| Zoom In/Out | Mouse Scroll Wheel |
| Select Car | Click car buttons in the bottom selector |
| Change Color | Click color swatches in the left panel |
| Fine-tune Color | Adjust R/G/B sliders |
| Change Paint Finish | Adjust Metallic and Smoothness sliders |
| Change Wheels | Use wheel panel (prev/next or quick wheel buttons) |
| AI Suggestions | Click theme buttons (Sporty/Luxury/etc.) |
| Apply AI Suggestion | Click APPLY on a suggestion card |
| Camera Preset | Click F/S/R/T buttons on the right |
| Reset Car | Click RESET |

---

## Generative AI Component

### How It Works

The AI system operates in two modes:

#### Mode 1: Built-in Presets (Default - No API Key Required)
25+ curated car configurations spanning 5 themes. Each config specifies:
- Body color, metallic value, smoothness (matte/satin/gloss/chrome)
- Wheel style index and wheel color
- Brake calliper color, window tint, interior color
- Config name and description explaining the design rationale

#### Mode 2: Hugging Face API Integration (Optional)
Set `HF_KEY` in a project-root `.env` file to enable generative suggestions through:
- Endpoint: `https://router.huggingface.co/v1/chat/completions`
- Default model: `meta-llama/Meta-Llama-3-8B-Instruct`

If API call or parsing fails, the system automatically falls back to built-in presets.

### Theme Examples

| Theme | Sample Configs |
|-------|---------------|
| Sporty | Track Day Red, Electric Blue Sprint, Neon Green Venom, Sunset Orange GT, Stealth Matte Black |
| Luxury | Champagne Prestige, Midnight Sapphire, Pearl White Diamond, British Racing Green, Rose Gold Edition |
| Classic | Cherry Red Classic, Cream and Chrome, Navy Blue Heritage, Forest Green Touring, Silver Arrow |
| Futuristic | Cyberpunk Neon, Holographic Chrome, Quantum Purple, Arctic White EV, Sunset Gradient |
| Offroad | Desert Storm, Jungle Hunter, Arctic Explorer, Volcanic Orange, Rock Crawler Gray |

---

## Project Structure

```
AutoShowcase/
|-- README.md
|-- Assets/Scripts/
    |-- Core/
    |   |-- Bootstrap.cs              <- Auto-starts everything
    |   |-- AutoSetup.cs              <- Builds scene + multi-car system at runtime
    |   |-- GameManager.cs            <- Central coordinator
    |   |-- CameraOrbitController.cs  <- Orbit camera
    |   |-- QuickSceneSetup.cs        <- Alternative manual setup (optional)
    |-- Customization/
    |   |-- CarCustomizer.cs          <- Paint/wheels/material system
    |-- Data/
    |   |-- CarConfiguration.cs       <- Configuration data model
    |-- AI/
    |   |-- AIStyleEngine.cs          <- Generative AI engine
    |-- Environment/
    |   |-- EnvironmentManager.cs     <- Lighting and atmosphere
    |   |-- ShowroomGenerator.cs      <- Procedural showroom support
    |   |-- TurntableRotator.cs       <- Platform rotation
    |-- UI/
        |-- UIController.cs           <- UI event handling
        |-- UIBuilder.cs              <- Runtime UI constructor (dynamic car selector)
        |-- SuggestionCard.cs         <- AI suggestion card component
        |-- UIInteractionFeedback.cs  <- Hover/click animations
```

---

## Building for Distribution

### Windows Build
1. File -> Build Settings -> PC, Mac and Linux Standalone
2. Add Open Scenes -> Build
3. Share the entire output folder (.exe + _Data)

### WebGL Build
1. File -> Build Settings -> WebGL -> Build
2. Host output on any web server

---

## Technical Notes

- Zero-setup runtime bootstrap: `[RuntimeInitializeOnLoadMethod]`
- All major systems are created at runtime: scene objects, UI, camera, AI, manager wiring
- Dynamic multi-car loading: imported prefabs can be discovered and integrated automatically
- Independent per-car customization: material instancing prevents cross-car side effects
- AI API key loading from `.env`: `HF_KEY=` is read by `GameManager`
- Built-in AI fallback: project remains fully functional without external API access
- Pipeline compatibility: designed around Standard/URP-compatible material handling

---

Built for the Solutions Engineer Evaluation Assignment - Automotive Theme
