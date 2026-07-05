# SFX Implementation List Report

Date: 2026-07-05  
Branch: feature/addvisual  
Sources:
- Google Doc: `Cure Me, Please`
- Local list: `C:/Users/kitti/OneDrive/เดสก์ท็อป/Sfx.txt`

## Purpose

This document maps the SFX/BGM requirements from the GDD to the currently listed audio asset names. It is intended as a handoff list for implementation and for checking which sounds are still missing.

## Available / Listed Audio

### Door

| Audio name | Intended use |
| --- | --- |
| `SFX_Door_Reverb_V001` | Door close |
| `SFX_Door_NOreverb_V001` | Door open |

Doc requirement covered:
- Door open/close when customer enters the clinic.

### Parasite

| Audio name | Intended use |
| --- | --- |
| `WormScream` | Parasite scream |

Doc requirement covered:
- Parasite sound when removed/damaged.

Still needs implementation decision:
- Use `WormScream` for parasite grabbed, cut, or final removed state.
- Add separate variants later if needed.

### Knife Mini Game

| Audio name | Intended use |
| --- | --- |
| `SFX_Cut_knife_V002` | Cutting / slicing flesh |
| `SFX_Cut_knife_V001` | Removing flesh piece |
| `Mini Game_complete` | Mini game / body part treatment complete notification |

Doc requirement covered:
- Knife grab/use sounds are partly covered by tool grab sounds.
- Knife cutting/open wound sounds are covered by cut sounds.

Still missing:
- Knife mistake / scrape / out-of-guide penalty sound.

### Tool Grab

| Audio name | Intended use |
| --- | --- |
| `SFX_Grab_metal_V004` | Grab Tongs |
| `SFX_Grab_metal_V003` | Grab Knife |
| `SFX_Grab_metal_V002` | Grab Needle / syringe |

Doc requirement covered:
- Tool pickup sounds for Tongs, Knife, Needle.

Still missing:
- Medicine bottle pickup.
- Medicine use.
- Tongs use / clamp sound if different from pickup.
- Needle inject / puncture / suction sounds if not covered elsewhere.

### Game Result

| Audio name | Intended use |
| --- | --- |
| `SFX_Game_Lose_V001` | Game over / lose |
| `SFX_Game_Win_V001` | Game complete / win |

Doc requirement covered:
- Win / lose resolution screen feedback.

### Room Transition

| Audio name | Intended use |
| --- | --- |
| `SFX_2Footstep_V001` | Treatment Room to Counter |
| `SFX_2Footstep_V002` | Counter to Treatment Room |

Doc requirement covered:
- Transition between counter and treatment room.

Implementation note:
- Same `SFX_2Footstep_V001` is also listed for character walking loop. If used for both transition and character movement, expose pitch/volume variants or separate AudioSource routing.

### Customer Footstep

| Audio name | Intended use |
| --- | --- |
| `SFX_2Footstep_V001` | Loop while customer walks until reaching target |

Doc requirement covered:
- Customer footstep before dialog.

### Candle / Light

| Audio name | Intended use |
| --- | --- |
| `candle_refill_complete` | Candle refill complete |
| `candle_flicker` | Candle flicker / candle out |

Doc requirement covered:
- Candle refill.
- Candle flicker / extinguish warning.

Still missing:
- Candle light / ignite sound.
- Optional candle low-light loop or warning layer.

### Patient Aggression

| Audio name | Intended use |
| --- | --- |
| `patient_twitch_start` | Patient starts twitching / moving |
| `patient_aggression_warning` | Warning at counter when customer is aggressive |

Doc requirement covered:
- Patient aggression warning.
- Body twitch / aggression obstacle cue.

Implementation note:
- `patient_aggression_warning` should trigger before treatment, likely during counter/dialog or when the aggressive customer appears.
- `patient_twitch_start` should trigger from `PatientAggressionController.AggressionStarted`.

### Ambience / BGM

| Audio name | Intended use |
| --- | --- |
| `AMB_Mainmenu_V001` | Main Menu ambience / BGM |
| `AMB_Treatment_room_V001` | Treatment Room ambience |
| `GameJam_AMB_V002` | Counter room ambience |
| `GameJam_AMB_V003` | Unassigned |
| `GameJam_AMB_V004` | Unassigned |
| `GameJam_AMB_V005` | Unassigned |

Doc requirement covered:
- Main menu ambience.
- Counter room ambience.
- Treatment room ambience.

Implementation note:
- The Google Doc previously listed Treatment Room as incomplete/nope. Local list now provides `AMB_Treatment_room_V001`; update doc/source of truth later if needed.

## Doc Requirements Still Not Fully Covered

### Sanity

GDD asks for sanity state sound feedback:

- Stable: no special sound.
- Middle sanity: heartbeat starts.
- High sanity: heartbeat becomes fast/intense.

Missing audio names:

- Sanity warning heartbeat.
- Sanity critical heartbeat.
- Transformation / sanity max event.

### Dialog / Expression

GDD asks for non-language expression sounds, male and female variants:

- Normal conversation expression.
- Aggressive expression.
- Confused / delirious expression.

Missing audio names:

- Male normal expression.
- Male aggressive expression.
- Male confused expression.
- Female normal expression.
- Female aggressive expression.
- Female confused expression.

### Tongs Mini Game

Partly covered:

- Grab tongs: `SFX_Grab_metal_V004`
- Parasite sound: `WormScream`

Missing / undecided:

- Tongs clamp.
- Parasite pull loop.
- Parasite hits wound edge / mistake.
- Parasite fully extracted.

### Needle Mini Game

Partly covered:

- Grab needle/syringe: `SFX_Grab_metal_V002`

Missing:

- Needle puncture.
- Pustule suction.
- Pustule drained / complete.
- Needle mistake / slip.

### Medicine

GDD includes:

- Sanity medicine.
- Sedative medicine for patient aggression.

Missing:

- Medicine bottle grab.
- Medicine use / drink / apply.
- Medicine overdose / bad effect.
- Sedative suppress aggression feedback.

## Recommended Implementation Order

1. Hook BGM / ambience:
   - Main menu
   - Counter room
   - Treatment room
2. Hook room and customer movement:
   - Door open / close
   - Footstep loop
   - Room transition footsteps
3. Hook tool pickup:
   - Tongs
   - Knife
   - Needle
4. Hook current playable mini games:
   - Knife cut sounds
   - Parasite scream / extraction placeholder
   - Mini game complete
5. Hook win/lose:
   - `SFX_Game_Win_V001`
   - `SFX_Game_Lose_V001`
6. Hook candle:
   - Refill complete
   - Flicker / out warning
7. Hook aggression:
   - Counter warning
   - Twitch start during treatment
8. Add missing sanity/dialog/medicine/needle/tongs detailed sounds later.

## Suggested Audio Event Names

Use stable event names in scripts so audio files can change without rewriting gameplay logic:

```text
door.open
door.close
customer.footstep.loop
room.transition.counter_to_treatment
room.transition.treatment_to_counter
tool.tongs.grab
tool.knife.grab
tool.needle.grab
knife.cut
knife.remove_flesh
tongs.parasite.scream
minigame.complete
game.win
game.lose
candle.refill_complete
candle.flicker
patient.aggression.warning
patient.aggression.twitch_start
```

## Notes For Dev Handoff

- Do not hard-reference audio clip names directly inside mini game logic if an audio manager/event layer is available.
- If no audio manager exists yet, start with serialized `AudioClip` fields on the relevant controller and keep the field names event-like.
- Keep implementation scoped: use available sounds first, then add missing sounds after the core flow is confirmed.
