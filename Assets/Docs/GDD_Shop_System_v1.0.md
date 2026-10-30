**GAME DESIGN DOCUMENT  |  SHOP SYSTEM** 

# **GAME DESIGN DOCUMENT SHOP SYSTEM** 

Dungeon Shop • Hub Facilities • Gold Economy • Character Revive 

|**Document Metadata**|**Value**|
|---|---|
|Version|1.0<br>i   i|
|Status|Design draft — confirmed rules captured; unconfirmed<br>details marked TBD|
|System Scope|Dungeon Shop, Hub shops, Gold transfer, character death<br>and revive|
|Related Systems|Dungeon progression, party management, equipment, skills,<br>save/run state|



Shop System v1.0  •  Draft for design alignment 

**GAME DESIGN DOCUMENT  |  SHOP SYSTEM** 

## **1. Overview** 

The game contains two shop contexts: a Dungeon Shop available during a dungeon run and a set of specialized shops/facilities located in the Hub. The Dungeon Shop supports survival and tactical decisions during the current run. Hub facilities support between-run preparation, character growth, and recovery of fallen characters. 

Character death is individual rather than immediately run-ending. A run can continue while at least one party character remains alive. The run fails when all party characters are dead. 

## **2. Design Goals** 

- Give Dungeon Shop purchases meaningful risk because Run Gold is lost on a failed run. 

- Reward successful dungeon completion by carrying all remaining Run Gold into the Hub without a conversion penalty. 

- Separate Hub services by purpose so players can quickly identify where to shop, upgrade, train, or revive. 

- Make character death consequential without ending a run after the first fallen character. 

- Create a clear recovery path for dead characters through the rare Dungeon revive item or the Hub Temple. 

## **3. Shop Types** 

### **3.1 Dungeon Shop** 

The Dungeon Shop is encountered inside a dungeon run. It uses Run Gold and is intended to help the party respond to current-run needs. Its exact appearance, placement frequency, inventory size, stock rules, and refresh rules have not yet been defined. 

- May offer consumables. 

- May offer equipment. 

- May offer run-support items. 

- Can offer a rare and expensive revive item. 

### **3.2 Hub Shops and Facilities** 

The Hub contains five distinct facilities. Hub purchases and services use the persistent Hub Gold balance. 

|**Facility Name**|**Primary Function**|**Confirmed Scope**|
|---|---|---|
|Shop|Consumables|Purchase consumable items, including<br>recoveryitems.|
|Armory|Equipment|Purchase equipment for characters.|
|Blacksmith|Equipment upgrades|Upgrade owned equipment.|
|Training Ground|Skills|Manage character skills and upgrade<br>skills.|
|Temple|Revive|Revive characters who are dead.|



## **4. Gold Economy** 

### **4.1 Gold Balances** 

|**Balance**|**Description**|**Persistence**|
|---|---|---|
||Gold acquired and held during the||
|Run Gold|current dungeon run; spent at Dungeon<br>Shop.|Temporary; cleared on run failure.|



Shop System v1.0  •  Draft for design alignment 

Hub Gold 

||**GAME DESIGN DOCUMENT  |  SHOP SYSTEM**|
|---|---|
|Gold secured by completing a dungeon;|Persistent between runs, subject to the|
|spent at Hub facilities.|game's overall reset rules.|



### **4.2 Successful Dungeon Completion** 

- All remaining Run Gold is transferred to Hub Gold. 

- The transfer is 1:1; there is no fee, loss, or conversion deduction. 

- The character party may return to the Hub with one or more characters dead; their status remains dead until revived. 

### **4.3 Failed Run** 

- All Run Gold remaining at the time of failure is lost. 

- Previously secured Hub Gold is not affected by the Run Gold loss. 

- The run failure and dungeon reset follow the game's established run-failure rules. 

### **4.4 Example** 

The party earns 500 Run Gold and spends 150 at the Dungeon Shop. If the dungeon is completed, the remaining 350 Gold is transferred in full to Hub Gold. If the entire party dies before completion, the remaining 350 Run Gold is lost. 

## **5. Character Death and Run Failure** 

### **5.1 Individual Death** 

