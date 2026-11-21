using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // Party Management — migrated to Figma 05---Party.uxml (1280x720 UI Toolkit).
    public class PartyScreen : GameScreen
    {
        // Red HP gauge and "not ready" note share this rule.
        public static bool IsLowHp(Unit u) => (float)u.hp / Math.Max(1, u.stats.maxHp) < 0.25f;

        public static int SelectedIndex { get; private set; }

        List<Unit> party;
        static GameState BackTo => Game.I == null || Game.I.LastBase == GameState.MainMenu ? GameState.Hub : Game.I.LastBase;
        UitkMenu menu;
        VisualElement[] cardSlots;
        readonly VisualElement[] cardPanels = new VisualElement[4];
        UiLabel cursorArrow;

        protected override void Build()
        {
            party = Run?.Party
                ?? Game?.Roster?.GetActiveParty().Select(d => new Unit(d)).ToList()
                ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            SelectedIndex = Mathf.Clamp(SelectedIndex, 0, Mathf.Max(0, party.Count - 1));

            MountFigma("05---Party");
            Rebuild();
        }

        void Select(int i)
        {
            if (i == SelectedIndex || i < 0 || i >= party.Count) return;
            SelectedIndex = i;
            UpdateSelectionVisuals();
        }

        void Rebuild()
        {
            menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);
            menu.onBack = () => Game?.Back(BackTo);

            // Header
            string size = party.Count >= CharacterRoster.PartySize ? $"{party.Count} / 4 · PARTY FULL" : $"{party.Count} / 4";
            var sizeTag = UiRoot.Q<VisualElement>(className: "tag-keep");
            if (sizeTag != null) FigmaUi.SetInstanceText(sizeTag, "text-0", size);

            var hubTag = UiRoot.Q<VisualElement>(className: "currency-tag-hub");
            if (hubTag != null)
            {
                FigmaUi.SetInstanceText(hubTag, "text-0", InDungeon ? $"{Run?.Gold ?? 0:#,##0} SPOILS" : $"{Run?.HubGold ?? 0:#,##0} GOLD");
                FigmaUi.AnchorRight(hubTag, 24f);
            }

            var backButtons = UiRoot.Query<Button>(className: "button-slab").ToList();
            foreach (var btn in backButtons)
            {
                if (btn.text == "Back")
                {
                    var item = menu.Add(btn, "Back", () => Game?.Back(BackTo));
                    FigmaUi.ApplyDefaultFocusVisual(item, btn);
                }
            }

            cursorArrow = UiRoot.Q<UiLabel>(className: "node-vars-3");

            // 4 Member cards: in 05---Party.uxml, card elements sit at absolute x = 28 + i*310, y = 76, w = 292, h = 332.
            string[] cardPanelClasses = { "panel-gold__i-3", "panel-default__i-8", "panel-default__i-9", "panel-default__i-10" };
            string[] wellClasses = { "rectangle-vars-67", "rectangle-vars-68", "rectangle-vars-69", "rectangle-vars-70" };
            string[] numeralClasses = { "i-vars", "ii-vars", "iii-vars", "iv-vars" };
            string[] lvClasses = { "lv-1-vars-2", "lv-1-vars-3", "lv-1-vars-4", "lv-1-vars-5" };
            string[] spriteClasses = { "sprite-knight", "sprite-rogue", "sprite-mage", "sprite-healer" };
            string[] nameClasses = { "knight-vars", "rogue-vars", "mage-vars", "healer-vars" };
            string[] hpValClasses = { "_125-vars", "_85-vars", "_60-vars", "_80-vars" };
            string[] mpValClasses = { "_20-vars", "_25-vars", "_60-vars-2", "_55-vars" };

            var hpGauges = UiRoot.Query<VisualElement>(className: "gauge-hp").ToList();
            var mpGauges = UiRoot.Query<VisualElement>(className: "gauge-mp").ToList();
            var screenBox = UiRoot.Q<VisualElement>(className: "_05-party") ?? UiRoot;
            cardSlots = new VisualElement[4];

            string[] numerals = { "I", "II", "III", "IV" };
            for (int i = 0; i < 4; i++)
            {
                var panel = UiRoot.Q<VisualElement>(className: cardPanelClasses[i]);
                cardPanels[i] = panel;
                var well = UiRoot.Q<VisualElement>(className: wellClasses[i]);
                var numLbl = UiRoot.Q<UiLabel>(className: numeralClasses[i]);
                var lvLbl = UiRoot.Q<UiLabel>(className: lvClasses[i]);
                var fig = UiRoot.Q<VisualElement>(className: spriteClasses[i]);
                var nameLbl = UiRoot.Q<UiLabel>(className: nameClasses[i]);
                var hpVal = UiRoot.Q<UiLabel>(className: hpValClasses[i]);
                var mpVal = UiRoot.Q<UiLabel>(className: mpValClasses[i]);
                var hpGauge = i < hpGauges.Count ? hpGauges[i] : null;
                var mpGauge = i < mpGauges.Count ? mpGauges[i] : null;
                var hpCap = UiRoot.Q<UiLabel>(className: i == 0 ? "hp-vars" : $"hp-vars-{i + 1}");
                var mpCap = UiRoot.Q<UiLabel>(className: i == 0 ? "mp-vars" : $"mp-vars-{i + 1}");

                // Tag container sits right after nameLbl in 05---Party.uxml
                VisualElement tagRow = null;
                if (nameLbl != null && nameLbl.parent != null)
                {
                    int idxInParent = nameLbl.parent.IndexOf(nameLbl);
                    if (idxInParent + 1 < nameLbl.parent.childCount)
                        tagRow = nameLbl.parent[idxInParent + 1];
                }

                if (i >= party.Count)
                {
                    if (well != null) well.style.display = DisplayStyle.None;
                    if (numLbl != null) numLbl.text = numerals[i];
                    if (lvLbl != null) lvLbl.text = "";
                    if (fig != null) fig.style.display = DisplayStyle.None;
                    if (nameLbl != null) { nameLbl.text = "+ Create member"; nameLbl.style.color = Palette.Mist3; }
                    if (tagRow != null) tagRow.style.display = DisplayStyle.None;
                    if (hpCap != null) hpCap.style.display = DisplayStyle.None;
                    if (mpCap != null) mpCap.style.display = DisplayStyle.None;
                    if (hpGauge != null) hpGauge.style.display = DisplayStyle.None;
                    if (mpGauge != null) mpGauge.style.display = DisplayStyle.None;
                    if (hpVal != null) hpVal.style.display = DisplayStyle.None;
                    if (mpVal != null) mpVal.style.display = DisplayStyle.None;
                    continue;
                }

                int cardIdx = i;
                var u = party[i];
                if (numLbl != null) { numLbl.text = numerals[i]; numLbl.style.color = Palette.Mist3; }
                if (lvLbl != null) { lvLbl.text = $"LV {u.level}"; lvLbl.style.color = Palette.Gold1; }

                if (fig != null)
                {
                    fig.style.display = DisplayStyle.Flex;
                    if (u.sprite != null && u.sprite.Length > 0)
                        FigmaUi.SetBackgroundContain(fig, FigmaUi.GetSpriteTexture(u.sprite, "party_" + (u.classDef?.id ?? u.name)));
                    fig.style.opacity = u.Alive ? 1f : 0.35f;
                }

                if (nameLbl != null)
                {
                    nameLbl.text = u.name ?? "Hero";
                    nameLbl.style.color = u.Alive ? Palette.Mist1 : Palette.Mist3;
                    nameLbl.style.maxWidth = 260;
                    nameLbl.style.overflow = Overflow.Hidden;
                }

                if (tagRow != null)
                {
                    tagRow.Clear();
                    foreach (var tagText in new[] { u.race?.name ?? "Human", u.classDef?.name ?? u.role ?? "" }.Where(s => !string.IsNullOrEmpty(s)))
                    {
                        var tag = FigmaUi.InstantiateTemplate("Tag-Cls");
                        if (tag != null)
                        {
                            FigmaUi.SetInstanceText(tag, "text-0", tagText.ToUpperInvariant());
                            tag.style.marginRight = 6;
                            tagRow.Add(tag);
                        }
                    }
                }

                float hpRatio = u.Alive ? (float)u.hp / Mathf.Max(1, u.stats.maxHp) : 0f;
                float mpRatio = (float)u.mp / Mathf.Max(1, u.stats.maxMp);
                FigmaUi.SetGaugeFill(hpGauge, hpRatio, u.Alive && IsLowHp(u) ? Palette.HpLow : Palette.Hp);
                FigmaUi.SetGaugeFill(mpGauge, mpRatio, Palette.Mp);
                if (hpVal != null) { hpVal.text = u.hp.ToString(); hpVal.style.color = u.Alive ? Palette.Mist1 : Palette.HpLow; }
                if (mpVal != null) mpVal.text = u.mp.ToString();

                // Interactive card overlay button for keyboard/mouse selection + KO badge
                var cardBtn = new Button { name = $"PartyCard_{i}", text = "" };
                cardBtn.style.position = Position.Absolute;
                cardBtn.style.left = 28 + i * 310;
                cardBtn.style.top = 76;
                cardBtn.style.width = 292;
                cardBtn.style.height = 332;
                cardBtn.style.backgroundColor = Color.clear;
                cardBtn.style.borderTopWidth = 2;
                cardBtn.style.borderBottomWidth = 2;
                cardBtn.style.borderLeftWidth = 2;
                cardBtn.style.borderRightWidth = 2;
                cardSlots[i] = cardBtn;

                if (!u.Alive)
                {
                    var koBadge = new UiLabel("KO · INCAPACITATED");
                    koBadge.style.position = Position.Absolute;
                    koBadge.style.left = 48;
                    koBadge.style.right = 48;
                    koBadge.style.top = 180;
                    koBadge.style.paddingTop = 4;
                    koBadge.style.paddingBottom = 4;
                    koBadge.style.unityTextAlign = TextAnchor.MiddleCenter;
                    koBadge.style.fontSize = 12;
                    koBadge.style.color = Palette.Hex(0xFFD2CC);
                    koBadge.style.backgroundColor = Palette.Hex(0x3A1418);
                    koBadge.style.borderTopWidth = 1;
                    koBadge.style.borderBottomWidth = 1;
                    koBadge.style.borderLeftWidth = 1;
                    koBadge.style.borderRightWidth = 1;
                    koBadge.style.borderTopColor = Palette.Err;
                    koBadge.style.borderBottomColor = Palette.Err;
                    koBadge.style.borderLeftColor = Palette.Err;
                    koBadge.style.borderRightColor = Palette.Err;
                    cardBtn.Add(koBadge);
                }

                screenBox.Add(cardBtn);
                menu.Add(cardBtn, u.name ?? numerals[i], () =>
                {
                    SelectedIndex = cardIdx;
                    Game?.Go(GameState.CharacterStatus);
                }, visual: on =>
                {
                    var col = on ? Palette.Gold1 : cardIdx == SelectedIndex ? Palette.Gold2 : Color.clear;
                    cardBtn.style.borderTopColor = col;
                    cardBtn.style.borderBottomColor = col;
                    cardBtn.style.borderLeftColor = col;
                    cardBtn.style.borderRightColor = col;
                });
            }

            // Party Overview section
            BindOverview(screenBox);

            // Bottom action buttons
            var viewBtn = FigmaUi.FindButton(UiRoot, "View Status");
            if (viewBtn != null)
                FigmaUi.ApplyDefaultFocusVisual(menu.Add(viewBtn, "View Status", () => Game?.Go(GameState.CharacterStatus)), viewBtn);

            var confirmBtn = FigmaUi.FindButton(UiRoot, "Confirm Party");
            if (confirmBtn != null)
            {
                confirmBtn.text = "Done"; // nothing to confirm: party order is fixed and placement is automatic
                var item = menu.Add(confirmBtn, "Done", () => Game?.Back(BackTo));
                item.visual = on =>
                {
                    confirmBtn.style.borderTopColor = on ? Palette.Mist1 : Palette.Gold2;
                    confirmBtn.style.borderBottomColor = on ? Palette.Mist1 : Palette.Gold2;
                    confirmBtn.style.borderLeftColor = on ? Palette.Mist1 : Palette.Gold2;
                    confirmBtn.style.borderRightColor = on ? Palette.Mist1 : Palette.Gold2;
                };
                item.visual(false);
            }

            menu.onFocus = item =>
            {
                if (item != null && item.element is Button b && b.name != null && b.name.StartsWith("PartyCard_"))
                {
                    if (int.TryParse(b.name.Substring("PartyCard_".Length), out int idx))
                        SelectedIndex = idx;
                }
                UpdateSelectionVisuals();
            };
            menu.onPrevPage = () => CycleMember(-1);
            menu.onNextPage = () => CycleMember(1);

            if (party.Count > 0)
            {
                int focusTarget = menu.items.FindIndex(it => it.element == cardSlots[SelectedIndex]);
                menu.Focus(focusTarget >= 0 ? focusTarget : 0, false, true);
            }
            UpdateSelectionVisuals();
        }

        void CycleMember(int delta)
        {
            if (party == null || party.Count == 0) return;
            SelectedIndex = (SelectedIndex + delta + party.Count) % party.Count;
            int target = menu.items.FindIndex(it => it.element == cardSlots[SelectedIndex]);
            if (target >= 0) menu.Focus(target, false, true);
            UpdateSelectionVisuals();
        }

        void UpdateSelectionVisuals()
        {
            bool cardFocused = menu != null && menu.Current?.element is Button fb && fb.name != null && fb.name.StartsWith("PartyCard_");
            if (cursorArrow != null && party.Count > 0)
            {
                cursorArrow.style.display = cardFocused ? DisplayStyle.Flex : DisplayStyle.None;
                cursorArrow.style.left = 8 + SelectedIndex * 310;
                cursorArrow.style.top = 230;
                cursorArrow.style.color = Palette.Gold2;
            }
            for (int i = 0; i < cardPanels.Length; i++)
            {
                var p = cardPanels[i]?.childCount > 0 ? cardPanels[i][0] : cardPanels[i];
                if (p == null) continue;
                var border = i == SelectedIndex ? Palette.Gold2 : Palette.Ink4;
                p.style.borderTopColor = border; p.style.borderBottomColor = border;
                p.style.borderLeftColor = border; p.style.borderRightColor = border;
            }
            if (cardSlots != null)
            {
                for (int i = 0; i < cardSlots.Length; i++)
                {
                    if (cardSlots[i] == null) continue;
                    bool focused = menu != null && menu.Current?.element == cardSlots[i];
                    var col = focused ? Palette.Gold1 : i == SelectedIndex ? Palette.Gold2 : Color.clear;
                    cardSlots[i].style.borderTopColor = col;
                    cardSlots[i].style.borderBottomColor = col;
                    cardSlots[i].style.borderLeftColor = col;
                    cardSlots[i].style.borderRightColor = col;
                }
            }
        }

        void BindOverview(VisualElement screenBox)
        {
            var rolesHdr = UiRoot.Q<UiLabel>(className: "roles-covered-vars");
            if (rolesHdr != null && rolesHdr.parent != null)
            {
                int idx = rolesHdr.parent.IndexOf(rolesHdr);
                if (idx + 1 < rolesHdr.parent.childCount)
                {
                    var row = rolesHdr.parent[idx + 1];
                    row.Clear();
                    row.style.flexWrap = Wrap.Wrap;
                    row.style.maxWidth = 390;
                    foreach (var role in party.Select(u => u.classDef?.role).Where(r => !string.IsNullOrEmpty(r)).Distinct())
                    {
                        var tag = FigmaUi.InstantiateTemplate("Tag-Cls");
                        if (tag != null)
                        {
                            FigmaUi.SetInstanceText(tag, "text-0", role.ToUpperInvariant());
                            tag.style.marginRight = 6;
                            tag.style.marginBottom = 4;
                            row.Add(tag);
                        }
                    }
                }
            }

            var dmgHdr = UiRoot.Q<UiLabel>(className: "damage-types-available-vars");
            if (dmgHdr != null && dmgHdr.parent != null)
            {
                int idx = dmgHdr.parent.IndexOf(dmgHdr);
                if (idx + 1 < dmgHdr.parent.childCount)
                {
                    var row = dmgHdr.parent[idx + 1];
                    row.Clear();
                    row.style.flexWrap = Wrap.Wrap;
                    row.style.maxWidth = 390;
                    var elements = party.SelectMany(u => u.skills ?? new string[0])
                        .Where(id => Database.Skills.ContainsKey(id)).Select(id => Database.Skills[id].element)
                        .Prepend(Element.Physical).Distinct().ToList();
                    foreach (var el in elements)
                    {
                        string code = Palette.ElementCode(el);
                        string elName = Palette.ElementName(el).ToUpperInvariant();
                        var entry = new VisualElement
                        {
                            style =
                            {
                                flexDirection = FlexDirection.Row,
                                alignItems = UnityEngine.UIElements.Align.Center,
                                marginRight = 10,
                                marginBottom = 4
                            }
                        };
                        var chip = FigmaUi.InstantiateTemplate($"Element-Chip-{code}");
                        if (chip != null)
                        {
                            FigmaUi.SetInstanceText(chip, "text-0", code);
                            chip.style.marginRight = 4;
                            entry.Add(chip);
                        }
                        entry.Add(new UiLabel(elName)
                        {
                            style =
                            {
                                fontSize = 11,
                                color = Palette.Mist2,
                                whiteSpace = WhiteSpace.NoWrap
                            }
                        });
                        row.Add(entry);
                    }
                }
            }

            var noteLbl = UiRoot.Q<UiLabel>(className: "all-members-ready-vars");
            if (noteLbl != null)
            {
                var ko = party.Where(u => !u.Alive).Select(u => u.name).ToList();
                var low = party.Where(u => u.Alive && IsLowHp(u)).Select(u => u.name).ToList(); // same rule as the red gauge
                string Are(List<string> n) => n.Count == 1 ? "IS" : "ARE";
                string note = ko.Count > 0 ? $"{string.Join(", ", ko)} {Are(ko)} KO — REVIVE AT THE TEMPLE BEFORE ENTERING A DUNGEON."
                    : low.Count > 0 ? $"{string.Join(", ", low)} {Are(low)} LOW ON HP — RECOVER AT THE TEMPLE."
                    : "ALL MEMBERS READY.";
                noteLbl.text = note.ToUpperInvariant();
                noteLbl.style.whiteSpace = WhiteSpace.Normal;
                noteLbl.style.maxWidth = 350;
                noteLbl.style.color = ko.Count > 0 ? Palette.Err : low.Count > 0 ? Palette.Ember1 : Palette.Ok;

                // Placement is automatic and cosmetic (CombatView.StagePositions); say so, so I-IV doesn't read as rows.
                var rowsLbl = new UiLabel("IN BATTLE: MELEE STAND IN FRONT, RANGED IN BACK, ROGUE FILLS IN. ROWS DON'T CHANGE WHO GETS HIT.")
                {
                    style = { fontSize = 11, color = Palette.Mist2, whiteSpace = WhiteSpace.Normal, maxWidth = 350, marginTop = 6 }
                };
                noteLbl.parent?.Insert(noteLbl.parent.IndexOf(noteLbl) + 1, rowsLbl);
            }
        }
    }
}
