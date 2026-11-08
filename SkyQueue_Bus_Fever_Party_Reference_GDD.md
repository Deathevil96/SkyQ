
## ART-STYLE MATCH + MESHY ASSET PROMPTS (MANDATORY)

### Art direction: match Bus Fever Party
The game's art style must match the supplied Bus Fever Party reference—not merely use “cartoon 3D” as a broad inspiration. Inspect the reference screenshots/recording and reproduce its visual language:
- Bright, saturated, playful casual-mobile 3D.
- Rounded, chunky, toy-like vehicle silhouettes and soft bevels.
- Smooth, clean materials with controlled glossy highlights; readable color blocks.
- Soft contact shadows and polished, friendly presentation.
- Passengers are dense, rounded, colorful, stylized 3D characters. Preserve the reference's crowd readability, scale, and visual density.
- Environment/background, UI, outlines/edges, lighting softness, shadow strength, and color palette should be visually calibrated to the reference.
- Avoid photorealism, low-poly minimalism, realistic airport simulation, hard industrial aircraft, flat 2D sprites, or a different mobile-game art style.
- Do not copy Bus Fever Party's specific branded assets. Create original airplane/passenger models while matching the broad art direction and preserving the user's gameplay adaptation.

### Passenger creativity: permitted and encouraged within the style
The user explicitly wants the passengers to look great and is open to richer character art. Make passengers appealing and varied while retaining the same visual scale and readability:
- Create a coherent family of original stylized passenger characters with varied silhouettes, hairstyles, hats, outfits, accessories, expressions, and body proportions.
- Keep passenger color coding clear and unambiguous for matching/boarding. The gameplay color must remain the dominant readable cue.
- Use tasteful variation—not excessive detail, tiny accessories, or noisy textures that make the crowd hard to read.
- Create a few base character archetypes and use material/color/accessory variants for performance. Avoid making every passenger a unique high-poly asset.
- Preserve the reference's dense crowd arrangement and curved loop shape. Character variety must not change crowd spacing or gameplay footprint.

### Meshy workflow: Claude must produce usable prompts
Before implementing final art, Claude must create `ART_ASSET_PLAN.md` with:
1. A prioritized asset list and intended in-game use.
2. A separate ready-to-copy Meshy prompt for each major asset or asset family.
3. Negative prompt / exclusions where supported.
4. Suggested model style, topology/detail budget, scale, orientation, pivot placement, and export format suitable for Unity.
5. Required variants and how to make them via materials/mesh variants.
6. A note on which assets need transparent/background-free output versus a 3D model.
7. Integration notes: Unity import settings, materials, colliders, LOD or instancing needs, and validation in the target camera.

Claude must not claim it has generated Meshy assets unless it actually has. If Meshy generation is external/manual, provide the prompts in the report and use clearly labeled temporary placeholders until the user supplies the generated models.

### Prompt-writing quality bar
Meshy prompts should describe one concrete model at a time, not a whole scene. Each prompt should specify:
- Original stylized casual-mobile 3D asset matching the Bus Fever Party visual reference.
- Shape language (rounded, chunky, smooth, toy-like), visual detail level, materials, and color treatment.
- Clear silhouette and details visible from the fixed game camera.
- Front/back/side readability where needed; no text, logos, watermarks, or baked-in UI.
- Clean, game-ready mesh with sensible topology and no floating parts.
- Correct orientation and pivot guidance (e.g. vehicle longitudinal axis and ground contact).
- For passenger prompts: one character per model, full body, grounded pose, clear separation of limbs, color-variant-friendly materials, and no scene/background.
- For airplane prompts: original toy-like airplane, readable top view, clear nose/tail and direction, and no airline branding. Produce variants that preserve footprint and orientation.

### Initial asset prompt pack to deliver
`ART_ASSET_PLAN.md` must include ready-to-copy prompts at minimum for:
- Main airplane model (base).
- Airplane variants corresponding to capacities 16, 24, and 40 (preserve the silhouette/footprint relationships required by gameplay).
- Passenger base archetypes (at least 4 distinct original character silhouettes).
- Passenger color/material variants for the gameplay palette.
- Circular passenger-loop track/environment piece.
- Boarding/runway slot and taxi strip pieces.
- Airport background/environment dressing pieces.
- Booster icons/models (VIP, Arrange, Jumble and any approved boosters).
- UI buttons and supporting icon set, with UI art separated from 3D model prompts.

Claude should include a compact prompt template the user can reuse for future passenger/airplane variants, and explain which Meshy output settings should be selected when those settings are available. If Meshy settings are version-dependent or unknown, state that and avoid inventing exact menu names.



# CRITICAL REFERENCE-FIDELITY DIRECTIVE — BUS FEVER PARTY

This directive overrides any earlier wording in this document that could be interpreted as permission to redesign, simplify, approximate, or creatively reinterpret the reference.

## Reference hierarchy
1. **Bus Fever Party gameplay recording and screenshots supplied by the user** are the visual and behavioral source of truth.
2. **The user's Paint sketch** is the source of truth for how the bus/passenger concept is translated into the airplane/runway concept.
3. This GDD describes the intended adaptation, but never overrides the supplied reference when it describes something that must be copied exactly.