When a character dies, that character cannot participate in combat while dead. The rest of the party may continue the dungeon as long as at least one character is alive. 

|**Party State**|**Run Result**|
|---|---|
|One character is dead;at least one is alive|Run continues.|
|Multiple characters are dead;at least one is alive|Run continues.|
|Allpartycharacters are dead|Run fails.|



### **5.2 Run Success Condition** 

The player must complete the dungeon while keeping at least one party character alive. A character who is dead at the end of a successful run remains dead when the party returns to the Hub. 

### **5.3 Run Failure Condition** 

If every party character dies during the dungeon run, the run is considered failed. Under the game's established rule, the player returns to the Main Menu and the dungeon progression/run is reset; a new game is required to begin again. Run Gold is lost. 

## **6. Revive System** 

### **6.1 Dungeon Revive Item** 

A character can be revived during a run by using a special revive item. The item is defined as rare and expensive, and it is available for purchase only through the Dungeon Shop when stocked. 

Shop System v1.0  •  Draft for design alignment 

**GAME DESIGN DOCUMENT  |  SHOP SYSTEM** 

- The item revives a dead character so that they can return to combat during the current run. 

- Its exact item name, price, stock quantity, drop sources, and use restrictions are TBD. 

### **6.2 Hub Revive — Temple** 

The Temple provides a service to revive dead characters after a successful return to the Hub. This allows players to restore their party without needing to spend a rare revive item during the dungeon. 

The Temple's exact cost, requirements, revival presentation, and any limits are TBD. 

## **7. Player Flow** 

1. Prepare the party in the Hub using Shop, Armory, Blacksmith, Training Ground, and Temple as needed. 

2. Enter a dungeon; the run begins with its Run Gold balance. 

3. Acquire and spend Run Gold at Dungeon Shop(s), including the option to purchase a rare revive item. 

4. Continue through the dungeon while at least one party character remains alive. 

5. If the dungeon is completed, transfer all remaining Run Gold to Hub Gold and return to the Hub. 

6. Use Hub facilities to buy, upgrade, manage skills, and revive dead characters before the next run. 

7. If all party characters die, the run fails and remaining Run Gold is lost; apply the game's run reset rules. 

## **8. Confirmed Rules and Open Design Items** 

### **8.1 Confirmed** 

- There are two shop contexts: Dungeon Shop and Hub shops. 

- Hub facilities are named Shop, Armory, Blacksmith, Training Ground, and Temple. 

- Run Gold is temporary; all remaining Run Gold transfers to Hub Gold after successful dungeon completion. 

- Run Gold is lost if the run fails. 

- A character's death does not end the run if at least one party character remains alive. 

- The run fails when all party characters are dead. 

- A rare, expensive revive item is purchasable at the Dungeon Shop. 

- Dead characters can be revived at the Hub's Temple. 

### **8.2 TBD — Not Yet Defined** 

- Dungeon Shop appearance frequency, placement, inventory size, stock, and refresh behavior. 

- Consumable and equipment inventory lists, prices, and purchase limits. 

- Revive item name, price, stock, and any target/use restrictions. 

- Temple revive price, requirements, and possible restrictions. 

- Blacksmith upgrade materials, costs, levels, and success/failure rules. 

- Training Ground skill management and upgrade costs/rules. 

- Whether Hub Gold persists across all new-game/reset cases beyond the currently established run-failure handling. 

## **9. Terminology** 

|**Term**|**Meaning**|
|---|---|
|Dungeon Shop|Shopencountered duringa dungeon run.|
|Hub|Town/base area between dungeon runs.|
|Run Gold|TemporaryGold held duringa dungeon run.|
|Hub Gold|Gold secured after successful dungeon completion.|



Shop System v1.0  •  Draft for design alignment 

||**GAME DESIGN DOCUMENT  |  SHOP SYSTEM**|
|---|---|
|Revive|Restore a dead character to a living/active state.|
|Dead|Character state in which the character cannot participate in<br>combat until revived.|
|Run Failure|Allpartycharacters are dead.|



_End of document_ 

Shop System v1.0  •  Draft for design alignment 

