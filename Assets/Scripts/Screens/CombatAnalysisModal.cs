using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Obscure
{
    using UiLabel = UnityEngine.UIElements.Label;

    // Live UI Toolkit controller for the Combat Analysis modal
    // (matches Figma 22d Enemies, 22d-b Active Focus, 22e Allies, and 22f Status Explanation rules).
    public sealed class CombatAnalysisModal
    {
        sealed class InspectEntry
        {
            public VisualElement element;
            public string title;
            public string categoryTag;
            public string tagTemplate;
            public string description;
            public string iconTemplate;
            public string activeText;
            public bool isActive;
            public string resistText;
            public bool resistMuted;
        }

        Battle battle;
        VisualElement screenRoot;
        GameObject menuHost;
        Action onClose;
        Action<string> onHint;

        VisualElement overlay;
        UitkMenu menu;
        bool onAllies;
        int unitIndex;

        readonly List<InspectEntry> inspectEntries = new List<InspectEntry>();
        int focusedInspectIndex = -1;
        VisualElement explainContainer;
        bool explainCompact;

        public bool IsOpen => overlay != null;

        public void Open(Battle battle, VisualElement screenRoot, GameObject menuHost, Action onClose, Action<string> onHint)
        {
            if (battle == null || screenRoot == null) return;
            this.battle = battle;
            this.screenRoot = screenRoot;
            this.menuHost = menuHost;
            this.onClose = onClose;
            this.onHint = onHint;
            onAllies = false;
            unitIndex = 0;
            Render();
        }

        public void Close()
        {
            FigmaUi.DestroyMenu(ref menu);
            if (overlay != null)
            {
                overlay.RemoveFromHierarchy();
                overlay = null;
            }
            inspectEntries.Clear();
            focusedInspectIndex = -1;
            explainContainer = null;

            var resume = onClose;
            onClose = null;
            resume?.Invoke();
        }

        public void HandleInput()
        {
            if (!IsOpen) return;
            if (Controls.Left)
            {
                CycleUnit(-1);
            }
            else if (Controls.Right)
            {
                CycleUnit(1);
            }
            else if (Controls.Up)
            {
                StepInspectFocus(-1);
            }
            else if (Controls.Down)
            {
                StepInspectFocus(1);
            }
        }

        void SwitchTab(bool allies)
        {
            if (onAllies == allies) return;
            onAllies = allies;
            unitIndex = 0;
            Sfx.Play(Sound.Move);
            Render();
        }

        void CycleUnit(int delta)
        {
            var units = GetUnits();
            if (units.Count <= 1) return;
            unitIndex = (unitIndex + delta + units.Count) % units.Count;
            Sfx.Play(Sound.Move);
            Render();
        }

        List<Combatant> GetUnits()
        {
            var source = onAllies ? battle.Party : battle.Enemies;
            var living = source.Where(u => u != null && u.Alive).ToList();
            return living.Count > 0 ? living : source.Where(u => u != null).ToList();
        }

        void Render()
        {
            if (overlay != null) overlay.RemoveFromHierarchy();
            inspectEntries.Clear();
            focusedInspectIndex = -1;
            explainContainer = null;
            explainCompact = onAllies;

            var units = GetUnits();
            if (units.Count > 0)
                unitIndex = ((unitIndex % units.Count) + units.Count) % units.Count;
            var currentUnit = units.Count > 0 ? units[unitIndex] : null;

            overlay = new VisualElement { name = "AnalysisOverlay", pickingMode = PickingMode.Position };
            overlay.AddToClassList("uitk-analysis-backdrop");

            var modal = new VisualElement { name = "AnalysisModal" };
            modal.AddToClassList("uitk-analysis-modal");

            // 1. Header bar
            var header = new VisualElement();
            header.AddToClassList("uitk-analysis-header");

            var titleLbl = new UiLabel("ANALYSIS — FREE INSPECTION");
            titleLbl.AddToClassList("uitk-analysis-title");
            header.Add(titleLbl);

            var costLbl = new UiLabel("COST: FREE (NO TURN)");
            costLbl.AddToClassList("uitk-analysis-cost");
            header.Add(costLbl);

            var enemiesTab = FigmaUi.CreateTabButton("ENEMIES", !onAllies, null, 34f, 16f);
            enemiesTab.AddToClassList("uitk-analysis-tab");
            enemiesTab.RegisterCallback<ClickEvent>(_ => SwitchTab(false));
            header.Add(enemiesTab);

            var alliesTab = FigmaUi.CreateTabButton("ALLIES", onAllies, null, 34f, 16f);
            alliesTab.AddToClassList("uitk-analysis-tab");
            alliesTab.RegisterCallback<ClickEvent>(_ => SwitchTab(true));
            header.Add(alliesTab);

            var closeBtn = FigmaUi.CreateButton("CLOSE", "button-slab");
            closeBtn.AddToClassList("uitk-analysis-close");
            if (menuHost == null)
            {
                closeBtn.RegisterCallback<ClickEvent>(_ =>
                {
                    Sfx.Play(Sound.Back);
                    Close();
                });
            }
            header.Add(closeBtn);
            modal.Add(header);

            // 2. Unit cycle bar
            var cycleBar = new VisualElement();
            cycleBar.AddToClassList("uitk-analysis-cycle");

            bool canCycle = units.Count > 1;
            var prevBtn = FigmaUi.CreateCompactButton("◂ PREV");
            prevBtn.AddToClassList("uitk-analysis-cycle-btn");
            if (!canCycle) prevBtn.AddToClassList("uitk-disabled");
            prevBtn.RegisterCallback<ClickEvent>(_ => CycleUnit(-1));
            cycleBar.Add(prevBtn);

            string unitBanner = "NO UNITS";
            if (currentUnit != null)
            {
                if (onAllies)
                {
                    string role = (currentUnit.unit?.role ?? currentUnit.unit?.classDef?.name ?? "HERO").ToUpperInvariant();
                    unitBanner = $"{currentUnit.name.ToUpperInvariant()} · {role}  ·  {unitIndex + 1} / {units.Count}";
                }
                else
                {
                    unitBanner = $"{currentUnit.name.ToUpperInvariant()}  ·  {unitIndex + 1} / {units.Count}";
                }
            }

            var unitNameLbl = new UiLabel(unitBanner);
            unitNameLbl.AddToClassList("uitk-analysis-unit-name");
            cycleBar.Add(unitNameLbl);

            var nextBtn = FigmaUi.CreateCompactButton("NEXT ▸");
            nextBtn.AddToClassList("uitk-analysis-cycle-btn");
            if (!canCycle) nextBtn.AddToClassList("uitk-disabled");
            nextBtn.RegisterCallback<ClickEvent>(_ => CycleUnit(1));
            cycleBar.Add(nextBtn);
            modal.Add(cycleBar);

            // 3. Four-column body
            var body = new VisualElement();
            body.AddToClassList("uitk-analysis-body");

            int defaultFocusIdx = 0;
            if (currentUnit != null)
            {
                if (!onAllies)
                {
                    var ev = AnalysisInspector.BuildEnemyView(battle, currentUnit);
                    defaultFocusIdx = BuildEnemyBody(body, currentUnit, ev);
                }
                else
                {
                    var av = AnalysisInspector.BuildAllyView(currentUnit);
                    defaultFocusIdx = BuildAllyBody(body, currentUnit, av);
                }
            }
            modal.Add(body);

            // 4. Footer key hints
            var footer = new VisualElement();
            footer.AddToClassList("uitk-analysis-footer");
            AddKeyHint(footer, "←/→", "CYCLE UNIT");
            AddKeyHint(footer, "↑/↓", "SELECT EFFECT / STATUS");
            AddKeyHint(footer, "Q/E", "ENEMIES / ALLIES");
            AddKeyHint(footer, "ESC", "RETURN");
            modal.Add(footer);

            overlay.Add(modal);
            overlay.RegisterCallback<DetachFromPanelEvent>(_ => FigmaUi.DestroyMenu(ref menu));
            screenRoot.Add(overlay);

            // Attach modal menu for Q/E/ESC & Close activation
            FigmaUi.DestroyMenu(ref menu);
            if (menuHost != null)
            {
                menu = menuHost.AddComponent<UitkMenu>();
                menu.AttachRoot(overlay);
                menu.onBack = Close;
                menu.onPrevPage = () => SwitchTab(false);
                menu.onNextPage = () => SwitchTab(true);
                menu.Add(closeBtn, "CLOSE", Close);
            }

            if (inspectEntries.Count > 0)
                SelectInspectIndex(Mathf.Clamp(defaultFocusIdx, 0, inspectEntries.Count - 1), sound: false);

            onHint?.Invoke("ANALYSIS · ←/→ CYCLE UNIT · ↑/↓ INSPECT STATUS · Q/E ENEMIES/ALLIES · ESC RETURN");
        }

        // ---- ENEMIES TAB (22d / 22d-b) ----
        int BuildEnemyBody(VisualElement body, Combatant unit, EnemyAnalysisView ev)
        {
            var resistLookup = new Dictionary<StatusId, AnalysisStatusRow>();
            foreach (var r in ev.statusResistances) resistLookup[r.status] = r;

            var activeLookup = new Dictionary<StatusId, int>();
            foreach (var a in ev.activeEffects)
            {
                if (a.statusId.HasValue && !activeLookup.ContainsKey(a.statusId.Value))
                    activeLookup[a.statusId.Value] = a.remainingTurns;
            }

            // Column 1: Identity + Combat Readout + Active Effects
            var colLeft = new VisualElement();
            colLeft.AddToClassList("uitk-analysis-col-left");

            // Identity Card
            var idCard = CreateCard("IDENTITY", "uitk-analysis-card-mb");
            if (ev.affinitiesShifted)
            {
                var alert = new UiLabel("AFFINITIES SHIFTED — CHECK KNOWN ELEMENTS");
                alert.AddToClassList("uitk-analysis-alert");
                idCard.Add(alert);
            }
            var idRow = new VisualElement();
            idRow.AddToClassList("uitk-analysis-identity-row");

            var spriteEl = new VisualElement();
            spriteEl.AddToClassList("uitk-analysis-sprite");
            ApplyUnitSprite(spriteEl, unit);
            idRow.Add(spriteEl);

            var idInfo = new VisualElement();
            idInfo.AddToClassList("uitk-analysis-id-info");
            var nameLbl = new UiLabel(ev.name.ToUpperInvariant());
            nameLbl.AddToClassList("uitk-analysis-id-name");
            idInfo.Add(nameLbl);

            var tagRow = new VisualElement();
            tagRow.AddToClassList("uitk-analysis-id-tags");
            var tierTag = CreateTag("Tag-Cls", ev.tierBadge);
            if (tierTag != null) tagRow.Add(tierTag);
            if (!string.IsNullOrEmpty(ev.phaseLabel))
            {
                var phaseLbl = new UiLabel(ev.phaseLabel);
                phaseLbl.AddToClassList("uitk-analysis-phase");
                tagRow.Add(phaseLbl);
            }
            idInfo.Add(tagRow);

            var metaLbl = new UiLabel($"{ev.family.ToUpperInvariant()} · {ev.role.ToUpperInvariant()} · {ev.dungeonName.ToUpperInvariant()}");
            metaLbl.AddToClassList("uitk-analysis-meta");
            idInfo.Add(metaLbl);

            idRow.Add(idInfo);
            idCard.Add(idRow);
            colLeft.Add(idCard);

            // Combat Readout Card
            var readoutCard = CreateCard("COMBAT READOUT", "uitk-analysis-card-mb");
            float hpRatio = ev.maxHp > 0 ? (float)ev.hp / ev.maxHp : 0f;
            AddGauge(readoutCard, "HP", $"{ev.hp} / {ev.maxHp}", hpRatio, hpRatio <= 0.3f ? "uitk-analysis-gauge-fill-hp-low" : "uitk-analysis-gauge-fill-hp");

            float brkRatio = ev.breakMax > 0 ? (float)ev.breakCurrent / ev.breakMax : 0f;
            AddGauge(readoutCard, "BREAK SHIELD", $"{ev.breakCurrent} / {ev.breakMax}", brkRatio, "uitk-analysis-gauge-fill-break");

            var intentRow = new VisualElement();
            intentRow.AddToClassList("uitk-analysis-intent-row");
            var intentK = new UiLabel("INTENT:");
            intentK.AddToClassList("uitk-analysis-intent-lbl");
            var intentV = new UiLabel(string.IsNullOrEmpty(ev.telegraphedIntent) ? "—" : ev.telegraphedIntent);
            intentV.AddToClassList("uitk-analysis-intent-val");
            if (string.IsNullOrEmpty(ev.telegraphedIntent) || !ev.telegraphedIntent.Contains("CHARG"))
                intentV.AddToClassList("uitk-analysis-intent-calm");
            intentRow.Add(intentK);
            intentRow.Add(intentV);
            readoutCard.Add(intentRow);
            colLeft.Add(readoutCard);

            // Active Effects Card
            var activeCard = CreateCard("ACTIVE EFFECTS", "uitk-analysis-card-grow");
            if (ev.activeEffects.Count == 0)
            {
                var empty = new UiLabel("NONE");
                empty.AddToClassList("uitk-analysis-empty");
                activeCard.Add(empty);
            }
            else
            {
                var scroll = FigmaUi.CreateVerticalScrollView();
                var wrap = new VisualElement();
                wrap.AddToClassList("uitk-analysis-badge-wrap");
                foreach (var eff in ev.activeEffects)
                {
                    var entry = BuildActiveEffectEntry(eff, resistLookup, isAlly: false);
                    var badge = CreateEffectBadge(eff, entry);
                    wrap.Add(badge);
                }
                scroll.Add(wrap);
                activeCard.Add(scroll);
            }
            colLeft.Add(activeCard);
            body.Add(colLeft);

            // Column 2: Elemental Affinities
            var colElem = new VisualElement();
            colElem.AddToClassList("uitk-analysis-col-elem");
            var elemCard = CreateCard("ELEMENTAL AFFINITIES", "uitk-analysis-card-grow");
            foreach (var el in ev.elements)
                elemCard.Add(CreateEnemyAffinityRow(el));

            var elemNote = new UiLabel("Revealed on first hit (DoT ticks count). WEAK ×1.5 · RESIST ×0.5 · NULL ×0 · DRAIN heals foe · REFLECT bounces ≤35%.");
            elemNote.AddToClassList("uitk-analysis-note");
            elemCard.Add(elemNote);
            colElem.Add(elemCard);
            body.Add(colElem);

            // Column 3: Status Resistance
            var colResist = new VisualElement();
            colResist.AddToClassList("uitk-analysis-col-resist");
            var resistCard = CreateCard("STATUS RESISTANCE", "uitk-analysis-card-grow");
            var resistScroll = FigmaUi.CreateVerticalScrollView();

            int firstResistIdx = inspectEntries.Count;
            int activeResistMatchIdx = -1;
            foreach (var sr in ev.statusResistances)
            {
                bool isActive = activeLookup.TryGetValue(sr.status, out int remTurns);
                var entry = BuildStatusResistEntry(sr, isActive, remTurns, isAlly: false);
                var row = CreateStatusResistRow(sr, entry, isAlly: false);
                resistScroll.Add(row);
                if (isActive && activeResistMatchIdx < 0)
                    activeResistMatchIdx = inspectEntries.Count - 1;
            }
            resistCard.Add(resistScroll);
            colResist.Add(resistCard);
            body.Add(colResist);

            // Column 4: Status Explanation
            var colRight = new VisualElement();
            colRight.AddToClassList("uitk-analysis-col-right");
            var explainCard = CreateCard("STATUS EXPLANATION", "uitk-analysis-card-grow", "uitk-analysis-card-explain");
            explainContainer = new VisualElement { style = { flexDirection = FlexDirection.Column, flexGrow = 1 } };
            explainCard.Add(explainContainer);
            colRight.Add(explainCard);
            body.Add(colRight);

            // Default focus: active resisted status (matches 22d) -> else first active effect -> else first resist row
            if (activeResistMatchIdx >= 0) return activeResistMatchIdx;
            if (firstResistIdx > 0) return 0;
            return firstResistIdx;
        }

        // ---- ALLIES TAB (22e) ----
        int BuildAllyBody(VisualElement body, Combatant unit, AllyAnalysisView av)
        {
            var resistLookup = new Dictionary<StatusId, AnalysisStatusRow>();
            foreach (var r in av.statusResistances) resistLookup[r.status] = r;

            var activeLookup = new Dictionary<StatusId, int>();
            foreach (var a in av.activeStatuses)
            {
                if (a.statusId.HasValue && !activeLookup.ContainsKey(a.statusId.Value))
                    activeLookup[a.statusId.Value] = a.remainingTurns;
            }

            // Column 1: Resources + Attributes + Derived Stats
            var colLeft = new VisualElement();
            colLeft.AddToClassList("uitk-analysis-col-left");

            // Resources Card
            var resCard = CreateCard("RESOURCES", "uitk-analysis-card-mb-sm");
            var resRow = new VisualElement();
            resRow.AddToClassList("uitk-analysis-identity-row");

            var spriteEl = new VisualElement();
            spriteEl.AddToClassList("uitk-analysis-sprite-ally");
            ApplyUnitSprite(spriteEl, unit);
            resRow.Add(spriteEl);

            var barsCol = new VisualElement();
            barsCol.AddToClassList("uitk-analysis-id-info");
            float hpRatio = av.maxHp > 0 ? (float)av.hp / av.maxHp : 0f;
            AddGauge(barsCol, "HP", $"{av.hp} / {av.maxHp}", hpRatio, hpRatio <= 0.3f ? "uitk-analysis-gauge-fill-hp-low" : "uitk-analysis-gauge-fill-hp", compact: true);

            float mpRatio = av.maxMp > 0 ? (float)av.mp / av.maxMp : 0f;
            AddGauge(barsCol, "MP", $"{av.mp} / {av.maxMp}", mpRatio, "uitk-analysis-gauge-fill-mp", compact: true);

            float poiseRatio = av.poiseMax > 0 ? (float)av.poiseCurrent / av.poiseMax : 0f;
            AddGauge(barsCol, "POISE", $"{av.poiseCurrent} / {av.poiseMax}", poiseRatio, "uitk-analysis-gauge-fill-break", compact: true);
            resRow.Add(barsCol);
            resCard.Add(resRow);

            var shieldRow = new VisualElement();
            shieldRow.AddToClassList("uitk-analysis-shield-row");
            if (av.shield > 0)
            {
                string shieldIconKey = av.shieldSources.Any(s => s.IndexOf("Ocean", StringComparison.OrdinalIgnoreCase) >= 0)
                    ? "Icon-Racial-oceans_embrace"
                    : "Icon-Status-DefenseUp";
                var shIcon = CreateIconBox(shieldIconKey, "uitk-analysis-shield-icon");
                shieldRow.Add(shIcon);
                var shLbl = new UiLabel($"SHIELD: {av.shield} ({string.Join(", ", av.shieldSources).ToUpperInvariant()})");
                shLbl.AddToClassList("uitk-analysis-shield-txt");
                shieldRow.Add(shLbl);
            }
            else
            {
                var shLbl = new UiLabel("SHIELD: 0");
                shLbl.AddToClassList("uitk-analysis-empty");
                shieldRow.Add(shLbl);
            }
            resCard.Add(shieldRow);
            colLeft.Add(resCard);

            // Attributes Card
            var attrCard = CreateCard("ATTRIBUTES", "uitk-analysis-card-mb-sm");
            var attrGrid = new VisualElement();
            attrGrid.AddToClassList("uitk-analysis-attr-grid");
            AddStatCell(attrGrid, "STR", av.str.ToString(), "uitk-analysis-attr-cell");
            AddStatCell(attrGrid, "DEX", av.dex.ToString(), "uitk-analysis-attr-cell");
            AddStatCell(attrGrid, "VIT", av.vit.ToString(), "uitk-analysis-attr-cell");
            AddStatCell(attrGrid, "INT", av.intel.ToString(), "uitk-analysis-attr-cell");
            AddStatCell(attrGrid, "WIS", av.wis.ToString(), "uitk-analysis-attr-cell");
            AddStatCell(attrGrid, "CHA", av.cha.ToString(), "uitk-analysis-attr-cell");
            attrCard.Add(attrGrid);
            colLeft.Add(attrCard);

            // Derived Stats Card
            var derivedCard = CreateCard("DERIVED STATS", "uitk-analysis-card-grow");
            var derivedGrid = new VisualElement();
            derivedGrid.AddToClassList("uitk-analysis-attr-grid");
            AddStatCell(derivedGrid, "ATK", av.atk.ToString(), "uitk-analysis-derived-cell");
            AddStatCell(derivedGrid, "MATK", av.matk.ToString(), "uitk-analysis-derived-cell");
            AddStatCell(derivedGrid, "DEF", av.def.ToString(), "uitk-analysis-derived-cell");
            AddStatCell(derivedGrid, "MDEF", av.mdef.ToString(), "uitk-analysis-derived-cell");
            AddStatCell(derivedGrid, "SPD", av.spd.ToString(), "uitk-analysis-derived-cell");
            AddStatCell(derivedGrid, "CRIT", $"{av.critChancePct:0.#}% ×{av.critDamagePct / 100f:0.#}", "uitk-analysis-derived-cell");
            AddStatCell(derivedGrid, "BLOCK", $"{av.blockPower}%", "uitk-analysis-derived-cell");
            AddStatCell(derivedGrid, "STAGGER", $"{av.staggerResistance}%", "uitk-analysis-derived-cell");
            AddStatCell(derivedGrid, "QTE", $"+{av.qteTimingBonusPct:0.#}%", "uitk-analysis-derived-cell");
            derivedCard.Add(derivedGrid);
            colLeft.Add(derivedCard);
            body.Add(colLeft);

            // Column 2: Elemental Affinities
            var colElem = new VisualElement();
            colElem.AddToClassList("uitk-analysis-col-elem");
            var elemCard = CreateCard("ELEMENTAL AFFINITIES", "uitk-analysis-card-grow");
            foreach (var el in av.elements)
                elemCard.Add(CreateAllyAffinityRow(el));

            var elemNote = new UiLabel("Allies show exact resistance %. Damage taken × (1 − RES).");
            elemNote.AddToClassList("uitk-analysis-note");
            elemCard.Add(elemNote);
            colElem.Add(elemCard);
            body.Add(colElem);

            // Column 3: Status Resistance
            var colResist = new VisualElement();
            colResist.AddToClassList("uitk-analysis-col-resist");
            var resistCard = CreateCard("STATUS RES", "uitk-analysis-card-grow");
            var resistScroll = FigmaUi.CreateVerticalScrollView();

            int firstResistIdx = inspectEntries.Count;
            int activeResistMatchIdx = -1;
            foreach (var sr in av.statusResistances)
            {
                bool isActive = activeLookup.TryGetValue(sr.status, out int remTurns);
                var entry = BuildStatusResistEntry(sr, isActive, remTurns, isAlly: true);
                var row = CreateStatusResistRow(sr, entry, isAlly: true);
                resistScroll.Add(row);
                if (isActive && activeResistMatchIdx < 0)
                    activeResistMatchIdx = inspectEntries.Count - 1;
            }
            resistCard.Add(resistScroll);

            var resNote = new UiLabel($"STATUS {av.statusResistPct:0}% · MENTAL {av.mentalResistPct:0}% + PER-STATUS · CHANCE = BASE × (1 − RES)");
            resNote.AddToClassList("uitk-analysis-note");
            resistCard.Add(resNote);
            colResist.Add(resistCard);
            body.Add(colResist);

            // Column 4: Active Statuses + Defensive Effects + Status Explanation
            var colRight = new VisualElement();
            colRight.AddToClassList("uitk-analysis-col-right");

            // Active Statuses Card
            var actCard = CreateCard("ACTIVE STATUSES", "uitk-analysis-card-mb-sm");
            int firstActiveIdx = inspectEntries.Count;
            if (av.activeStatuses.Count == 0)
            {
                var empty = new UiLabel("NONE");
                empty.AddToClassList("uitk-analysis-empty");
                actCard.Add(empty);
            }
            else
            {
                var wrap = new VisualElement();
                wrap.AddToClassList("uitk-analysis-badge-wrap");
                foreach (var eff in av.activeStatuses)
                {
                    var entry = BuildActiveEffectEntry(eff, resistLookup, isAlly: true);
                    wrap.Add(CreateEffectBadge(eff, entry));
                }
                actCard.Add(wrap);
            }
            colRight.Add(actCard);

            // Defensive Effects Card
            var defCard = CreateCard("DEFENSIVE EFFECTS", "uitk-analysis-card-mb-sm");
            if (av.defensiveEffects.Count == 0)
            {
                var empty = new UiLabel("NONE");
                empty.AddToClassList("uitk-analysis-empty");
                defCard.Add(empty);
            }
            else
            {
                var defScroll = FigmaUi.CreateVerticalScrollView();
                defScroll.style.maxHeight = 112;
                foreach (var def in av.defensiveEffects)
                    defScroll.Add(CreateDefensiveEffectRow(def));
                defCard.Add(defScroll);
            }
            colRight.Add(defCard);

            // Status Explanation Card
            var explainCard = CreateCard("STATUS EXPLANATION", "uitk-analysis-card-grow", "uitk-analysis-card-explain");
            explainContainer = new VisualElement { style = { flexDirection = FlexDirection.Column, flexGrow = 1 } };
            explainCard.Add(explainContainer);
            colRight.Add(explainCard);
            body.Add(colRight);

            if (activeResistMatchIdx >= 0) return activeResistMatchIdx;
            if (av.activeStatuses.Count > 0) return firstActiveIdx;
            return firstResistIdx;
        }

        // ---- Inspect Focus & Explanation Panel ----
        void StepInspectFocus(int delta)
        {
            if (inspectEntries.Count == 0) return;
            int next = focusedInspectIndex < 0
                ? 0
                : (focusedInspectIndex + delta + inspectEntries.Count) % inspectEntries.Count;
            SelectInspectIndex(next, sound: true);
        }

        void SelectInspectIndex(int idx, bool sound)
        {
            if (idx < 0 || idx >= inspectEntries.Count) return;
            if (idx == focusedInspectIndex && explainContainer != null && explainContainer.childCount > 0) return;

            if (focusedInspectIndex >= 0 && focusedInspectIndex < inspectEntries.Count)
                inspectEntries[focusedInspectIndex].element?.RemoveFromClassList("uitk-analysis-focused");

            focusedInspectIndex = idx;
            var entry = inspectEntries[idx];
            entry.element?.AddToClassList("uitk-analysis-focused");

            if (entry.element != null)
            {
                for (var p = entry.element.parent; p != null; p = p.parent)
                {
                    if (p is ScrollView sv && sv.contentViewport != null && sv.contentViewport.layout.height > 0f)
                    {
                        sv.ScrollTo(entry.element);
                        break;
                    }
                }
            }

            RenderExplanation(entry);
            if (sound) Sfx.Play(Sound.Move);
        }

        void RenderExplanation(InspectEntry entry)
        {
            if (explainContainer == null || entry == null) return;
            explainContainer.Clear();

            var head = new VisualElement();
            head.AddToClassList("uitk-analysis-explain-head");

            var iconBox = CreateIconBox(entry.iconTemplate, explainCompact ? "uitk-analysis-explain-icon-sm" : "uitk-analysis-explain-icon");
            head.Add(iconBox);

            var meta = new VisualElement();
            meta.AddToClassList("uitk-analysis-explain-meta");
            var nameLbl = new UiLabel(entry.title);
            nameLbl.AddToClassList("uitk-analysis-explain-name");
            if (explainCompact) nameLbl.AddToClassList("uitk-analysis-explain-name-sm");
            meta.Add(nameLbl);

            if (!explainCompact)
            {
                var tag = CreateTag(entry.tagTemplate, entry.categoryTag);
                if (tag != null) meta.Add(tag);
            }
            head.Add(meta);

            if (explainCompact)
            {
                var tag = CreateTag(entry.tagTemplate, entry.categoryTag);
                if (tag != null) head.Add(tag);
            }
            explainContainer.Add(head);

            var descLbl = new UiLabel(entry.description);
            descLbl.AddToClassList("uitk-analysis-explain-desc");
            explainContainer.Add(descLbl);

            var stateBox = new VisualElement();
            stateBox.AddToClassList("uitk-analysis-state-box");

            var actRow = new VisualElement();
            actRow.AddToClassList("uitk-analysis-state-row");
            var actK = new UiLabel("ACTIVE");
            actK.AddToClassList("uitk-analysis-state-k");
            var actV = new UiLabel(entry.activeText);
            actV.AddToClassList("uitk-analysis-state-v");
            if (entry.isActive) actV.AddToClassList("uitk-analysis-state-v-active");
            else actV.AddToClassList("uitk-analysis-state-v-muted");
            actRow.Add(actK);
            actRow.Add(actV);
            stateBox.Add(actRow);

            var resRow = new VisualElement();
            resRow.AddToClassList("uitk-analysis-state-row");
            resRow.AddToClassList("uitk-analysis-state-row-last");
            var resK = new UiLabel("RESIST");
            resK.AddToClassList("uitk-analysis-state-k");
            var resV = new UiLabel(entry.resistText);
            resV.AddToClassList("uitk-analysis-state-v");
            if (entry.resistMuted) resV.AddToClassList("uitk-analysis-state-v-muted");
            resRow.Add(resK);
            resRow.Add(resV);
            stateBox.Add(resRow);

            explainContainer.Add(stateBox);
        }

        // ---- Element / Row Builders ----
        VisualElement CreateEnemyAffinityRow(AnalysisElementRow row)
        {
            var el = new VisualElement();
            el.AddToClassList("uitk-analysis-elem-row");

            el.Add(CreateIconBox($"Icon-Element-{row.element}", "uitk-analysis-elem-icon"));

            var shortLbl = new UiLabel(row.elementShort);
            shortLbl.AddToClassList("uitk-analysis-elem-short");
            el.Add(shortLbl);

            var nameLbl = new UiLabel(row.element.ToString().ToUpperInvariant());
            nameLbl.AddToClassList("uitk-analysis-elem-name");
            el.Add(nameLbl);

            string detail;
            string pillClass;
            if (!row.known.HasValue)
            {
                detail = row.label == "?!" ? "1 MORE HIT" : "";
                pillClass = row.label == "?!" ? "uitk-pill-warn" : "uitk-pill-muted";
            }
            else
            {
                switch (row.known.Value)
                {
                    case Effectiveness.Weak: detail = "×1.5 DMG"; pillClass = "uitk-pill-ok"; break;
                    case Effectiveness.Resist: detail = "×0.5 DMG"; pillClass = "uitk-pill-warn"; break;
                    case Effectiveness.Immune: detail = "×0 DMG"; pillClass = "uitk-pill-null"; break;
                    case Effectiveness.Drain: detail = "HEALS FOE"; pillClass = "uitk-pill-drain"; break;
                    case Effectiveness.Reflect: detail = "BOUNCES"; pillClass = "uitk-pill-err"; break;
                    default: detail = "×1.0"; pillClass = "uitk-pill-normal"; break;
                }
            }

            var detailLbl = new UiLabel(detail);
            detailLbl.AddToClassList("uitk-analysis-elem-detail");
            el.Add(detailLbl);

            el.Add(CreatePill(row.label, pillClass));
            return el;
        }

        VisualElement CreateAllyAffinityRow(AnalysisElementRow row)
        {
            var el = new VisualElement();
            el.AddToClassList("uitk-analysis-elem-row");

            el.Add(CreateIconBox($"Icon-Element-{row.element}", "uitk-analysis-elem-icon"));

            var shortLbl = new UiLabel(row.elementShort);
            shortLbl.AddToClassList("uitk-analysis-elem-short");
            el.Add(shortLbl);

            var nameLbl = new UiLabel(row.element.ToString().ToUpperInvariant());
            nameLbl.AddToClassList("uitk-analysis-elem-name");
            el.Add(nameLbl);

            var eff = row.known ?? Effectiveness.Neutral;
            string pctText = $"{row.resistancePct:0}%";
            string tag = EnemyCodex.FormatEffectivenessTag(eff);
            string pillClass;
            switch (eff)
            {
                case Effectiveness.Weak: pillClass = "uitk-pill-err"; break;
                case Effectiveness.Resist: pillClass = "uitk-pill-ok"; break;
                case Effectiveness.Immune: pillClass = "uitk-pill-null"; break;
                case Effectiveness.Drain: pillClass = "uitk-pill-drain"; break;
                case Effectiveness.Reflect: pillClass = "uitk-pill-warn"; break;
                default: pillClass = row.resistancePct >= 15f ? "uitk-pill-ok" : "uitk-pill-normal"; break;
            }

            var detailLbl = new UiLabel(pctText);
            detailLbl.AddToClassList("uitk-analysis-elem-detail");
            el.Add(detailLbl);

            el.Add(CreatePill(tag, pillClass));
            return el;
        }

        VisualElement CreateStatusResistRow(AnalysisStatusRow sr, InspectEntry entry, bool isAlly)
        {
            var row = new VisualElement { pickingMode = PickingMode.Position };
            row.AddToClassList("uitk-analysis-resist-row");

            var cursor = new UiLabel("▸");
            cursor.AddToClassList("uitk-analysis-resist-cursor");
            row.Add(cursor);

            row.Add(CreateIconBox($"Icon-Status-{sr.status}", "uitk-analysis-resist-icon"));

            string displayName = sr.status == StatusId.MindControl ? "MIND CTRL" : sr.statusName.ToUpperInvariant();
            var nameLbl = new UiLabel(displayName);
            nameLbl.AddToClassList("uitk-analysis-resist-name");
            row.Add(nameLbl);

            string pillClass;
            if (isAlly)
            {
                if (sr.immune) pillClass = "uitk-pill-null";
                else if (sr.exactResistancePct <= 0.05f) pillClass = "uitk-pill-err";
                else if (sr.exactResistancePct >= 25f) pillClass = "uitk-pill-ok";
                else pillClass = "uitk-pill-normal";
            }
            else if (!sr.knownTier.HasValue)
            {
                pillClass = "uitk-pill-muted";
            }
            else
            {
                switch (sr.knownTier.Value)
                {
                    case StatusResistTier.Vulnerable: pillClass = "uitk-pill-ok"; break;
                    case StatusResistTier.Resistant: pillClass = "uitk-pill-warn"; break;
                    case StatusResistTier.Immune: pillClass = "uitk-pill-err"; break;
                    default: pillClass = "uitk-pill-normal"; break;
                }
            }

            row.Add(CreatePill(sr.label, pillClass));

            entry.element = row;
            int myIdx = inspectEntries.Count;
            inspectEntries.Add(entry);

            row.RegisterCallback<ClickEvent>(_ => SelectInspectIndex(myIdx, sound: true));
            row.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (Controls.MouseActive) SelectInspectIndex(myIdx, sound: false);
            });
            return row;
        }

        VisualElement CreateEffectBadge(AnalysisActiveEffectRow eff, InspectEntry entry)
        {
            var badge = new VisualElement { pickingMode = PickingMode.Position };
            badge.AddToClassList("uitk-analysis-effect-badge");

            if (!string.IsNullOrEmpty(entry.iconTemplate))
                badge.Add(CreateIconBox(entry.iconTemplate, "uitk-analysis-badge-icon"));

            var nameLbl = new UiLabel(ShortBadgeName(eff));
            nameLbl.AddToClassList("uitk-analysis-badge-name");
            badge.Add(nameLbl);

            if (eff.remainingTurns > 0)
            {
                var turnsLbl = new UiLabel($"{eff.remainingTurns}T");
                turnsLbl.AddToClassList("uitk-analysis-badge-turns");
                badge.Add(turnsLbl);
            }

            entry.element = badge;
            int myIdx = inspectEntries.Count;
            inspectEntries.Add(entry);

            badge.RegisterCallback<ClickEvent>(_ => SelectInspectIndex(myIdx, sound: true));
            badge.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (Controls.MouseActive) SelectInspectIndex(myIdx, sound: false);
            });
            return badge;
        }

        VisualElement CreateDefensiveEffectRow(AnalysisActiveEffectRow def)
        {
            var row = new VisualElement();
            row.AddToClassList("uitk-analysis-def-row");

            string iconKey = !string.IsNullOrEmpty(def.iconKey)
                ? def.iconKey
                : def.statusId.HasValue ? $"Icon-Status-{def.statusId.Value}" : "Icon-Status-Guardian";
            row.Add(CreateIconBox(iconKey, "uitk-analysis-def-icon"));

            var info = new VisualElement();
            info.AddToClassList("uitk-analysis-def-info");

            string title = def.label;
            int paren = title.IndexOf('(');
            if (paren > 0 && !string.IsNullOrEmpty(def.subtitle))
                title = title.Substring(0, paren).Trim();

            var nameLbl = new UiLabel(title);
            nameLbl.AddToClassList("uitk-analysis-def-name");
            info.Add(nameLbl);

            string sub = !string.IsNullOrEmpty(def.subtitle)
                ? def.subtitle
                : (def.remainingTurns > 0 ? $"{def.remainingTurns}T REMAINING" : "ACTIVE");
            var subLbl = new UiLabel(sub);
            subLbl.AddToClassList("uitk-analysis-def-sub");
            info.Add(subLbl);

            row.Add(info);
            return row;
        }

        InspectEntry BuildStatusResistEntry(AnalysisStatusRow sr, bool isActive, int remTurns, bool isAlly)
        {
            var (cat, tagTpl) = StatusCategory(sr.status);
            string activeText = isActive
                ? (remTurns > 0 ? $"YES · {remTurns}T LEFT" : "YES")
                : "NO";
            return new InspectEntry
            {
                title = sr.statusName.ToUpperInvariant(),
                categoryTag = cat,
                tagTemplate = tagTpl,
                description = StatusInfo.Description(sr.status),
                iconTemplate = $"Icon-Status-{sr.status}",
                activeText = activeText,
                isActive = isActive,
                resistText = sr.label,
                resistMuted = !isAlly && !sr.knownTier.HasValue,
            };
        }

        InspectEntry BuildActiveEffectEntry(AnalysisActiveEffectRow eff, Dictionary<StatusId, AnalysisStatusRow> resistLookup, bool isAlly)
        {
            string title = eff.statusId.HasValue ? StatusInfo.Name(eff.statusId.Value).ToUpperInvariant() : eff.label;
            string desc = !string.IsNullOrEmpty(eff.subtitle)
                ? eff.subtitle
                : eff.statusId.HasValue ? StatusInfo.Description(eff.statusId.Value) : "Active combat state.";
            string iconKey = !string.IsNullOrEmpty(eff.iconKey)
                ? eff.iconKey
                : eff.statusId.HasValue ? $"Icon-Status-{eff.statusId.Value}" : "Icon-Status-Broken";

            var (cat, tagTpl) = eff.statusId.HasValue
                ? StatusCategory(eff.statusId.Value)
                : ("SPECIAL", "Tag-Gold");

            string activeText = eff.remainingTurns > 0 ? $"YES · {eff.remainingTurns}T LEFT" : "YES";
            string resistText = "—  NOT A RESISTED STATUS";
            bool resistMuted = true;
            if (eff.statusId.HasValue && resistLookup.TryGetValue(eff.statusId.Value, out var sr))
            {
                resistText = sr.label;
                resistMuted = !isAlly && !sr.knownTier.HasValue;
            }

            return new InspectEntry
            {
                title = title,
                categoryTag = cat,
                tagTemplate = tagTpl,
                description = desc,
                iconTemplate = iconKey,
                activeText = activeText,
                isActive = true,
                resistText = resistText,
                resistMuted = resistMuted,
            };
        }

        static string ShortBadgeName(AnalysisActiveEffectRow eff)
        {
            if (eff.statusId.HasValue)
            {
                return eff.statusId.Value switch
                {
                    StatusId.DefenseDown => "DEF DOWN",
                    StatusId.DefenseUp => "DEF UP",
                    StatusId.MagicDefenseDown => "MDEF DOWN",
                    StatusId.MagicDefenseUp => "MDEF UP",
                    StatusId.AttackDown => "ATK DOWN",
                    StatusId.AttackUp => "ATK UP",
                    StatusId.ResistanceDown => "RES DOWN",
                    StatusId.MindControl => "MIND CTRL",
                    _ => eff.label,
                };
            }
            return eff.label;
        }

        static (string label, string template) StatusCategory(StatusId id)
        {
            if (StatusInfo.IsDoT(id)) return ("DOT", "Tag-Temp");
            if (id == StatusId.Broken) return ("SPECIAL", "Tag-Gold");
            if (StatusInfo.IsBuff(id)) return ("BUFF", "Tag-Gold");
            if (StatusInfo.IsMental(id)) return ("MENTAL", "Tag-Cond");
            if (StatusInfo.IsControl(id)) return ("CONTROL", "Tag-Cond");
            return ("DEBUFF", "Tag-Cond");
        }

        // ---- Shared UI Helpers ----
        static VisualElement CreateCard(string headerTitle, params string[] extraClasses)
        {
            var card = new VisualElement();
            card.AddToClassList("uitk-analysis-card");
            foreach (var cls in extraClasses)
                if (!string.IsNullOrEmpty(cls)) card.AddToClassList(cls);

            var hdr = FigmaUi.CreateSectionHeader(headerTitle, 260f);
            if (hdr != null)
            {
                hdr.style.width = StyleKeyword.Auto;
                hdr.AddToClassList("uitk-analysis-sec-hdr");
                card.Add(hdr);
            }
            return card;
        }

        static void AddGauge(VisualElement parent, string label, string valueText, float ratio, string fillClass, bool compact = false)
        {
            var head = new VisualElement();
            head.AddToClassList("uitk-analysis-gauge-head");
            var lbl = new UiLabel(label);
            lbl.AddToClassList("uitk-analysis-gauge-lbl");
            var val = new UiLabel(valueText);
            val.AddToClassList("uitk-analysis-gauge-val");
            head.Add(lbl);
            head.Add(val);
            parent.Add(head);

            var track = new VisualElement();
            track.AddToClassList("uitk-analysis-gauge-track");
            if (compact) track.AddToClassList("uitk-analysis-gauge-track-sm");
            var fill = new VisualElement();
            fill.AddToClassList(fillClass);
            fill.style.width = Length.Percent(Mathf.Clamp01(ratio) * 100f);
            track.Add(fill);
            parent.Add(track);
        }

        static void AddStatCell(VisualElement grid, string key, string value, string cellClass)
        {
            var cell = new VisualElement();
            cell.AddToClassList(cellClass);
            var k = new UiLabel(key);
            k.AddToClassList("uitk-analysis-stat-k");
            var v = new UiLabel(value);
            v.AddToClassList("uitk-analysis-stat-v");
            cell.Add(k);
            cell.Add(v);
            grid.Add(cell);
        }

        static VisualElement CreatePill(string text, string variantClass)
        {
            var pill = new VisualElement();
            pill.AddToClassList("uitk-analysis-pill");
            if (!string.IsNullOrEmpty(variantClass)) pill.AddToClassList(variantClass);
            var lbl = new UiLabel(text);
            lbl.AddToClassList("uitk-analysis-pill-lbl");
            pill.Add(lbl);
            return pill;
        }

        static VisualElement CreateTag(string templateKey, string text)
        {
            var tag = FigmaUi.InstantiateTemplate(templateKey);
            if (tag == null) return null;
            FigmaUi.SetInstanceText(tag, "text-0", text);
            return tag;
        }

        static VisualElement CreateIconBox(string templateKey, string boxClass)
        {
            var box = new VisualElement();
            box.AddToClassList(boxClass);
            if (!string.IsNullOrEmpty(templateKey))
            {
                var inst = FigmaUi.InstantiateTemplate(templateKey);
                if (inst != null)
                {
                    inst.style.width = Length.Percent(100);
                    inst.style.height = Length.Percent(100);
                    box.Add(inst);
                }
            }
            return box;
        }

        static void AddKeyHint(VisualElement parent, string key, string label)
        {
            var hint = FigmaUi.InstantiateTemplate("Key-Hint");
            if (hint == null) return;
            FigmaUi.SetInstanceText(hint, "text-0", key);
            FigmaUi.SetInstanceText(hint, "text-1", label);
            parent.Add(hint);
        }

        static void ApplyUnitSprite(VisualElement spriteEl, Combatant unit)
        {
            if (spriteEl == null || unit?.unit == null) return;
            Texture2D tex = null;
            if (unit.IsEnemy)
            {
                var frames = UnitArt.Enemy(UnitArt.EnemyArtId(unit.enemy?.id, unit.phase), unit.unit.sprite);
                if (frames != null && frames.Length > 0) tex = frames[0];
            }
            else
            {
                var frames = UnitArt.Idle(unit.unit.sprite);
                if (frames != null && frames.Length > 0) tex = frames[0];
            }
            if (tex == null)
                tex = FigmaUi.GetSpriteTexture(unit.unit.sprite, unit.IsEnemy ? unit.enemy?.id : unit.name);
            if (tex != null)
                FigmaUi.SetBackgroundContain(spriteEl, tex);
        }
    }
}