Do not use Colony Flow as a reference. Do not substitute another traffic-jam game. Do not invent a “similar” layout.

## The required result
Recreate Bus Fever Party's gameplay and presentation as closely as possible, changing the theme and object identity to airplanes and the user's airport interpretation. The user specifically requires:
- Exact gameplay pattern and interaction order from Bus Fever Party.
- Exact camera projection/type, camera angle, rotation/orientation, zoom, framing, and amount of the board visible.
- Exact composition and relative screen-space proportions of the top passenger loop, central boarding/slot zone, and bottom jammed puzzle board.
- Exact visual density, object scale, spacing, arrangement, diagonal orientation, arrows, UI placement, and movement/selection behavior.
- No “inspired by,” no alternate camera, no layout cleanup, no generic airport tycoon systems, no additional mechanics, and no aesthetic redesign.

## Mandatory reference inspection before coding
Before creating or changing any gameplay or camera code:
1. Open and inspect all supplied Bus Fever Party reference screenshots and the supplied gameplay recording.
2. Scrub the recording frame-by-frame around taps, selection, movement, passenger boarding, slot occupancy, and takeoff/clearing. Record the observable rules.
3. Compare the reference screen to the user's Paint sketch. The screenshot/recording controls exact Bus Fever Party presentation and gameplay; the Paint sketch controls the airplane-specific interpretation.
4. Write a short reference breakdown in the project (e.g. `REFERENCE_BREAKDOWN.md`) documenting the screen regions, camera framing, object orientation, board arrangement, tap/move rules, passenger matching, slot behavior, and the observed sequence. Mark anything not visibly confirmed as unknown—do not guess.
5. Do not start implementation until this breakdown is complete.

## Camera and composition are locked
The camera is not a matter of taste. Match the reference image:
- Use the same projection as the reference. Do not decide “orthographic” merely because this GDD previously said orthographic; inspect the supplied capture and determine the matching projection.
- Match camera rotation/orientation, tilt, zoom, and framing against the full portrait screenshot.
- Match the screen-space boundaries and proportions of the top loop, central slots/runway, bottom puzzle field, and bottom booster row.
- Match the bottom puzzle's tight diagonal packing and each object's long-axis direction. Do not straighten it into a conventional grid.
- Match the loop's curved path and crowd density. Do not replace it with a simple circular ring or a generic queue.
- Keep camera parameters centralized and editable. Create a camera calibration/debug scene or overlay so the developer can compare a captured game frame with the reference.
- Never fabricate exact numeric camera values from a screenshot. Derive and tune them by visual comparison. If the project cannot reproduce a feature exactly from the supplied reference, state the limitation rather than silently approximating it.
- Do not move the camera dynamically, rotate the board to fit, or zoom to make the implementation easier unless the reference does so.

## Gameplay fidelity rules
- Observe Bus Fever Party's actual gameplay in the recording; reproduce the same input affordances, move eligibility, blocked-path behavior, movement timing, completion/clear behavior, and slot interactions.
- Translate buses to airplanes only where required by the user's concept. Preserve the puzzle's functional relationships and sequence.
- Use the user's specified airplane passenger capacities (16, 24, and 40) as the capacity values for this adaptation. Do not alter them.
- The user's Paint interpretation is specific: a rotating/looping colored passenger group at the top, runway/boarding slots in the middle, and planes in the lower jammed puzzle arrangement. Preserve that concept; do not add an airport-management layer.
- Keep all systems deterministic and testable. Visual movement must reflect the real board state; it must not be faked with animations that bypass puzzle rules.

## Required visual verification before calling any phase complete
For each implementation milestone:
1. Run the scene on the same portrait aspect ratio as the supplied reference.
2. Capture a screenshot.
3. Compare it side-by-side with the Bus Fever Party reference and the user's Paint sketch.
4. Check camera angle, rotation, zoom, framing, screen-region proportions, puzzle object scale/spacing/diagonal orientation, loop curvature/density, slot locations, UI positions, and color/arrow readability.
5. Fix differences before proceeding. Do not ask the user to accept a merely “close enough” camera.
6. Include the screenshot and a concise list of remaining differences in the completion report.

## Claude Code behavior
- First inspect the existing Unity project and the reference assets. Do not overwrite working project files or build an entirely new architecture without checking what exists.
- Implement only the requested phase. Do not jump ahead or add speculative systems.
- If any instruction conflicts with this reference-fidelity directive, follow this directive.
- Never tell the user the recreation is exact unless the result has actually been checked against the supplied reference captures.

# AIRPLANE PASSENGER TRAFFIC PUZZLE — GAME DESIGN DOCUMENT
## Claude Code Implementation Specification
**Working title:** Project SkyQueue (temporary; do not hardcode as final brand)  
**Genre:** Casual mobile traffic / passenger color-sorting puzzle  
**Target:** Android first, architecture ready for iOS  
**Engine:** Unity 3D (URP), C#  
**Orientation:** Portrait  
**Document status:** Implementation baseline

---

# 0. IMPORTANT IMPLEMENTATION INSTRUCTIONS FOR CLAUDE CODE

