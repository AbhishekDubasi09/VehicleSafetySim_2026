# Live gaze and hand monitoring of a simulated driver: technical description

## Purpose
A simulated driver-monitoring test system. It follows the idea of eye and hand tracking for an XR test system (Unity, OpenCV, MediaPipe, C#) that evaluates how a person reacts to safety-relevant events, with image capture running live during the test session.

A synthetic driver (Reallusion Character Creator 5 character "Kevin") sits in the simulator's car in Unity and follows a scripted 58 s drive. Virtual cameras stream live to a Python program that analyses the frames with OpenCV and MediaPipe while the drive runs, shows a dashboard, records annotated video, and afterwards produces a results figure and report.

This is a simulation: no headset, no physical camera, no real person. The reaction delays (0.45 s and 1.05 s) and the glance timings are scripted, so the results show that the measurement chain works. They are not findings about real drivers.

## Components

### Unity (C#), `Vehicle_Sim/Assets/Kevin_Driver`
| File | Role |
|---|---|
| `KevinDriverBehaviour.cs` | The driver: seat pose, eyes, blinks, head, talking, steering, grip, reach to the display. Deterministic function of time, no Animator. |
| `DriveScenario.cs` | The 58 s timeline, pedestrian hazards, car speed and braking, per-frame ground truth. |
| `KevinLiveStreamer.cs` | Renders the three cameras, JPEG-encodes them and streams them over TCP. |
| `Editor/KevinSceneBuilder.cs` | Builds the scene (car, road, pedestrian, cameras, materials). |
| `Editor/KevinLiveLauncher.cs` | Enters play mode for a live session and quits the editor when it ends. |
| `Editor/KevinRecorder.cs` | Test tools: single-frame renders, automatic grip fit (`GripFit`), screen probe. |

### Python, `driver_monitoring/`
| File | Role |
|---|---|
| `live_dms.py` | Receiver, MediaPipe analysis, live dashboard, annotated recording. |
| `session_metrics.py` | Post-drive numbers, comparing the live measurements with the simulator's ground truth. |
| `report.py` | Results figure (`session_report.png`) and HTML report. |
| `typo.py` | Typeface (JetBrains Mono). |
| `run_live.ps1`, `run_unity.ps1`, `setup.ps1` | Launcher, Unity batch runner, one-time setup. |

## How it works

### Scene and cameras
- Car prefab with the VR rig and driving script switched off, a procedural road with buildings and trees, a pedestrian with a walking loop, a cabin fill light.
- Face camera on the dashboard, 1280x720, vertical field of view 38 degrees.
- Hand camera on the roof (headliner) on the driver's side, looking forward and down: car-local position (-0.32, 1.36, 0.00), aim (-0.30, 0.95, 0.42), 72 degree vertical field of view, 1024x576. It keeps both hands, both forearms, the wheel and the centre display in frame.
- Scene camera outside the car, 640x360, shown but not analysed.

### The driver
- Eyes: saccades with a duration of 22 ms plus 2.2 ms per degree, small fixational jitter, eye bones rotated in the head's frame.
- Head: follows the gaze target with critically damped smoothing (about 70 % head bone, 30 % neck).
- Blinks: Eye_Blink blendshapes, 120 to 200 ms, fast close and slower open, occasional double blinks.
- Talking: jaw bone (axis (0, 0, -1), up to 10 degrees) plus viseme blendshapes, alternating speaking and pausing.
- Hands: two-bone IK for the arms; wrist roll spread over the forearm twist bones; finger curl about the same axis on both hands. The grip parameters come from an automatic search (`GripFit`, about 22,000 candidates) that scores how well the fingertips wrap the rim and penalises wrist roll beyond about 75 degrees. A reach to the centre display uses a target found by raycasting the interior mesh.

### The drive (58 s)
0 to 12 s calibration on four labelled gaze targets (road, left mirror, passenger, hazard side); baseline driving; right hand reaches toward the display at about 17 to 20 s; pedestrian 1 at 22 s with eyes on the road (scripted gaze latency 0.45 s); talking to an off-screen passenger from 27.5 to 48.5 s with four passenger glances; pedestrian 2 at 46 s while looking at the passenger (latency 1.05 s); second reach at about 54 to 57 s. The car brakes after the driver's gaze reaches the hazard.

### Streaming
Each frame the three cameras are read back asynchronously, JPEG-encoded (quality 85) and sent to `127.0.0.1:5555` as `[int32 header length][int32 JPEG length][JSON header][JPEG]`. The header carries frame number, camera id, time, wall-clock time and the simulator's ground truth. Unity starts its clock 2 s after Python connects. If the consumer lags, the oldest queued frames are dropped.

### Analysis (Python)
- A receiver thread assembles frames; an inference thread runs MediaPipe Face Landmarker (iris, blendshapes, head pose); the Hand Landmarker runs on its own thread; the main thread draws the dashboard and pipes frames to ffmpeg (H.264).
- Gaze: horizontal angle from a linear fit of [head yaw, mean iris position] against the labelled targets during the 12 s calibration. Zones by nearest calibrated target, two-frame debounce, three-frame median filter.
- Blinks from the blink blendshape (above 0.30 is closed); PERCLOS over 60 s; speech from the variability of lip aperture over 1 s; glances and eyes-off-road time per zone; hazard reaction time as the first frame of the debounced hazard-zone run after the event marker.
- Hands: MediaPipe hand landmarks cannot follow a closed hand around the rim, so wheel contact and reaches use OpenCV skin segmentation (HSV) in fixed regions of the hand-camera image. When the right hand leaves its region while the left stays, the dashboard raises a hazard banner, in the same way as for looking away. MediaPipe skeletons are drawn only when they look plausible.
- Simulator ground truth is stored separately and used only for the calibration and for the comparison panels.

## Results of the recorded session (`data/session_metrics.json`)
- Gaze yaw error: 1.17 degrees mean, 2.82 RMSE, correlation 0.985 (867 frames).
- Gaze zone correct in 99.9 % of 718 steady frames (four zones).
- Gaze reaction to the pedestrian, measured live against true gaze arrival: 0.568 s against 0.454 s (pedestrian 1) and 1.307 s against 1.111 s (pedestrian 2).
- Braking starts 0.68 s and 1.37 s after the pedestrian appears; closest gap 8.5 m and 2.8 m.
- Eyes off the road: baseline 11 %, conversation 38 % with two glances over 2 s (longest 2.1 s), second hazard window 27 %.
- Blinks: 17 found for 17 rendered. Speech: precision 99 %, recall 97 %.
- Both hands on the wheel in 91.5 % of frames; both reaches to the display found.
- Pipeline: 1123 frames at 19.4 fps, median 204 ms from capture to analysed frame (267 ms at the 95th percentile), inference 29 ms per frame.

Figures vary a little between runs (for example the gaze reaction error ranged from 0 to about 200 ms across runs).

## Limits
- One synthetic subject, one session, clean rendering: no lighting changes, glasses or occlusion.
- Scripted delays and glances. The thresholds and the calibration were tuned on earlier runs of the same simulation, so the numbers are optimistic.
- The skin regions and thresholds are fixed for this camera position and skin tone.
- MediaPipe hand landmarks are found in most frames but not reliably on the closed grip, so they are not used for the wheel-contact decision.
- Horizontal gaze only; vertical iris position was too noisy.
- The pipeline ran at about 17 to 21 fps with Unity, three cameras and the analysis on one PC.
- A headset hides the eyes, so cabin-camera gaze estimation applies to a physical simulator or screen setup. Quest 3 has no eye tracking.

## Possible next steps
- Replace the skin regions with a trained in-cabin hand detector.
- Calibrate gaze with an on-screen target routine so it works with a real participant, and run the same receiver on a webcam.
- Study with real participants under a proper protocol, with confidence intervals.
- Capture from a built player instead of the editor for a higher frame rate.
