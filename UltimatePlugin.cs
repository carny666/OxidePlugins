using Oxide.Core;
using System;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{

    [Info("UltimatePlugin", "CARNY666", "1.1.5")]
	class UltimatePlugin : RustPlugin
	{
        private const string ServerName = "DAYTIMEFRIENDLY";

        private const float CollectiblePickupMultiplier = 3;
        private const float DispenserGatherMultiplier = 3;
        private const float HarvestMultiplier = 3;

        //private bool InstantCraft = false;
        private bool AutoLights = false;
        private bool BetterLoot = false;
        private bool TimeAnnouncments = false;
        private bool SmeltRateEnabled = false;

        private const int StackSize = 10000;   
        private const int LootMultiplier = 1;
        private const int LootMaxSlots = 10;
        private const int SmeltingRate = 10; // per slot
        private const int maxFoodItems = 4;
        private const int maxCrateItems = 7;
        private const int MaxRareItems = 4;
        private const int maxTrashItems = 4;
        private const int PercentageChanceRareItem = 10;

        private int MultiplyerMultiplier = 1; 

        private bool chickenGun = false;
        private string[] mods = { "reboot", "buuuford", "carny666" }; // lowercase

        int lastHour = 0;
        //int lastPlayerCount = 0;
        DateTime lastspawn;

        #region events
        void Init()
		{
			try
			{
                PrintToMods($"UltimatePlugin {Version.ToString()} initializing...");
                lastspawn = DateTime.Now;
                if (StackSize > 0)
                    InitStackSizes();

                if (TimeAnnouncments)
                    TimeAnnouncementInit();

                if (AutoLights)
                    AutoLightingInit();

                PrintToMods($"UltimatePlugin {Version.ToString()} initialized");
			}
			catch (Exception ex)
			{
				PrintToMods(ex.StackTrace);
			}
		}

        void OnServerInitialized()
        {
            if (StackSize > 0)
                InitStackSizes();
        }

        #endregion

        #region stacksize adjustments
        void InitStackSizes() {
            try
            {
                var itemList = ItemManager.itemList;
                foreach (var item in itemList)
                {
                    if (item.condition.enabled && item.condition.max > 0) { continue; }
                    item.stackable = StackSize;
                }
            }
            catch (Exception ex)
            {
                PrintToMods("error int InitStackSizes" + ex.Message);
            }

        }
        #endregion

        #region time announce 
        void TimeAnnouncementInit()
        {
            PrintToMods("TimeAnnouncement Initialized.");            

            timer.Repeat(30f, 0, () =>
            {
                var hour = TimeSpan.FromHours(ConVar.Env.time).Hours;
                if (hour != lastHour)
                {
                    var est = DateTime.Now.AddHours(5);
                    lastHour = hour;
                    var t = new DateTime(1978, 1, 1, TimeSpan.FromHours(ConVar.Env.time).Hours, TimeSpan.FromHours(ConVar.Env.time).Minutes, TimeSpan.FromHours(ConVar.Env.time).Seconds);
                    PrintToChat($"<color=#007700>Rust Time is {t.ToString("hh:mm tt")}</color>");
                }
            });
        }
        #endregion
        
        #region control ovens and lights
        void AutoLightingInit()
        {
            timer.Repeat(30f, 0, () =>
            {
                try
                {
                    string[] lighting = { "lantern", "searchlight", "tunalight", "ceilinglight" };

                    var hour = TimeSpan.FromHours(ConVar.Env.time).Hours;

                    if (hour >= 6 && hour < 19) // daytime.. 6am and 7pm 
                    {
                        MultiplyerMultiplier = 1;                        

                        foreach (var entry in GameObject.FindObjectsOfType<BaseOven>())
                            if (lighting.Any(entry.PrefabName.Contains))
                                entry.SetFlag(BaseEntity.Flags.On, false);
                    }
                    else                        // nighttime..
                    {
                        MultiplyerMultiplier = 2;
                        foreach (var entry in GameObject.FindObjectsOfType<BaseOven>())
                            if (lighting.Any(entry.PrefabName.Contains))
                                entry.SetFlag(BaseEntity.Flags.On, true);

                    }
                } catch(Exception ex)
                {
                    PrintToMods("AutoLightingInit " + ex.Message);
                }
               
            });

        }
        #endregion

        #region gather multiplying
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            var multiplier = DispenserGatherMultiplier * MultiplyerMultiplier;
            string[] neverMultiplyTheseItems =
            {                
                "Human Skull",
                "Raw Human Meat",
                "Raw Deer Meat",
                "Raw Pork",
                "Raw Wolf Meat",
                "Raw Chicken",
                "Bear Meat",
                "Animal Fat",
                "Bone Fragments",
                "Cloth",
                "Leather"
            };

            if (!entity.ToPlayer()) return;
            if (neverMultiplyTheseItems.Contains(item.info.displayName.english)) multiplier = 1;

            if (multiplier > 0)
                item.amount = (int)(item.amount * multiplier);                
            
        }

        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            var multiplier = CollectiblePickupMultiplier * MultiplyerMultiplier;

            string[] itemNames = 
                {
                    "cloth",
                    "seed.hemp",                    
                    "mushroom",                    
                    "pumpkin",
                    "seed.pumpkin",
                    "corn",
                    "seed.corn",
                    "stones",
                    "metal.ore",
                    "sulfur.ore",
                };

            if (item.ToString().Contains("pumpkin"))
                return; // multiplier = CollectiblePickupMultiplier / CollectiblePickupMultiplier;
            if (item.ToString().Contains("mushroom"))
                return; //multiplier = CollectiblePickupMultiplier / CollectiblePickupMultiplier;
            if (item.ToString().Contains("corn"))
                return; //multiplier = CollectiblePickupMultiplier / CollectiblePickupMultiplier;
            if (item.ToString().Contains("cloth"))
                return; //multiplier = CollectiblePickupMultiplier / 4;
            if (!item.ToString().Contains("seed"))
                return; //multiplier = CollectiblePickupMultiplier / 8;

            if (multiplier > 0)
                item.amount = (int)(item.amount * multiplier);

            //PrintToDeveloper($"OnCollectiblePickup {item.ToString()} x{multiplier}");
        }

        void OnCropGather(PlantEntity plant, Item item, BasePlayer player)
        {
            var actMultiplier = HarvestMultiplier * MultiplyerMultiplier;

            if (actMultiplier > 0)
                item.amount = (int)(item.amount * actMultiplier);

            //PrintToMods($"OnCropGather {item.ToString()} x{actMultiplier}");
            //TODO: Change crop multip.
        }
        #endregion

        #region higher consumption/output rates (unlimited light fuel)
        void OnConsumeFuel(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {

            #region unlim light fuel
                string[] lighting = { "lantern", "searchlight", "tunalight", "ceilinglight" };

            // unlimited lighting
            if (lighting.Any(oven.PrefabName.Contains) && AutoLights)
            {
                //PrintToDeveloper($"{oven.PrefabName} fueled?");
                fuel.amount = 2;
                return;
            }
            #endregion


            if (!SmeltRateEnabled) return;

            var consumption = SmeltingRate;

            if (fuel.amount < consumption)
                consumption = fuel.amount;

            fuel.amount = fuel.amount - consumption;

            burnable.byproductAmount = 1 * (int)consumption;
            burnable.byproductChance = 0f;

            for (var i = 0; i < oven.inventorySlots; i++)
            {
                try
                {
                    // Check for and ignore invalid items
                    var slotItem = oven.inventory.GetSlot(i);
                    if (slotItem == null || !slotItem.IsValid()) continue;

                    // Check for and ignore non-cookables
                    var cookable = slotItem.info.GetComponent<ItemModCookable>();
                    if (cookable == null) continue;

                    // Skip already cooked food items
                    if (slotItem.info.shortname.EndsWith(".cooked")) continue;

                    // Check how many are actually in the furnace, before we try removing too many
                    var inFurnaceAmount = slotItem.amount;
                    if (inFurnaceAmount < consumption) consumption = inFurnaceAmount;

                    // Set consumption to however many we can pull from this actual stack
                    consumption = TakeFromInventorySlot(oven.inventory, slotItem.info.itemid, SmeltingRate, i);

                    // If we took nothing, then... we can't create any
                    if (consumption <= 0) continue;

                    // Create the item(s) that are now cooked
                    var cookedItem = ItemManager.Create(cookable.becomeOnCooked, cookable.amountOfBecome * SmeltingRate);
                    if (!cookedItem.MoveToContainer(oven.inventory)) cookedItem.Drop(oven.inventory.dropPosition, oven.inventory.dropVelocity);
                }
                catch (InvalidOperationException) { }


            }
        }

        #endregion

        #region welcome message    
        void OnPlayerInit(BasePlayer player)
        {
            //WelcomePlayer(player);
        }

        void OnPlayerRespawned(BasePlayer player)
        {
           //WelcomePlayer(player);
        }

        void WelcomePlayer(BasePlayer player)
        {
            string[] welcomeMessage = {
                    $"Welcome to {ServerName}.",
                    $"Dispenser gather rate is x{DispenserGatherMultiplier * MultiplyerMultiplier}. It doubles at night.",
                    $"Collectable pickup rate is x{CollectiblePickupMultiplier * MultiplyerMultiplier}.",
                    $"Harvest rate is x{HarvestMultiplier * MultiplyerMultiplier}.",
                    $"Stacksize is @ {StackSize} (for most items).",
                    //$" {(InstantCraft ? "Instant Crafting is enabled." : "")}",
                    $" {(AutoLights ? "Auto Lighting is enabled." : "")}",
                    $" {(BetterLoot ? $"Better Loot is enabled @ x{LootMultiplier}." : "")}",
                    $" {(TimeAnnouncments ? $"Time Announcements are enabled." : "")}",
                    $" {(SmeltRateEnabled ? $"Smelting rate is enabled @ x{SmeltingRate}." : "")}"
            };

            if (StackSize > 0)
                InitStackSizes();

            //if (!WelcomeMessage) return;

            var hour = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, TimeSpan.FromHours(ConVar.Env.time).Hours, 0, 0).ToString("h tt");
            foreach (var s in welcomeMessage)
                PrintToChat(player, s);
        }

        #endregion

        #region instant crafting
        //object OnItemCraft(ItemCraftTask task, BasePlayer crafter)
        //{
        //    // Instant Craft
        //    if (InstantCraft)
        //    {
        //        task.endTime = 1f;

        //        if (task.amount > 1)
        //        {
        //            var finalamount = task.amount * task.blueprint.amountToCreate;
        //            crafter.GiveItem(ItemManager.CreateByItemID(task.blueprint.targetItem.itemid, finalamount, (ulong)task.skinID));
        //            task.cancelled = true;
        //        }
                
        //        // Instant Craft
        //    }
        //    else
        //        return null;
        //}
        #endregion

        #region better loot
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!BetterLoot) return;

            if (entity == null) return;
            var container = entity as LootContainer;
            if (container == null) return;

            container.minSecondsBetweenRefresh = -1;
            container.maxSecondsBetweenRefresh = 0;
            container.CancelInvoke("SpawnLoot");

            if (entity.PrefabName.Contains("oil")) return;
            if (entity.PrefabName.Contains("supply")) return;
            if (entity.PrefabName.Contains("crate_mine")) return;
            if (entity.PrefabName.Contains("minecart")) return;

            NextTick(() =>
            {
                try
                {
                    if (entity == null) return;
                    var lootContainer = entity as LootContainer;
                    if (lootContainer == null) return;
                    CreateRandomLootInBoxes(lootContainer);

                }
                catch (Exception ex)
                {
                    PrintToMods("error in NextTick: " + ex.Message);
                }
            });
        }

        void CreateRandomLootInBoxes(StorageContainer entity)
		{
            #region prefab names
            string[] trash = {
                "wolfmeat.spoiled",
                "chicken.spoiled",
                "apple.spoiled",
                "can.tuna.empty",
                "can.beans.empty",
                "techparts",
                "cctv.camera",
                "targeting.computer"
            };

            string[] gunBody = { "smgbody", "riflebody", "semibody" };

            string[] food = {
                "meat.boar",
                "meat.pork.cooked",
                "meat.pork.burned",
                "techparts",

                "wolfmeat.raw",
                "wolfmeat.cooked",
                "wolfmeat.burned",
                "wolfmeat.spoiled",

                "bearmeat",
                "bearmeat.cooked",
                "bearmeat.burned",

                "chicken.raw",
                "chicken.cooked",
                "chicken.burned",
                "chicken.spoiled",

                "deermeat.raw",
                "deermeat.cooked",
                "deermeat.burned",

                "fish.minnows",
                "fish.troutsmall",
                "fish.raw",
                "fish.cooked",

                "granolabar",

                "mushroom",
                "corn",
                "pumpkin",
                "apple",
                "apple.spoiled",
                "black.raspberries",
                "blueberries",
                "cactusflesh",

                "chocholate",

                "can.tuna",
                "can.tuna.empty",
                "can.beans",
                "can.beans.empty",
                "candycane",

                "humanmeat.spoiled"
            };

            string[] singleRare = {
                "techparts",
                "hazmatsuit",
                "techparts",
                "cctv.camera",
                "targeting.computer",
                "axe.salvaged",
                "icepick.salvaged",
                "riflebody",
                "semibody",
                "smgbody",
                "explosive.satchel",
                "explosive.timed",
                "tool.binoculars"
            };

            string[] components = {
                "semibody",
                "metalpipe",
				"gears",
				"tarp",
				"sheetmetal",
				"propanetank",
				"metalblade",
				"metalspring",
				"roadsigns",
				"sewingkit",
				"rope",
                "metalpipe",
                "gears",
                "tarp",
                "sheetmetal",
                "propanetank",
                "metalblade",
                "metalspring",
                "roadsigns",
                "sewingkit",
                "rope",
                "techparts",
            };
            #endregion

            try
            {
                if (entity.PrefabName.Contains("oil")) return;
                if (entity.PrefabName.Contains("supply")) return;
                if (entity.PrefabName.Contains("crate_mine")) return;

                for (int ii = 0; ii < Oxide.Core.Random.Range(3, LootMaxSlots); ii++)
                {
                    var itemCount = Oxide.Core.Random.Range(1, maxCrateItems);
                    var randomComp = components[Oxide.Core.Random.Range(1, components.Length - 1)];

                    if (entity.PrefabName.Contains("foodbox"))
                    {
                        itemCount = Oxide.Core.Random.Range(1, maxFoodItems);
                        randomComp = food[Oxide.Core.Random.Range(1, food.Length - 1)];
                    }

                    if (entity.PrefabName.Contains("loot_trash") || entity.PrefabName.Contains("trash-pile"))
                    {
                        itemCount = Oxide.Core.Random.Range(1, maxTrashItems);
                        randomComp = trash[Oxide.Core.Random.Range(1, trash.Length - 1)];
                    }

                    if (entity.PrefabName.Contains("crate_normal") && (Oxide.Core.Random.Range(1, 100) < PercentageChanceRareItem))
                    {
                        itemCount = MaxRareItems;
                        randomComp = singleRare[Oxide.Core.Random.Range(1, singleRare.Length - 1)];
                    }

                    if (entity.PrefabName.Contains("crate_tools") && (Oxide.Core.Random.Range(1, 100) < PercentageChanceRareItem))
                    {
                        itemCount = MaxRareItems;
                        randomComp = singleRare[Oxide.Core.Random.Range(1, singleRare.Length - 1)];
                    }

                    var i = ItemManager.FindItemDefinition(randomComp);

                    if (i == null)
                    {
                        PrintToMods($"{randomComp} doesn't exit or we cannot FindItemDefinition.");
                        return;
                    }

                    entity.inventory.AddItem(ItemManager.FindItemDefinition("scrap"), Oxide.Core.Random.Range(100));
                    entity.inventory.AddItem(i, itemCount);

                    if (entity.PrefabName.Contains("barrel")) return; // don't show
                    if (entity.PrefabName.Contains("crate_normal")) return; // don't show
                    if (entity.PrefabName.Contains("crate_tools")) return; // don't show
                    if (entity.PrefabName.Contains("foodbox")) return; // don't show
                    if (entity.PrefabName.Contains("loot_trash")) return;
                    if (entity.PrefabName.Contains("trash-pile")) return;

                    PrintToMods($"{randomComp} added to {entity.PrefabName}");
                }
            }
			catch (Exception ex)
			{
				PrintToMods($"ERROR: {this.Title}: CreateRandomLootInBoxes: {ex.Message}");
			}


		}
        #endregion

        #region common stuff
        Item makeItem(string shortname, int amount)
        {
            try
            {
                var definition = ItemManager.FindItemDefinition(shortname);
                if (definition != null)
                {
                    Item item = ItemManager.CreateByItemID((int)definition.itemid, amount, 0);
                    return item;
                }
                PrintToMods($"WARNING: {this.Title}: makeItem: Error making item {shortname}");
                return null;
            }
            catch (Exception ex)
            {
                PrintToMods($"ERROR: {this.Title}: makeItem: Error making item {shortname} {ex.Message}");
                return null;
            }
        }

        Vector3 GetGroundPosition(Vector3 sourcePos)
        {
            RaycastHit hitInfo;

            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo, LayerMask.GetMask("Terrain", "World", "Construction")))
            {
                sourcePos.y = hitInfo.point.y;
            }
            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
            return sourcePos;
        }

        int TestAccess(BasePlayer player, string command)
        {
            var retval = 1;
            var Developer = "carny666";
            if (!mods.Contains(player.displayName.ToLower()))
            {
                PrintToMods($"{player.displayName} has attempted the {command} command.");
                if (player.displayName.ToLower() == Developer)
                    retval = 2;
                else
                    retval = 0;
            }
            return retval;
        }
        
        void PrintToMods(object Message)
        {
            foreach (var m in mods) {
                var c = BasePlayer.activePlayerList.Find(x => x.displayName.ToLower().Equals(m.ToLower()));
                if (c != null)
                    PrintToChat(c, Message.ToString());
            }
            PrintWarning(Message.ToString());
        }

        Vector3 LerpByDistance(Vector3 A, Vector3 B, float x)
        {
            Vector3 P = x * Vector3.Normalize(B - A) + A;
            return P;
        }

        BaseEntity spawnAnimal(string animal, Vector3 position, BaseEntity target, Vector3 velocity)
        {
            string[] possibleAnimalPrefabs = {
                "assets/rust.ai/agents/bear/bear.prefab",
                "assets/rust.ai/agents/boar/boar.prefab",
                "assets/rust.ai/agents/chicken/chicken.prefab",
                "assets/rust.ai/agents/horse/horse.prefab",
                "assets/rust.ai/agents/stag/stag.prefab",
                "assets/rust.ai/agents/wolf/wolf.prefab",
                "assets/rust.ai/agents/zombie/zombie.prefab"
            };

            try
            {
                var prefabName = possibleAnimalPrefabs.Where(x => x.Contains(animal)).First();
                PrintToMods($"Spawning a {prefabName}");
                BaseEntity entity = GameManager.server.CreateEntity(prefabName, GetGroundPosition(position));
                if (entity == null)
                {
                    PrintToMods($"Error in spawnAnimal {prefabName}");
                    return null;
                }

                // have entity face target..
                entity.transform.LookAt(target.transform);
                //entity.name = prefabName;
                entity.enabled = true;
                entity.Spawn();
                entity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

                return entity;
            }
            catch (Exception ex)
            {
                PrintToMods("Error in spawnAnimal " + ex.Message);
                return null;
            }
        }

        int TakeFromInventorySlot(ItemContainer container, int itemId, int amount, int slot)
        {
            var item = container.GetSlot(slot);
            if (item.info.itemid != itemId) return 0;

            if (item.amount > amount)
            {
                item.MarkDirty();
                item.amount -= amount;
                return amount;
            }

            amount = item.amount;
            item.RemoveFromContainer();
            return amount;
        }

        BaseEntity SpawnAirdrop(Vector3 position)
        {
            BaseEntity planeEntity = GameManager.server.CreateEntity("assets/prefabs/npc/cargo plane/cargo_plane.prefab", new Vector3(), new Quaternion(1f, 0f, 0f, 0f));

            if (planeEntity != null)
            {
                CargoPlane plane = planeEntity.GetComponent<CargoPlane>();
                plane.InitDropPosition(position);
                planeEntity.Spawn();
            }
            return planeEntity;
        }

        void test()
        {
            var spawns = Resources.FindObjectsOfTypeAll<LootContainer>()
                .Where(c => c.isActiveAndEnabled).
                OrderBy(c => c.transform.position.x).ThenBy(c => c.transform.position.z).ThenBy(c => c.transform.position.z)
                .ToList();

        }

        void killLootableAroundLocation(Vector3 location)
        {
            var spawns = Resources.FindObjectsOfTypeAll<LootContainer>()
                .Where(c => c.isActiveAndEnabled).
                OrderBy(c => c.transform.position.x).ThenBy(c => c.transform.position.z).ThenBy(c => c.transform.position.z)
                .ToList();

            foreach (var ent in spawns)
            {
                if (Vector3.Distance(location, ent.transform.position)  <= 200) 
                    ent.Kill(BaseNetworkable.DestroyMode.None);
            }

        }

        #endregion
    }
}