Build the game described in this document. Do not reinterpret it as an airport tycoon, idle game, flight simulator, or generic match-3 game.

The core gameplay is a vehicle traffic puzzle:
- A tightly packed, fixed arrangement of aircraft occupies the lower play area.
- Each aircraft has a direction arrow and can only leave by moving in its indicated direction.
- The player taps aircraft to attempt to move them out of the jam.
- A separate passenger queue rotates continuously around a circular loop near the top of the screen.
- Passengers are visually grouped by color.
- Aircraft travel to a limited number of boarding slots.
- Passengers of the matching color board the corresponding aircraft when they reach the boarding point.
- Aircraft have capacities of 16, 24, or 40 passengers.
- Once an aircraft reaches capacity, it performs a takeoff animation and leaves, freeing the boarding slot.
- The player must plan traffic order and boarding-slot occupancy. An incorrect sequence can block progress or cause a fail condition.

The visual arrangement should follow the user's hand-drawn reference:
1. Circular rotating people loop at top.
2. Runway / movement lane separating the loop from the aircraft area.
3. A horizontal row of limited aircraft boarding slots.
4. A dense, fixed aircraft traffic-jam board beneath the slots.
5. Boosters at the bottom.

Use the supplied sketch and gameplay screenshot as layout references, not as permission to copy their exact art, UI styling, text, icons, level data, or branded assets. Create original visuals and levels.

Before implementation:
- Inspect the existing Unity project and report its current structure.
- Reuse existing systems if sound and relevant.
- Do not overwrite functioning project systems without explaining why.
- Implement in small testable phases.
- Use data-driven level definitions; do not make 1,000 hand-authored scene files.
- Keep all economy, level, unlock, and difficulty values configurable.
- Avoid placeholder systems that appear complete but do not work.
- If a requirement is ambiguous, use the default specified below and document the decision rather than silently inventing a different gameplay loop.

---

# 1. GAME VISION

A colorful, satisfying casual puzzle where players clear a jam of directional airplanes, send aircraft to boarding stands, and load rotating color-coded passengers. Full aircraft taxi and fly away.

The appeal:
- Readable, compact puzzle board.
- Immediate feedback when a plane moves or a passenger boards.
- Satisfying passenger flow and takeoff.
- Simple rules, increasing planning complexity.
- Short levels suitable for mobile sessions.
- Rewarding progression with coins and unlockable boosters.

## 1.1 Core loop
1. Start a level.
2. Inspect the jammed aircraft, their arrows, colors, capacities, passenger loop, and available boarding slots.
3. Tap a plane.
4. If its directional path is clear, it exits the jam and enters an available boarding slot.
5. Passengers circulate around the loop.
6. At the boarding point, passengers board an aircraft of the matching color that has available capacity.
7. If the plane reaches its capacity, it departs via taxi + takeoff animation.
8. Its boarding slot becomes free.
9. Continue until the passenger queue is cleared and all required aircraft have departed.
10. Award 20 coins for a win, plus configured milestone bonuses.
11. Load the next level.

## 1.2 Design pillars
- **Clarity:** Direction, color, capacity, passenger match, and slot occupancy are always legible.
- **Planning:** Player choices have consequences; the board is not random tapping.
- **Satisfying motion:** Passenger boarding and plane departure feel rewarding, without slowing the puzzle.
- **Fairness:** Every generated or authored level must be solvable under its stated rules.
- **Progression:** New mechanics appear gradually and are taught before being combined.
- **Mobile performance:** Stable 60 FPS target on mid-range devices; scalable effects.
- **Rendering:** Real-time 3D scene with a fixed orthographic camera; gameplay rules remain grid-based and independent of rendered transforms.

---

# 2. CORE GAME RULES

## 2.1 Passenger loop
- A circular track contains passenger units.
- Passenger units have a color ID and a stable position in the queue.
- The loop moves continuously in one direction at a configurable speed.
- Passengers are not randomly reordered while the level is active.
- The player sees enough of the loop to make meaningful choices.
- A fixed boarding point connects the loop to the aircraft boarding slots.
- Passenger movement pauses briefly or eases during a boarding interaction only if required for readability. Avoid stopping the entire loop for every passenger by default.
- Passengers board one at a time or in a small readable batch. Use one-at-a-time as the default implementation.
- A passenger can board only a plane with the same color and remaining capacity.
- If no matching plane is in an available boarding slot, the passenger continues around the loop.
- Passengers may not disappear, change color, or board a mismatched plane.
- The queue is considered cleared only when all required passenger units have boarded.

## 2.2 Aircraft
Aircraft have:
- Unique level-local ID.
- Color ID.
- Capacity: 16, 24, or 40.
- Current passenger count.
- Board state.
- Traffic-board footprint.
- Direction: Up, Down, Left, or Right.
- Optional special property (locked, frozen, etc.); special properties are introduced later.
- An aircraft can be selected only while it remains on the traffic board and is not blocked by another aircraft or obstacle in its exit path.
- A plane's path is evaluated in its arrow direction. It moves in a straight line until it clears the traffic board.
- A plane cannot turn.
- Tapping a blocked plane produces a short shake / blocked-path visual and does not alter the board.
- If an available boarding slot exists, a clear plane moves into that slot.
- If no boarding slot exists, selection is rejected with a clear “boarding stands full” response.
- When boarding count equals capacity, plane state changes to Full → Departing → Departed. It flies away and its boarding slot is freed.
- Plane capacity is exact; no overflow allowed.

