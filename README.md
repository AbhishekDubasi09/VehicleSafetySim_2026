# VehicleSafetySim 2026

A Unity/VR driving simulator for studying driver behavior in **Autonomous Emergency Braking (AEB) pedestrian-conflict scenarios**, modeled on Euro NCAP-style test protocols (CPNA, CPNCO, and related crossing-pedestrian scenarios). Drivers pilot an instrumented ego vehicle through proving-ground scenes while AI traffic, dynamic weather, and scripted pedestrian crossings create realistic hazard events; each run is logged to CSV for post-session analysis.

## Overview

The simulator places a driver behind the wheel of a physically simulated vehicle (wheel colliders, adjustable center of mass) inside VR, using head-mounted display and hand tracking via OpenXR/Oculus. Pedestrians cross the road on scripted paths and trigger points, replicating standardized AEB test scenarios so that driver reaction time, braking response, and collision outcomes can be recorded and reviewed.

## Key Features

- **Physically-based vehicle model** — wheel-collider driven car controller (`EGO_V1`) with tunable stability, HUD, and dashboard gauges
- **Standardized pedestrian conflict scenarios** — scripted crossing triggers (`CPNATrigger`, `CPNOTrigger`) reproducing Euro NCAP-style AEB test cases, with configurable target speed, impact point, and pedestrian variants
- **AI traffic** — independent traffic agents (`TrafficAI`, `NPCSpawner`) with speed, steering, braking, and sensor-based behavior
- **Dynamic weather & lighting** — runtime-adjustable rain, snow, fog, and time-of-day via `DynamicWeatherManager` / `WeatherController`
- **VR interaction** — Unity XR Interaction Toolkit + XR Hands support for in-cockpit controls (physical buttons, touch interactors)
- **Proving-ground scenes** — multiple purpose-built test scenes (City Master, Proving Grounds Master, CPNCO PG, Roads Testing) authored with Road Architect
- **Session data logging** — each test run is exported as a timestamped CSV under `Vehicle_Sim/Test Data/` for offline analysis

## Tech Stack

| Component | Details |
|---|---|
| Engine | Unity **6000.0.60f1** (Unity 6) |
| Render pipeline | Universal Render Pipeline (URP) 17.0.4 |
| XR | XR Interaction Toolkit 3.0.9, XR Hands 1.5.1, OpenXR 1.15.1, Oculus 4.5.2 |
| Input | Unity Input System 1.14.2 |
| Road authoring | Road Architect (`com.barmetler.roadsystem`) |
| Version control | Git + Git LFS (binary/art assets) |

## Project Structure

```
VehicleSafetySim_2026/
├── Vehicle_Sim/                  # Unity project root
│   ├── Assets/
│   │   ├── Scripts/              # Gameplay & simulation logic
│   │   │   ├── EGO_V1.cs             # Player vehicle controller
│   │   │   ├── TrafficAI.cs          # AI traffic behavior
│   │   │   ├── CPNATrigger.cs        # Pedestrian scenario trigger (AEB test)
│   │   │   ├── CPNOTrigger.cs        # Pedestrian scenario trigger (AEB test)
│   │   │   ├── PedestrainController.cs
│   │   │   ├── DynamicWeatherManager.cs
│   │   │   ├── CarCollisionHUD.cs / CarCollisionReset.cs
│   │   │   ├── GaugeController.cs    # Dashboard instrumentation
│   │   │   └── Spawner/              # NPC spawning & prefabs
│   │   ├── Scenes/                # City Master, Proving Grounds, CPNCO PG, Roads Testing
│   │   ├── Main Car/               # Ego vehicle assets
│   │   ├── AI Traffic Cars/        # Traffic vehicle assets
│   │   ├── VRUs/                   # Vulnerable road user (pedestrian) assets
│   │   ├── KajamansRoads/, RoadArchitect/  # Road authoring assets
│   │   └── ...                     # Terrain, environment, materials, XR assets
│   ├── Packages/                   # Unity Package Manager manifest
│   ├── ProjectSettings/            # Unity project configuration
│   └── Test Data/                  # Logged CSV output from simulation runs
├── .gitattributes                  # Git LFS tracking rules
└── .gitignore                      # Unity-specific ignore rules
```

## Getting Started

### Prerequisites

- [Unity Hub](https://unity.com/download) with editor version **6000.0.60f1** installed
- [Git LFS](https://git-lfs.com/) (`git lfs install`) — required before cloning, since art/binary assets are tracked via LFS
- A VR headset with OpenXR or Oculus runtime support (optional — required for VR play mode)

### Clone & Open

```bash
git lfs install
git clone https://github.com/AbhishekDubasi09/VehicleSafetySim_2026.git
```

1. Open **Unity Hub** → **Add project from disk** → select `VehicleSafetySim_2026/Vehicle_Sim`
2. Let Unity resolve packages from `Packages/manifest.json`
3. Open a scene from `Assets/Scenes` (e.g. `Proving Grounds Master.unity`)
4. Press **Play** (desktop) or connect a headset for VR mode

### Test Data

Each session run writes a timestamped CSV to `Vehicle_Sim/Test Data/` for reviewing driver response and collision outcomes across scenario runs.

## License

Licensed under the [MIT License](LICENSE).
