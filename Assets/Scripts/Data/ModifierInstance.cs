using System;

namespace Obscure
{
    // One active stack of a Dungeon Modifier inside a run (GDD Bab 15 §3.1, §4.1, §6.1).
    // Each stack tracks its own remaining duration independently and does not refresh older stacks.
    [Serializable]
    public class DungeonModifierInstance
    {
        public string instanceId;
        public string modifierId;
        public int remainingDuration;
        public ElementType element = ElementType.None;
        public bool triggered;
        public int acquiredOrder;

        [NonSerialized]
        private DungeonModifierDef _localDef;

        public DungeonModifierDef Definition =>
            (!string.IsNullOrEmpty(modifierId) && Database.DungeonModifiers.TryGetValue(modifierId, out var def))
                ? def
                : _localDef;

        public DungeonModifierInstance() { }

        public DungeonModifierInstance(DungeonModifierDef def, int remainingDuration, ElementType element = ElementType.None, int acquiredOrder = 0)
        {
            this.instanceId = Guid.NewGuid().ToString("N");
            this.modifierId = def?.modifierId ?? string.Empty;
            this.remainingDuration = remainingDuration;
            this.element = element;
            this.acquiredOrder = acquiredOrder;
            this._localDef = def;
        }

        public DungeonModifierSaveData ToSaveData() => new DungeonModifierSaveData
        {
            instanceId = instanceId,
            modifierId = modifierId,
            remainingDuration = remainingDuration,
            element = (int)element,
            triggered = triggered,
            acquiredOrder = acquiredOrder
        };

        public static DungeonModifierInstance FromSaveData(DungeonModifierSaveData data)
        {
            if (data == null || string.IsNullOrEmpty(data.modifierId)) return null;
            return new DungeonModifierInstance
            {
                instanceId = string.IsNullOrEmpty(data.instanceId) ? Guid.NewGuid().ToString("N") : data.instanceId,
                modifierId = data.modifierId,
                remainingDuration = data.remainingDuration,
                element = Enum.IsDefined(typeof(ElementType), data.element) ? (ElementType)data.element : ElementType.None,
                triggered = data.triggered,
                acquiredOrder = data.acquiredOrder
            };
        }
    }

    [Serializable]
    public class DungeonModifierSaveData
    {
        public string instanceId;
        public string modifierId;
        public int remainingDuration;
        public int element;
        public bool triggered;
        public int acquiredOrder;
    }
}
