using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UiAlign = UnityEngine.UIElements.Align;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // Presents RunState.PendingReward — migrated to Figma 21b---Combat-Victory---Confirm-Reward--v2.uxml (1280x720 UI Toolkit).
    public class RewardScreen : GameScreen
    {
        int selectedChoiceIdx = -1;
        int selectedHeroIdx = -1;
        int selectedModifierIdx = -1;
        ElementType selectedAttunementElement = ElementType.Fire;

        RewardOffer currentOffer;
        List<Unit> currentParty;
        bool isBossReward;
        bool isStageBossReward;
        UitkMenu menu;
        VisualElement cardsHost;
        VisualElement confirmHost;

        protected override void Build()
        {
            selectedChoiceIdx = -1;
            selectedHeroIdx = -1;
            selectedModifierIdx = -1;
            selectedAttunementElement = ElementType.Fire;

            currentOffer = Run?.PendingReward ?? new RewardOffer { title = "VICTORY", text = "SPOILS OF WAR", choices = new List<RewardOption>() };
            // Taking a modifier is mandatory: start with the first one picked (the player can switch, not drop it).
            if (currentOffer.modifierChoices != null && currentOffer.modifierChoices.Count > 0)
            {
                selectedModifierIdx = 0;
                var first = currentOffer.modifierChoices[0];
                if (first.id == "MOD-024" && first.element.IsValidElement()) selectedAttunementElement = first.element;
            }
            currentParty = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            isBossReward = Run != null && Run.IsAtFinalDungeonBoss;
            isStageBossReward = Run != null && Run.IsAtStageBoss;

            MountFigma("21b---Combat-Victory---Confirm-Reward--v2");
            var screen = UiRoot.Q<VisualElement>(className: "_21b-combat-victory-confirm-reward-v2") ?? UiRoot;

            // Header title + top gold tags
            var titleLbl = UiRoot.Q<UiLabel>(className: "victory-vars-2");
            if (titleLbl != null) titleLbl.text = (currentOffer.title ?? "VICTORY").ToUpperInvariant();

            var topTags = UiRoot.Query<VisualElement>(className: "tag-gold").ToList();
            int standing = currentParty.Count(u => u.Alive);
            if (topTags.Count >= 1)
            {
                string firstTag = isBossReward
                    ? "BOSS CLEARED"
                    : isStageBossReward
                        ? $"STAGE {Run.Stage}/{Run.TotalStages} CLEARED"
                        : (Run != null && Run.TotalStages > 1 ? $"ACT {Run.Act} · S{Run.Stage}/{Run.TotalStages}" : $"ACT {Run?.Act ?? 1}");
                FigmaUi.SetInstanceText(topTags[0], "text-0", firstTag);
                topTags[0].style.marginRight = 8;
            }
            if (topTags.Count >= 2)
            {
                FigmaUi.SetInstanceText(topTags[1], "text-0", standing == currentParty.Count ? "NO HERO FELL" : $"{standing}/{currentParty.Count} STANDING");
                topTags[1].style.marginRight = 8;
            }
            if (topTags.Count >= 3)
            {
                string thirdTag = currentOffer.bonusGoldFromModifier > 0
                    ? $"+{currentOffer.bonusGoldFromModifier} FORTUNE"
                    : $"RELICS {Run?.Relics.Count ?? 0}/{RelicManager.MaxRelicCapacity}";
                FigmaUi.SetInstanceText(topTags[2], "text-0", thirdTag);
            }
            // Hide the baked 4th Tag-Gold ("★ LEVEL UP · LV 2") inside row 2 of the left panel
            if (topTags.Count >= 4)
                topTags[3].style.display = DisplayStyle.None;

            // Left Battle Spoils panel
            var goldDeltaLbl = UiRoot.Q<UiLabel>(className: "_24-run-gold-vars-2");
            if (goldDeltaLbl != null)
            {
                string extra = currentOffer.bonusGoldFromModifier > 0 ? $" (+{currentOffer.bonusGoldFromModifier})" : "";
                goldDeltaLbl.text = $"◆ +{currentOffer.gold} SPOILS{extra}";
            }

            var goldTotalLbl = UiRoot.Q<UiLabel>(className: "_120-144-vars");
            if (goldTotalLbl != null)
            {
                goldTotalLbl.text = Run != null ? $"{Run.Gold:#,##0} › {Run.Gold + currentOffer.gold:#,##0}" : $"+{currentOffer.gold:#,##0}";
                goldTotalLbl.style.left = StyleKeyword.Auto;
                goldTotalLbl.style.right = 10;
                goldTotalLbl.style.unityTextAlign = TextAnchor.MiddleRight;
            }

            string[] spriteClasses = { "sprite-knight", "sprite-rogue", "sprite-mage", "sprite-healer" };
            string[] frameClasses = { "frame-vars-54", "frame-vars-55", "frame-vars-56", "frame-vars-57" };
            string[] nameClasses = { "knight-vars-9", "rogue-vars-8", "mage-vars-8", "healer-vars-9" };
            string[] deltaClasses = { "_32-exp-vars-5", "_32-exp-vars-6", "_32-exp-vars-7", "_32-exp-vars-8" };
            string[] barTextClasses = { "_62-100-exp-vars-2", null, "_55-100-exp-vars-2", "_70-100-exp-vars-2" };
            var xpGauges = UiRoot.Query<VisualElement>(className: "gauge-xp").ToList();

            // Add row 1 sub-label where the baked LEVEL UP tag used to sit
            var row1SubLbl = new UiLabel("")
            {
                style =
                {
                    position = Position.Absolute, left = 104, top = 360,
                    fontSize = 12, letterSpacing = 0.48f, color = Palette.Mist2,
                    whiteSpace = WhiteSpace.NoWrap
                }
            };
            screen.Add(row1SubLbl);

            var leveled = new List<Unit>();
            for (int i = 0; i < 4; i++)
            {
                var frame = UiRoot.Q<VisualElement>(className: frameClasses[i]);
                var fig = frame != null ? frame.Q<VisualElement>(className: spriteClasses[i]) : UiRoot.Q<VisualElement>(className: spriteClasses[i]);
                var nLbl = UiRoot.Q<UiLabel>(className: nameClasses[i]);
                var dLbl = UiRoot.Q<UiLabel>(className: deltaClasses[i]);
                var bLbl = i == 1 ? row1SubLbl : UiRoot.Q<UiLabel>(className: barTextClasses[i]);
                var g = i < xpGauges.Count ? xpGauges[i] : null;

                if (i >= currentParty.Count)
                {
                    if (frame != null) frame.style.display = DisplayStyle.None;
                    if (nLbl != null) nLbl.style.display = DisplayStyle.None;
                    if (dLbl != null) dLbl.style.display = DisplayStyle.None;
                    if (bLbl != null) bLbl.style.display = DisplayStyle.None;
                    if (g != null) g.style.display = DisplayStyle.None;
                    continue;
                }

                var u = currentParty[i];
                if (fig != null && u.sprite != null && u.sprite.Length > 0)
                    FigmaUi.SetBackgroundContain(fig, FigmaUi.GetSpriteTexture(u.sprite, "rw_" + (u.classDef?.id ?? u.name)));
                // EXP was already applied in RunState.WinCombat; the offer says how much and whether it levelled.
                int gained = i < currentOffer.expGained.Length ? currentOffer.expGained[i] : 0;
                int levels = i < currentOffer.levelsGained.Length ? currentOffer.levelsGained[i] : 0;
                // KO'd heroes earn nothing (they may already have risen at 1 HP). No EXP data = preview offer, not a KO.
                bool fell = i < currentOffer.expGained.Length && gained == 0 && !u.IsMaxLevel;
                if (levels > 0)
                {
                    leveled.Add(u);
                    // Gold arrows climb the hero once the screen has laid out, one hero after another.
                    if (fig != null)
                        fig.schedule.Execute(() =>
                        {
                            var r = fig.ChangeCoordinatesTo(screen, fig.contentRect);
                            Fx.Spawn(screen, "buff", r.center.x, r.yMax, 2f, loops: 2);
                            Sfx.Play(Sound.SkillUnlock);
                        }).StartingIn(300 + leveled.Count * 200);
                }

                if (frame != null)
                {
                    var border = levels > 0 ? Palette.Gold2 : Palette.Ink5;
                    frame.style.borderTopColor = border; frame.style.borderBottomColor = border;
                    frame.style.borderLeftColor = border; frame.style.borderRightColor = border;
                }
                if (nLbl != null) { nLbl.text = u.name; nLbl.style.color = fell ? Palette.Mist3 : Palette.Mist1; }
                if (dLbl != null)
                {
                    dLbl.text = fell ? "KO · +0 EXP" : u.IsMaxLevel && gained == 0 ? "MAX LEVEL" : $"+{gained} EXP";
                    dLbl.style.color = fell ? Palette.HpLow : Palette.Xp;
                    dLbl.style.left = 264;
                    dLbl.style.width = 114;
                    dLbl.style.unityTextAlign = TextAnchor.MiddleRight;
                }
                FigmaUi.SetGaugeFill(g, u.IsMaxLevel ? 1f : (float)u.exp / Mathf.Max(1, u.ExpToNext), Palette.Xp);
                if (bLbl != null)
                {
                    bLbl.text = levels > 0 ? $"★ LEVEL UP · LV {u.level}"
                        : u.IsMaxLevel ? $"LV {u.level} · MAX"
                        : $"LV {u.level} · {u.exp} / {u.ExpToNext} EXP";
                    bLbl.style.color = levels > 0 ? Palette.Gold1 : Palette.Mist2;
                }
            }

            var noteTop = UiRoot.Q<UiLabel>(className: "_1-sp-for-the-rogue-level-up-vars");
            if (noteTop != null)
            {
                if (isBossReward)
                {
                    noteTop.style.display = DisplayStyle.None;
                }
                else if (isStageBossReward)
                {
                    noteTop.text = $"NEXT: STAGE {Run.Stage + 1} OF {Run.TotalStages}";
                    noteTop.style.color = Palette.Gold2;
                }
                else if (!string.IsNullOrEmpty(currentOffer.bonusItemId) && Database.Items.TryGetValue(currentOffer.bonusItemId, out var bi))
                {
                    noteTop.text = $"BONUS LOOT: +{bi.name.ToUpperInvariant()}";
                    noteTop.style.color = Palette.Ok;
                }
                else
                {
                    noteTop.style.display = DisplayStyle.None; // flavour line ("The dead lie still once more.")
                }
                noteTop.style.width = 340;
                noteTop.style.whiteSpace = WhiteSpace.Normal;
            }

            var noteBottom = UiRoot.Q<UiLabel>(className: "spend-sp-at-a-rest-site-or-in-the-hub-vars");
            if (noteBottom != null)
            {
                if (leveled.Count == 0) noteBottom.style.display = DisplayStyle.None;
                else
                {
                    string who = string.Join(", ", leveled.Select(u => $"{u.name} LV {u.level}")).ToUpperInvariant();
                    string talent = leveled.Any(u => u.AvailableTalentSelections > 0) ? " · NEW TALENT" : "";
                    noteBottom.text = $"LEVEL UP: {who} · +SP{talent}. SPEND AT A REST SITE OR IN THE HUB.";
                    noteBottom.style.color = Palette.Gold2;
                    noteBottom.style.width = 340;
                    noteBottom.style.whiteSpace = WhiteSpace.Normal;
                }
            }

            if (noteTop != null && noteBottom != null)
            {
                var notesCol = new VisualElement
                {
                    style = { position = Position.Absolute, left = 40, top = 594, width = 340, height = 84, flexDirection = FlexDirection.Column, overflow = Overflow.Hidden }
                };
                screen.Add(notesCol);
                noteTop.RemoveFromHierarchy();
                noteBottom.RemoveFromHierarchy();
                noteTop.style.position = Position.Relative; noteTop.style.left = StyleKeyword.Auto; noteTop.style.top = StyleKeyword.Auto;
                noteTop.style.width = StyleKeyword.Auto; noteTop.style.marginBottom = 4;
                noteBottom.style.position = Position.Relative; noteBottom.style.left = StyleKeyword.Auto; noteBottom.style.top = StyleKeyword.Auto;
                noteBottom.style.width = StyleKeyword.Auto;
                notesCol.Add(noteTop);
                notesCol.Add(noteBottom);
            }
            var rewardHdr = UiRoot.Q<VisualElement>(className: "section-header__i-32");
            if (rewardHdr != null)
                FigmaUi.SetInstanceText(rewardHdr, "text-0", currentOffer.choices.Count > 0 ? "Choose one reward" : "No prize this time");

            var subHdr = UiRoot.Q<UiLabel>(className: "pick-1-of-3-your-pick-is-final-kept-items-survive-a-wipe-vars");
            if (subHdr != null)
            {
                // With no prize, the header and the confirm panel already say so.
                if (currentOffer.choices.Count > 0) subHdr.text = $"PICK 1 OF {currentOffer.choices.Count}";
                else subHdr.style.display = DisplayStyle.None;
            }

            // Remove baked static cards, baked overlay icons, and baked confirm panel children (including baked <Button>s)
            StripBakedRewardElements(screen);
            // Position-matched leftovers can only be found once layout exists (see FigmaUi.OnFirstLayout).
            FigmaUi.OnFirstLayout(screen, () => StripBakedRewardElements(screen));

            // Dynamic hosts matching exact 21b coordinates
            cardsHost = new VisualElement
            {
                name = "RewardCardsHost",
                style =
                {
                    position = Position.Absolute, left = 412, top = 132, width = 844, height = 380,
                    flexDirection = FlexDirection.Row, alignItems = UiAlign.FlexStart
                }
            };
            screen.Add(cardsHost);

            confirmHost = new VisualElement
            {
                name = "RewardConfirmHost",
                style =
                {
                    position = Position.Absolute, left = 428, top = 524, width = 812, height = 160,
                    paddingLeft = 10, paddingRight = 10, paddingTop = 6, paddingBottom = 12,
                    flexDirection = FlexDirection.Column, justifyContent = Justify.SpaceBetween,
                    overflow = Overflow.Hidden
                }
            };
            screen.Add(confirmHost);

            menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);
            menu.onBack = () =>
            {
                if (selectedChoiceIdx >= 0)
                {
                    int prev = selectedChoiceIdx;
                    selectedChoiceIdx = -1;
                    selectedHeroIdx = -1;
                    RebuildInteractive(focusIndex: prev);
                }
            };

            RebuildInteractive(focusIndex: 0);
        }

        static void StripBakedRewardElements(VisualElement screen)
        {
            var toRemove = new List<VisualElement>();
            foreach (var child in screen.Children())
            {
                if (child.ClassListContains("reward-card__i") ||
                    child.ClassListContains("reward-card-2__i") ||
                    child.ClassListContains("reward-card__i-2") ||
                    child.ClassListContains("sprite-iconbattle") ||
                    child.ClassListContains("give-tideglass-wand-to-vars"))
                {
                    toRemove.Add(child);
                    continue;
                }
                // Remove baked overlay icon containers sitting over the cards (top 198..199, left >= 500).
                // Read layout, not style: exported UXML positions read as 0 through element.style.
                if (child.name == "RewardCardsHost" || child.name == "RewardConfirmHost") continue;
                float top = child.layout.y;
                float left = child.layout.x;
                if (child.resolvedStyle.position == Position.Absolute &&
                    ((Mathf.Abs(top - 198f) < 4f && left >= 500f) ||
                     (Mathf.Abs(top - 562f) < 4f && left >= 420f) ||
                     (Mathf.Abs(top - 636f) < 4f && left >= 600f)))
                {
                    toRemove.Add(child);
                }
            }
            foreach (var el in toRemove) el.RemoveFromHierarchy();
        }

        void RebuildInteractive(int focusIndex = 0, bool focusConfirm = false)
        {
            cardsHost.Clear();
            confirmHost.Clear();
            menu.Clear();

            int choiceCount = Mathf.Min(3, currentOffer.choices.Count);
            bool hasModifierOffers = currentOffer.modifierChoices != null && currentOffer.modifierChoices.Count > 0;

            // 1. Build 1..3 Figma Reward-Card / Reward-Card-Selected cards
            for (int i = 0; i < choiceCount; i++)
            {
                int choiceIdx = i;
                var o = currentOffer.choices[i];
                bool isSel = selectedChoiceIdx == i;
                var tpl = FigmaUi.InstantiateTemplate(isSel ? "Reward-Card-Selected" : "Reward-Card");
                VisualElement cardRoot = tpl ?? new VisualElement();
                var inner = cardRoot.Q<VisualElement>(className: isSel ? "reward-card-2" : "reward-card") ?? cardRoot;
                cardRoot.style.width = 268;
                cardRoot.style.height = 380;
                cardRoot.style.marginRight = i < choiceCount - 1 ? 16 : 0;
                inner.style.width = 268;
                inner.style.height = 380;

                var (rawLabel, rawDetail) = RewardGenerator.Describe(o, Run?.ModifierManager, currentParty);
                string displayTitle = FormatCardTitle(o, rawLabel);
                string displayDetail = rawDetail;

                var (catText, rarityText, rarityColor) = GetCardCategoryAndRarity(o);
                string compareText = BuildComparisonLine(o);
                bool keep = o.kind == RewardKind.Relic || o.kind == RewardKind.Equipment || o.kind == RewardKind.Upgrade;

                FigmaUi.SetInstanceText(cardRoot, "text-0", catText);
                FigmaUi.SetInstanceText(cardRoot, "text-1", rarityText);
                FigmaUi.SetInstanceText(cardRoot, "text-2", displayTitle);
                FigmaUi.SetInstanceText(cardRoot, "text-3", displayDetail.ToUpperInvariant());
                FigmaUi.SetInstanceText(cardRoot, "text-4", compareText);
                FigmaUi.SetInstanceText(cardRoot, "text-5", isSel ? "✓ SELECTED" : (i + 1).ToString());
                FigmaUi.SetInstanceText(cardRoot, "text-6", keep ? "KEPT" : "RUN ONLY");
                if (isSel) FigmaUi.SetInstanceText(cardRoot, "text-7", "✓");

                // Apply rarity color to rarity tag, icon well border, and card title
                var rarityLbl = cardRoot.Q<UiLabel>("text-1");
                if (rarityLbl != null) rarityLbl.style.color = rarityColor;
                var titleLbl = cardRoot.Q<UiLabel>("text-2");
                if (titleLbl != null)
                {
                    titleLbl.style.color = rarityColor;
                    titleLbl.style.fontSize = 19;
                    titleLbl.style.whiteSpace = WhiteSpace.Normal;
                }
                var detailLbl = cardRoot.Q<UiLabel>("text-3");
                if (detailLbl != null) detailLbl.style.fontSize = 12;
                var cmpLbl = cardRoot.Q<UiLabel>("text-4");
                if (cmpLbl != null) cmpLbl.style.fontSize = 12;

                // Tag badge styling for RUN ONLY vs KEPT
                var tagBox = cardRoot.Q<VisualElement>(className: isSel ? "reward-card-2__e-12" : "reward-card__e-12");
                var tagLbl = cardRoot.Q<UiLabel>("text-6");
                if (!keep && tagBox != null && tagLbl != null)
                {
                    tagBox.style.backgroundColor = Palette.Hex(0x2A160A);
                    tagBox.style.borderTopColor = Palette.Ember1;
                    tagBox.style.borderBottomColor = Palette.Ember1;
                    tagBox.style.borderLeftColor = Palette.Ember1;
                    tagBox.style.borderRightColor = Palette.Ember1;
                    tagLbl.style.color = Palette.Ember1;
                }

                // Icon well content
                var well = cardRoot.Q<VisualElement>(className: isSel ? "reward-card-2__e-4" : "reward-card__e-4");
                if (well != null)
                {
                    well.Clear();
                    well.style.borderTopColor = rarityColor;
                    well.style.borderBottomColor = rarityColor;
                    well.style.borderLeftColor = rarityColor;
                    well.style.borderRightColor = rarityColor;
                    well.style.justifyContent = Justify.Center;
                    well.style.alignItems = UiAlign.Center;

                    var tex = o.kind switch
                    {
                        RewardKind.Item => IconArt.Item(o.id) ?? IconArt.Reward("item"),
                        RewardKind.Equipment => IconArt.Gear(o.id) ?? IconArt.Reward("equipment"),
                        RewardKind.Relic => IconArt.Reward("relic"),
                        RewardKind.Upgrade => IconArt.Reward("upgrade"),
                        RewardKind.Modifier => IconArt.Reward("modifier"),
                        RewardKind.Gold => IconArt.Reward("gold"),
                        _ => IconArt.Reward("gold"),
                    };
                    if (tex != null)
                    {
                        var img = new VisualElement { style = { width = 64, height = 64 } };
                        FigmaUi.SetBackgroundContain(img, tex);
                        well.Add(img);
                    }
                    else
                    {
                        var sym = new UiLabel(PrizeSymbol(o))
                        {
                            style =
                            {
                                fontSize = 36, color = rarityColor,
                                unityFontStyleAndWeight = FontStyle.Bold,
                                unityTextAlign = TextAnchor.MiddleCenter
                            }
                        };
                        well.Add(sym);
                    }
                }

                cardsHost.Add(cardRoot);

                var cardItem = menu.Add(inner, displayTitle, () => SelectRewardCard(choiceIdx), tooltip: displayDetail.ToUpperInvariant());
                cardItem.visual = on =>
                {
                    if (isSel)
                    {
                        inner.style.backgroundColor = Palette.Hex(0x2B2210);
                        var b = on ? Palette.Mist1 : Palette.Gold2;
                        inner.style.borderTopColor = b; inner.style.borderBottomColor = b;
                        inner.style.borderLeftColor = b; inner.style.borderRightColor = b;
                    }
                    else
                    {
                        inner.style.backgroundColor = on ? Palette.Hex(0x1E2640) : Palette.Ink2;
                        var b = on ? Palette.Gold2 : Palette.Ink5;
                        inner.style.borderTopColor = b; inner.style.borderBottomColor = b;
                        inner.style.borderLeftColor = b; inner.style.borderRightColor = b;
                    }
                };
                cardItem.visual(false);
            }

            // 2. Build Bottom Confirmation Panel
            int confirmFocusIdx = -1;

            if (selectedChoiceIdx < 0 || selectedChoiceIdx >= choiceCount)
            {
                // State A: Nothing selected yet
                var topRow = new VisualElement { style = { flexDirection = FlexDirection.Column } };
                var prompt = new UiLabel(choiceCount > 0
                    ? "SELECT A REWARD CARD ABOVE TO PREVIEW & CONFIRM"
                    : "NO PRIZE AVAILABLE — COLLECT YOUR SPOILS AND CONTINUE")
                {
                    style = { fontSize = 13, color = Palette.Gold2, letterSpacing = 0.5f, marginBottom = 6 }
                };
                topRow.Add(prompt);

                if (hasModifierOffers)
                {
                    topRow.Add(BuildCompactModifierRow());
                }
                else if (choiceCount > 0)
                {
                    var subNote = new UiLabel("PRESS ENTER ON A CARD TO SELECT IT, THEN CONFIRM BELOW. OR SKIP TO TAKE ONLY THE SPOILS.")
                    {
                        style = { fontSize = 11, color = Palette.Mist2, whiteSpace = WhiteSpace.Normal }
                    };
                    topRow.Add(subNote);
                }
                confirmHost.Add(topRow);

                var btnRow = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd, alignItems = UiAlign.Center }
                };
                string skipLabel = choiceCount > 0 ? "Skip prize" : "Continue to map";
                if (choiceCount == 0)
                {
                    confirmFocusIdx = menu.items.Count;
                    btnRow.Add(MakePrimaryConfirmButton(skipLabel, () => Claim(-1, -1)));
                }
                else
                {
                    btnRow.Add(MakeSlabActionButton(skipLabel, () => Claim(-1, -1)));
                }
                confirmHost.Add(btnRow);
            }
            else
            {
                var selOpt = currentOffer.choices[selectedChoiceIdx];
                var (selLabel, selDetail) = RewardGenerator.Describe(selOpt, Run?.ModifierManager, currentParty);

                // State B/C: Hero selector for Equipment OR Relic/Consumable/Upgrade/Gold + Modifier picker if offered
                if (selOpt.kind == RewardKind.Equipment && Database.Equipment.TryGetValue(selOpt.id, out var eqDef))
                    {
                        var hdr = new UiLabel($"GIVE {eqDef.name.ToUpperInvariant()} TO")
                        {
                            style = { fontSize = 13, color = Palette.Gold2, letterSpacing = 0.56f, marginBottom = 4 }
                        };
                        confirmHost.Add(hdr);

                        var heroRow = new VisualElement
                        {
                            style = { flexDirection = FlexDirection.Row, alignItems = UiAlign.Center, marginBottom = 6 }
                        };
                        int bestHeroIdx = FindBestHeroForEquipment(eqDef);

                        int heroCount = Mathf.Min(currentParty.Count, 4);
                        for (int h = 0; h < heroCount; h++)
                        {
                            int hIdx = h;
                            var u = currentParty[h];
                            bool canEquip = CanHeroEquip(u, eqDef, out _);
                            bool isPicked = selectedHeroIdx == h;
                            bool isBest = canEquip && h == bestHeroIdx;

                            var hCard = new VisualElement
                            {
                                style =
                                {
                                    flexDirection = FlexDirection.Row, alignItems = UiAlign.Center,
                                    paddingTop = 5, paddingBottom = 5, paddingLeft = 8, paddingRight = 8,
                                    marginRight = h < heroCount - 1 ? 8 : 0,
                                    flexGrow = 1, flexBasis = 0, minWidth = 0, maxWidth = 197, height = 52,
                                    overflow = Overflow.Hidden,
                                    borderTopWidth = isPicked ? 3 : 2, borderBottomWidth = isPicked ? 3 : 2,
                                    borderLeftWidth = isPicked ? 3 : 2, borderRightWidth = isPicked ? 3 : 2,
                                    backgroundColor = isPicked ? Palette.Hex(0x2B2210) : Palette.Ink2,
                                    borderTopColor = isPicked ? Palette.Gold2 : Palette.Ink5,
                                    borderBottomColor = isPicked ? Palette.Gold2 : Palette.Ink5,
                                    borderLeftColor = isPicked ? Palette.Gold2 : Palette.Ink5,
                                    borderRightColor = isPicked ? Palette.Gold2 : Palette.Ink5,
                                    opacity = canEquip ? 1f : 0.45f
                                }
                            };

                            var spr = new VisualElement { style = { width = 28, height = 34, marginRight = 8, flexShrink = 0 } };
                            if (u.sprite != null && u.sprite.Length > 0)
                                FigmaUi.SetBackgroundContain(spr, FigmaUi.GetSpriteTexture(u.sprite, "rw_h_" + (u.classDef?.id ?? u.name)));
                            hCard.Add(spr);

                            var txtCol = new VisualElement { style = { flexDirection = FlexDirection.Column } };
                            txtCol.Add(new UiLabel(u.name) { style = { fontSize = 15, color = Palette.Mist1 } });
                            string subTxt = !canEquip ? "CAN'T USE" : (isBest ? "✓ " : "") + GainText(u, eqDef);
                            txtCol.Add(new UiLabel(subTxt)
                            {
                                style = { fontSize = 11, color = !canEquip ? Palette.Mist3 : (isBest ? Palette.Ok : Palette.Gold2) }
                            });
                            hCard.Add(txtCol);
                            heroRow.Add(hCard);

                            if (canEquip)
                            {
                                var hItem = menu.Add(hCard, $"Give to {u.name}", () =>
                                {
                                    selectedHeroIdx = hIdx;
                                    RebuildInteractive(focusConfirm: true);
                                });
                                hItem.visual = on =>
                                {
                                    hCard.style.backgroundColor = isPicked ? Palette.Hex(0x2B2210) : (on ? Palette.Hex(0x1E2640) : Palette.Ink2);
                                    var b = on ? Palette.Mist1 : (isPicked ? Palette.Gold2 : Palette.Ink5);
                                    hCard.style.borderTopColor = b; hCard.style.borderBottomColor = b;
                                    hCard.style.borderLeftColor = b; hCard.style.borderRightColor = b;
                                };
                                hItem.visual(false);
                            }
                        }
                        confirmHost.Add(heroRow);

                        if (hasModifierOffers)
                            confirmHost.Add(BuildCompactModifierRow());

                        var btnRow = new VisualElement
                        {
                            style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd, alignItems = UiAlign.Center }
                        };
                        btnRow.Add(MakeSlabActionButton("Back", () =>
                        {
                            int prev = selectedChoiceIdx;
                            selectedChoiceIdx = -1;
                            selectedHeroIdx = -1;
                            RebuildInteractive(focusIndex: prev);
                        }, marginRight: 8));
                        btnRow.Add(MakeSlabActionButton("Store in bag", () => Claim(selectedChoiceIdx, -1), marginRight: 8));

                        string confirmText = selectedHeroIdx >= 0 && selectedHeroIdx < currentParty.Count
                            ? $"Confirm & equip on {currentParty[selectedHeroIdx].name}"
                            : "Confirm & store in bag";
                        confirmFocusIdx = menu.items.Count;
                        btnRow.Add(MakePrimaryConfirmButton(confirmText, () => Claim(selectedChoiceIdx, selectedHeroIdx)));
                        confirmHost.Add(btnRow);
                    }
                    else
                    {
                        var topCol = new VisualElement { style = { flexDirection = FlexDirection.Column } };
                        topCol.Add(new UiLabel($"CONFIRM REWARD · {FormatCardTitle(selOpt, selLabel).ToUpperInvariant()}")
                        {
                            style = { fontSize = 14, color = Palette.Gold2, letterSpacing = 0.56f, marginBottom = 4 }
                        });

                        if (hasModifierOffers)
                        {
                            topCol.Add(BuildCompactModifierRow());
                        }
                        else
                        {
                            topCol.Add(new UiLabel(selDetail.ToUpperInvariant())
                            {
                                style = { fontSize = 12, color = Palette.Mist2, whiteSpace = WhiteSpace.Normal }
                            });
                        }
                        confirmHost.Add(topCol);

                        var btnRow = new VisualElement
                        {
                            style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd, alignItems = UiAlign.Center }
                        };
                        btnRow.Add(MakeSlabActionButton("Back", () =>
                        {
                            int prev = selectedChoiceIdx;
                            selectedChoiceIdx = -1;
                            RebuildInteractive(focusIndex: prev);
                        }, marginRight: 8));
                        confirmFocusIdx = menu.items.Count;
                        btnRow.Add(MakePrimaryConfirmButton("Confirm reward", () => Claim(selectedChoiceIdx, -1)));
                        confirmHost.Add(btnRow);
                    }
            }

            // Focus target item cleanly
            int targetIdx = focusConfirm && confirmFocusIdx >= 0
                ? confirmFocusIdx
                : Mathf.Clamp(focusIndex, 0, Mathf.Max(0, menu.items.Count - 1));
            if (menu.items.Count > 0)
                menu.Focus(targetIdx, false, true);
        }

        void SelectRewardCard(int choiceIdx)
        {
            selectedChoiceIdx = choiceIdx;
            selectedHeroIdx = -1;
            if (choiceIdx >= 0 && choiceIdx < currentOffer.choices.Count)
            {
                var o = currentOffer.choices[choiceIdx];
                if (o.kind == RewardKind.Equipment && Database.Equipment.TryGetValue(o.id, out var eqDef))
                    selectedHeroIdx = FindBestHeroForEquipment(eqDef);
            }
            Sfx.Play(Sound.Confirm);
            RebuildInteractive(focusConfirm: true);
        }

        VisualElement BuildCompactModifierRow()
        {
            var wrap = new VisualElement { style = { flexDirection = FlexDirection.Column, marginBottom = 4 } };
            wrap.Add(new UiLabel("CHOOSE A MODIFIER (REQUIRED)")
            {
                style = { fontSize = 10, color = Palette.Gold2, letterSpacing = 0.4f, marginBottom = 3 }
            });
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = UiAlign.Center } };
            wrap.Add(row);

            for (int i = 0; i < currentOffer.modifierChoices.Count && i < 3; i++)
            {
                int mIdx = i;
                var mOpt = currentOffer.modifierChoices[i];
                if (mOpt.id == "MOD-024" && mOpt.element.IsValidElement() && selectedModifierIdx != mIdx)
                    selectedAttunementElement = mOpt.element;

                bool isSel = selectedModifierIdx == mIdx;
                var elem = mOpt.id == "MOD-024" ? selectedAttunementElement : mOpt.element;
                var (mLabel, mDetail) = RewardGenerator.Describe(new RewardOption
                {
                    kind = RewardKind.Modifier, id = mOpt.id, element = elem
                }, Run?.ModifierManager);

                var box = new VisualElement
                {
                    style =
                    {
                        width = 262, minHeight = 52, marginRight = i < 2 ? 8 : 0,
                        paddingLeft = 8, paddingRight = 8, paddingTop = 4, paddingBottom = 4,
                        borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                        backgroundColor = isSel ? Palette.Hex(0x1E3328) : Palette.Ink2,
                        borderTopColor = isSel ? Palette.Ok : Palette.Ink5,
                        borderBottomColor = isSel ? Palette.Ok : Palette.Ink5,
                        borderLeftColor = isSel ? Palette.Ok : Palette.Ink5,
                        borderRightColor = isSel ? Palette.Ok : Palette.Ink5,
                        justifyContent = Justify.FlexStart, overflow = Overflow.Hidden
                    }
                };
                box.Add(new UiLabel((isSel ? "✓ " : "") + mLabel.ToUpperInvariant())
                {
                    style = { fontSize = 12, color = isSel ? Palette.Ok : Palette.Mist1, whiteSpace = WhiteSpace.NoWrap, flexShrink = 0, overflow = Overflow.Hidden }
                });
                // The effect itself, so the choice is informed (not only in the tooltip).
                box.Add(new UiLabel(mDetail)
                {
                    style = { fontSize = 11, color = Palette.Mist2, whiteSpace = WhiteSpace.Normal }
                });
                row.Add(box);

                int keepIdx = menu.items.Count;
                var mItem = menu.Add(box, mLabel, () =>
                {
                    if (selectedModifierIdx == mIdx)
                    {
                        // Already picked: Elemental Attunement cycles its element; others stay picked (mandatory).
                        if (mOpt.id == "MOD-024")
                        {
                            int curIdx = Array.IndexOf(ElementTypes.All, selectedAttunementElement);
                            selectedAttunementElement = ElementTypes.All[(curIdx + 1) % ElementTypes.All.Length];
                        }
                    }
                    else
                    {
                        selectedModifierIdx = mIdx;
                    }
                    RebuildInteractive(focusIndex: keepIdx);
                }, tooltip: mDetail.ToUpperInvariant());
                mItem.visual = on =>
                {
                    var b = on ? Palette.Gold2 : (isSel ? Palette.Ok : Palette.Ink5);
                    box.style.borderTopColor = b; box.style.borderBottomColor = b;
                    box.style.borderLeftColor = b; box.style.borderRightColor = b;
                };
                mItem.visual(false);
            }
            return wrap;
        }

        VisualElement MakeSlabActionButton(string label, Action onClick, float marginRight = 0f)
        {
            var btn = new Button { text = label };
            btn.AddToClassList("button-slab");
            btn.style.height = 38;
            btn.style.paddingLeft = 14;
            btn.style.paddingRight = 14;
            btn.style.marginRight = marginRight;
            btn.style.fontSize = 14;
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(btn, label, onClick), btn);
            return btn;
        }

        VisualElement MakePrimaryConfirmButton(string label, Action onClick)
        {
            var tpl = FigmaUi.InstantiateTemplate("Button-Primary-Focus");
            if (tpl != null)
            {
                FigmaUi.SetInstanceText(tpl, "text-0", "▸");
                FigmaUi.SetInstanceText(tpl, "text-1", label);
                var btn = tpl.Q<Button>() ?? tpl;
                btn.style.height = 38;
                var item = menu.Add(btn, label, onClick);
                item.visual = on =>
                {
                    FigmaUi.SetInstanceText(tpl, "text-0", on ? "▸" : "");
                    FigmaUi.ApplyDefaultFocusVisual(btn, on, false, true);
                };
                item.visual(false);
                return tpl;
            }
            return MakeSlabActionButton(label, onClick);
        }

        static string FormatCardTitle(RewardOption o, string fallback) => o.kind switch
        {
            RewardKind.Equipment => Database.Equipment.TryGetValue(o.id, out var eq) ? eq.name : fallback,
            RewardKind.Relic => Database.Relics.TryGetValue(o.id, out var rel) ? rel.name : fallback,
            RewardKind.Item => Database.Items.TryGetValue(o.id, out var itm) ? itm.name : fallback,
            _ => fallback
        };

        static (string category, string rarity, Color color) GetCardCategoryAndRarity(RewardOption o)
        {
            switch (o.kind)
            {
                case RewardKind.Equipment:
                    if (Database.Equipment.TryGetValue(o.id, out var eq))
                    {
                        var rc = eq.rarity switch
                        {
                            EquipmentRarity.Common => Palette.Mist1,
                            EquipmentRarity.Uncommon => Palette.Ok,
                            EquipmentRarity.Rare => Palette.Mp,
                            EquipmentRarity.Epic => Palette.ArcaneViolet,
                            EquipmentRarity.Legendary => Palette.Gold2,
                            _ => Palette.Mist1
                        };
                        return ("EQUIPMENT", eq.rarity.ToString().ToUpperInvariant(), rc);
                    }
                    return ("EQUIPMENT", "COMMON", Palette.Mist1);
                case RewardKind.Relic:
                    bool isBoss = o.id == Database.BossRelic;
                    return ("RELIC", isBoss ? "BOSS RELIC" : "UNCOMMON", isBoss ? Palette.Gold2 : Palette.Ok);
                case RewardKind.Item:
                    bool isElixir = o.id == "elixir";
                    return ("CONSUMABLE", isElixir ? "RARE" : "COMMON", isElixir ? Palette.Mp : Palette.Mist1);
                case RewardKind.Upgrade:
                    return ("UPGRADE", "RARE", Palette.Gold2);
                case RewardKind.Modifier:
                    return ("MODIFIER", "RUN BUFF", Palette.ArcaneViolet);
                default:
                    return ("TREASURE", "GOLD", Palette.Gold2);
            }
        }

        string BuildComparisonLine(RewardOption o)
        {
            switch (o.kind)
            {
                case RewardKind.Relic:
                {
                    int cur = Run?.Relics.Count ?? 0;
                    int next = Mathf.Min(RelicManager.MaxRelicCapacity, cur + 1);
                    return $"RELIC SLOTS {cur}/{RelicManager.MaxRelicCapacity} › {next}/{RelicManager.MaxRelicCapacity}";
                }
                case RewardKind.Item:
                {
                    int owned = Run?.CarryCount(o.id) ?? 0;
                    if (Run != null && Run.CarrySpace(o.id) <= 0)
                        return $"CARRY LIMIT {owned}/{Database.Items[o.id].maxStack} · NOTHING ADDED";
                    return $"YOU OWN {owned} › {owned + 1}";
                }
                case RewardKind.Equipment:
                {
                    if (Database.Equipment.TryGetValue(o.id, out var eq))
                    {
                        int best = FindBestHeroForEquipment(eq);
                        string slotName = EquipmentSlots.DisplayName(eq.slot).ToUpperInvariant();
                        return best >= 0 && best < currentParty.Count
                            ? $"BEST FOR {currentParty[best].name.ToUpperInvariant()} · {GainText(currentParty[best], eq)}"
                            : $"STORED IN BAG · {slotName}";
                    }
                    return "STORED IN INVENTORY";
                }
                case RewardKind.Upgrade:
                {
                    // One hero, always: the same one RunState.Upgrade will boost.
                    return $"PERMANENT BOOST FOR {RunState.ResolveUpgrade(currentParty, o.id).hero.name.ToUpperInvariant()}";
                }
                default:
                    return $"+{o.amount} EXTRA SPOILS";
            }
        }

        static bool CanHeroEquip(Unit u, EquipmentDef def, out EquipmentSlot validSlot)
        {
            validSlot = def.slot;
            if (u == null || def == null) return false;
            if (EquipmentValidator.SlotMatches(def, def.slot, u.classDef) &&
                EquipmentValidator.Validate(def, def.slot, u.classDef, u.race, u.level, u.FinalScores, u.id) == null)
            {
                validSlot = def.slot;
                return true;
            }
            if (def.GetEquipmentCategory() == EquipmentType.Accessory || EquipmentValidator.IsAccessorySlot(def.slot))
            {
                if (EquipmentValidator.Validate(def, EquipmentSlot.Accessory1, u.classDef, u.race, u.level, u.FinalScores, u.id) == null)
                {
                    validSlot = EquipmentSlot.Accessory1;
                    return true;
                }
            }
            if (EquipmentValidator.SlotMatches(def, EquipmentSlot.MainHand, u.classDef) &&
                EquipmentValidator.Validate(def, EquipmentSlot.MainHand, u.classDef, u.race, u.level, u.FinalScores, u.id) == null)
            {
                validSlot = EquipmentSlot.MainHand;
                return true;
            }
            if (EquipmentValidator.SlotMatches(def, EquipmentSlot.SecondHand, u.classDef) &&
                EquipmentValidator.Validate(def, EquipmentSlot.SecondHand, u.classDef, u.race, u.level, u.FinalScores, u.id) == null)
            {
                validSlot = EquipmentSlot.SecondHand;
                return true;
            }
            return false;
        }

        // The hero who gains the most from it (largest single stat change, as on the Equipment screen);
        // with no gain anywhere, the first hero with that slot empty, else the first who can use it.
        int FindBestHeroForEquipment(EquipmentDef def)
        {
            if (def == null || currentParty == null) return -1;
            int firstValid = -1, firstEmpty = -1, best = -1, bestGain = 0;
            for (int i = 0; i < currentParty.Count; i++)
            {
                var u = currentParty[i];
                if (!CanHeroEquip(u, def, out var slot)) continue;
                if (firstValid < 0) firstValid = i;
                if (firstEmpty < 0 && !u.equipped.ContainsKey(slot)) firstEmpty = i;
                int gain = BestChange(u, def, slot).delta;
                if (gain > bestGain) { bestGain = gain; best = i; }
            }
            return best >= 0 ? best : firstEmpty >= 0 ? firstEmpty : firstValid;
        }

        static (DerivedStatId stat, int delta) BestChange(Unit u, EquipmentDef def, EquipmentSlot slot)
        {
            var cand = new EquipmentInstance(def);
            return EquipmentScreen.ComparedStats
                .Select(s => (s, EquipmentManager.CalculateStatDelta(u, slot, cand, s)))
                .OrderByDescending(t => t.Item2).First();
        }

        // "+3 PATK", "NEW SLOT" (fills an empty slot), "NO GAIN" or the smallest loss, for one hero.
        string GainText(Unit u, EquipmentDef def)
        {
            if (!CanHeroEquip(u, def, out var slot)) return "CAN'T USE";
            if (!u.equipped.ContainsKey(slot)) return "NEW SLOT";
            var (st, d) = BestChange(u, def, slot);
            if (d == 0) return "NO GAIN";
            string name = Database.DerivedStatDefs[st].shortName.ToUpperInvariant();
            return $"{(d > 0 ? "+" : "")}{d}{(DerivedStats.IsPercentage(st) ? "%" : "")} {name}";
        }

        static string PrizeSymbol(RewardOption o) => o.kind switch
        {
            RewardKind.Relic => "R",
            RewardKind.Item => "P",
            RewardKind.Equipment => "E",
            RewardKind.Upgrade => "U",
            RewardKind.Modifier => "M",
            _ => "+",
        };

        void Claim(int choice, int equipHeroIdx)
        {
            int gainedGold = Run != null && Run.PendingReward != null ? Run.PendingReward.gold : 0;
            RewardOption picked = (choice >= 0 && Run?.PendingReward != null && choice < Run.PendingReward.choices.Count)
                ? Run.PendingReward.choices[choice]
                : null;
            int prevEquipCount = Run?.Inventory?.equipment?.Count ?? 0;
            bool endsPrologue = Run.IsPrologue && Run.Current?.type == NodeType.Boss;

            // The chosen element only applies to Elemental Attunement; other element modifiers keep their rolled element.
            var mods = Run.PendingReward?.modifierChoices;
            bool attunement = mods != null && selectedModifierIdx >= 0 && selectedModifierIdx < mods.Count && mods[selectedModifierIdx].id == "MOD-024";
            Run.ClaimReward(choice, selectedModifierIdx, attunement ? selectedAttunementElement : (ElementType?)null);

            // Auto-equip newly granted equipment onto the selected hero if requested
            if (equipHeroIdx >= 0 && picked != null && picked.kind == RewardKind.Equipment &&
                Run?.Party != null && equipHeroIdx < Run.Party.Count && Run.Inventory?.equipment != null)
            {
                var hero = Run.Party[equipHeroIdx];
                EquipmentInstance newInst = Run.Inventory.equipment.Count > prevEquipCount
                    ? Run.Inventory.equipment[Run.Inventory.equipment.Count - 1]
                    : Run.Inventory.equipment.LastOrDefault(e => e.definitionId == picked.id && !e.isEquipped);
                if (newInst != null && newInst.Definition != null)
                {
                    if (newInst.Definition.GetEquipmentCategory() == EquipmentType.Accessory ||
                        EquipmentValidator.IsAccessorySlot(newInst.Definition.slot))
                    {
                        EquipmentManager.AutoEquipAccessory(hero, newInst, Run.Inventory, out _);
                    }
                    else if (newInst.Definition.slot == EquipmentSlot.MainHand || newInst.Definition.slot == EquipmentSlot.SecondHand)
                    {
                        EquipmentManager.AutoEquipWeapon(hero, newInst, Run.Inventory, out _);
                    }
                    else
                    {
                        EquipmentManager.EquipFromInventory(hero, newInst.Definition.slot, newInst, Run.Inventory, out _);
                    }
                }
            }

            if (choice >= 0) Sfx.Play(Sound.Success);
            else if (gainedGold > 0) Sfx.Play(Sound.Coin);

            if (endsPrologue)
            {
                Game.Go(GameState.PrologueClear);
            }
            else if (Run.Won)
            {
                PersistenceCommitHandler.CommitDungeonClear(Run);
                SaveManager.AutoSave(out _);
                Game.Go(Run.Act >= 8 ? GameState.Victory : GameState.DungeonClear);
            }
            else
            {
                Game.Go(GameState.Map);
            }
        }
    }

    // Dungeon Clear — migrated to Figma 23---Dungeon-Clear.uxml (1280x720 UI Toolkit).
    public class DungeonClearScreen : GameScreen
    {
        // Rule reminders baked into 23---Dungeon-Clear: the "COMMITTED · KEPT IF A LATER RUN FAILS" note and the
        // footer chip row (PROGRESS COMMITTED · MODIFIERS END HERE · N RELICS STAY). Shared with EndScreen.
        internal static void HideDungeonClearReminders(VisualElement root)
        {
            FigmaUi.HideByClass(root, "committed-kept-if-a-later-run-fails-vars");
            var keepTags = root.Query<VisualElement>(className: "tag-keep").ToList();
            var chipRow = keepTags.Count > 0 ? keepTags[keepTags.Count - 1].GetFirstAncestorOfType<TemplateContainer>()?.parent : null;
            if (chipRow != null && chipRow != root) chipRow.style.display = DisplayStyle.None;
        }

        protected override void Build()
        {
            Sfx.Play(Sound.VictoryStinger);
            MountFigma("23---Dungeon-Clear");
            var menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);

            int act = Run != null ? Run.Act : 1;
            var stats = Run != null ? Run.Stats : new RunStats();
            int earned = stats.goldEarned, total = Run != null ? Run.HubGold : 0;

            var dLbl = UiRoot.Q<UiLabel>(className: "dungeon-1-of-8-vars-2");
            if (dLbl != null) dLbl.text = $"DUNGEON {act} OF 8";

            var mapLbl = UiRoot.Q<UiLabel>(className: "the-sunken-crypt-vars-3");
            if (mapLbl != null) mapLbl.text = Run?.Map?.name ?? "Dungeon";

            var pipLbl = UiRoot.Q<UiLabel>(className: "node-vars-13");
            if (pipLbl != null) pipLbl.text = string.Concat(Enumerable.Range(1, 8).Select(i => i <= act ? "◆" : "◇"));

            var runTags = UiRoot.Query<VisualElement>(className: "currency-tag-run").ToList();
            if (runTags.Count > 0) FigmaUi.SetInstanceText(runTags[0], "text-0", $"{earned} SPOILS");

            var hubTags = UiRoot.Query<VisualElement>(className: "currency-tag-hub").ToList();
            if (hubTags.Count > 0) FigmaUi.SetInstanceText(hubTags[0], "text-0", $"+{earned} GOLD");

            var totalLbl = UiRoot.Q<UiLabel>(className: "_745-vars");
            if (totalLbl != null) totalLbl.text = total.ToString("#,##0");

            var encLbl = UiRoot.Q<UiLabel>(className: "_6-vars-3");
            if (encLbl != null) encLbl.text = stats.battlesWon.ToString();

            var eliteLbl = UiRoot.Q<UiLabel>(className: "_1-vars-9");
            if (eliteLbl != null) eliteLbl.text = stats.elitesWon.ToString();

            var chLbl = UiRoot.Q<UiLabel>(className: "_11-vars-2");
            if (chLbl != null) chLbl.text = stats.nodesCompleted.ToString();

            var relLbl = UiRoot.Q<UiLabel>(className: "_1-vars-10");
            if (relLbl != null) relLbl.text = (Run?.Relics.Count ?? 0).ToString();

            var nextLbl = UiRoot.Q<UiLabel>(className: "next-dungeon-2-of-8-unlocked-vars");
            if (nextLbl != null) nextLbl.text = $"NEXT: DUNGEON {Mathf.Min(8, act + 1)} OF 8 UNLOCKED";

            HideDungeonClearReminders(UiRoot);

            var retInst = UiRoot.Q<VisualElement>(className: "button-primary-2__i-9");
            if (retInst != null)
            {
                FigmaUi.SetInstanceText(retInst, "text-1", "Return to Hub");
                var btn = retInst.Q<Button>() ?? retInst;
                var item = menu.Add(btn, "Return to Hub", () => Game.Go(GameState.Hub));
                item.visual = on =>
                {
                    FigmaUi.SetInstanceText(retInst, "text-0", on ? "▸" : "");
                    FigmaUi.ApplyDefaultFocusVisual(btn, on, false, true);
                };
                item.visual(false);
            }
        }
    }

    // Legacy shared result helpers kept public cleanly for any callers.
    public static class ResultArt
    {
        public static void Bricks(Transform root) { }
        public static void Ornament(Transform root, int cx, int y, int w, Color c) { }
        public static void Title(Transform root, string text, int cx, int y, Color c) { }
    }
}