## 2.3 Plane capacity presentation
Default capacity variants:
- Small aircraft: 16 passengers.
- Medium aircraft: 24 passengers.
- Large aircraft: 40 passengers.

Display the numeric capacity and boarding progress when the aircraft is at a boarding slot. Example: `12 / 24`.
For board aircraft, capacity can be indicated by a subtle label or distinctive silhouette. Do not rely on color alone.

## 2.4 Boarding stands / slots
- Default level starts with 3 boarding slots.
- Slot count can be increased by a booster or configured per level.
- A slot can contain at most one aircraft.
- An aircraft occupies its slot until it departs.
- Slots must have a clear occupied / empty state.
- If all slots are occupied, no additional aircraft can leave the traffic board.
- Slot count and unlock status are level configuration data.
- Optional VIP slot is a special slot with defined rules (see boosters).

## 2.5 Level completion
A level wins only when:
- All passenger units in the level have boarded.
- All required aircraft have departed.
- No remaining required objectives exist.

Do not grant a win merely because the passenger loop is temporarily empty if passengers remain queued off-screen or aircraft remain.

## 2.6 Fail conditions
Use these default fail conditions:
- The player has no legal move and the level still has remaining passengers / aircraft.
- The level's configured move limit is exhausted (only on levels that use a move limit).
- The player explicitly chooses Restart.

A blocked tap is not itself a fail.
A full boarding stand is not itself a fail.
Never trigger a fail while a valid move remains.

Provide a “No moves available” state with options to use a booster, restart, or exit. Do not automatically spend coins.

---

# 3. TRAFFIC BOARD AND PLANE MOVEMENT

## 3.1 Board representation
Use a grid-based traffic board. Each plane occupies one or more grid cells according to its footprint.
- Horizontal plane footprint: width 2 or 3 cells, height 1 cell.
- Vertical plane footprint: width 1 cell, height 2 or 3 cells.
- Exact footprint values are configurable by aircraft type.
- The grid supports different level dimensions, default 6 columns × 8 rows.
- The board is a traffic puzzle, not free placement.
- Aircraft positions and direction are fixed in the level definition.
- Planes cannot be moved sideways, rotated, swapped, or dragged.
- The player taps to move the plane along its arrow.
- Movement is allowed only if all cells from the aircraft to the board exit are empty.
- A plane's path must not cross an occupied cell or obstacle.
- Aircraft exit toward the corresponding outer edge and then transition to a boarding stand using a defined staging animation/path.

## 3.2 Input
- Tap plane: attempt exit.
- Do not use drag as the main control.
- Prevent double-tap / duplicate move execution during animation.
- Input lock is local to moving plane / transition, not unnecessarily global.
- A tap on an empty cell does nothing.
- A tap on a blocked aircraft gives visual feedback.
- Optional accessibility setting: reduce motion.

## 3.3 Aircraft board states
`OnBoard → MovingOut → AtBoardingSlot → Boarding → Full → Departing → Departed`
A plane may transition to `WaitingAtSlot` if no matching passenger is currently at the boarding point.
The state machine must reject invalid transitions.

## 3.4 Obstacles
Introduce only after the basic game is taught:
- Barrier: occupies cells and blocks plane path.
- Locked plane: cannot move until a condition is met.
- Frozen plane: needs a booster or adjacent event to unlock.
- Passenger gate: delays boarding.
- Limited-move level.
- Hidden-color passengers (revealed after a trigger).
All obstacles must be configured in level data and implemented as modular rule components. Do not add arbitrary obstacle types without a tutorial and design purpose.

---

# 4. GAME SCREEN / UX

## 4.1 Portrait screen composition
Follow the user's sketch:
- Header: level number, coin balance, settings/pause.
- Upper area: circular passenger loop, visible color groups and people.
- A boarding point at the bottom of the loop.
- Runway / separation lane below the loop.
- Horizontal row of boarding stands / slots.
- Lower area: fixed aircraft traffic-jam board.
- Bottom toolbar: unlocked boosters.

Avoid putting a large logo over the live puzzle area. The gameplay board should remain the visual focus.

## 4.2 HUD
Header:
- Level label.
- Coin balance and + button.
- Pause/settings button.
- Optional objective display.

Boarding slots:
- Clearly mark slots.
- Locked extra slot shows lock and unlock cost / unlock trigger.
- Use readable progress numbers over planes.

Bottom boosters:
- Undo.
- Shuffle / Arrange.
- Extra Boarding Slot.
- Bomb / Clear.
- Hint.
Only show boosters after unlock level. Before unlock, do not show disabled clutter; a small, non-interactive unlock preview may be shown in level-complete or shop UI.

