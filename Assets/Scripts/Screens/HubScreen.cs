using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Hub Screen:
    // Conforms to 640x360 reference resolution, Serif headers, and retro JRPG aesthetics.
    // Represents the central sanctuary between dungeon expeditions.
    // Houses the 5 Hub Facilities using GOLD:
    // 1. Shop (Consumables, unlimited stock)
    // 2. Armory (Equipment merchant)
    // 3. Blacksmith (Upgrade equipment)
    // 4. Training Ground (Manage skills & loadouts)
    // 5. Temple (Revive fallen party members)
    public class HubScreen : GameScreen
    {
        enum HubFacility { Shop, Armory, Blacksmith, TrainingGround, Temple }

        HubFacility activeFacility = HubFacility.Shop;
        ShopInventory armoryStock;
        Transform facilityContent;
        Label feedbackLabel;
        Label hubGoldLabel;
        readonly Label[] partyStatusRows = new Label[4];
        readonly SpriteRenderer[] partyHpGauges = new SpriteRenderer[4];

        protected override void Build()
        {
            armoryStock = EquipmentShopManager.GenerateShop(Run?.Act ?? 1, itemCount: 6);
            Backdrop.Create(Root, false, 24, Palette.DeepCharcoal, Palette.Hex(0x1B2133), Palette.Gold2);

            // Header banner per FELNA — UI System (Page 09: Geistwald City Hub)
            Ui.Label(Root, "GEISTWALD CITY — SANCTUARY HUB", 320, 14, Palette.AntiqueGoldBright, Align.Center, 2, FontKind.DotGothic);
            Ui.Divider(Root, 120, 36, 400, Palette.DarkBorder, Palette.AntiqueGoldBright);

            // Hub Gold badge (solid gold frame)
            int currentGold = Run != null ? Run.Gold : 0;
            Ui.Panel(Root, 460, 10, 164, 24, Palette.Hex(0x1C2210), Palette.Gold1);
            hubGoldLabel = Ui.Label(Root, $"{currentGold:#,##0} HUB GOLD", 542, 16, Palette.Gold1, Align.Center, font: FontKind.DotGothic);

            // Navigation bar: 5 Facilities + Embark
            var navMenu = Root.gameObject.AddComponent<Menu>();
            int btnW = 84;
            var shopBtn = Ui.Button(Root, "SHOP", 20, 48, btnW, () => SwitchFacility(HubFacility.Shop), 20, style: activeFacility == HubFacility.Shop ? ButtonStyle.Primary : ButtonStyle.Slab, font: FontKind.DotGothic);
            var armoryBtn = Ui.Button(Root, "ARMORY", 108, 48, btnW, () => SwitchFacility(HubFacility.Armory), 20, style: activeFacility == HubFacility.Armory ? ButtonStyle.Primary : ButtonStyle.Slab, font: FontKind.DotGothic);
            var blacksmithBtn = Ui.Button(Root, "BLACKSMITH", 196, 48, 92, () => SwitchFacility(HubFacility.Blacksmith), 20, style: activeFacility == HubFacility.Blacksmith ? ButtonStyle.Primary : ButtonStyle.Slab, font: FontKind.DotGothic);
            var trainingBtn = Ui.Button(Root, "TRAINING", 292, 48, btnW, () => SwitchFacility(HubFacility.TrainingGround), 20, style: activeFacility == HubFacility.TrainingGround ? ButtonStyle.Primary : ButtonStyle.Slab, font: FontKind.DotGothic);
            var templeBtn = Ui.Button(Root, "TEMPLE", 380, 48, btnW, () => SwitchFacility(HubFacility.Temple), 20, style: activeFacility == HubFacility.Temple ? ButtonStyle.Primary : ButtonStyle.Slab, font: FontKind.DotGothic);
            navMenu.Add(shopBtn);
            navMenu.Add(armoryBtn);
            navMenu.Add(blacksmithBtn);
            navMenu.Add(trainingBtn);
            navMenu.Add(templeBtn);

            var embarkBtn = Ui.Button(Root, "EMBARK >>", 480, 48, 140, OnEmbarkClicked, 22, style: ButtonStyle.Primary, font: FontKind.DotGothic);
            navMenu.Add(embarkBtn);

            Ui.Divider(Root, 20, 72, 600, Palette.DarkBorder, Palette.AntiqueGoldDim);

            // Left panel (390x230): Facility Content
            Ui.Panel(Root, 20, 76, 390, 230, Palette.DeepCharcoal, Palette.DarkBorder);

            // Right panel (200x230): Party Status
            Ui.Panel(Root, 420, 76, 200, 230, Palette.DeepCharcoal, Palette.DarkBorder);
            Ui.Label(Root, "EXPEDITION ROSTER", 430, 84, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.DotGothic);
            Ui.Divider(Root, 430, 98, 180, Palette.DarkBorder, Palette.AntiqueGoldDim);

            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            for (int i = 0; i < partyStatusRows.Length && i < party.Count; i++)
            {
                var u = party[i];
                int y = 108 + i * 42;
                Color hpCol = Palette.ForHp(u.hp, u.stats.maxHp);
                Ui.Label(Root, PixelFont.Fit(u.name.ToUpperInvariant(), 100, FontKind.DotGothic), 430, y, u.Alive ? Palette.WarmIvory : Palette.SpecCrimson, Align.Left, 1, FontKind.DotGothic);
                partyStatusRows[i] = Ui.Label(Root, u.Alive ? $"HP {u.hp}/{u.stats.maxHp}" : "FALLEN (DEAD)", 610, y, u.Alive ? hpCol : Palette.SpecCrimson, Align.Right, font: FontKind.DotGothic);
                partyHpGauges[i] = Ui.Image(Root, PixelArt.GaugeBar(180, 5, u.Alive ? hpCol : Palette.HpLow, (float)u.hp / Mathf.Max(1, u.stats.maxHp)), 430, y + 14, Ui.Widget);
            }

            // Bottom bar: feedback & Hub utility buttons
            feedbackLabel = Ui.Label(Root, "WELCOME TO THE HUB. PREPARE GEAR, HEAL, UPGRADE, AND TRAIN BEFORE EMBARKING.", 24, 316, Palette.WarmIvory, Align.Left, font: FontKind.DotGothic);

            var utilMenu = Root.gameObject.AddComponent<Menu>();
            var saveBtn = Ui.Button(Root, "SAVE", 430, 312, 56, () => Game.Go(GameState.Save), 22, style: ButtonStyle.Slab, font: FontKind.DotGothic);
            var loadBtn = Ui.Button(Root, "LOAD", 490, 312, 56, () => Game.Go(GameState.Load), 22, style: ButtonStyle.Slab, font: FontKind.DotGothic);
            var titleBtn = Ui.Button(Root, "TITLE", 550, 312, 70, OnTitleClicked, 22, style: ButtonStyle.Slab, font: FontKind.DotGothic);
            utilMenu.Add(saveBtn);
            utilMenu.Add(loadBtn);
            utilMenu.Add(titleBtn);

            // Auto-save on Hub arrival if not already auto-saved
            if (Run != null && PersistenceCommitHandler.ActiveCommittedSnapshot == null)
            {
                PersistenceCommitHandler.SetActiveCommittedSnapshot(PersistenceCommitHandler.CreateCommittedSnapshot(Run));
                SaveManager.AutoSave(out _);
            }

            RenderFacility();
        }

        void SwitchFacility(HubFacility facility)
        {
            activeFacility = facility;
            RenderFacility();
        }

        void RefreshHeaderAndParty()
        {
            int currentGold = Run != null ? Run.Gold : 0;
            hubGoldLabel?.Set($"{currentGold} GOLD");

            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            for (int i = 0; i < partyStatusRows.Length && i < party.Count; i++)
            {
                var u = party[i];
                if (partyStatusRows[i] != null) partyStatusRows[i].Set(u.Alive ? $"HP {u.hp}/{u.stats.maxHp}" : "FALLEN (DEAD)");
                if (partyHpGauges[i] != null) partyHpGauges[i].sprite = PixelArt.GaugeBar(180, 5, u.Alive ? Palette.Emerald : Palette.HpLow, (float)u.hp / Mathf.Max(1, u.stats.maxHp));
            }
        }

        void RenderFacility()
        {
            if (facilityContent != null) UnityEngine.Object.Destroy(facilityContent.gameObject);
            facilityContent = new GameObject("FacilityContent").transform;
            facilityContent.SetParent(Root, false);

            RefreshHeaderAndParty();

            var menu = facilityContent.gameObject.AddComponent<Menu>();

            switch (activeFacility)
            {
                case HubFacility.Shop:
                    RenderHubShop(menu);
                    break;
                case HubFacility.Armory:
                    RenderArmory(menu);
                    break;
                case HubFacility.Blacksmith:
                    RenderBlacksmith(menu);
                    break;
                case HubFacility.TrainingGround:
                    RenderTrainingGround(menu);
                    break;
                case HubFacility.Temple:
                    RenderTemple(menu);
                    break;
            }
        }

        void RenderHubShop(Menu menu)
        {
            Ui.Label(facilityContent, "CONSUMABLES OUTPOST - UNLIMITED STOCK", 28, 82, Palette.Gold2, Align.Left, 1, FontKind.Serif);
            var catalog = HubEconomy.GenerateHubShopCatalog();
            int y = 104;
            foreach (var c in catalog)
            {
                string text = $"{c.name.ToUpperInvariant()} (UNLIMITED) - {c.cost} HUB GOLD";
                var btn = Ui.Button(facilityContent, PixelFont.Fit(text, 370), 28, y, 374, () =>
                {
                    if (HubEconomy.TryBuyHubConsumable(Run, c.itemId, 1, out string err))
                    {
                        Sfx.Play(Sound.Success);
                        feedbackLabel?.Set(PixelFont.Fit($"PURCHASED {c.name.ToUpperInvariant()} WITH GOLD!", 520));
                        RenderFacility();
                    }
                    else
                    {
                        Sfx.Play(Sound.Deny);
                        feedbackLabel?.Set(PixelFont.Fit(err.ToUpperInvariant(), 520));
                    }
                }, 24);
                btn.tooltip = c.description.ToUpperInvariant();
                menu.Add(btn);
                y += 32;
            }
        }

        void RenderArmory(Menu menu)
        {
            Ui.Label(facilityContent, "ARMORY - WEAPONS & ARMOR", 28, 82, Palette.Gold2, Align.Left, 1, FontKind.Serif);
            var items = armoryStock.items;
            int y = 104;
            for (int i = 0; i < items.Count && i < 5; i++)
            {
                var itm = items[i];
                string name = itm.item?.Definition?.name ?? "Equipment";
                string status = itm.isSold ? "[SOLD OUT]" : $"{itm.cost} HUB GOLD";
                var btn = Ui.Button(facilityContent, PixelFont.Fit($"{name.ToUpperInvariant()} - {status}", 370), 28, y, 374, () =>
                {
                    if (HubEconomy.TryBuyHubEquipment(Run, armoryStock, itm.shopItemId, out var bought, out string err))
                    {
                        Sfx.Play(Sound.Success);
                        feedbackLabel?.Set(PixelFont.Fit($"PURCHASED {bought.Definition?.name.ToUpperInvariant()} FOR {itm.cost} GOLD!", 520));
                        RenderFacility();
                    }
                    else
                    {
                        Sfx.Play(Sound.Deny);
                        feedbackLabel?.Set(PixelFont.Fit(err.ToUpperInvariant(), 520));
                    }
                }, 24);
                btn.tooltip = itm.item?.Definition?.description?.ToUpperInvariant() ?? "";
                if (itm.isSold) btn.label.Color = Palette.Mist3;
                menu.Add(btn);
                y += 32;
            }
        }

        void RenderBlacksmith(Menu menu)
        {
            Ui.Label(facilityContent, "BLACKSMITH FORGE - UPGRADE EQUIPMENT", 28, 82, Palette.Gold2, Align.Left, 1, FontKind.Serif);
            var invEquip = Run?.Inventory?.equipment ?? new List<EquipmentInstance>();
            if (invEquip.Count == 0)
            {
                Ui.Label(facilityContent, "NO EQUIPMENT IN INVENTORY TO UPGRADE.", 215, 160, Palette.Mist3, Align.Center);
                return;
            }

            int y = 104;
            for (int i = 0; i < invEquip.Count && i < 5; i++)
            {
                var eq = invEquip[i];
                int cost = eq.GetUpgradeCostGold();
                string title = $"{eq.Definition?.name.ToUpperInvariant()} +{eq.upgradeLevel} -> +{eq.upgradeLevel + 1} ({cost}G)";
                var btn = Ui.Button(facilityContent, PixelFont.Fit(title, 370), 28, y, 374, () =>
                {
                    if (HubEconomy.TryBlacksmithUpgrade(Run, eq, out string err))
                    {
                        Sfx.Play(Sound.Success);
                        feedbackLabel?.Set(PixelFont.Fit($"UPGRADED {eq.Definition?.name.ToUpperInvariant()} TO +{eq.upgradeLevel}!", 520));
                        RenderFacility();
                    }
                    else
                    {
                        Sfx.Play(Sound.Deny);
                        feedbackLabel?.Set(PixelFont.Fit(err.ToUpperInvariant(), 520));
                    }
                }, 24);
                btn.tooltip = $"UPGRADE STATS BY +5% BASE STATS. REQUIRES {cost} GOLD.";
                menu.Add(btn);
                y += 32;
            }
        }

        void RenderTrainingGround(Menu menu)
        {
            Ui.Label(facilityContent, "TRAINING GROUND - SKILLS & LOADOUT", 28, 82, Palette.Gold2, Align.Left, 1, FontKind.Serif);
            Ui.Label(facilityContent, "MANAGE ACTIVE AND PASSIVE CLASS SKILL LOADOUTS.", 28, 104, Palette.WarmIvory);

            var openSkillsBtn = Ui.Button(facilityContent, "OPEN FULL SKILL TREE SCREEN >>", 28, 130, 374, () =>
            {
                Game.Go(GameState.Skills);
            }, 26, style: ButtonStyle.Primary);
            openSkillsBtn.tooltip = "INSPECT 5-TIER SKILL TREES, UPGRADE RANKS, AND ALLOCATE SKILL POINTS.";
            menu.Add(openSkillsBtn);

            var openTalentsBtn = Ui.Button(facilityContent, "MANAGE HERO TALENTS & RESPEC >>", 28, 164, 374, () =>
            {
                Game.Go(GameState.Talents);
            }, 26, style: ButtonStyle.Primary);
            openTalentsBtn.tooltip = "CHOOSE TALENT MILESTONES (LVL 3, 6, 9, 12, 15) AND RESPEC TALENTS.";
            menu.Add(openTalentsBtn);
        }

        void RenderTemple(Menu menu)
        {
            Ui.Label(facilityContent, "TEMPLE OF RESURRECTION - REVIVE HEROES", 28, 82, Palette.Gold2, Align.Left, 1, FontKind.Serif);
            var party = Run?.Party ?? new List<Unit>();
            int y = 104;
            for (int i = 0; i < party.Count; i++)
            {
                var u = party[i];
                string status = u.Alive ? "ALIVE (NORMAL)" : $"FALLEN (REVIVE FOR {HubEconomy.TempleReviveCost} GOLD)";
                var btn = Ui.Button(facilityContent, PixelFont.Fit($"{u.name.ToUpperInvariant()} - {status}", 370), 28, y, 374, () =>
                {
                    if (HubEconomy.TryTempleRevive(Run, u, out string err))
                    {
                        Sfx.Play(Sound.Success);
                        feedbackLabel?.Set(PixelFont.Fit($"REVIVED {u.name.ToUpperInvariant()} AT THE TEMPLE!", 520));
                        RenderFacility();
                    }
                    else
                    {
                        Sfx.Play(Sound.Deny);
                        feedbackLabel?.Set(PixelFont.Fit(err.ToUpperInvariant(), 520));
                    }
                }, 24);
                btn.tooltip = u.Alive ? "HERO IS ALIVE AND HEALTHY" : $"EXPEND {HubEconomy.TempleReviveCost} GOLD TO REVIVE HERO TO 50% HP";
                if (u.Alive) btn.label.Color = Palette.Emerald;
                else btn.label.Color = Palette.SpecCrimson;
                menu.Add(btn);
                y += 32;
            }
        }

        void OnEmbarkClicked()
        {
            if (Run == null) return;
            if (Run.Party.All(u => !u.Alive))
            {
                Sfx.Play(Sound.Deny);
                feedbackLabel?.Set("ALL HEROES ARE DEAD! VISIT THE TEMPLE TO REVIVE AT LEAST ONE HERO BEFORE EMBARKING.");
                return;
            }

            // Pre-embark auto save (Hub lifecycle trigger)
            SaveManager.AutoSave(out _);

            int targetAct = PersistenceCommitHandler.ActiveCommittedSnapshot != null
                ? PersistenceCommitHandler.ActiveCommittedSnapshot.nextDungeonAct
                : Run.Act;

            if (targetAct > 8)
            {
                Game.Go(GameState.Victory);
                return;
            }

            if (Run.Map == null || Run.Map.act < targetAct)
            {
                var nextMap = MapGenerator.Generate(Run.Seed + targetAct, targetAct);
                Run.AdvanceToNextDungeon(nextMap);
            }
            Game.Go(GameState.Map);
        }

        void OnTitleClicked()
        {
            new ConfirmationModal(Root, "RETURN TO TITLE",
                "Return to the Main Menu?\nEnsure your progress is saved before exiting the Hub.",
                "RETURN TO TITLE",
                () => Game.Go(GameState.MainMenu));
        }
    }
}
