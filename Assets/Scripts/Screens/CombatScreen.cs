using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // 2DObscure — Battle UI migrated to Figma 22b---Combat-HUD---Target-Statuses & 22c---Boss-Combat-HUD---Zoroa (1280x720 UI Toolkit).
    public class CombatScreen : GameScreen
    {
        protected override void Build()
        {
            var enc = Run?.CurrentEncounter ?? Database.Encounters.Values.First();
            bool isBossHud = enc.tier == Tier.Boss;
            MountFigma(isBossHud ? "22c---Boss-Combat-HUD---Zoroa" : "22b---Combat-HUD---Target-Statuses");
            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            var items = Run?.Items ?? new List<string>(Database.StartingItems);
            var rng = Run?.Rng ?? new System.Random(1);
            var battle = new Battle(party, enc.enemies.Select(id => Database.Enemies[id]), items, rng, Run);
            if (Run != null)
            {
                foreach (var buff in Run.TakeBattleBuffs())
                    foreach (var p in battle.Party.Where(p => p.Alive)) p.AddStatus(buff.id, buff.turns, buff.magnitude, p);
            }
            Root.gameObject.AddComponent<CombatView>().Init(battle, enc, UiRoot);
        }
    }

    public class CombatView : MonoBehaviour
    {
        const float EnemyThink = 0.5f, AfterAction = 0.6f;
        const int LogLines = 6;

        // Staging positions inside StageHost (1280x398, top=82)
        static readonly Vector2[] PartySlots =
        {
            new Vector2(260, 86),
            new Vector2(310, 206),
            new Vector2(130, 126),
            new Vector2(180, 246)
        };

        // Lower staging positions for 22c Boss Combat HUD so party sits below the 840px top boss bar
        static readonly Vector2[] PartySlotsBoss =
        {
            new Vector2(260, 134),
            new Vector2(310, 238),
            new Vector2(130, 166),
            new Vector2(180, 272)
        };

        public Battle Battle { get; private set; }
        public VisualElement UiRoot { get; private set; }

        class Hud
        {
            public Vector2 pos, size;
            public VisualElement spriteEl, enemyCard, vitalsCard, hpGauge, mpGauge, brkGauge, targetArrow;
            public UiLabel nameLbl, roleLbl, hpLbl, mpLbl, statusLbl, shieldLbl, weakLbl, intentLbl;
            public VisualElement statusRow;      // status row inside vitals card / enemy card / boss bar
            public VisualElement stageStatusRow; // floating status badges above hero stage sprites (22b/22c)
            public string statusKey;
            public int phase = 1;
            public bool isPrimaryBoss;
            public bool flashing;
            public Texture2D[] idle; // remodeled idle loop, null for row-art units
            public float idleOffset;
            public int idleFrame;
            public string artKey;    // enemy id the idle frames belong to; a boss phase swaps the def and the art with it
            public bool busy;        // a one-shot clip (attack, hit, KO) owns the sprite; the idle loop waits
            public bool fallen;      // KO played and held until the hero is revived
        }

        class ActiveFx
        {
            public VisualElement el;
            public Fx.Clip clip;
            public float age;
        }

        class ActivePopup
        {
            public UiLabel lbl;
            public float age, startY;
        }

        readonly Dictionary<Combatant, Hud> huds = new Dictionary<Combatant, Hud>();
        readonly Dictionary<Combatant, (UitkMenu.Item item, Action choose)> activeTargets = new Dictionary<Combatant, (UitkMenu.Item, Action)>();
        readonly List<UiLabel> logLabels = new List<UiLabel>();
        readonly List<string> logLines = new List<string>();
        readonly List<ActiveFx> activeFxs = new List<ActiveFx>();
        readonly List<ActivePopup> activePopups = new List<ActivePopup>();

        VisualElement screenRoot, turnBar, stageHost, menuHost, dossierBox, dossierStatusRow, dossierTargetBox, qteHost, outcomeOverlay;
        UiLabel roundLabel, hintLabel, actBanner;
        UiLabel dossierTitle, dossierCostType, dossierTarget, dossierDesc;
        UiLabel activeMarker;
        GameObject phaseGo;
        UitkMenu currentMenu;

        // 22c Boss HUD bindings
        bool isBossHud;
        Combatant primaryBoss;
        UiLabel bossTitleLbl, bossSubLbl, bossPhaseLbl, bossBarLbl, bossHpLbl, bossBrkLbl, bossIntentLbl;
        VisualElement bossHpGauge, bossBrkGauge, bossStatusRow;

        string turnText = "";
        BattleAction chosen;
        Qte activeQte;
        Combatant pendingPhaseTransitionBoss;
        readonly System.Random qteRng = new System.Random();

        static Vector2[] EnemySlots(int count) => count switch
        {
            1 => new[] { new Vector2(930, 124) },
            2 => new[] { new Vector2(840, 86), new Vector2(1040, 182) },
            3 => new[] { new Vector2(760, 76), new Vector2(920, 174), new Vector2(1080, 82) },
            _ => new[] { new Vector2(730, 56), new Vector2(790, 206), new Vector2(960, 56), new Vector2(1060, 206) },
        };

        public void Init(Battle battle, EncounterDef encounter, VisualElement root)
        {
            Battle = battle;
            UiRoot = root;
            isBossHud = encounter.tier == Tier.Boss;
            screenRoot = UiRoot.Q<VisualElement>(className: isBossHud ? "_22c-boss-combat-hud-zoroa" : "_22b-combat-hud-target-statuses") ?? UiRoot;

            actBanner = UiRoot.Q<UiLabel>(className: isBossHud ? "act-1-ashen-ruins-vars-3" : "act-1-ashen-ruins-vars-2");
            if (actBanner != null)
            {
                int act = Game.I?.Run?.Act ?? 1;
                int stage = Game.I?.Run?.Stage ?? 1;
                int totalStages = Game.I?.Run?.TotalStages ?? 1;
                string stageSuffix = totalStages > 1 ? $" · STAGE {stage}/{totalStages}" : "";
                actBanner.text = isBossHud
                    ? $"ACT {act}{stageSuffix}: THE INNER SANCTUM"
                    : $"ACT {act}{stageSuffix}: {(Game.I?.Run?.Map?.name ?? "ASHEN RUINS").ToUpperInvariant()}";
            }

            roundLabel = UiRoot.Q<UiLabel>(className: isBossHud ? "round-1-vars-3" : "round-1-vars-2");

            // Find Turn-Chip flex container
            if (roundLabel != null && roundLabel.parent != null)
            {
                int rIdx = roundLabel.parent.IndexOf(roundLabel);
                if (rIdx + 1 < roundLabel.parent.childCount)
                {
                    turnBar = roundLabel.parent[rIdx + 1];
                    turnBar.style.maxWidth = 810;
                }
            }

            hintLabel = UiRoot.Q<UiLabel>(className: isBossHud
                ? "zoroa-charges-final-reaping-all-releases-next-turn-break-it-to-interrupt-vars"
                : "choose-a-target-squire-a-vars");
            if (hintLabel != null)
            {
                hintLabel.style.left = 20;
                hintLabel.style.width = 1240;
                hintLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                hintLabel.text = "";
            }

            // Hide baked static sprites/enemies between telegraph banner and vitals panel
            var telegraphBorder = UiRoot.Q<VisualElement>(className: isBossHud ? "rectangle-vars-234" : "rectangle-vars-232");
            var vitalsPanel = UiRoot.Q<VisualElement>(className: isBossHud ? "panel-default__i-46" : "panel-default__i-44");
            if (telegraphBorder != null && vitalsPanel != null)
            {
                int sIdx = screenRoot.IndexOf(hintLabel ?? telegraphBorder) + 1;
                int vIdx = screenRoot.IndexOf(vitalsPanel);
                for (int i = sIdx; i < vIdx; i++)
                    screenRoot[i].style.display = DisplayStyle.None;
            }

            stageHost = new VisualElement { name = "CombatStageHost" };
            stageHost.style.position = Position.Absolute;
            stageHost.style.left = 0;
            stageHost.style.top = 82;
            stageHost.style.width = 1280;
            stageHost.style.height = 398;
            stageHost.style.overflow = Overflow.Hidden;
            if (vitalsPanel != null)
                screenRoot.Insert(screenRoot.IndexOf(vitalsPanel), stageHost);
            else
                screenRoot.Add(stageHost);

            activeMarker = new UiLabel("▾");
            activeMarker.style.position = Position.Absolute;
            activeMarker.style.width = 24;
            activeMarker.style.fontSize = 22;
            activeMarker.style.color = Palette.Gold1;
            activeMarker.style.unityTextAlign = TextAnchor.MiddleCenter;
            stageHost.Add(activeMarker);

            // Clean baked command menu, baked floating status rows, and baked dossier labels (and bind Boss HUD if 22c)
            HideBakedCommandMenuAndExtras();
            if (isBossHud)
                BindBossHeaderElements();

            // Bind party sprites + bottom 4 vitals cards
            var slots = isBossHud ? PartySlotsBoss : PartySlots;
            for (int i = 0; i < battle.Party.Count; i++) AddPartyCombatant(battle.Party[i], i, slots[i]);
            for (int i = battle.Party.Count; i < 4; i++) HideUnusedVitalsCard(i);

            if (isBossHud)
            {
                primaryBoss = battle.Enemies.FirstOrDefault(e => e.enemy.tier == Tier.Boss) ?? battle.Enemies.FirstOrDefault();
                var adds = battle.Enemies.Where(e => e != primaryBoss).ToList();
                if (primaryBoss != null) AddBossCombatant(primaryBoss);
                if (adds.Count > 0)
                {
                    var addSlots = adds.Count == 1
                        ? new[] { new Vector2(680, 176) }
                        : new[] { new Vector2(660, 148), new Vector2(680, 248) };
                    for (int i = 0; i < adds.Count; i++)
                        AddEnemyCombatant(adds[i], addSlots[Mathf.Min(i, addSlots.Length - 1)]);
                }
            }
            else
            {
                var eSlots = EnemySlots(battle.Enemies.Count);
                for (int i = 0; i < battle.Enemies.Count; i++) AddEnemyCombatant(battle.Enemies[i], eSlots[Mathf.Min(i, eSlots.Length - 1)]);
            }

            if (!isBossHud)
                ModifierHudHelper.BuildStrip(stageHost, battle.run, 14f, 6f, 700f, stageHost);

            // Command menu column inside right console panel
            menuHost = new VisualElement { name = "CombatCommandMenu" };
            menuHost.style.position = Position.Absolute;
            menuHost.style.left = 676;
            menuHost.style.top = 492;
            menuHost.style.width = 236;
            menuHost.style.height = 218;
            menuHost.style.flexDirection = FlexDirection.Column;
            screenRoot.Add(menuHost);

            // Right Dossier / Log box over panel-raised
            dossierBox = new VisualElement { name = "CombatDossierBox" };
            dossierBox.style.position = Position.Absolute;
            dossierBox.style.left = 936;
            dossierBox.style.top = 498;
            dossierBox.style.width = 322;
            dossierBox.style.height = 204;
            dossierBox.style.flexDirection = FlexDirection.Column;
            dossierBox.style.overflow = Overflow.Hidden;
            screenRoot.Add(dossierBox);

            dossierTitle = new UiLabel("") { style = { fontSize = 18, color = Palette.Gold1, marginBottom = 4, display = DisplayStyle.None } };
            dossierDesc = new UiLabel("") { style = { fontSize = 13, color = Palette.Mist1, whiteSpace = WhiteSpace.Normal, marginBottom = 8, display = DisplayStyle.None } };
            dossierCostType = new UiLabel("") { style = { fontSize = 12, color = Palette.Mist2, marginBottom = 2, display = DisplayStyle.None } };
            dossierTarget = new UiLabel("") { style = { fontSize = 12, color = Palette.Mist3, display = DisplayStyle.None } };
            dossierStatusRow = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    alignItems = UnityEngine.UIElements.Align.Center,
                    marginTop = 6,
                    display = DisplayStyle.None
                }
            };
            dossierTargetBox = new VisualElement
            {
                name = "CombatTargetDossier",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    flexDirection = FlexDirection.Column,
                    justifyContent = Justify.SpaceBetween,
                    flexGrow = 1,
                    display = DisplayStyle.None
                }
            };
            dossierBox.Add(dossierTitle);
            dossierBox.Add(dossierDesc);
            dossierBox.Add(dossierCostType);
            dossierBox.Add(dossierTarget);
            dossierBox.Add(dossierStatusRow);
            dossierBox.Add(dossierTargetBox);

            for (int i = 0; i < LogLines; i++)
            {
                var l = new UiLabel("") { style = { fontSize = 12, color = Palette.WarmIvory, whiteSpace = WhiteSpace.Normal, marginBottom = 4 } };
                logLabels.Add(l);
                dossierBox.Add(l);
            }

            // Dedicated QTE overlay over right console during QTE
            qteHost = new VisualElement { name = "CombatQteHost" };
            qteHost.style.position = Position.Absolute;
            qteHost.style.left = 674;
            qteHost.style.top = 488;
            qteHost.style.width = 596;
            qteHost.style.height = 224;
            qteHost.style.backgroundColor = Palette.Ink2;
            qteHost.style.borderTopWidth = 2; qteHost.style.borderBottomWidth = 2;
            qteHost.style.borderLeftWidth = 2; qteHost.style.borderRightWidth = 2;
            qteHost.style.borderTopColor = Palette.Gold3; qteHost.style.borderBottomColor = Palette.Gold3;
            qteHost.style.borderLeftColor = Palette.Gold3; qteHost.style.borderRightColor = Palette.Gold3;
            qteHost.style.display = DisplayStyle.None;
            screenRoot.Add(qteHost);

            battle.Logged += AddLog;
            battle.BossPhaseTransitioning += boss => pendingPhaseTransitionBoss = boss;
            battle.StatusApplied += (_, _, isBuff) => Sfx.Play(isBuff ? Sound.Buff : Sound.Debuff);
            AddLog(isBossHud ? $"{Database.Enemies[encounter.enemies[0]].name} awakens." : MapScreen.EnemyNames(encounter) + (encounter.enemies.Length > 1 ? " attack!" : " attacks!"));
            StartCoroutine(Loop());
        }

        void HideBakedCommandMenuAndExtras()
        {
            var rightPanel = UiRoot.Q<VisualElement>(className: isBossHud ? "panel-default__i-47" : "panel-default__i-45");
            var raisedPanel = UiRoot.Q<VisualElement>(className: isBossHud ? "panel-raised__i-3" : "panel-raised__i-2");
            var bossTitleEl = isBossHud ? UiRoot.Q<VisualElement>(className: "zoroa-harvester-of-souls-vars") : null;
            int bossHeaderStart = bossTitleEl != null ? screenRoot.IndexOf(bossTitleEl) : int.MaxValue;

            if (rightPanel != null)
            {
                int start = screenRoot.IndexOf(rightPanel) + 1;
                var toRemove = new List<VisualElement>();
                for (int i = start; i < screenRoot.childCount; i++)
                {
                    var child = screenRoot[i];
                    if (child == raisedPanel) continue;
                    // On 22c, preserve the top Boss HUD bar elements starting at zoroa-harvester-of-souls-vars
                    if (isBossHud && i >= bossHeaderStart) continue;

                    // Remove containers holding baked <Button>s so no orphaned buttons exist in UI Toolkit
                    if (child.Q<Button>() != null)
                    {
                        toRemove.Add(child);
                        continue;
                    }
                    // On 22c, keep the subtle ground aura (left: 800, top: 380) right behind the Boss sprite
                    if (isBossHud && Mathf.Abs(child.style.left.value.value - 800f) < 2f && Mathf.Abs(child.style.top.value.value - 380f) < 2f)
                        continue;

                    child.style.display = DisplayStyle.None;
                }
                foreach (var el in toRemove) el.RemoveFromHierarchy();
            }
        }

        void BindBossHeaderElements()
        {
            bossTitleLbl = UiRoot.Q<UiLabel>(className: "zoroa-harvester-of-souls-vars");
            if (bossTitleLbl != null)
            {
                bossTitleLbl.style.left = 220;
                bossTitleLbl.style.width = 840;
                bossTitleLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            }
            bossSubLbl = UiRoot.Q<UiLabel>(className: "soul-entity-controller-vars");
            bossPhaseLbl = UiRoot.Q<UiLabel>(className: "phase-1-2-vars");
            if (bossPhaseLbl != null)
            {
                bossPhaseLbl.style.left = 860;
                bossPhaseLbl.style.width = 200;
                bossPhaseLbl.style.unityTextAlign = TextAnchor.MiddleRight;
            }
            bossBarLbl = UiRoot.Q<UiLabel>(className: "bar-1-2-vars");
            bossHpLbl = UiRoot.Q<UiLabel>(className: "_1-376-1-840-vars");
            if (bossHpLbl != null)
            {
                bossHpLbl.style.left = 220;
                bossHpLbl.style.width = 840;
                bossHpLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            }
            bossHpGauge = screenRoot.Q<VisualElement>(className: "gauge-hp-low");
            bossBrkGauge = screenRoot.Q<VisualElement>(className: "gauge-break");
            bossBrkLbl = UiRoot.Q<UiLabel>(className: "_64-100-vars");
            bossIntentLbl = UiRoot.Q<UiLabel>(className: "intent-final-reaping-4-heavy-all-vars");
            if (bossIntentLbl != null)
            {
                bossIntentLbl.style.left = 220;
                bossIntentLbl.style.width = 840;
                bossIntentLbl.style.unityTextAlign = TextAnchor.MiddleRight;
            }

            // Locate the Boss status badge container (left: 434, top: 128) and clear its baked icons,
            // and hide the static HP chip flash bar (left: 848, top: 155)
            foreach (var child in screenRoot.Children())
            {
                if (child.style.position != Position.Absolute) continue;
                float l = child.style.left.value.value;
                float t = child.style.top.value.value;
                if (Mathf.Abs(l - 434f) < 4f && Mathf.Abs(t - 128f) < 4f)
                {
                    bossStatusRow = child;
                    bossStatusRow.Clear();
                    bossStatusRow.style.overflow = Overflow.Visible;
                }
                else if (Mathf.Abs(l - 848f) < 4f && Mathf.Abs(t - 155f) < 4f)
                {
                    child.style.display = DisplayStyle.None;
                }
            }
        }

        void HideUnusedVitalsCard(int slot)
        {
            string sfx = isBossHud ? "-3" : "-2";
            string[] cls = { $"vitals-knight-vars{sfx}", $"vitals-rogue-vars{sfx}", $"vitals-mage-vars{sfx}", $"vitals-healer-vars{sfx}" };
            var card = UiRoot.Q<VisualElement>(className: cls[slot]);
            if (card != null) card.style.display = DisplayStyle.None;
        }

        void AddPartyCombatant(Combatant c, int idx, Vector2 pos)
        {
            var spr = c.unit.sprite;
            float w = 88f, h = 108f;
            var fig = new VisualElement
            {
                style = { position = Position.Absolute, left = pos.x, top = pos.y, width = w, height = h }
            };
            if (spr != null && spr.Length > 0)
                FigmaUi.SetBackgroundContain(fig, FigmaUi.GetSpriteTexture(spr, "cbt_p_" + (c.unit.classDef?.id ?? c.name)));
            RegisterStageTargetEvents(fig, c);
            stageHost.Add(fig);

            // Floating status badges above hero stage sprite (matches 22b & 22c)
            var stageStatusRow = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = Mathf.Max(4f, pos.x - 18f),
                    top = Mathf.Max(2f, pos.y - 26f),
                    width = w + 36f,
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.Center,
                    alignItems = UnityEngine.UIElements.Align.Center
                }
            };
            stageHost.Add(stageStatusRow);

            var arrow = new UiLabel("▾")
            {
                style = { position = Position.Absolute, left = pos.x + w / 2f - 12f, top = pos.y - 44f, width = 24, fontSize = 22, color = Palette.Err, unityTextAlign = TextAnchor.MiddleCenter, display = DisplayStyle.None }
            };
            stageHost.Add(arrow);

            string sfx = isBossHud ? "-3" : "-2";
            string[] cardClasses = { $"vitals-knight-vars{sfx}", $"vitals-rogue-vars{sfx}", $"vitals-mage-vars{sfx}", $"vitals-healer-vars{sfx}" };
            string[] nameClasses = isBossHud
                ? new[] { "knight-vars-12", "rogue-vars-11", "mage-vars-11", "healer-vars-12" }
                : new[] { "knight-vars-11", "rogue-vars-10", "mage-vars-10", "healer-vars-11" };
            string[] roleClasses = { $"defender-vars{sfx}", $"striker-vars{sfx}", $"caster-vars{sfx}", $"support-vars{sfx}" };
            string[] hpClasses = isBossHud
                ? new[] { "_125-125-vars-4", "_85-85-vars-3", "_60-60-vars-5", "_80-80-vars-3" }
                : new[] { "_125-125-vars-3", "_85-85-vars-2", "_60-60-vars-3", "_80-80-vars-2" };
            string[] mpClasses = isBossHud
                ? new[] { "_20-20-vars-5", "_25-25-vars-3", "_60-60-vars-6", "_55-55-vars-3" }
                : new[] { "_20-20-vars-4", "_25-25-vars-2", "_60-60-vars-4", "_55-55-vars-2" };

            var card = idx < 4 ? UiRoot.Q<VisualElement>(className: cardClasses[idx]) : null;
            if (card != null)
            {
                // Strip any baked Status-Icon containers inside this vitals card
                var bakedBadges = card.Children().Where(ch =>
                    ch.Q<VisualElement>(className: "status-icon-buff") != null ||
                    ch.Q<VisualElement>(className: "status-icon-dot") != null ||
                    ch.Q<VisualElement>(className: "status-icon-debuff") != null ||
                    ch.Q<VisualElement>(className: "status-icon-control") != null ||
                    ch.Q<VisualElement>(className: "status-icon-broken") != null).ToList();
                foreach (var b in bakedBadges) b.RemoveFromHierarchy();
            }

            var statusRow = new VisualElement { pickingMode = PickingMode.Ignore, style = { position = Position.Absolute, right = 8, top = 8, flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center } };
            var statusLbl = new UiLabel("") { style = { fontSize = 11, color = Palette.Frost, marginRight = 3 } };
            statusRow.Add(statusLbl);
            card?.Add(statusRow);

            var hud = new Hud
            {
                pos = pos,
                size = new Vector2(w, h),
                spriteEl = fig,
                idle = UnitArt.Idle(spr),
                idleOffset = idx * 0.25f, // stagger so the party doesn't breathe in unison
                targetArrow = arrow,
                vitalsCard = card,
                nameLbl = idx < 4 ? card?.Q<UiLabel>(className: nameClasses[idx]) : null,
                roleLbl = idx < 4 ? card?.Q<UiLabel>(className: roleClasses[idx]) : null,
                hpLbl = idx < 4 ? card?.Q<UiLabel>(className: hpClasses[idx]) : null,
                mpLbl = idx < 4 ? card?.Q<UiLabel>(className: mpClasses[idx]) : null,
                hpGauge = card?.Q<VisualElement>(className: "gauge-hp"),
                mpGauge = card?.Q<VisualElement>(className: "gauge-mp"),
                statusLbl = statusLbl,
                statusRow = statusRow,
                stageStatusRow = stageStatusRow,
            };
            if (hud.nameLbl != null) hud.nameLbl.text = c.name.ToUpperInvariant();
            if (hud.roleLbl != null) hud.roleLbl.text = (c.unit.role ?? "HERO").ToUpperInvariant();
            huds[c] = hud;
        }

        void AddBossCombatant(Combatant c)
        {
            var spr = c.unit.sprite;
            float w = 192f, h = 224f;
            var pos = new Vector2(854f, 162f); // matches 22c sprite-warden (left: 860, top: 244 - 82 = 162)

            var fig = new VisualElement
            {
                style = { position = Position.Absolute, left = pos.x, top = pos.y, width = w, height = h }
            };
            var idle = UnitArt.Enemy(c.enemy?.id, spr);
            if (idle != null)
                FigmaUi.SetBackgroundContain(fig, idle[0]);
            else if (spr != null && spr.Length > 0)
                FigmaUi.SetBackgroundContain(fig, FigmaUi.GetSpriteTexture(spr, "cbt_boss_" + (c.enemy?.id ?? c.name)));
            RegisterStageTargetEvents(fig, c);
            stageHost.Add(fig);

            var arrow = new UiLabel("▾")
            {
                style = { position = Position.Absolute, left = pos.x + w / 2f - 14f, top = pos.y - 26f, width = 28, fontSize = 24, color = Palette.Gold1, unityTextAlign = TextAnchor.MiddleCenter, display = DisplayStyle.None }
            };
            stageHost.Add(arrow);

            var bossGuardLbl = new UiLabel("") { style = { fontSize = 11, color = Palette.Frost, marginRight = 4 } };
            if (bossStatusRow != null) bossStatusRow.Add(bossGuardLbl);

            huds[c] = new Hud
            {
                pos = pos,
                size = new Vector2(w, h),
                isPrimaryBoss = true,
                enemyCard = fig,
                spriteEl = fig,
                idle = idle,
                artKey = c.enemy?.id,
                idleOffset = 0f,
                targetArrow = arrow,
                nameLbl = bossTitleLbl,
                hpGauge = bossHpGauge,
                brkGauge = bossBrkGauge,
                weakLbl = bossSubLbl,
                statusLbl = bossGuardLbl,
                statusRow = bossStatusRow,
                intentLbl = bossIntentLbl,
            };
        }

        void AddEnemyCombatant(Combatant c, Vector2 pos)
        {
            var spr = c.unit.sprite;
            bool boss = c.enemy.tier == Tier.Boss;
            float w = boss ? 192f : 96f;  // boss art 48x60, enemies 24x26: both exact 4x
            float h = boss ? 240f : 104f;
            // Slots were laid out for a 120px boss box: keep the slot's horizontal centre and lift the box
            // so its name, bars and status labels still fit inside the 398px stage.
            if (boss) pos = new Vector2(pos.x + (120f - w) / 2f, Mathf.Min(pos.y, 60f));

            var card = new VisualElement
            {
                style = { position = Position.Absolute, left = pos.x - 24f, top = pos.y - 26f, width = w + 48f, alignItems = UnityEngine.UIElements.Align.Center }
            };

            var intentLbl = new UiLabel("") { style = { fontSize = 12, color = Palette.Ember1, marginBottom = 2, unityTextAlign = TextAnchor.MiddleCenter } };
            card.Add(intentLbl);

            var fig = new VisualElement { style = { width = w, height = h } };
            var idle = UnitArt.Enemy(c.enemy?.id, spr);
            if (idle != null)
                FigmaUi.SetBackgroundContain(fig, idle[0]);
            else if (spr != null && spr.Length > 0)
                FigmaUi.SetBackgroundContain(fig, FigmaUi.GetSpriteTexture(spr, "cbt_e_" + (c.enemy?.id ?? c.name)));
            card.Add(fig);

            Color tierCol = c.enemy.tier == Tier.Boss ? Palette.SpecCrimson : c.enemy.tier == Tier.Elite ? Palette.Ember : Palette.WarmIvory;
            var nameLbl = new UiLabel(c.name.ToUpperInvariant()) { style = { fontSize = 14, color = tierCol, marginTop = 2, unityTextAlign = TextAnchor.MiddleCenter } };
            var shieldLbl = new UiLabel("") { style = { fontSize = 12, color = Palette.Gold1, unityTextAlign = TextAnchor.MiddleCenter } };

            var hpBarBg = new VisualElement { style = { width = 120, height = 8, paddingTop = 1, paddingBottom = 1, paddingLeft = 1, paddingRight = 1, backgroundColor = Palette.Ink0, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = Palette.Ink4, borderBottomColor = Palette.Ink4, borderLeftColor = Palette.Ink4, borderRightColor = Palette.Ink4, marginTop = 2 } };
            hpBarBg.Add(new VisualElement { style = { width = Length.Percent(100), height = Length.Percent(100), backgroundColor = Palette.HpLow } });

            var brkBarBg = new VisualElement { style = { width = 120, height = 6, paddingTop = 1, paddingBottom = 1, paddingLeft = 1, paddingRight = 1, backgroundColor = Palette.Ink0, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = Palette.Ink4, borderBottomColor = Palette.Ink4, borderLeftColor = Palette.Ink4, borderRightColor = Palette.Ink4, marginTop = 2 } };
            brkBarBg.Add(new VisualElement { style = { width = Length.Percent(100), height = Length.Percent(100), backgroundColor = Palette.Gold2 } });

            var weakLbl = new UiLabel("") { style = { fontSize = 11, color = Palette.Mist2, marginTop = 2, unityTextAlign = TextAnchor.MiddleCenter } };
            var statusLbl = new UiLabel("") { style = { fontSize = 11, color = Palette.Frost, unityTextAlign = TextAnchor.MiddleCenter, marginRight = 2 } };
            var statusRow = new VisualElement { pickingMode = PickingMode.Ignore, style = { flexDirection = FlexDirection.Row, justifyContent = Justify.Center, alignItems = UnityEngine.UIElements.Align.Center, marginTop = 2 } };
            statusRow.Add(statusLbl);

            card.Add(nameLbl);
            card.Add(shieldLbl);
            card.Add(hpBarBg);
            card.Add(brkBarBg);
            card.Add(weakLbl);
            card.Add(statusRow);
            RegisterStageTargetEvents(card, c);
            stageHost.Add(card);

            var arrow = new UiLabel("▾")
            {
                style = { position = Position.Absolute, left = pos.x + w / 2f - 12f, top = pos.y - 44f, width = 24, fontSize = 22, color = Palette.Gold1, unityTextAlign = TextAnchor.MiddleCenter, display = DisplayStyle.None }
            };
            stageHost.Add(arrow);

            huds[c] = new Hud
            {
                pos = pos,
                size = new Vector2(w, h),
                enemyCard = card,
                spriteEl = fig,
                idle = idle,
                artKey = c.enemy?.id,
                idleOffset = (huds.Count % 4) * 0.25f,
                targetArrow = arrow,
                nameLbl = nameLbl,
                shieldLbl = shieldLbl,
                hpGauge = hpBarBg,
                brkGauge = brkBarBg,
                weakLbl = weakLbl,
                statusLbl = statusLbl,
                statusRow = statusRow,
                intentLbl = intentLbl,
            };
        }

        static List<(StatusId id, int turns, int stacks)> CollectActiveStatuses(Combatant c)
        {
            var result = new List<(StatusId id, int turns, int stacks)>();
            if (c == null || !c.Alive) return result;

            var seen = new HashSet<StatusId>();
            foreach (var s in c.statuses)
            {
                if (!seen.Add(s.id)) continue;
                int stacks = c.StatusController != null
                    ? Math.Max(1, c.StatusController.ActiveInstances.Count(i => i.statusId == s.id && !i.IsExpired))
                    : 1;
                result.Add((s.id, s.turns, stacks));
            }

            if (c.StatusController != null)
            {
                foreach (var g in c.StatusController.ActiveInstances.Where(i => !i.IsExpired).GroupBy(i => i.statusId))
                {
                    if (!seen.Add(g.Key)) continue;
                    result.Add((g.Key, g.Max(i => i.remainingTurns), g.Count()));
                }
            }

            if (c.stunTurns > 0 && seen.Add(StatusId.Stun))
                result.Add((StatusId.Stun, c.stunTurns, 1));

            if (c.IsBroken && seen.Add(StatusId.Broken))
                result.Add((StatusId.Broken, 1, 1));

            return result;
        }

        static string StatusTemplateName(StatusId id)
        {
            if (id == StatusId.Broken) return "Status-Icon-Broken";
            if (StatusInfo.IsBuff(id)) return "Status-Icon-Buff";
            if (StatusInfo.IsDoT(id)) return "Status-Icon-DoT";
            if (StatusInfo.IsControl(id)) return "Status-Icon-Control";
            return "Status-Icon-Debuff";
        }

        // Builds a Figma Status-Icon-* badge enriched with the 16x16 pixel-art status icon.
        static VisualElement BuildStatusIconBadge(StatusId id, int turns, int stacks)
        {
            var tpl = FigmaUi.InstantiateTemplate(StatusTemplateName(id));
            if (tpl != null)
            {
                FigmaUi.SetInstanceText(tpl, "text-0", StatusInfo.Tag(id));
                FigmaUi.SetInstanceText(tpl, "text-1", stacks > 1 ? $"{turns}×{stacks}" : turns.ToString());
                tpl.style.marginRight = 3;
                tpl.style.flexShrink = 0;

                var row = tpl.childCount > 0 ? tpl[0] : tpl;
                row.style.alignItems = UnityEngine.UIElements.Align.Center;
                var iconEl = IconArt.Inline(IconArt.Status(id), 1, 3f);
                if (iconEl != null)
                {
                    iconEl.style.alignSelf = UnityEngine.UIElements.Align.Center;
                    row.Insert(0, iconEl);
                }
                return tpl;
            }
            return IconArt.StatusBadge(id, turns, stacks, 1, 3f);
        }

        // Rebuilds the status badge strip only when the set of statuses or remaining turns changes (Refresh runs often).
        static void SyncStatusIcons(Hud h, Combatant c)
        {
            if (h.statusRow == null && h.stageStatusRow == null) return;
            var active = CollectActiveStatuses(c);
            string key = string.Join(",", active.Select(s => $"{(int)s.id}:{s.turns}:{s.stacks}"));
            if (key == h.statusKey) return;
            h.statusKey = key;

            if (h.statusRow != null)
            {
                while (h.statusRow.childCount > 1) h.statusRow.RemoveAt(1); // child 0 is statusLbl
                foreach (var s in active.Take(c.IsEnemy ? 5 : 3))
                {
                    var el = BuildStatusIconBadge(s.id, s.turns, s.stacks);
                    if (el != null) h.statusRow.Add(el);
                }
            }

            if (h.stageStatusRow != null)
            {
                h.stageStatusRow.Clear();
                foreach (var s in active.Take(3))
                {
                    var el = BuildStatusIconBadge(s.id, s.turns, s.stacks);
                    if (el != null) h.stageStatusRow.Add(el);
                }
            }
        }

        void Refresh()
        {
            if (roundLabel != null) roundLabel.text = $"ROUND {Battle.Round}";
            foreach (var kv in huds)
            {
                var c = kv.Key;
                var h = kv.Value;
                if (c.IsEnemy)
                {
                    if (h.enemyCard != null) h.enemyCard.style.display = c.Alive ? DisplayStyle.Flex : DisplayStyle.None;
                    if (!c.Alive)
                    {
                        if (h.isPrimaryBoss && bossHpLbl != null)
                        {
                            FigmaUi.SetGaugeFill(bossHpGauge, 0f, Palette.HpLow);
                            bossHpLbl.text = $"0 / {c.Stats.maxHp:#,##0}";
                        }
                        continue;
                    }

                    FigmaUi.SetGaugeFill(h.hpGauge, (float)c.unit.hp / Mathf.Max(1, c.Stats.maxHp), Palette.HpLow);
                    if (h.brkGauge != null)
                    {
                        h.brkGauge.style.display = c.HasBreakGauge ? DisplayStyle.Flex : DisplayStyle.None;
                        FigmaUi.SetGaugeFill(h.brkGauge, (float)c.CurrentBreakGauge / Mathf.Max(1, c.MaxBreakGauge), c.IsBroken ? Palette.Mist3 : Palette.Gold2);
                    }

                    if (h.isPrimaryBoss)
                    {
                        if (bossTitleLbl != null) bossTitleLbl.text = c.name.ToUpperInvariant();
                        bool multiPhase = c.enemy.phase2At > 0 || c.enemy.nextPhase != null || c.phase > 1;
                        if (bossPhaseLbl != null)
                            bossPhaseLbl.text = multiPhase ? (c.phase >= 2 ? "PHASE ◆◆  2 / 2" : "PHASE ◆◇  1 / 2") : "PHASE ◆  1 / 1";
                        if (bossBarLbl != null)
                            bossBarLbl.text = multiPhase ? $"BAR {c.phase}/2" : "HP";
                        if (bossHpLbl != null)
                            bossHpLbl.text = $"{c.unit.hp:#,##0} / {c.Stats.maxHp:#,##0}";
                        if (bossBrkLbl != null)
                        {
                            bossBrkLbl.text = c.IsBroken ? "BROKEN!" : (c.HasBreakGauge ? $"{c.CurrentBreakGauge} / {c.MaxBreakGauge}" : "—");
                            bossBrkLbl.style.color = c.IsBroken ? Palette.Gold1 : Palette.Mist2;
                        }
                        if (bossSubLbl != null)
                        {
                            var weaks = c.Affinities?.weak;
                            bossSubLbl.text = weaks != null && weaks.Length > 0
                                ? "BOSS · WEAK: " + string.Join(" · ", weaks.Select(ElementConfig.Short))
                                : "BOSS · INNER SANCTUM";
                        }
                        if (h.statusLbl != null)
                        {
                            h.statusLbl.text = c.guarding ? "GD" : "";
                            SyncStatusIcons(h, c);
                        }
                        if (bossIntentLbl != null)
                        {
                            var intent = EnemyAI.IntentText(c);
                            bossIntentLbl.text = !string.IsNullOrEmpty(intent) ? $"INTENT: {intent}" : "";
                            bossIntentLbl.style.color = c.IsCharging || (c.Intent?.move.HasHeavy ?? false) ? Palette.CrimsonBright : Palette.Ember1;
                        }
                    }
                    else
                    {
                        if (h.shieldLbl != null)
                        {
                            h.shieldLbl.text = c.IsBroken ? "[BREAK!] STAGGERED (150% DMG)"
                                : c.HasBreakGauge ? $"SHIELD {c.CurrentBreakGauge}" : "";
                            h.shieldLbl.style.color = c.IsBroken ? Palette.Gold1 : Palette.Gold2;
                        }

                        if (h.weakLbl != null)
                        {
                            var weaks = c.Affinities?.weak;
                            h.weakLbl.text = weaks != null && weaks.Length > 0
                                ? "WEAK: " + string.Join(" · ", weaks.Select(ElementConfig.Short))
                                : "";
                        }

                        if (h.statusLbl != null)
                        {
                            h.statusLbl.text = (c.enemy.phase2At > 0 || c.enemy.nextPhase != null || c.phase > 1 ? $"PHASE {c.phase} " : "") + (c.guarding ? "GD" : "");
                            SyncStatusIcons(h, c);
                            h.statusLbl.style.color = c.phase > 1 ? Palette.CrimsonBright : c.HasDebuff ? Palette.Ember : Palette.Frost;
                        }

                        if (h.intentLbl != null)
                        {
                            var intent = EnemyAI.IntentText(c);
                            h.intentLbl.text = intent ?? "";
                            h.intentLbl.style.color = c.IsCharging || (c.Intent?.move.HasHeavy ?? false) ? Palette.CrimsonBright : Palette.Ember;
                        }
                    }

                    if (c.phase != h.phase)
                    {
                        h.phase = c.phase;
                        Hint($"{c.name.ToUpperInvariant()} ENTERS PHASE {c.phase}!");
                        StartCoroutine(Flash(h, c));
                    }
                    continue;
                }

                if (h.spriteEl != null && !h.flashing)
                    h.spriteEl.style.opacity = c.Alive ? 1f : 0.35f;

                bool isActive = c == Battle.Active;
                if (h.vitalsCard != null)
                {
                    h.vitalsCard.style.backgroundColor = isActive ? Palette.Hex(0x2B2210) : Palette.Ink2;
                    var border = isActive ? Palette.Gold2 : Palette.Ink5;
                    h.vitalsCard.style.borderTopColor = border; h.vitalsCard.style.borderBottomColor = border;
                    h.vitalsCard.style.borderLeftColor = border; h.vitalsCard.style.borderRightColor = border;
                }
                if (h.nameLbl != null) h.nameLbl.style.color = !c.Alive ? Palette.SpecCrimson : isActive ? Palette.Gold1 : Palette.WarmIvory;
                if (h.hpLbl != null) h.hpLbl.text = $"{c.unit.hp}/{c.Stats.maxHp}";
                if (h.mpLbl != null)
                {
                    h.mpLbl.text = c.Alive ? $"{c.unit.mp}/{c.Stats.maxMp}" : "DOWN";
                    h.mpLbl.style.color = c.Alive ? Palette.WarmIvory : Palette.SpecCrimson;
                }
                if (h.statusLbl != null)
                {
                    h.statusLbl.text = c.Alive && c.guarding ? "GD" : "";
                    SyncStatusIcons(h, c);
                    h.statusLbl.style.color = c.HasDebuff ? Palette.SpecCrimson : Palette.Frost;
                }

                float hpRatio = c.Alive ? (float)c.unit.hp / Mathf.Max(1, c.Stats.maxHp) : 0f;
                FigmaUi.SetGaugeFill(h.hpGauge, hpRatio, hpRatio <= 0.25f ? Palette.HpLow : Palette.Hp);
                FigmaUi.SetGaugeFill(h.mpGauge, (float)c.unit.mp / Mathf.Max(1, c.Stats.maxMp), Palette.Mp);
            }

            if (activeMarker != null)
                activeMarker.style.display = Battle.Active != null ? DisplayStyle.Flex : DisplayStyle.None;
            BuildTurnBar();
        }

        void BuildTurnBar()
        {
            if (turnBar == null) return;
            turnBar.Clear();

            void Chip(Combatant c, string template, bool active = false)
            {
                var ch = FigmaUi.InstantiateTemplate(template);
                if (ch == null) return;
                FigmaUi.SetInstanceText(ch, "text-0", active ? $"▸{c.tag}" : c.tag);
                ch.style.marginRight = 4;
                turnBar.Add(ch);
            }

            if (Battle.Active != null) Chip(Battle.Active, "Turn-Chip-Active", true);
            foreach (var c in Battle.Upcoming.Take(6)) Chip(c, c.IsEnemy ? "Turn-Chip-Enemy" : "Turn-Chip-Hero");
            turnBar.Add(new UiLabel("‖") { style = { fontSize = 18, color = Palette.Mist3, marginLeft = 4, marginRight = 4 } });
            foreach (var c in Battle.NextRoundOrder.Take(4)) Chip(c, "Turn-Chip-Next-Round");
        }

        void Update()
        {
            var a = Battle?.Active;
            if (a != null && activeMarker != null && activeMarker.style.display == DisplayStyle.Flex && huds.TryGetValue(a, out var ah))
            {
                float bob = Mathf.Sin(Time.time * 6f) * 3f;
                bool hasStageBadges = ah.stageStatusRow != null && ah.stageStatusRow.childCount > 0;
                activeMarker.style.left = ah.pos.x + ah.size.x / 2f - 12f;
                activeMarker.style.top = Mathf.Max(2f, ah.pos.y - (hasStageBadges ? 46f : 24f) + bob);
            }

            // Idle loops (fallen units hold their frame). A boss phase swaps the combatant's enemy def: reload its art.
            foreach (var kv in huds)
            {
                var c = kv.Key;
                var h = kv.Value;
                if (c.IsEnemy && c.enemy?.id != h.artKey)
                {
                    h.artKey = c.enemy?.id;
                    h.idle = UnitArt.Enemy(h.artKey, c.unit.sprite);
                    h.idleFrame = -1;
                }
                // A hero who falls (any cause: a blow, poison, a status) slumps once and holds it; revived, they stand again.
                if (!c.IsEnemy && !c.Alive && !h.fallen && h.spriteEl != null)
                {
                    h.fallen = true;
                    var ko = UnitArt.HeroClip(c.unit.sprite, "ko");
                    if (ko != null) StartCoroutine(PlayClip(h, ko));
                }
                if (h.fallen && c.Alive) { h.fallen = false; h.idleFrame = -1; }
                if (h.idle == null || h.spriteEl == null || !c.Alive || h.busy) continue;
                int f = UnitArt.FrameAt(Time.time + h.idleOffset);
                if (f == h.idleFrame) continue;
                h.idleFrame = f;
                h.spriteEl.style.backgroundImage = new StyleBackground(h.idle[f]);
            }

            // Advance UITK sprite-sheet VFX
            for (int i = activeFxs.Count - 1; i >= 0; i--)
            {
                var fx = activeFxs[i];
                fx.age += Time.deltaTime;
                int frame = (int)(fx.age * fx.clip.fps);
                if (frame >= fx.clip.frames.Length)
                {
                    fx.el.RemoveFromHierarchy();
                    activeFxs.RemoveAt(i);
                }
                else
                {
                    fx.el.style.backgroundImage = new StyleBackground(fx.clip.frames[frame]);
                }
            }

            // Advance floating combat popups
            for (int i = activePopups.Count - 1; i >= 0; i--)
            {
                var p = activePopups[i];
                p.age += Time.deltaTime;
                float t = Mathf.Clamp01(p.age / 0.75f);
                p.lbl.style.top = p.startY - Mathf.Sin(t * Mathf.PI * 0.5f) * 36f;
                if (p.age > 0.75f)
                {
                    p.lbl.RemoveFromHierarchy();
                    activePopups.RemoveAt(i);
                }
            }
        }

        void Hint(string text)
        {
            if (hintLabel != null) hintLabel.text = (text ?? "").ToUpperInvariant();
        }

        void AddLog(string line)
        {
            logLines.AddRange(PixelFont.Wrap(line.ToUpperInvariant(), 158).Split('\n'));
            while (logLines.Count > LogLines) logLines.RemoveAt(0);
            for (int i = 0; i < LogLines && i < logLabels.Count; i++)
                logLabels[i].text = i < logLines.Count ? logLines[i] : "";
        }

        void ShowDossier(string title, string costType, string target, string desc, IEnumerable<(StatusId id, string label)> statusChips = null)
        {
            foreach (var l in logLabels) l.style.display = DisplayStyle.None;
            if (dossierTargetBox != null)
            {
                dossierTargetBox.Clear();
                dossierTargetBox.style.display = DisplayStyle.None;
            }
            dossierTitle.style.display = DisplayStyle.Flex;
            dossierDesc.style.display = DisplayStyle.Flex;
            dossierCostType.style.display = string.IsNullOrEmpty(costType) ? DisplayStyle.None : DisplayStyle.Flex;
            dossierTarget.style.display = string.IsNullOrEmpty(target) ? DisplayStyle.None : DisplayStyle.Flex;

            dossierTitle.text = title ?? "";
            dossierDesc.text = desc ?? "";
            dossierCostType.text = costType ?? "";
            dossierTarget.text = target ?? "";

            if (dossierStatusRow != null)
            {
                dossierStatusRow.Clear();
                if (statusChips != null)
                {
                    foreach (var sc in statusChips)
                    {
                        var chip = IconArt.StatusChip(sc.id, sc.label, 1, 5f);
                        if (chip != null) dossierStatusRow.Add(chip);
                    }
                }
                dossierStatusRow.style.display = dossierStatusRow.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        // Rich 22b Target Inspection Dossier with per-status Figma badges, pixel icons, durations, and descriptions.
        void ShowTargetDossier(Combatant cand)
        {
            foreach (var l in logLabels) l.style.display = DisplayStyle.None;
            dossierTitle.style.display = DisplayStyle.None;
            dossierDesc.style.display = DisplayStyle.None;
            dossierCostType.style.display = DisplayStyle.None;
            dossierTarget.style.display = DisplayStyle.None;
            if (dossierStatusRow != null)
            {
                dossierStatusRow.Clear();
                dossierStatusRow.style.display = DisplayStyle.None;
            }
            if (dossierTargetBox == null || cand == null) return;

            dossierTargetBox.Clear();
            dossierTargetBox.style.display = DisplayStyle.Flex;

            var topCol = new VisualElement { style = { flexDirection = FlexDirection.Column } };

            // Row 1: Target Name + HP/Shield (or HP/MP)
            var hdrRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = UnityEngine.UIElements.Align.Center, marginBottom = 2 }
            };
            hdrRow.Add(new UiLabel(cand.name.ToUpperInvariant())
            {
                style = { fontSize = 18, color = Palette.Gold1, flexShrink = 1 }
            });
            string vitalsText = !cand.Alive
                ? "FALLEN (KO)"
                : cand.IsEnemy
                    ? $"HP {cand.unit.hp}/{cand.Stats.maxHp}" + (cand.HasBreakGauge ? $" · SHD {cand.CurrentBreakGauge}" : "")
                    : $"HP {cand.unit.hp}/{cand.Stats.maxHp} · MP {cand.unit.mp}/{cand.Stats.maxMp}";
            hdrRow.Add(new UiLabel(vitalsText)
            {
                style = { fontSize = 11, color = Palette.Mist1, letterSpacing = 0.4f }
            });
            topCol.Add(hdrRow);

            // Row 2: Weaknesses (enemies) or Role (allies)
            string subText = cand.IsEnemy
                ? (cand.Affinities?.weak != null && cand.Affinities.weak.Length > 0
                    ? "WEAK: " + string.Join(" · ", cand.Affinities.weak.Select(ElementConfig.Short))
                    : cand.Affinities.Describe())
                : (cand.unit.role ?? "ALLY").ToUpperInvariant();
            if (!string.IsNullOrEmpty(subText))
            {
                topCol.Add(new UiLabel(subText)
                {
                    style = { fontSize = 11, color = Palette.Gold2, letterSpacing = 0.48f, marginBottom = 6 }
                });
            }

            // Row 3..N: Active Status rows (22b style: Figma Status-Icon badge + Name/Turns + Description)
            var active = CollectActiveStatuses(cand);
            if (active.Count == 0)
            {
                topCol.Add(new UiLabel(cand.guarding ? "GUARDING — INCOMING DAMAGE HALVED" : "NO ACTIVE STATUS EFFECTS")
                {
                    style = { fontSize = 12, color = Palette.Mist2, marginTop = 6 }
                });
            }
            else
            {
                int shown = 0;
                foreach (var s in active.Take(3))
                {
                    var row = new VisualElement
                    {
                        style = { flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.FlexStart, marginBottom = 6 }
                    };
                    var badge = BuildStatusIconBadge(s.id, s.turns, s.stacks);
                    if (badge != null) row.Add(badge);

                    var textCol = new VisualElement { style = { flexDirection = FlexDirection.Column, flexShrink = 1, marginLeft = 4 } };
                    string turnWord = s.turns == 1 ? "TURN" : "TURNS";
                    string stackNote = s.stacks > 1 ? $" ×{s.stacks}" : "";
                    textCol.Add(new UiLabel($"{StatusInfo.Name(s.id)} · {s.turns} {turnWord}{stackNote}")
                    {
                        style = { fontSize = 13, color = Palette.Mist1, letterSpacing = 0.4f }
                    });
                    textCol.Add(new UiLabel(StatusInfo.Description(s.id).ToUpperInvariant())
                    {
                        style = { fontSize = 11, color = Palette.Mist2, whiteSpace = WhiteSpace.Normal }
                    });
                    row.Add(textCol);
                    topCol.Add(row);
                    shown++;
                }
                if (active.Count > shown)
                {
                    topCol.Add(new UiLabel($"+{active.Count - shown} MORE STATUS EFFECTS")
                    {
                        style = { fontSize = 10, color = Palette.Mist3 }
                    });
                }
            }

            dossierTargetBox.Add(topCol);

            // Bottom action hint matching 22b line 273
            dossierTargetBox.Add(new UiLabel(cand.IsEnemy ? "▸ ENTER TO STRIKE · ESC BACK" : "▸ ENTER TO SELECT · ESC BACK")
            {
                style = { fontSize = 11, color = Palette.Gold2, letterSpacing = 0.48f }
            });
        }

        void HideDossier()
        {
            dossierTitle.style.display = DisplayStyle.None;
            dossierDesc.style.display = DisplayStyle.None;
            dossierCostType.style.display = DisplayStyle.None;
            dossierTarget.style.display = DisplayStyle.None;
            if (dossierStatusRow != null)
            {
                dossierStatusRow.Clear();
                dossierStatusRow.style.display = DisplayStyle.None;
            }
            if (dossierTargetBox != null)
            {
                dossierTargetBox.Clear();
                dossierTargetBox.style.display = DisplayStyle.None;
            }
            foreach (var l in logLabels) l.style.display = DisplayStyle.Flex;
        }

        IEnumerator Loop()
        {
            Refresh();
            yield return new WaitForSeconds(0.4f);
            while (Battle.Outcome == Outcome.None)
            {
                var c = Battle.NextTurn();
                Refresh();
                if (pendingPhaseTransitionBoss != null)
                {
                    var boss = pendingPhaseTransitionBoss;
                    pendingPhaseTransitionBoss = null;
                    yield return RunBossPhaseTransition(boss);
                }
                if (c == null) { yield return null; continue; }

                if (c.IsEnemy)
                {
                    Hint($"{c.name.ToUpperInvariant()} ACTS");
                    yield return new WaitForSeconds(EnemyThink);
                    var act = EnemyAI.Choose(Battle, c);
                    if (act == null) continue;
                    var qte = QteFactory.Defense(act, Assist);
                    if (qte == null)
                    {
                        Telegraph(act);
                        yield return new WaitForSeconds(EnemyThink);
                        yield return Resolve(act);
                        continue;
                    }
                    yield return RunQte(qte, Telegraph(act), act);
                    if (qte.Cancelled) break;
                    var defender = act.target;
                    yield return TryRetryQte(defender, qte, () => QteFactory.Defense(act, Assist), Telegraph(act), act, retried => qte = retried);
                    if (qte.Cancelled) break;
                    yield return Resolve(act, 1f, qte is DefenseQte d ? d.Results : ((AutoQte)qte).Results);
                }
                else
                {
                    chosen = null;
                    ShowActions(c);
                    while (chosen == null) yield return null;
                    var qte = QteFactory.Offense(chosen, Battle.Tier, Assist, qteRng);
                    float power = 1f;
                    if (qte != null)
                    {
                        c.ResetQteLuckyFlag();
                        yield return RunQte(qte, OffenseHeading(chosen), null);
                        if (qte.Cancelled) break;
                        yield return TryRetryQte(c, qte, () => QteFactory.Offense(chosen, Battle.Tier, Assist, qteRng), OffenseHeading(chosen), null, retried => qte = retried);
                        if (qte.Cancelled) break;
                        power = Battle.QtePower(chosen, qte.Result);
                    }
                    yield return Resolve(chosen, power);
                }
            }
            Refresh();
            ShowOutcome();
        }

        IEnumerator TryRetryQte(Combatant owner, Qte qte, Func<Qte> makeRetry, string heading, BattleAction enemyAction, Action<Qte> onRetried)
        {
            if (owner == null || qte == null || qte.Cancelled || qte.Result != QteResult.Miss) yield break;

            string retryTag = null;
            if (owner.CanTriggerLuckyRetry(qte.Result, () => (float)qteRng.NextDouble()))
            {
                owner.RecordLuckyRetry();
                retryTag = "LUCKY ACTIVATED! RETRYING QTE...";
            }
            else if (owner.ConsumeSecondChanceRetry())
            {
                retryTag = "SECOND CHANCE! RETRYING QTE...";
            }

            if (retryTag == null) yield break;
            Sfx.Play(Sound.QteRetry);
            Hint(retryTag);
            yield return new WaitForSeconds(0.4f);
            var retryQte = makeRetry();
            if (retryQte == null) yield break;
            yield return RunQte(retryQte, "RETRY! " + heading, enemyAction);
            onRetried?.Invoke(retryQte);
        }

        IEnumerator Resolve(BattleAction action, float power = 1f, IReadOnlyList<QteResult> defense = null)
        {
            ClearPhase();
            Hint("");
            HideDossier();
            if (action.kind == ActionKind.Guard) Sfx.Play(Sound.Guard);
            // The actor swings, shoots, casts, pounces or reaps first; the blows land on the clip's strike frame.
            // Melee and pouncing attackers slide toward the other side (heroes face right, enemies left).
            // An enemy that only charges this turn does not attack yet.
            var actor = action.actor;
            if (actor != null && !action.charging && (action.kind == ActionKind.Attack || action.kind == ActionKind.Skill)
                && huds.TryGetValue(actor, out var actorHud)
                && (actor.IsEnemy ? UnitArt.EnemyClip(actor.enemy?.id, actor.unit.sprite) : UnitArt.HeroClip(actor.unit.sprite, "act")) is UnitArt.Clip act)
            {
                float lunge = act.style == "melee" || act.style == "pounce" ? (actor.IsEnemy ? -MeleeLunge : MeleeLunge) : 0f;
                StartCoroutine(PlayClip(actorHud, act, lunge));
                yield return new WaitForSeconds(act.TimeTo(act.strike));
            }
            int idx = 0;
            foreach (var h in Battle.Perform(action, power, defense))
            {
                var defGrade = defense != null && idx < defense.Count ? defense[idx] : (QteResult?)null;
                Feedback(h, action, defGrade);
                idx++;
            }
            Refresh();
            if (pendingPhaseTransitionBoss != null)
            {
                var boss = pendingPhaseTransitionBoss;
                pendingPhaseTransitionBoss = null;
                yield return RunBossPhaseTransition(boss);
            }
            else
            {
                yield return new WaitForSeconds(AfterAction);
            }
        }

        IEnumerator RunBossPhaseTransition(Combatant boss)
        {
            ClearPhase();
            HideDossier();
            MusicManager.I?.StopImmediately();
            Hint(boss.enemy?.phase2Text ?? $"{boss.name.ToUpperInvariant()}'S FIRST SOUL-SHELL SHATTERS!");
            var hud = huds.TryGetValue(boss, out var h) ? h : null;
            if (hud != null)
            {
                hud.phase = boss.phase;
                StartCoroutine(Flash(hud, boss));
            }
            MusicManager.I?.PlayZoroaPhaseTransition(() =>
            {
                Hint($"{boss.name.ToUpperInvariant()} AWAKENS — THE SOUL REAPING (PHASE 2)!");
                Refresh();
            });
            for (int step = 0; step < 28; step++)
            {
                if (step == 10) Hint("THE CHOIR OF LOST SOULS AWAKENS...");
                if (step == 20) Hint("THE HARVESTER RISES ANEW — ASCENSION IMMINENT!");
                if (hud?.spriteEl != null) hud.spriteEl.style.opacity = step % 2 == 0 ? 0.5f : 1f;
                yield return new WaitForSeconds(0.2f);
            }
            if (hud?.spriteEl != null) hud.spriteEl.style.opacity = 1f;
            Refresh();
            yield return new WaitForSeconds(0.4f);
        }

        static QteAssist Assist => Game.I != null ? Game.I.Settings.Assist : QteAssist.Standard;

        static string OffenseHeading(BattleAction a) =>
            a.kind == ActionKind.Attack ? $"STRIKE {a.target.name.ToUpperInvariant()}!"
            : a.skill.kind == SkillKind.Physical ? $"{a.skill.name.ToUpperInvariant()}!"
            : $"CAST {a.skill.name.ToUpperInvariant()}";

        string Telegraph(BattleAction a)
        {
            var move = a.attack;
            if (move != null && move.self)
            {
                var effects = (move.apply ?? Array.Empty<StatusApply>()).Select(ap => StatusInfo.Name(ap.id).ToUpperInvariant()).ToList();
                if (move.restoreMp > 0f) effects.Add($"RESTORES {move.restoreMp * 100:0}% MP");
                Hint($"{move.name.ToUpperInvariant()}: " + string.Join(", ", effects));
                return null;
            }
            string who = move != null && move.allTargets ? "ALL" : a.target.name.ToUpperInvariant();
            if (a.charging)
            {
                Hint($"{a.actor.name.ToUpperInvariant()} CHARGES {move.name.ToUpperInvariant()} > {who} - RELEASES NEXT TURN. BREAK IT TO INTERRUPT!");
                return null;
            }
            if (move != null && move.StatusOnly)
            {
                Hint($"{move.name.ToUpperInvariant()} > {who}: " + string.Join(", ", move.apply.Select(ap => StatusInfo.Name(ap.id).ToUpperInvariant())));
                return null;
            }
            string element = move != null && move.element != Element.Physical ? " " + move.element.ToString().ToUpperInvariant() : "";
            Hint(move == null ? $"{a.actor.name.ToUpperInvariant()} ATTACKS {who}"
                : $"{move.name.ToUpperInvariant()}{(move.hits > 1 ? $" X{move.hits}" : "")} > {who}  {EnemyAI.Severity(move)}{element}");
            return $"GUARD {who}!";
        }

        IEnumerator RunQte(Qte qte, string heading, BattleAction enemyAction)
        {
            ClearPhase();
            HideDossier();
            foreach (var h in huds.Values)
                if (h.targetArrow != null) h.targetArrow.style.display = DisplayStyle.None;
            if (enemyAction != null)
            {
                foreach (var t in enemyAction.attack != null && enemyAction.attack.allTargets ? Battle.Party.Where(p => p.Alive) : new[] { enemyAction.target })
                    if (t != null && huds.TryGetValue(t, out var th) && th.targetArrow != null)
                    {
                        th.targetArrow.style.color = Palette.Err;
                        th.targetArrow.style.display = DisplayStyle.Flex;
                    }
            }

            menuHost.style.display = DisplayStyle.None;
            qteHost.style.display = DisplayStyle.Flex;
            if (!(qte is AutoQte)) Sfx.Play(enemyAction != null ? Sound.QteTelegraph : Sound.QteStart);
            var panel = phaseGo.AddComponent<QtePanel>();
            panel.Init(qte, heading, qteHost);
            activeQte = qte;
            if (qte is AutoQte) yield return new WaitForSeconds(enemyAction != null ? EnemyThink : 0.15f);
            while (!qte.Done)
            {
                if (Battle.Outcome != Outcome.None) qte.Cancel();
                yield return null;
            }
            activeQte = null;
            if (!qte.Cancelled && !(qte is AutoQte)) yield return new WaitForSecondsRealtime(0.45f);
            qteHost.style.display = DisplayStyle.None;
            menuHost.style.display = DisplayStyle.Flex;
            foreach (var h in huds.Values)
                if (h.targetArrow != null) h.targetArrow.style.display = DisplayStyle.None;
        }

        void OnDestroy() => activeQte?.Cancel();

        void Feedback(Hit hit, BattleAction action, QteResult? defGrade = null)
        {
            if (!huds.TryGetValue(hit.target, out var h)) return;
            float cx = h.pos.x + h.size.x / 2f;
            bool immune = hit.effect == Effectiveness.Immune;
            PlayHitFx(hit, action, h, cx, defGrade);

            SpawnPopup(hit.blocked ? "PARRY" : immune ? "IMMUNE" : hit.amount.ToString(), cx, h.pos.y - 10f,
                hit.blocked ? Palette.FrostBright : immune ? Palette.MutedGray : hit.heal ? Palette.Emerald : Palette.CrimsonBright, 20);
            if (hit.brokeTarget)
                SpawnPopup("@ BREAK! @", cx, h.pos.y - 36f, Palette.Gold1, 18);
            if (hit.effect == Effectiveness.Weak || hit.effect == Effectiveness.Resist)
                SpawnPopup(hit.effect == Effectiveness.Weak ? "* WEAK! *" : "RESIST", cx, h.pos.y - 24f,
                    hit.effect == Effectiveness.Weak ? Palette.Gold1 : Palette.Mist3, 14);
            if (!hit.heal && hit.amount > 0) StartCoroutine(Flash(h, hit.target));
            // A hero who takes damage and stays up flinches (a fall is handled by the KO in Update).
            if (!hit.heal && hit.amount > 0 && !hit.target.IsEnemy && hit.target.Alive && !h.busy
                && UnitArt.HeroClip(hit.target.unit.sprite, "hit") is UnitArt.Clip flinch)
                StartCoroutine(PlayClip(h, flinch));
        }

        // Melee attackers slide this far toward the other side while they strike (UI px; 7 sprite px at 4x).
        const float MeleeLunge = 28f;
        // How far along the lunge each frame of a melee clip sits: hold, lean back, strike, follow through, return, stance.
        static readonly float[] LungeCurve = { 0f, -0.15f, 1f, 1f, 0.4f, 0f };

        // Plays a one-shot hero clip; the idle loop waits while it runs. A KO's last frame simply stays up
        // (the idle loop never repaints a fallen hero).
        IEnumerator PlayClip(Hud h, UnitArt.Clip clip, float lunge = 0f)
        {
            if (h.spriteEl == null) yield break;
            h.busy = true;
            for (int i = 0; i < clip.frames.Length; i++)
            {
                if (h.fallen && clip.style != "ko") break; // the hero fell mid-clip: the KO owns the sprite now
                h.spriteEl.style.backgroundImage = new StyleBackground(clip.frames[i]);
                if (lunge != 0f) h.spriteEl.style.translate = new Translate(lunge * (i < LungeCurve.Length ? LungeCurve[i] : 0f), 0);
                yield return new WaitForSeconds(clip.delays[i]);
            }
            if (lunge != 0f) h.spriteEl.style.translate = new Translate(0, 0);
            if (h.fallen && clip.style != "ko") yield break; // leave busy to the KO clip
            h.busy = false;
            h.idleFrame = -1;
        }

        void SpawnPopup(string text, float cx, float y, Color color, int fontSize)
        {
            var lbl = new UiLabel(text);
            lbl.style.position = Position.Absolute;
            lbl.style.left = cx - 90f;
            lbl.style.top = y;
            lbl.style.width = 180f;
            lbl.style.fontSize = fontSize;
            lbl.style.color = color;
            lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            stageHost.Add(lbl);
            activePopups.Add(new ActivePopup { lbl = lbl, startY = y });
        }

        void PlayHitFx(Hit hit, BattleAction action, Hud h, float cx, QteResult? defGrade = null)
        {
            float cy = h.pos.y + h.size.y / 2f, feet = h.pos.y + h.size.y;
            bool flip = !hit.target.IsEnemy;
            void Play(string name)
            {
                var clip = Fx.Load(name);
                if (clip == null || clip.frames.Length == 0) return;
                float fw = clip.frames[0].rect.width * 2f;
                float fh = clip.frames[0].rect.height * 2f;
                var fxEl = new VisualElement
                {
                    style =
                    {
                        position = Position.Absolute,
                        left = cx - fw / 2f,
                        top = (clip.grounded ? feet - fh * 0.85f : cy - fh / 2f),
                        width = fw,
                        height = fh,
                        backgroundImage = new StyleBackground(clip.frames[0]),
                    }
                };
                if (flip) fxEl.style.scale = new Scale(new Vector2(-1, 1));
                stageHost.Add(fxEl);
                activeFxs.Add(new ActiveFx { el = fxEl, clip = clip });
            }

            if (hit.blocked) { Play("parry"); Sfx.Play(Sound.Parry); }
            else if (hit.effect == Effectiveness.Immune) { Sfx.Play(Sound.Immune); }
            else if (hit.heal) { Play("heal"); Sfx.Play(Sound.Heal); }
            else if (hit.amount > 0)
            {
                var element = action?.skill?.element ?? action?.attack?.element ?? Element.Physical;
                var (fxName, sfx) = hit.critical
                    ? ("crit", Sound.Crit)
                    : element switch
                    {
                        Element.Fire => ("fire", Sound.Fire),
                        Element.Ice => ("ice", Sound.Ice),
                        Element.Lightning => ("lightning", Sound.Lightning),
                        Element.Arcane => ("arcane", Sound.Arcane),
                        Element.Holy => ("holy", Sound.Holy),
                        Element.Dark => ("dark", Sound.Dark),
                        Element.Earth => ("earth", Sound.Earth),
                        Element.Wind => ("wind", Sound.Wind),
                        Element.Water => ("water", Sound.Water),
                        _ => ("hit", Sound.Hit),
                    };
                Play(fxName);
                Sfx.Play(defGrade == QteResult.Good ? Sound.Block : sfx);
            }
            if (hit.brokeTarget) { Play("break"); Sfx.Play(Sound.Break); }
            if (hit.killed) { Play("death"); Sfx.Play(Sound.Death); }
        }

        IEnumerator Flash(Hud h, Combatant c)
        {
            h.flashing = true;
            if (h.spriteEl != null) h.spriteEl.style.unityBackgroundImageTintColor = Palette.SpecCrimson;
            yield return new WaitForSeconds(0.12f);
            if (h.spriteEl != null)
            {
                h.spriteEl.style.unityBackgroundImageTintColor = Color.white;
                h.spriteEl.style.opacity = c.Alive ? 1f : 0.35f;
            }
            h.flashing = false;
        }

        void RegisterStageTargetEvents(VisualElement el, Combatant c)
        {
            if (el == null || c == null) return;
            el.pickingMode = PickingMode.Position;
            el.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (Controls.Locked || currentMenu == null || UitkMenu.ActiveMenu != currentMenu) return;
                if (activeTargets.TryGetValue(c, out var entry) && entry.item != null && entry.item.interactable)
                    currentMenu.Focus(entry.item, false);
            });
            el.RegisterCallback<ClickEvent>(_ =>
            {
                if (Controls.Locked || currentMenu == null || UitkMenu.ActiveMenu != currentMenu) return;
                if (activeTargets.TryGetValue(c, out var entry) && entry.item != null)
                {
                    if (!entry.item.interactable) { Sfx.Play(Sound.Deny); return; }
                    Sfx.Play(Sound.Confirm);
                    entry.choose?.Invoke();
                }
            });
        }

        void ClearPhase()
        {
            activeTargets.Clear();
            if (phaseGo != null) Destroy(phaseGo);
            phaseGo = new GameObject("CombatPhase");
            phaseGo.transform.SetParent(transform, false);
            menuHost?.Clear();
            qteHost?.Clear();
            if (qteHost != null) qteHost.style.display = DisplayStyle.None;
            if (menuHost != null) menuHost.style.display = DisplayStyle.Flex;
            foreach (var h in huds.Values)
                if (h.targetArrow != null) h.targetArrow.style.display = DisplayStyle.None;
            HideDossier();
        }

        UitkMenu NewMenu()
        {
            ClearPhase();
            currentMenu = phaseGo.AddComponent<UitkMenu>();
            currentMenu.AttachRoot(UiRoot);
            return currentMenu;
        }

        void Option(UitkMenu m, string text, Action onClick, bool enabled = true, string tooltip = null, string costType = null, string target = null, string rawTitle = null, Texture2D icon = null, IEnumerable<(StatusId id, string label)> statusChips = null)
        {
            var btn = FigmaUi.CreateButton(text, "button-slab", 236f);
            btn.style.height = 46;
            btn.style.marginBottom = 6;
            btn.style.paddingLeft = icon != null ? 44 : 10;
            btn.style.paddingRight = 8;
            btn.style.overflow = Overflow.Hidden;
            var iconEl = IconArt.Inline(icon, 2, 0);
            if (iconEl != null)
            {
                iconEl.style.position = Position.Absolute; iconEl.style.left = 6; iconEl.style.top = 5;
                if (!enabled) iconEl.style.opacity = 0.45f;
                btn.Add(iconEl);
            }
            var lbl = btn.Q<UiLabel>("uitkLabel");
            if (lbl != null)
            {
                lbl.style.flexGrow = 1;
                lbl.style.flexShrink = 1;
                lbl.style.whiteSpace = WhiteSpace.Normal;
                lbl.style.unityTextAlign = icon != null ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
                lbl.style.fontSize = icon != null
                    ? (text.Length > 18 ? 13 : text.Length > 14 ? 14 : 16)
                    : 18;
            }
            btn.SetEnabled(enabled);
            menuHost.Add(btn);

            var chipsList = statusChips?.ToList();
            var item = m.Add(btn, rawTitle ?? text, onClick, interactable: enabled, tooltip: tooltip ?? "");
            item.visual = on =>
            {
                btn.style.backgroundColor = !enabled ? Palette.Ink1 : on ? Palette.Hex(0x2B2210) : Palette.Ink2;
                var b = on ? Palette.Gold1 : Palette.Ink4;
                btn.style.borderTopColor = b; btn.style.borderBottomColor = b;
                btn.style.borderLeftColor = b; btn.style.borderRightColor = b;
                var col = !enabled ? Palette.Mist3 : on ? Palette.Gold1 : Palette.Mist1;
                btn.style.color = col;
                if (lbl != null) lbl.style.color = col;
                if (on)
                {
                    Hint(!string.IsNullOrEmpty(tooltip) ? tooltip : turnText);
                    if (!string.IsNullOrEmpty(tooltip)) ShowDossier(rawTitle ?? text, costType, target, tooltip, chipsList);
                    else HideDossier();
                }
            };
            item.visual(false);
        }

        void ShowActions(Combatant c)
        {
            turnText = $"{c.name.ToUpperInvariant()}'S TURN — {c.unit.role.ToUpperInvariant()}";
            var m = NewMenu();
            Option(m, "ATTACK", () => ChooseTarget(Battle.Opponents(c), t => BattleAction.Attack(c, t), () => ShowActions(c)),
                tooltip: "STRIKE ONE ENEMY WITH YOUR WEAPON. TIMED QTE RAISES DAMAGE.",
                costType: "COST: —   TYPE: PHYSICAL", target: "TARGET: ENEMY", rawTitle: "ATTACK");
            Option(m, "SKILL", () => ShowSkills(c),
                tooltip: "EXECUTE CLASS SKILLS & SPELLS",
                costType: "COST: MP", target: "TARGET: VARIES", rawTitle: "SKILL");
            Option(m, "ITEM", () => ShowItems(c), Battle.Items.Count > 0,
                Battle.Items.Count > 0 ? "USE A BATTLE INVENTORY ITEM" : "NO ITEMS REMAINING",
                costType: "COST: 1 ITEM", target: "TARGET: ALLY", rawTitle: "ITEM");
            Option(m, "GUARD", () => Choose(BattleAction.Guard(c)),
                tooltip: "HALVE INCOMING DAMAGE UNTIL NEXT TURN",
                costType: "COST: —   DEFENSE UP", target: "TARGET: SELF", rawTitle: "GUARD",
                statusChips: new[] { (StatusId.DefenseUp, "DEFENSE UP · 1T") });
        }

        void ShowSkills(Combatant c)
        {
            var skills = c.unit.GetCombatActiveSkillIds().Select(id => Database.Skills[id]).ToList();
            var m = NewMenu();
            foreach (var s in skills)
            {
                var sk = s;
                int cd = c.Cooldown(sk.id);
                int effCost = c.GetEffectiveMpCost(sk, Battle.run);
                bool hasShield = !sk.requiresShield || (c.unit != null && c.unit.HasEquippedShield());
                bool hasWeapon = sk.requiredWeaponType == WeaponType.None || (c.unit != null && c.unit.HasEquippedWeaponType(sk.requiredWeaponType));
                string element = sk.kind == SkillKind.Magic || sk.kind == SkillKind.Heal ? sk.element.ToString().ToUpperInvariant() + " " : "";
                string costText = cd > 0 ? $"CD {cd}" : $"{effCost} MP";
                string typeText = $"{element}{sk.kind.ToString().ToUpperInvariant()}";
                string targetText = $"TARGET: {sk.target.ToString().ToUpperInvariant()}";
                var skChips = sk.apply != null && sk.apply.Length > 0
                    ? sk.apply.Select(ap => (ap.id, $"{StatusInfo.Name(ap.id).ToUpperInvariant()} {Mathf.RoundToInt(ap.chance * 100)}% · {ap.turns}T"))
                    : null;
                Option(m, $"{sk.name.ToUpperInvariant()} {costText}", () => UseSkill(c, sk),
                    cd == 0 && c.unit.mp >= effCost && hasShield && hasWeapon, sk.description.ToUpperInvariant(),
                    $"COST: {costText}   TYPE: {typeText}", targetText, sk.name.ToUpperInvariant(), IconArt.Skill(sk.id), skChips);
            }
            Option(m, "BACK", () => ShowActions(c), rawTitle: "BACK");
            m.onBack = () => ShowActions(c);
        }

        void UseSkill(Combatant c, SkillDef s)
        {
            switch (s.target)
            {
                case TargetType.Enemy: ChooseTarget(Battle.Opponents(c), t => BattleAction.Skill(c, s, t), () => ShowSkills(c)); break;
                case TargetType.Ally: ChooseTarget(Battle.Allies(c), t => BattleAction.Skill(c, s, t), () => ShowSkills(c)); break;
                default: Choose(BattleAction.Skill(c, s, s.target == TargetType.Self ? c : null)); break;
            }
        }

        void ShowItems(Combatant c)
        {
            var stacks = Battle.Items.GroupBy(id => id).ToList();
            var m = NewMenu();
            foreach (var stack in stacks)
            {
                var item = Database.Items[stack.Key];
                Option(m, $"{item.name.ToUpperInvariant()} x{stack.Count()}",
                    () =>
                    {
                        var candidates = item.isRevive ? Battle.Allies(c).Where(a => !a.Alive) : Battle.Allies(c).Where(a => a.Alive);
                        ChooseTarget(candidates, t => BattleAction.Item(c, item, t), () => ShowItems(c));
                    },
                    tooltip: item.description.ToUpperInvariant(), costType: "CONSUMABLE", target: item.isRevive ? "FALLEN ALLY" : "LIVING ALLY", rawTitle: item.name.ToUpperInvariant(), icon: IconArt.Item(item.id));
            }
            Option(m, "BACK", () => ShowActions(c), rawTitle: "BACK");
            m.onBack = () => ShowActions(c);
        }

        void ChooseTarget(IEnumerable<Combatant> candidates, Func<Combatant, BattleAction> make, Action back)
        {
            var list = candidates.ToList();
            if (list.Count == 0) { Hint("NO VALID TARGETS"); back(); return; }
            var m = NewMenu();
            m.onBack = back;
            int firstValid = -1;
            for (int i = 0; i < list.Count; i++)
            {
                var cand = list[i];
                var h = huds[cand];
                bool valid = Battle.Validate(make(cand)) == null;
                if (firstValid < 0 && valid) firstValid = i;
                string brk = cand.IsEnemy && cand.HasBreakGauge ? $" SHIELD:{cand.CurrentBreakGauge}/{cand.MaxBreakGauge}" : "";
                string tip = cand.Alive
                    ? $"{cand.name.ToUpperInvariant()} {cand.unit.hp}/{cand.Stats.maxHp}{brk} " + (cand.IsEnemy ? cand.Affinities.Describe() : cand.StatusTags)
                    : $"{cand.name.ToUpperInvariant()} - FALLEN (DEAD)";

                // Add target row in command menu
                var activeStatuses = CollectActiveStatuses(cand);
                var btn = FigmaUi.CreateButton(cand.name.ToUpperInvariant(), "button-slab", 236f);
                btn.style.height = 46;
                btn.style.marginBottom = 6;
                btn.style.fontSize = 16;
                btn.style.overflow = Overflow.Hidden;
                if (activeStatuses.Count > 0)
                {
                    var iconStrip = new VisualElement
                    {
                        pickingMode = PickingMode.Ignore,
                        style = { flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center, marginLeft = 6 }
                    };
                    foreach (var s in activeStatuses.Take(3))
                    {
                        var stIcon = IconArt.Inline(IconArt.Status(s.id), 1, 2f);
                        if (stIcon != null) iconStrip.Add(stIcon);
                    }
                    btn.Add(iconStrip);
                }
                btn.SetEnabled(valid);
                menuHost.Add(btn);

                var actionToTake = make(cand);
                var item = m.Add(btn, cand.name.ToUpperInvariant(), () => Choose(actionToTake), interactable: valid, tooltip: tip);
                activeTargets[cand] = (item, () => Choose(actionToTake));
                item.visual = on =>
                {
                    btn.style.backgroundColor = !valid ? Palette.Ink1 : on ? Palette.Hex(0x2B2210) : Palette.Ink2;
                    var b = on ? Palette.Gold1 : Palette.Ink4;
                    btn.style.borderTopColor = b; btn.style.borderBottomColor = b; btn.style.borderLeftColor = b; btn.style.borderRightColor = b;
                    btn.style.color = !valid ? Palette.Mist3 : on ? Palette.Gold1 : Palette.Mist1;
                    if (h.targetArrow != null)
                    {
                        h.targetArrow.style.color = Palette.Gold1;
                        h.targetArrow.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                    }
                    if (on)
                    {
                        Hint($"CHOOSE A TARGET · {cand.name.ToUpperInvariant()}");
                        ShowTargetDossier(cand);
                    }
                };
                item.visual(false);
            }
            if (firstValid >= 0) m.Focus(firstValid, false, true);
        }

        void Choose(BattleAction action)
        {
            var error = Battle.Validate(action);
            if (error != null) { Sfx.Play(Sound.Deny); Hint(error.ToUpperInvariant()); return; }
            ClearPhase();
            chosen = action;
        }

        void ShowOutcome()
        {
            bool won = Battle.Outcome == Outcome.Victory;
            Sfx.Play(won ? Sound.VictoryStinger : Sound.DefeatStinger);
            ClearPhase();
            turnBar?.Clear();
            Hint("");

            outcomeOverlay = new VisualElement { name = "CombatOutcomeModal" };
            outcomeOverlay.AddToClassList("uitk-modal-backdrop");
            var card = new VisualElement();
            card.AddToClassList("uitk-modal-panel");
            card.style.alignItems = UnityEngine.UIElements.Align.Center;
            card.style.borderTopColor = won ? Palette.Gold3 : Palette.HpLow;
            card.style.borderBottomColor = won ? Palette.Gold3 : Palette.HpLow;
            card.style.borderLeftColor = won ? Palette.Gold3 : Palette.HpLow;
            card.style.borderRightColor = won ? Palette.Gold3 : Palette.HpLow;

            var title = new UiLabel(won ? "VICTORY" : "DEFEAT")
            {
                style = { fontSize = 48, color = won ? Palette.Gold1 : Palette.Hex(0xFF8A7A), marginBottom = 18 }
            };
            title.AddToClassList("uitk-font-display");
            card.Add(title);

            var contBtn = FigmaUi.CreateButton("CONTINUE", "button-primary", 220f);
            contBtn.style.height = 44;
            var contLbl = contBtn.Q<UiLabel>("uitkLabel");
            if (contLbl != null)
            {
                contLbl.style.color = Palette.Ink0;
                contLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            }
            card.Add(contBtn);
            outcomeOverlay.Add(card);
            screenRoot.Add(outcomeOverlay);

            var m = phaseGo.AddComponent<UitkMenu>();
            m.AttachRoot(outcomeOverlay);
            var contItem = m.Add(contBtn, "CONTINUE", () =>
            {
                if (won) { Game.I.Run.WinCombat(); Game.I.Go(GameState.Reward); }
                else { Game.I.Run.LoseCombat(); Game.I.Go(GameState.GameOver); }
            });
            contItem.visual = on =>
            {
                FigmaUi.ApplyDefaultFocusVisual(contBtn, on, contItem.selected, contItem.interactable);
                contBtn.style.backgroundColor = on ? Palette.Gold1 : Palette.Gold2;
                var border = on ? Palette.Gold1 : Palette.Gold3;
                contBtn.style.borderTopColor = border;
                contBtn.style.borderBottomColor = border;
                contBtn.style.borderLeftColor = border;
                contBtn.style.borderRightColor = border;
                if (contLbl != null) contLbl.style.color = Palette.Ink0;
            };
            contItem.visual(false);
        }
    }

    public class Popup : MonoBehaviour { }
}