## 4.3 Tutorial
Tutorial overlays are brief and interactive. Do not require lengthy text.
- First plane tap: “Tap a plane with a clear path.”
- First matching board: “Matching passengers board automatically.”
- First full aircraft: “Full planes take off and free a stand.”
- First booster unlock: explain the specific booster in one step.
- Introduce one new mechanic at a time.

## 4.4 Animations
- Plane move: smooth, clear movement along its arrow; no teleporting.
- Blocked tap: short shake or path highlight.
- Passenger: walks from loop toward plane, boards, and increments count.
- Full plane: short boarding-complete effect, taxi along runway, takeoff, shrink / move into sky.
- Empty slot: visual vacancy and subtle availability pulse.
- Win: concise celebration, coin reward animation, Continue button.
- Loss: clear reason and retry / booster / exit options.
Animation speed should be configurable and reduced-motion setting should skip non-essential flourishes.

## 4.5 Audio
Original sound effects only:
- Plane selection.
- Blocked movement.
- Passenger boarding.
- Capacity reached.
- Taxi / takeoff.
- Coin reward.
- Level win / fail.
Provide master, music, and SFX toggles. Do not use copyrighted game audio.

---

# 5. LEVEL STRUCTURE AND DATA

## 5.1 Level count
Initial release target: 1,000 levels.
The game must be able to load more levels through data updates.
Do not manually create 1,000 unique scenes.

## 5.2 Level data
Each level definition must include:
- Level ID.
- Difficulty tag.
- Grid dimensions.
- Passenger queue sequence.
- Loop speed.
- Boarding stand count.
- Plane list: ID, color, capacity, footprint, position, direction.
- Obstacles and unlock conditions.
- Move limit if applicable.
- Booster availability.
- Tutorial event flags.
- Validation metadata / solution or generated solution sequence.
- Optional seed for procedural generation.
- Reward multiplier / special rewards.
Use ScriptableObjects or JSON with a versioned schema. Prefer ScriptableObjects for editor-authored data and JSON for generated / remotely delivered content. Keep runtime systems independent of the storage source.

## 5.3 Level generation and validation
A level generator must not simply randomize aircraft and passenger colors. It must produce a solvable puzzle.
Recommended approach:
1. Generate a valid solved sequence of aircraft departures and passenger allocation.
2. Construct a traffic-board layout consistent with that solution.
3. Create a passenger queue whose color counts exactly match aircraft capacities.
4. Reverse / constrain moves to create the initial jam.
5. Run a solver or deterministic validation pass.
6. Reject any level that is unsolvable or has impossible color totals.
7. Store the intended solution for hints and QA.
8. Ensure the solution is not the only legal move at every point unless intentional.
For the first playable prototype, use hand-authored level data for levels 1–30 and a basic validated generator for later levels. Expand generation only after gameplay is stable.

## 5.4 Passenger count consistency
For every level:
`Total passengers of color C = sum of capacities of all aircraft of color C`
unless the level explicitly includes non-capacity objectives. No passenger may be orphaned and no aircraft may require more passengers than exist.

## 5.5 Difficulty curve
Difficulty is based on:
- Number of aircraft.
- Number of colors.
- Grid density.
- Number of boarding stands.
- Queue complexity / color order.
- Plane capacity distribution.
- Number and type of obstacles.
- Number of forced sequencing decisions.
- Optional move limit.
- Number of available legal moves at the start and throughout the solution.

Avoid difficulty based only on increasing object count. The puzzle should remain readable.

---

# 6. LEVEL DIFFICULTY SCHEDULE

Default cadence:
- Every 5th level is Hard.
- Every 10th level is Super Hard; Super Hard overrides Hard.
- All other levels are Normal.
- Difficulty is also represented in level data, so handcrafted exceptions can be made.

Suggested initial progression:

| Levels | Difficulty / Features |
|---|---|
| 1–4 | Normal tutorial: 2–3 colors, small planes, generous stands |
| 5 | Hard: introduce a meaningful forced order |
| 6–9 | Normal, build on learned mechanic |
| 10 | Super Hard: more aircraft / tighter slot management |
| 11–19 | Introduce medium aircraft and 3–4 colors |
| 20 | Super Hard |
| 21–49 | More dense traffic, 4–5 colors, mixed capacities |
| 50 | Super Hard checkpoint |
| 51–99 | Introduce first obstacle family and more sequencing |
| 100 | Super Hard milestone |
| 101–199 | Multi-step jam patterns, mixed capacity, restricted stands |
| 200 | Super Hard milestone |
| 201–499 | More combinations, optional move limits, special passenger order |
| 500 | Super Hard milestone |
| 501–999 | High density, multiple obstacle combinations, expert puzzles |
| 1000 | Final launch milestone; special level and reward |

## 6.1 Difficulty quality gates
- Normal: usually 2–4 colors, a reasonable number of legal opening moves, no harsh move limit.
- Hard: 3–5 colors, at least one meaningful sequence dependency, moderate stand pressure.
- Super Hard: 4–6 colors, tight slot management and multiple dependencies; avoid relying on hidden information unless taught.
- Do not create difficulty by making the passenger queue unreadably fast.
- A Super Hard level must be solver-validated and playtested.
- Add a difficulty badge / level intro label where appropriate.
- A player may retry without losing earned coins.

