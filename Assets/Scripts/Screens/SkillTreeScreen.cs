using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Skill Tree & Loadout Management Screen — Starlit Reliquary "J · Training Ground / Skill Tree" board (640x360).
    // Features 5-Tier level gating, 3 branch specializations, rank upgrades (Rank 1-3),
    // 3 Active + 3 Passive loadouts, and SP respec functionality.
    public class SkillTreeScreen : GameScreen
    {
        readonly GameState returnState;
        readonly List<Unit> party;
        int heroIndex = 0;

        public Unit CurrentHero => party[Math.Clamp(heroIndex, 0, party.Count - 1)];

        Label titleLabel;
        Label heroLabel;
        Label spLabel;
        Label feedbackLabel;

        // Skill detail panel labels
        Label detailName;
        Label detailTierType;
        Label detailStats;
        Label detailRank;
        Label detailDescription;
        Label detailRequirements;

        // Action buttons
        Selectable unlockBtn;
        Selectable upgradeBtn;
        Selectable equipSlot1Btn;
        Selectable equipSlot2Btn;
        Selectable equipSlot3Btn;
        Selectable unequipBtn;
        Selectable respecBtn;

        // Equipped loadout labels (bottom panel)
        readonly Label[] activeSlotLabels = new Label[SkillLoadout.ActiveSlotCount];
        readonly Label[] passiveSlotLabels = new Label[SkillLoadout.PassiveSlotCount];

        // Tracked selectable nodes in the skill tree
        readonly List<Selectable> nodeButtons = new List<Selectable>();
        string selectedSkillId;
        bool isSelectedPassive;

        bool confirmRespecActive = false;

        public SkillTreeScreen() : this(null, 0, null) { }

        public SkillTreeScreen(IEnumerable<Unit> partyUnits = null, int initialHero = 0, GameState? backTo = null)
        {
            if (partyUnits != null)
                party = partyUnits.ToList();
            else if (Game.I?.Run?.Party != null)
                party = Game.I.Run.Party;
            else if (Game.I?.Roster != null)
                party = Game.I.Roster.GetActiveParty().Select(d => new Unit(d)).ToList();
            else
                party = Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();

            heroIndex = Math.Clamp(initialHero, 0, Math.Max(0, party.Count - 1));
            returnState = backTo ?? (Game.I?.Run != null ? GameState.Map : GameState.MainMenu);
        }

        protected override void Build()
        {
            Backdrop.Create(Root, false, 0, Palette.Ink1, Palette.Ink1);

            // 1. Header: title, hero switcher, shortcuts, Skill Points badge per FELNA — UI System (Page 16)
            titleLabel = Ui.Label(Root, "SKILL TREE", 12, 10, Palette.Gold2, Align.Left, 2, FontKind.DotGothic);
            Ui.Button(Root, "<", 148, 8, 20, () => SwitchHero(-1), 20, showCursor: false);
            heroLabel = Ui.Label(Root, "", 256, 14, Palette.Mist2, Align.Center);
            Ui.Button(Root, ">", 344, 8, 20, () => SwitchHero(1), 20, showCursor: false);

            var statusBtn = Ui.Button(Root, "STATUS", 370, 8, 46, () => Game?.Go(GameState.CharacterStatus), 20, showCursor: false);
            statusBtn.tooltip = "VIEW CHARACTER ATTRIBUTES & STATS";
            var talentsBtn = Ui.Button(Root, "TALENTS", 420, 8, 46, () => Game?.Go(GameState.Talents), 20, showCursor: false);
            talentsBtn.tooltip = "VIEW & SELECT HERO TALENTS";
            var gearBtn = Ui.Button(Root, "LOADOUT", 470, 8, 44, () => Game?.Go(GameState.Equipment), 20, showCursor: false);
            gearBtn.tooltip = "VIEW & MODIFY HERO EQUIPMENT";

            Ui.Image(Root, PixelArt.Badge(112, 20, Palette.Hex(0x1D1608), Palette.Gold3), 516, 8, Ui.Widget);
            spLabel = Ui.Label(Root, "", 572, 15, Palette.Gold1, Align.Center, order: Ui.Widget + 1);

            // 2. Tree panel: 3 branch columns, 5 tiers of skill nodes
            Ui.Panel(Root, 12, 36, 420, 214);
            BuildTreeNodes();

            // 3. Details panel (gold): selected skill + actions
            Ui.Panel(Root, 438, 36, 190, 214, Palette.Ink2, Palette.Gold3);
            detailName = Ui.Label(Root, "SELECT A SKILL", 448, 44, Palette.Gold2, Align.Left, 1, FontKind.DotGothic);
            detailTierType = Ui.Label(Root, "", 448, 58, Palette.Mist3);
            detailStats = Ui.Label(Root, "", 448, 70, Palette.Mist1);
            detailRank = Ui.Label(Root, "", 448, 82, Palette.Gold3);
            detailDescription = Ui.Label(Root, "", 448, 96, Palette.Mist2);
            detailRequirements = Ui.Label(Root, "", 448, 136, Palette.HpLow);

            unlockBtn = Ui.Button(Root, "UNLOCK (1 SP)", 446, 156, 174, OnUnlockClicked, 18, showCursor: false, style: ButtonStyle.Primary);
            upgradeBtn = Ui.Button(Root, "UPGRADE RANK", 446, 176, 174, OnUpgradeClicked, 18, showCursor: false);
            equipSlot1Btn = Ui.Button(Root, "EQUIP: SLOT 1", 446, 196, 84, () => OnEquipSlotClicked(0), 16, showCursor: false);
            equipSlot2Btn = Ui.Button(Root, "EQUIP: SLOT 2", 536, 196, 84, () => OnEquipSlotClicked(1), 16, showCursor: false);
            equipSlot3Btn = Ui.Button(Root, "EQUIP: SLOT 3", 446, 214, 84, () => OnEquipSlotClicked(2), 16, showCursor: false);
            unequipBtn = Ui.Button(Root, "UNEQUIP", 536, 214, 84, OnUnequipClicked, 16, showCursor: false);

            // 4. Loadout strip: 3 Active + 3 Passive equipped skills, respec
            Ui.Panel(Root, 12, 256, 616, 74);
            Ui.Header(Root, "ACTIVE LOADOUT", 26, 264, 190);
            for (int i = 0; i < SkillLoadout.ActiveSlotCount; i++)
                activeSlotLabels[i] = Ui.Label(Root, $"[A{i + 1}] ---", 22, 282 + (i * 12), Palette.Mist1);

            Ui.Header(Root, "PASSIVE LOADOUT", 226, 264, 200);
            for (int i = 0; i < SkillLoadout.PassiveSlotCount; i++)
                passiveSlotLabels[i] = Ui.Label(Root, $"[P{i + 1}] ---", 222, 282 + (i * 12), Palette.Mist1);

            respecBtn = Ui.Button(Root, "RESPEC ALL SP", 448, 264, 170, OnRespecClicked, 20, showCursor: false);
            respecBtn.tooltip = "REFUNDS ALL SPENT SKILL POINTS AND RESETS TREE";
            feedbackLabel = Ui.Label(Root, "", 448, 292, Palette.Mist2);

            int hx = Ui.KeyHint(Root, 12, 338, "SELECT", "CLICK");
            Ui.KeyHint(Root, hx, 338, "BACK", "ESC");

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.onBack = () => Game?.Go(returnState);

            RefreshUI();
        }

        void BuildTreeNodes()
        {
            nodeButtons.Clear();
            var hero = CurrentHero;
            var cDef = hero.classDef;
            if (cDef == null) return;

            // Branch Header labels
            string b0 = (cDef.branchNames != null && cDef.branchNames.Length > 0) ? cDef.branchNames[0].ToUpper() : "BRANCH I";
            string b1 = (cDef.branchNames != null && cDef.branchNames.Length > 1) ? cDef.branchNames[1].ToUpper() : "BRANCH II";
            string b2 = (cDef.branchNames != null && cDef.branchNames.Length > 2) ? cDef.branchNames[2].ToUpper() : "BRANCH III";
            if (b0.Length > 12) b0 = b0.Substring(0, 12);
            if (b1.Length > 12) b1 = b1.Substring(0, 12);
            if (b2.Length > 12) b2 = b2.Substring(0, 12);
            Ui.Header(Root, b0, 30, 44, 140);
            Ui.Header(Root, b1, 168, 44, 138);
            Ui.Header(Root, b2, 306, 44, 132);

            // Populate all unlockable nodes from the hero's class definition
            var nodes = cDef.skillUnlockProgression ?? Array.Empty<SkillUnlockNode>();
            int[] branchX = { 22, 160, 298 };

            // Render starting skills at Tier 1 top
            if (cDef.startingSkills != null)
            {
                for (int i = 0; i < cDef.startingSkills.Length; i++)
                {
                    string sid = cDef.startingSkills[i];
                    if (string.IsNullOrEmpty(sid) || !Database.Skills.TryGetValue(sid, out var sDef)) continue;
                    int bIdx = Math.Clamp(sDef.branchIndex, 0, 2);
                    int x = branchX[bIdx];
                    int y = 64;
                    CreateNodeButton(sid, sDef.name, false, 1, x, y);
                }
            }

            // Render Starter Passive
            if (!string.IsNullOrEmpty(cDef.starterPassiveId) && Database.Passives.TryGetValue(cDef.starterPassiveId, out var starterP))
            {
                CreateNodeButton(starterP.id, starterP.name, true, 1, branchX[0], 82);
            }

            // Render Tier 2 to Tier 5 progression nodes
            int[] tierY = { 64, 100, 136, 172, 208 };
            var groupedByTier = nodes.GroupBy(n => n.tier).OrderBy(g => g.Key);

            foreach (var group in groupedByTier)
            {
                int t = group.Key;
                int baseY = tierY[Math.Clamp(t - 1, 0, tierY.Length - 1)];

                var branchCounts = new int[3];
                foreach (var node in group)
                {
                    int bIdx = Math.Clamp(node.branchIndex, 0, 2);
                    int x = branchX[bIdx];
                    int y = baseY + (branchCounts[bIdx] * 18);
                    branchCounts[bIdx]++;

                    string sName = node.skillId;
                    if (node.isPassive && Database.Passives.TryGetValue(node.skillId, out var p)) sName = p.name;
                    else if (!node.isPassive && Database.Skills.TryGetValue(node.skillId, out var s)) sName = s.name;

                    CreateNodeButton(node.skillId, sName, node.isPassive, node.tier, x, y);
                }
            }
        }

        void CreateNodeButton(string skillId, string displayName, bool isPassive, int tier, int x, int y)
        {
            string shortName = displayName.Length > 11 ? displayName.Substring(0, 11) : displayName;
            string prefix = isPassive ? "[P]" : "[A]";
            string labelText = $"{prefix} {shortName}";

            var btn = Ui.Button(Root, labelText, x, y, 120, () => SelectSkill(skillId, isPassive), 16, showCursor: false);
            btn.tooltip = $"{displayName} (Tier {tier} {(isPassive ? "Passive" : "Active")})";
            nodeButtons.Add(btn);

            if (string.IsNullOrEmpty(selectedSkillId))
            {
                selectedSkillId = skillId;
                isSelectedPassive = isPassive;
            }
        }

        void SwitchHero(int delta)
        {
            heroIndex = (heroIndex + delta + party.Count) % party.Count;
            selectedSkillId = null;
            confirmRespecActive = false;
            BuildTreeNodes();
            RefreshUI();
        }

        void SelectSkill(string skillId, bool isPassive)
        {
            selectedSkillId = skillId;
            isSelectedPassive = isPassive;
            confirmRespecActive = false;
            RefreshUI();
        }

        void RefreshUI()
        {
            var hero = CurrentHero;
            heroLabel.Set($"{hero.name.ToUpper()} (LV {hero.level} {hero.classDef?.name?.ToUpper()})");
            spLabel.Set($"SKILL POINTS  {hero.skillLoadout.SkillPoints}");

            // Update equipped active slots
            for (int i = 0; i < SkillLoadout.ActiveSlotCount; i++)
            {
                string sid = hero.skillLoadout.ActiveSlots[i];
                if (!string.IsNullOrEmpty(sid) && Database.Skills.TryGetValue(sid, out var s))
                {
                    int rank = hero.skillLoadout.GetRank(sid);
                    activeSlotLabels[i].Set($"[A{i + 1}] {s.name} (R{rank})");
                }
                else
                {
                    activeSlotLabels[i].Set($"[A{i + 1}] --- EMPTY ---");
                }
            }

            // Update equipped passive slots
            for (int i = 0; i < SkillLoadout.PassiveSlotCount; i++)
            {
                string pid = hero.skillLoadout.PassiveSlots[i];
                if (!string.IsNullOrEmpty(pid) && Database.Passives.TryGetValue(pid, out var p))
                {
                    int rank = hero.skillLoadout.GetRank(pid);
                    passiveSlotLabels[i].Set($"[P{i + 1}] {p.name} (R{rank})");
                }
                else
                {
                    passiveSlotLabels[i].Set($"[P{i + 1}] --- EMPTY ---");
                }
            }

            // Update Detail Card
            UpdateDetailCard();
        }

        void UpdateDetailCard()
        {
            var hero = CurrentHero;
            if (string.IsNullOrEmpty(selectedSkillId))
            {
                detailName.Set("NO SKILL SELECTED");
                detailTierType.Set("");
                detailStats.Set("");
                detailRank.Set("");
                detailDescription.Set("");
                detailRequirements.Set("");
                unlockBtn.label.Set("---");
                upgradeBtn.label.Set("---");
                return;
            }

            bool isUnlocked = isSelectedPassive
                ? hero.skillLoadout.UnlockedPassiveSkills.Contains(selectedSkillId)
                : hero.skillLoadout.UnlockedActiveSkills.Contains(selectedSkillId);

            int currentRank = hero.skillLoadout.GetRank(selectedSkillId);
            bool isEquipped = isSelectedPassive
                ? hero.skillLoadout.PassiveSlots.Contains(selectedSkillId)
                : hero.skillLoadout.ActiveSlots.Contains(selectedSkillId);

            SkillDef activeDef = null;
            PassiveDef passiveDef = null;
            if (isSelectedPassive) Database.Passives.TryGetValue(selectedSkillId, out passiveDef);
            else Database.Skills.TryGetValue(selectedSkillId, out activeDef);

            string sName = activeDef != null ? activeDef.name : passiveDef != null ? passiveDef.name : selectedSkillId;
            int tier = activeDef != null ? activeDef.tier : passiveDef != null ? passiveDef.tier : 1;
            int maxRank = activeDef != null ? activeDef.maxRank : passiveDef != null ? passiveDef.maxRank : 3;

            detailName.Set(sName.ToUpper());
            detailTierType.Set($"TIER {tier} • {(isSelectedPassive ? "PASSIVE SKILL" : "ACTIVE SKILL")}");

            if (activeDef != null)
            {
                detailStats.Set($"MP: {activeDef.GetMpCost(Math.Max(1, currentRank))} | CD: {activeDef.cooldown}t | PWR: {activeDef.GetPower(Math.Max(1, currentRank)):F1}x");
            }
            else
            {
                detailStats.Set("INNATE PASSIVE MODIFIER");
            }

            detailRank.Set(isUnlocked ? $"RANK: {currentRank} / {maxRank}" : $"RANK: LOCKED (MAX {maxRank})");

            string desc = activeDef != null ? activeDef.description : passiveDef != null ? passiveDef.description : "";
            detailDescription.Set(PixelFont.Wrap(desc, 172));

            // Validate Requirements & Actions
            var node = hero.classDef?.skillUnlockProgression?.FirstOrDefault(n => n.skillId == selectedSkillId);
            int reqLevel = node?.requiredLevel ?? 1;

            if (!isUnlocked)
            {
                bool canUnlock = hero.skillLoadout.CanUnlockSkill(selectedSkillId, hero.level, hero.classDef, out string err);
                int cost = node?.skillPointCost ?? 1;
                detailRequirements.Set(canUnlock ? $"READY (COST: {cost} SP)" : (err ?? $"REQ LV {reqLevel}"));
                unlockBtn.label.Set($"UNLOCK ({cost} SP)");
                upgradeBtn.label.Set("UPGRADE RANK");
            }
            else
            {
                detailRequirements.Set(isEquipped ? "EQUIPPED IN LOADOUT" : "UNLOCKED (STANDBY)");
                unlockBtn.label.Set("UNLOCKED");
                if (currentRank < maxRank)
                {
                    int upCost = hero.skillLoadout.GetUpgradeCost(selectedSkillId, currentRank);
                    upgradeBtn.label.Set($"UPGRADE (COST: {upCost} SP)");
                }
                else
                {
                    upgradeBtn.label.Set("MAX RANK REACHED");
                }
            }
        }

        void OnUnlockClicked()
        {
            var hero = CurrentHero;
            if (string.IsNullOrEmpty(selectedSkillId)) return;

            if (hero.skillLoadout.TryUnlockSkill(selectedSkillId, hero.level, hero.classDef, out string error))
            {
                feedbackLabel.Set($"Unlocked {selectedSkillId}!");
                Sfx.Play(Sound.Confirm);
                RefreshUI();
            }
            else
            {
                feedbackLabel.Set(error ?? "Cannot unlock skill.");
            }
        }

        void OnUpgradeClicked()
        {
            var hero = CurrentHero;
            if (string.IsNullOrEmpty(selectedSkillId)) return;

            if (hero.skillLoadout.TryUpgradeRank(selectedSkillId, out string error))
            {
                int r = hero.skillLoadout.GetRank(selectedSkillId);
                feedbackLabel.Set($"Upgraded to Rank {r}!");
                Sfx.Play(Sound.Confirm);
                RefreshUI();
            }
            else
            {
                feedbackLabel.Set(error ?? "Cannot upgrade rank.");
            }
        }

        void OnEquipSlotClicked(int slotIndex)
        {
            var hero = CurrentHero;
            if (string.IsNullOrEmpty(selectedSkillId)) return;

            bool success;
            string err;
            if (isSelectedPassive)
                success = hero.skillLoadout.TryEquipPassive(slotIndex, selectedSkillId, out err);
            else
                success = hero.skillLoadout.TryEquipActive(slotIndex, selectedSkillId, out err);

            if (success)
            {
                feedbackLabel.Set($"Equipped in Slot {slotIndex + 1}!");
                Sfx.Play(Sound.Confirm);
                RefreshUI();
            }
            else
            {
                feedbackLabel.Set(err ?? "Failed to equip.");
            }
        }

        void OnUnequipClicked()
        {
            var hero = CurrentHero;
            if (string.IsNullOrEmpty(selectedSkillId)) return;

            if (isSelectedPassive)
            {
                for (int i = 0; i < SkillLoadout.PassiveSlotCount; i++)
                    if (hero.skillLoadout.PassiveSlots[i] == selectedSkillId)
                        hero.skillLoadout.PassiveSlots[i] = null;
            }
            else
            {
                for (int i = 0; i < SkillLoadout.ActiveSlotCount; i++)
                    if (hero.skillLoadout.ActiveSlots[i] == selectedSkillId)
                        hero.skillLoadout.ActiveSlots[i] = null;
            }

            feedbackLabel.Set("Skill unequipped.");
            Sfx.Play(Sound.Move);
            RefreshUI();
        }

        void OnRespecClicked()
        {
            var hero = CurrentHero;
            if (!confirmRespecActive)
            {
                confirmRespecActive = true;
                feedbackLabel.Set("PRESS RESPEC AGAIN TO CONFIRM");
                return;
            }

            hero.skillLoadout.Respec(hero.classDef, hero.level);
            confirmRespecActive = false;
            feedbackLabel.Set("All SP refunded & skill tree reset!");
            Sfx.Play(Sound.Success);
            RefreshUI();
        }
    }
}
