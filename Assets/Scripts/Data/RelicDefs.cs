using System;

namespace Obscure
{
    public enum RelicCategory
    {
        Offensive,
        Defensive,
        Resource,
        Survival,
        Utility,
        Economy,
        Exploration,
        Break,
        Progression,
        Hybrid
    }

    public enum RelicProposedRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic
    }

    public enum RelicTriggerType
    {
        Combat,
        Qte,
        PostBattle,
        Boss,
        Consumable,
        Reward,
        Exploration,
        Event,
        Map
    }

    public enum RelicActivationLimit
    {
        Passive,
        Counter,
        OncePerBattle,
        OncePerDungeon,
        CapPerBattle,
        Conditional,
        StackTrigger
    }
}

