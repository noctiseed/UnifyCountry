# UnifyCountry

Unity 2D side-scrolling tower defense game prototype with strategy, cards, and roguelike progression.

## Project Layout

```text
Assets/
└─ _Project/
   ├─ Art/
   ├─ Audio/
   ├─ Code/
   │  ├─ Runtime/
   │  │  ├─ Core/
   │  │  ├─ GameLoop/
   │  │  ├─ Cards/
   │  │  ├─ Deck/
   │  │  ├─ Towers/
   │  │  ├─ Enemies/
   │  │  ├─ Combat/
   │  │  ├─ Waves/
   │  │  ├─ Map/
   │  │  ├─ Roguelike/
   │  │  ├─ Economy/
   │  │  ├─ UI/
   │  │  ├─ Save/
   │  │  ├─ Config/
   │  │  └─ Utils/
   │  └─ Editor/
   ├─ Configs/
   ├─ Prefabs/
   ├─ Scenes/
   ├─ UI/
   ├─ ThirdParty/
   └─ Sandbox/
```

## Core Game Loop

```text
MainMenu -> RunMap -> Battle -> Reward -> RunMap
```

The first playable milestone should focus on:

- Starting a run
- Entering a battle scene
- Drawing cards
- Playing cards to place towers or trigger effects
- Spawning one enemy wave
- Resolving victory and rewards
- Returning to the run map

## Naming Prefixes

```text
PF_   Prefab
SO_   ScriptableObject
MAT_  Material
TEX_  Texture
SPR_  Sprite
AUD_  Audio
ANIM_ Animation Clip
CTRL_ Animator Controller
SCN_  Scene
```