---

# 7. ECONOMY

## 7.1 Currency
Primary currency: Gold / Coins. Choose one UI term and use consistently. Default term: `Coins`.
No premium currency is required for the first playable build.

## 7.2 Win rewards
- Standard level win: **20 coins**.
- Every 10 levels completed: **+50 milestone bonus coins** for the first 100 levels.
- Levels 101 onward: milestone bonus is disabled by default; standard win remains 20 coins.
- Super Hard level: configurable additional bonus; default +10 coins, but disabled for initial balance unless playtesting supports it.
- First-time level completion only grants level reward. Replaying a completed level does not grant the base 20 coins again, to prevent infinite farming.
- Daily reward / ad rewards are optional future systems, not required for MVP.

Milestone example:
- Complete level 10: 20 base + 50 milestone = 70 coins.
- Complete level 20: 20 base + 50 milestone = 70 coins.
- Complete level 100: 20 base + 50 milestone = 70 coins.
- Complete level 110: 20 coins.

All rewards must be configurable via EconomyConfig.

## 7.3 Booster costs
Default initial costs:
- Undo: 100 coins per use.
- Shuffle / Arrange: **900 coins per use**.
- Extra Boarding Slot: 600 coins per use / level.
- Bomb / Clear: 1,200 coins per use.
- Hint: 150 coins per use.

Do not charge a player until booster use is confirmed and can execute. If use fails, refund / do not deduct.
Do not automatically buy boosters.
The shop must clearly show cost, inventory count (if any), and what the booster does.

## 7.4 Economy progression philosophy
- Early levels should give enough coins to teach and try boosters.
- The player should not be able to purchase every expensive booster every level.
- The 900-coin shuffle is a strategic premium-feeling utility, not a mandatory level gate.
- Avoid impossible levels designed to force purchases.
- Ensure free players can beat every level with skill.
- Track coin sources and sinks for QA.
- All economy values are data-driven and remotely adjustable.

## 7.5 Coin persistence
Persist:
- Current coin balance.
- Levels completed.
- First-win reward claims.
- Milestone claims.
- Booster inventory if inventory is enabled.
- Settings.

Use a safe save system:
- Versioned save schema.
- Atomic write / backup.
- Validate data on load.
- Do not allow a negative coin balance.
- For prototype, local save is acceptable. Cloud save is future scope.

---

# 8. BOOSTERS AND UNLOCKS

Boosters unlock at defined progression points. Unlocking is not the same as owning a use; use can require coins or an inventory charge.

| Booster | Unlock level | Default cost | Effect |
|---|---:|---:|---|
| Undo | 5 | 100 coins | Reverses the last valid player move, if state can be restored safely |
| Hint | 8 | 150 coins | Highlights one recommended legal move from the level solution |
| Shuffle / Arrange | 15 | 900 coins | Reconfigures the remaining traffic arrangement into a validated, solvable arrangement; must preserve remaining passenger and plane counts |
| Extra Boarding Slot | 20 | 600 coins | Adds one temporary boarding slot for the current level |
| Bomb / Clear | 30 | 1,200 coins | Removes or clears one eligible blocking aircraft/obstacle according to a level-safe rule; cannot delete required passenger capacity |

### 8.1 Booster definitions
**Undo**
- Undo one latest player-initiated move.
- Restore aircraft position, slot occupancy, boarding progress, passenger queue positions, and relevant state.
- Do not undo a completed departure or coin award unless the full state snapshot supports it.
- Prefer full deterministic snapshot for the last reversible action.
- If undo is not safe during boarding/departure animation, disable it until stable.

**Hint**
- Show one valid next move from the validated solution or solver.
- Highlight plane and exit path.
- Do not auto-execute the move.
- If no hint solution exists, show a fallback “No hint available” and do not charge.

**Shuffle / Arrange**
- Re-arranges eligible remaining aircraft or board state.
- Must never create an unsolvable state.
- Preserve all required plane colors, capacities, passengers, and objectives.
- Must not reset already completed progress.
- If a safe shuffle cannot be found, don't charge and show an error.

**Extra Boarding Slot**
- Adds one temporary slot until the level ends.
- The slot can hold one aircraft.
- Does not permanently increase default slot count.
- Only one extra slot may be active by default; configurable cap.

**Bomb / Clear**
- MVP rule: remove one eligible aircraft from the traffic board only if its full passenger capacity is also accounted for by a safe conversion: either it is a non-required blocker explicitly tagged in level data, or the bomb grants its passengers to the correct color queue in a clearly animated way.
- Never simply delete a required aircraft and leave the passenger totals invalid.
- For initial build, prefer “clear one tagged blocker” and do not allow bomb on ordinary required aircraft.
- If a level contains no eligible target, booster is disabled.

### 8.2 Unlock presentation
- On the unlock level win, show a small reward/tutorial modal explaining the new booster.
- Do not interrupt the active level before completion.
- Unlocks persist.
- Show booster icon in the bottom bar from that point onward.

---

# 9. MONETIZATION — FUTURE, NOT MVP

