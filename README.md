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

## Driver Monitoring Extension (live gaze and hand tracking)

An added module that runs **live gaze and hand monitoring of a simulated driver** in the ego vehicle. A synthetic driver follows a scripted 58 s drive with two pedestrian events. Virtual cameras in Unity stream to a Python program that analyses them in real time with MediaPipe and OpenCV, shows a dashboard, records annotated video and writes a results report.

> **Simulation only.** The driver is a synthetic character seen by virtual cameras on one PC. There is no headset, no physical camera and no real participant. The reaction delays and glance times are scripted, so the results show that the measurement chain works; they are not findings about real drivers. The thresholds and calibration were tuned on earlier runs of the same simulation.

![Dashboard during the hand-off-wheel hazard](docs/driver_monitoring/screenshots/02_hand_off_wheel_hazard.png)

### What it does
- Scripted driver: eye saccades, blinks, head turns, talking, steering, two-handed grip and a reach to the centre display.
- Three virtual cameras (face, hands and wheel from the roof, outside view) streamed from Unity over a local TCP socket.
- Live analysis: gaze zone, eyes-off-road time, glances, blink rate, PERCLOS, speech activity, hands on wheel, hazard flag when the hand leaves the wheel, hazard reaction time.
- Live dashboard, recorded to video, followed by a results figure and HTML report.

### Results of the recorded session
| Measure | Result |
|---|---|
| Gaze yaw error against the simulator's true gaze | 1.17° mean, 2.82° RMSE (r = 0.985) |
| Gaze zone correct (4 zones, steady frames) | 99.9 % of 718 frames |
| Gaze reaction to pedestrian 1 / 2 (measured / true) | 0.568 s / 0.454 s and 1.307 s / 1.111 s |
| Braking starts after pedestrian appears | 0.68 s and 1.37 s |
| Blinks found / rendered | 17 / 17 |
| Speech detection | precision 99 %, recall 97 % |
| Both hands on the wheel | 91.5 % of frames; both display reaches found |
| Pipeline | 19.4 fps, median 204 ms capture to analysed frame |

Details, method and limits: [docs/driver_monitoring/PROJECT_DESCRIPTION.md](docs/driver_monitoring/PROJECT_DESCRIPTION.md).

### Media (Git LFS)
- [Dashboard recording, 1080p](docs/driver_monitoring/annotated_live.mp4)
- [Dashboard window recording](docs/driver_monitoring/dashboard_window_recording.mp4)
- [Face camera](docs/driver_monitoring/face_camera_raw.mp4) and [hand camera](docs/driver_monitoring/hand_camera_raw.mp4), unannotated
- [Results figure](docs/driver_monitoring/session_report.png) and [HTML report](docs/driver_monitoring/session_report.html)
- [Per-frame data and metrics](docs/driver_monitoring/data)

### Code
| Where | What |
|---|---|
| `Vehicle_Sim/Assets/Kevin_Driver/` | Unity (C#): driver behaviour, drive scenario, camera streamer, scene builder |
| `driver_monitoring/` | Python: live analysis, dashboard, metrics, report, launch scripts |

### Run it
1. Obtain the driver model: export a Reallusion Character Creator 5 character as FBX into `Vehicle_Sim/Assets/Kevin_Driver/Kevin/Kevin.fbx` (it is not included, it is a licensed asset). Import the Reallusion CC/iC Unity tools and the URP17 shader package. The project already depends on `com.unity.nuget.newtonsoft-json`.
2. Close any Unity editor that has `Vehicle_Sim` open, then build the scene once: `driver_monitoring\run_unity.ps1 -Method KevinSceneBuilder.Build`
3. Set up Python once: `driver_monitoring\setup.ps1` (Python 3.11, MediaPipe models, font).
4. Run a live session: `driver_monitoring\run_live.ps1`. The "Driver Monitoring - Live" window shows "Waiting for Unity..." until Unity has loaded (about a minute), then the dashboard starts. The session lasts about 60 s and records itself to `driver_monitoring\output\`.
5. Build the report: `driver_monitoring\.venv\Scripts\python.exe driver_monitoring\report.py`

Requires Windows, Unity 6000.0.60f1 and a GPU that can render three cameras.

## License

Licensed under the [MIT License](LICENSE).
