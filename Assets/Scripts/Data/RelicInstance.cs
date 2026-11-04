using System;

namespace Obscure
{
    [Serializable]
    public class RelicInstance
    {
        public string relicId;
        public int counter;                  // e.g. R002 QTE counter, R022 kill counter
        public bool battleTriggered;         // e.g. R009, R010, R020 once per battle
        public int battleUsageCount;         // e.g. R013 max 3 MP per battle
        public bool dungeonTriggered;        // e.g. R014 max 1 minor trap per dungeon
        public bool turnTriggered;           // e.g. R019 max once per character turn
        public bool isBattleBuffActive;      // e.g. R022 +4% damage once 5 kills reached

        [NonSerialized]
        private RelicDef _localDef;

        public RelicDef Definition =>
            (!string.IsNullOrEmpty(relicId) && Database.Relics.TryGetValue(relicId, out var def))
                ? def
                : _localDef;

        public RelicInstance() { }

        public RelicInstance(string relicId)
        {
            this.relicId = relicId;
        }

        public RelicInstance(RelicDef def)
        {
            this.relicId = def?.id ?? "";
            this._localDef = def;
        }

        public void ResetBattleState()
        {
            battleTriggered = false;
            battleUsageCount = 0;
            turnTriggered = false;
            isBattleBuffActive = false;
            if (relicId == "R022") counter = 0; // kill counter is per-battle
        }

        public void ResetDungeonState()
        {
            dungeonTriggered = false;
        }

        public void ResetTurnState()
        {
            turnTriggered = false;
        }

        public RelicInstanceSaveData ToSaveData() => new RelicInstanceSaveData
        {
            relicId = relicId,
            counter = counter,
            battleTriggered = battleTriggered,
            battleUsageCount = battleUsageCount,
            dungeonTriggered = dungeonTriggered,
            turnTriggered = turnTriggered,
            isBattleBuffActive = isBattleBuffActive
        };

        public static RelicInstance FromSaveData(RelicInstanceSaveData data)
        {
            if (data == null || string.IsNullOrEmpty(data.relicId)) return null;
            return new RelicInstance
            {
                relicId = data.relicId,
                counter = data.counter,
                battleTriggered = data.battleTriggered,
                battleUsageCount = data.battleUsageCount,
                dungeonTriggered = data.dungeonTriggered,
                turnTriggered = data.turnTriggered,
                isBattleBuffActive = data.isBattleBuffActive
            };
        }
    }

    [Serializable]
    public class RelicInstanceSaveData
    {
        public string relicId;
        public int counter;
        public bool battleTriggered;
        public int battleUsageCount;
        public bool dungeonTriggered;
        public bool turnTriggered;
        public bool isBattleBuffActive;
    }
}