MVP should be fun without monetization implementation.
Possible future:
- Rewarded ad for a small coin bonus after a win.
- Optional rewarded ad for one extra retry.
- Cosmetic plane skins / trails / airport themes.
- Remove-ads purchase.
Do not add forced ads between every level.
Do not use deceptive purchase UI.
Do not make paid boosts the only path through a level.
Use platform-compliant purchase flows only when monetization is approved.

---

# 10. CONTENT AND VISUAL STYLE

## 10.1 Original art direction
- Bright, glossy, friendly 3D casual-game look.
- Use actual 3D meshes and materials for planes, passengers, airport props, and the runway; do not make the primary scene a 2D sprite board.
- Miniature toy-like airplanes with readable silhouettes and clear color identity.
- Tiny stylized 3D passengers with strong color identity and readable poses.
- Airport apron / runway visual language.
- Soft real-time or baked shadows, clean materials, high contrast.
- Distinct original logo, UI icons, planes, passengers, sounds, and level layouts.
- Keep the camera fixed and orthographic (or near-orthographic) so the traffic puzzle reads clearly like a flat board.
- Use 3D world-space objects for the gameplay board and characters. Use Unity Canvas UI for HUD, buttons, booster bar, level panels, and counters.
- Plane movement and takeoff are animated in 3D, but the authoritative traffic logic is a 2D grid model. Never use physics collisions as the source of puzzle truth.
- Use URP with mobile-friendly lighting, material count, shadows, and post-processing. Add quality settings to scale effects for lower-end devices.
- Keep object transforms as visual representations of logical board coordinates. Board logic must not depend on rendered positions.

## 10.2 Readability
- Colors must remain distinguishable for color-vision accessibility. Add a secondary pattern / icon to each color where possible.
- Direction arrows must be high contrast and readable at phone size.
- Passenger counts must not overlap the plane.
- Plane colors should be recognizable even when small.
- The board should not be hidden by particle effects.

## 10.3 Theme mapping
| Bus-based genre element | SkyQueue implementation |
|---|---|
| Bus | Airplane |
| Vehicle direction | Plane exit arrow |
| Passenger queue | Circular passenger loop |
| Bus parking / waiting | Boarding stands |
| Bus full and leaves | Plane fills, taxis, takes off |
| Traffic board | Jammed aircraft apron |
| Vehicle capacity | 16 / 24 / 40 seats |
| Traffic shuffle | Arrange aircraft safely |
| Bus departure | Runway takeoff |

---

# 11. TECHNICAL ARCHITECTURE (UNITY / C#)

Implement clean, modular systems. Names may be adjusted to fit the existing project, but responsibilities must remain separated.

Suggested folders:
```
Assets/
  SkyQueue/
    Art/
    Audio/
    Prefabs/
      Planes/
      Passengers/
      Slots/
      UI/
      Effects/
    Scenes/
      Boot/
      MainMenu/
      Gameplay/
    Scripts/
      Core/
      Gameplay/
        TrafficBoard/
        PassengerLoop/
        Boarding/
        Aircraft/
        Level/
      Economy/
      Boosters/
      UI/
      Save/
      Audio/
      Validation/
    Data/
      Levels/
      Config/
```

Suggested core classes:
- `GameBootstrap`
- `GameStateManager`
- `LevelManager`
- `LevelDefinition` (ScriptableObject or serializable data)
- `LevelLoader`
- `LevelValidator`
- `TrafficBoardController`
- `TrafficBoardGrid`
- `AircraftController`
- `AircraftDefinition`
- `AircraftStateMachine`
- `PassengerLoopController`
- `PassengerController`
- `PassengerQueue`
- `BoardingSlotController`
- `BoardingManager`
- `LevelCompletionController`
- `LevelFailController`
- `EconomyManager`
- `EconomyConfig`
- `BoosterManager`
- `BoosterDefinition`
- `SaveManager`
- `AudioManager`
- `HUDController`
- `TutorialController`

## 11.3 System contracts
- Gameplay state should not be owned by UI.
- UI calls commands on gameplay managers; gameplay raises events for UI.
- Use events such as `OnAircraftMoved`, `OnPassengerBoarded`, `OnAircraftDeparted`, `OnCoinsChanged`, `OnLevelWon`, `OnLevelFailed`, `OnBoosterUnlocked`.
- Level configuration is data-driven.
- Avoid singleton sprawl. If a singleton is used, document lifetime and ownership.
- Use object pooling for passenger and aircraft visuals if necessary.
- Keep logic testable without animations.
- Use deterministic random seeds for generated levels.
- Avoid relying on animation callbacks as the only source of gameplay truth.
- Do not let a passenger visual's transform determine authoritative queue order.

## 11.4 Authoritative state
Gameplay logic owns:
- Board occupancy.
- Aircraft states and positions.
- Passenger sequence and color counts.
- Boarding-slot occupancy.
- Current passenger counts.
- Level completion / fail state.
- Coins, unlocks, and save progress.

Visuals mirror authoritative state.

## 11.5 State machine
Global level states:
`Loading → Tutorial (optional) → Playing → Resolving → Won / Failed → Results`

