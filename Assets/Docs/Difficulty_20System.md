# Difficulty System

**Game Design Document --- Draft**

-   **Version:** 0.1
-   **Status:** Working draft based on current design decisions
-   **Game Genre:** Party-based turn-based roguelike RPG
-   **Related Systems:** Combat, QTE, Dungeon Progression, Run Economy,
    Recovery, Main Menu

------------------------------------------------------------------------

## 1. Design Overview

The game uses a **Hardcore Tactical Survival** difficulty direction.
Combat is intended to reward tactical decisions, resource management,
party composition, and careful navigation through dungeon nodes. A
failed run carries a meaningful cost: the expedition ends and the player
must begin a new game.

The difficulty is built around the following principles:

-   **Tactical combat:** Turn-based individual character actions with
    reaction QTEs.
-   **Resource pressure:** HP, MP, healing supplies, and run Gold must
    be managed.
-   **Roguelike risk:** Run failure ends the current expedition.
-   **Sequential challenge:** The player must clear all 8 dungeons in a
    fixed order.
-   **Successful progression persists:** Clearing a dungeon allows the
    current run to continue to the next dungeon.

This document records the rules currently established. Unconfirmed
values and details are labeled **TBD** rather than assumed.

## 2. Difficulty Identity

  Element                Current Design
  ---------------------- --------------------------------------------------
  Difficulty direction   Hardcore Tactical Survival
  Party size             4 characters
  Combat format          Turn-based, individual character turns
  Reaction mechanic      QTE
  Dungeon count          8
  Dungeon progression    Sequential; fixed order
  Run currency           Gold is run-based
  Failure                Run ends; return to Main Menu; New Game required

## 3. Core Difficulty Rules

### 3.1 Combat and Party Survival

-   Combat is turn-based, with individual character turns.
-   Reaction QTEs are part of combat.
-   The party consists of 4 characters.
-   Character HP and MP are important combat resources.
-   The detailed HP 0 / Character Down / revival / recovery rules are
    maintained in the Combat System and remain subject to their defined
    specifications.

### 3.2 Character Down and Party Wipe

-   If one or more party members are down, battle can continue as long
    as at least one party member remains active.
-   If the entire party is defeated, the run is considered failed.
-   A party wipe ends the current run and returns the player to the Main
    Menu.
-   The player must select **New Game** to begin another expedition.

> **Important:** A party wipe is not a checkpoint reload. The failed run
> is reset.

## 4. Dungeon Progression

### 4.1 Sequential Progression

The expedition consists of **8 dungeons**, cleared in a predetermined
sequence.

-   Dungeon order is fixed.
-   The player cannot choose or skip the order of dungeons.
-   Clearing a dungeon advances the current run to the next dungeon.
-   Clearing Dungeon 8 completes the expedition.

Dungeon themes, layouts, node counts, encounter rosters, bosses, and
reward tables are **TBD**.

### 4.2 Dungeon Clear

When a dungeon is successfully cleared:

-   The run continues rather than ending.
-   Remaining Gold is retained for the next dungeon.
-   The player advances to the next dungeon in the fixed sequence.
-   The party continues as part of the same successful expedition.

The exact persistence rules for character levels, SP, equipment,
consumables, and party HP/MP between cleared dungeons require
confirmation unless specified in their respective system documents.

### 4.3 Run Failure

When the party is wiped:

-   The current expedition ends.
-   The current dungeon progression is reset.
-   Run Gold is lost.
-   The player returns to the Main Menu.
-   A new expedition requires **New Game**.

There is no continuation of the failed run from the current dungeon or a
checkpoint.

## 5. Run Economy and Resource Pressure

### 5.1 Gold

Gold is a run-based currency.

-   Gold retained after a successful dungeon clear remains available
    during the same run.
-   Gold is removed when the run ends.
-   Gold does not persist after a failed expedition.

Shop pricing, sell values, refresh costs, and stock generation are
defined in the Shop System document and may contain additional rules.

### 5.2 HP, MP, and Recovery

HP and MP management are central to the survival difficulty.

-   MP is spent to use Active Skills.
-   Healing and recovery opportunities are limited by the run's
    available resources and systems.
-   Recovery Camp and other recovery-node details should follow their
    dedicated design specifications.
-   Exact recovery amounts, availability, and whether resources are
    restored between dungeons are **TBD** unless explicitly specified
    elsewhere.

## 6. Run Loop

The intended high-level loop is:

1.  Select **New Game** to start an expedition.
2.  Prepare the party and its loadouts.
3.  Navigate the dungeon node map.
4.  Resolve nodes, such as combat, events, shops, and rewards.
5.  Manage party HP, MP, items, and Gold.
6.  Clear the dungeon to retain the run and advance.
7.  Repeat through the fixed sequence of 8 dungeons.
8.  Clear Dungeon 8 to complete the run.

If the party is wiped at any point, the loop ends in failure and the
player must start a New Game.

## 7. Difficulty Feedback and Player Clarity

The game should communicate the consequences of the Hardcore Tactical
Survival rules clearly.

-   Show current dungeon number and overall progress through the
    8-dungeon sequence.
-   Clearly communicate that Gold is run-based.
-   Communicate party member Down states and party-wipe danger.
-   Make the difference between a dungeon clear and a run failure clear.
-   Show node rewards and costs before the player commits when
    applicable.
-   Clearly indicate when the player must return to Main Menu and start
    a New Game after failure.

UI presentation and accessibility details, including QTE timing options
and feedback, remain subject to the UI/UX specification.

## 8. Out of Scope / Open Design Questions

The following details are not defined by this document and should not be
invented as part of the difficulty rules:

1.  Enemy stat scaling by dungeon or node.
2.  Exact enemy AI, targeting, and encounter composition.
3.  Exact HP and MP restoration between battles or between dungeons.
4.  Exact Recovery Camp functionality, costs, and limits.
5.  Whether level, SP, equipment, consumables, and character status
    persist between dungeon clears.
6.  Exact Character Down, revival, and recovery mechanics.
7.  Whether any meta-progression or unlocks persist after a failed run.
8.  Dungeon-specific reward tables and boss difficulty curves.

These should be decided in the relevant systems or through a dedicated
balancing pass.

------------------------------------------------------------------------

## Change Log

  -----------------------------------------------------------------------
  Version                             Notes
  ----------------------------------- -----------------------------------
  0.1                                 Initial Difficulty System draft
                                      assembled from current project
                                      decisions. Records Hardcore
                                      Tactical Survival, 8 sequential
                                      dungeons, run-based Gold,
                                      successful dungeon continuation,
                                      and full run failure requiring New
                                      Game.

  -----------------------------------------------------------------------