Prevent inputs during:
- Level loading.
- A conflicting aircraft transition.
- Save / results finalization.
But keep unrelated board interaction responsive when safe.

## 11.6 Save behavior
- Save after level win, coin transactions, booster unlocks, and settings changes.
- Store schema version.
- Handle missing or corrupted data gracefully.
- Save only authoritative state needed for progress, not transient animation transforms.

---

# 12. REQUIRED QA AND AUTOMATED TESTS

Create tests for:
1. Plane can exit when the full directional path is empty.
2. Plane cannot exit when a cell in its path is occupied.
3. Plane cannot move in any direction other than its arrow.
4. Plane cannot enter an occupied boarding slot.
5. No aircraft is accepted if all boarding slots are full.
6. Passenger boards only matching color.
7. Passenger does not board a full aircraft.
8. Plane departs exactly at capacity, not before or after.
9. Departure frees the boarding slot.
10. Level win requires all passengers and required aircraft cleared.
11. Level fail occurs only when no legal move remains or configured move limit is exhausted.
12. Level color totals are capacity-valid.
13. Level generator output is solver-validated.
14. Standard first-time win awards 20 coins.
15. Replay win does not re-award first-time base coins.
16. Milestone bonus awards +50 every 10 levels through level 100, exactly once.
17. Level 101+ does not receive milestone bonus by default.
18. Booster unlocks occur at levels 5, 8, 15, 20, and 30 as specified.
19. Booster cost is deducted only when use succeeds.
20. Undo restores all state supported by its snapshot.
21. Shuffle preserves solvability and remaining objectives.
22. Extra slot is temporary and resets after level.
23. Bomb cannot invalidate passenger/aircraft totals.
24. Save/load preserves coins, progression, unlocks, and claimed rewards.

Add play-mode tests for complete representative levels:
- A tutorial level.
- A normal mixed-color level.
- A hard level.
- A super-hard level.
- A level with a booster.
- A level with a failed move / no-moves state.

---

# 13. IMPLEMENTATION PHASES

Do not attempt the entire game in one unreviewed pass.

## Phase 1 — playable core prototype
Implement:
- Portrait gameplay scene.
- Simple placeholder art.
- Grid traffic board.
- Directional plane exit.
- Three boarding slots.
- Circular passenger loop.
- Passenger-to-plane color matching.
- Capacities 16, 24, 40.
- Full plane departure / slot freeing.
- Win / fail.
- 10 handcrafted levels.
- Basic level restart.

Exit criteria:
- Core loop is playable.
- Plane path validation works.
- Passenger count and plane capacity stay consistent.
- One level can be won and restarted reliably.

## Phase 2 — level pipeline and progression
Implement:
- Data-driven level format.
- Level validator.
- 30 handcrafted tutorial / progression levels.
- Normal / Hard / Super Hard tags.
- Every-5 / every-10 difficulty cadence.
- 1,000-level content architecture.
- First-win coin rewards.
- Milestone bonus.
- Save system.

## Phase 3 — boosters and polish
Implement:
- Undo.
- Hint.
- Shuffle / Arrange.
- Extra Boarding Slot.
- Bomb / Clear with safe target rules.
- Unlock sequence and tutorial modals.
- UI polish, sound, animations, accessibility.

## Phase 4 — validated scale
Implement:
- Solvable level generator.
- Solver / solution validation.
- Automated test coverage.
- Level analytics events (local/debug first).
- Performance optimization.
- Build validation on target Android devices.

---

# 14. ACCEPTANCE CRITERIA

The build is not considered complete unless:
- The player can see a rotating circular queue of colored humans.
- Aircraft are jammed in a fixed directional layout.
- Tapping a plane attempts to move it only in its arrow direction.
- Blocked aircraft cannot move.
- Cleared aircraft move to a boarding stand.
- Passengers board only matching-color planes.
- The player can use small / medium / large planes with capacities 16 / 24 / 40.
- Full planes taxi and take off.
- Takeoff frees the boarding slot.
- There are meaningful win and fail conditions.
- Levels are data-driven and validated.
- Level 1 through 30 are playable.
- Economy awards 20 coins on first-time standard win.
- Milestone bonus behavior is correct.
- Booster unlock levels and costs are configurable and functional.
- A player cannot spend coins for a failed booster action.
- Save/load works.
- There are no fatal console errors.
- The game remains playable at portrait mobile resolution.
- No borrowed game assets, branding, or copied exact level data are included.

---

# 15. FIRST TASK FOR CLAUDE CODE

Start by inspecting the current project and identifying:
1. Unity version and render pipeline.
2. Existing scenes and scripts.
3. Whether there is already a board, passenger loop, or vehicle movement implementation.
4. Existing input, save, and UI architecture.
5. Any compile errors or missing assets.

Then report a concise implementation plan.

After the inspection, implement **Phase 1 only** unless the user explicitly asks to continue. Build a playable prototype in a 3D URP scene using simple original placeholder meshes/materials. Use the fixed orthographic camera and grid-driven gameplay architecture. Do not create final art before the gameplay rules are tested.

Do not silently change the core layout: circular passenger loop at top, runway and boarding stands in the middle, jammed directional aircraft board below, boosters at bottom.
