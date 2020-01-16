using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using System;
using UnityEngine;

namespace Oxide.Plugins
{

    [Info("CarnysMod", "CARNY666", "1.0.1")]
	class CarnysMod : RustPlugin
	{

        const string usePermission = "CarnysMod.use";

        private const string ServerName = "server name not defined.";
        private bool chickenGun = false;
        private bool TimeChangeAsked = false;

        private bool fastNight = false;

        private List<BuildingBlock> spawnedBuildingBlocks = new List<BuildingBlock>();
        private List<BaseEntity> spawnedEntities = new List<BaseEntity>();


        int lastHour = 0;
        int lastPlayerCount = 0;
        DateTime lastspawn;


        BaseEntity body;

        [ChatCommand("xx")]
        void tBaby(BasePlayer player, string command, string[] args)
        {
            if (body != null)
            {
                body.Kill();
                return;
            }

            var prefabName = "assets/rust.ai/agents/npcplayer/npcplayertest.prefab"; // player
            body = GameManager.server.CreateEntity(prefabName);

            body.transform.position = player.transform.position + (player.eyes.BodyForward() * 2);
            body.transform.position = new Vector3(body.transform.position.x, player.transform.position.y, body.transform.position.z);

            PrintToChat(player, $"{body.name} spawned.");
            body.Spawn();
        }

        #region events+
        void Loaded()
        {
            try
            {
                permission.RegisterPermission(usePermission, this);
            }
            catch (Exception ex)
            {
                PrintError($"Error in Loaded, {ex.Message}");
            }

        }

        void Unload()
        {
            foreach (var se in spawnedEntities)
                se.Kill(BaseNetworkable.DestroyMode.None);

            foreach (var sbb in spawnedBuildingBlocks)
                sbb.Kill(BaseNetworkable.DestroyMode.None);

        }

        void Init()
		{
			try
			{
                PrintWarning($"CarnysMod {Version.ToString()} initializing...");
                lastspawn = DateTime.Now;
			}
			catch (Exception ex)
			{
                PrintError(ex.StackTrace);
			}
		}

        #endregion

        #region airdrops
        void InitializeAirDropForPlayer(BasePlayer player)
        {
            timer.Once(500f, () =>
            {
                try
                {
                    int supplyId = -1625468793;
                    Item item = ItemManager.CreateByItemID(supplyId, 1, 0);

                    if (player.inventory.FindItemIDs(supplyId).Count() < 1)
                    {
                        if (!player.inventory.containerMain.IsFull())
                        {
                            player.inventory.GiveItem(item, player.inventory.containerMain);
                            PrintToChat(player, $"<color=#00AA00>Complimentary airdrop signal for ya!</color>");
                        }
                    }

                }
                catch (Exception ex)
                {
                    PrintError("error in InitializeAirDropForPlayer. " + ex.Message);
                }
            });
        }

        #endregion

        [ChatCommand("helpcm")]
        private void helpus(BasePlayer player, string command, string[] args)
        {
            if (!CheckAccess(player, "helpcm")) return;
            PrintToChat(player, "copter - Spawns a minicopter behind you, provides some lgf.");
            PrintToChat(player, "spawna [thing] - things = barrel, rec, sci, tree1, tree2, sentry, rhib, rowboat, sedan, copter, body, ch47, pat");
            PrintToChat(player, "realremove - removes items, really.");
            PrintToChat(player, "holding - what am i holding?");
            PrintToChat(player, "zeroloot - destroy ALL lootables.");
            PrintToChat(player, "animal [animal] - spawns animal.");
            PrintToChat(player, "players - See players.");
            PrintToChat(player, "sleepers - See sleepers.");
            PrintToChat(player, "whatis - What am i holding?.");
                
        }



        #region admin commands

        [ChatCommand("copter")]
        private void copter(BasePlayer player, string command, string[] args)
        {

            if (!CheckAccess(player, "copter")) return;
            var p = "assets/content/vehicles/minicopter/minicopter.entity.prefab";

            try
            {
                var _entityName = p;

                var _woodenBox = GameManager.server.CreateEntity(_entityName, player.transform.position + (player.eyes.BodyForward() * 3));
                _woodenBox.Spawn();

                var addonDef = ItemManager.FindItemDefinition("lowgradefuel");
                var item = ItemManager.CreateByItemID((int)addonDef.itemid, 500, 0);
                player.inventory.GiveItem(item, player.inventory.containerMain);

                
                //addonDef = ItemManager.FindItemDefinition("hazmatsuit");
                //item = ItemManager.CreateByItemID((int)addonDef.itemid, 1, 0);
                //player.inventory.GiveItem(item, player.inventory.containerWear);


                //return _woodenBox;
            }
            catch (Exception ex)
            {
                PrintError($"ERROR: Error creating entity. {ex.Message}");
                //return null;
            }

        }

        [ChatCommand("spawna")]
        private void spawna(BasePlayer player, string command, string[] args)
        {
            if (!CheckAccess(player, "spawna")) return;

            if (args.Length < 1) 
            {
                PrintToChat(player, $"Try: /{command} ");
                foreach (var s in new string[] { "barrel", "rec", "sci", "tree1", "tree2", "sentry", "rhib", "rowboat", "sedan", "copter", "body", "ch47", "pat", "wolf", "bear" })
                    PrintToChat(player, s);

                foreach (var se in spawnedEntities)
                    se.Kill(BaseNetworkable.DestroyMode.None);

                return;
            }

            var prefabName = "";

            switch (args[0].ToLower())
            {
                case "rhib":
                    prefabName = "assets/content/vehicles/boats/rhib/rhib.prefab";
                    break;
                case "rowboat":
                    prefabName = "assets/content/vehicles/boats/rowboat/rowboat.prefab";
                    break;
                case "copter":
                    prefabName = "assets/content/vehicles/minicopter/minicopter.entity.prefab";
                    break;
                case "sedan":
                    prefabName = "assets/content/vehicles/sedan_a/sedantest.entity.prefab";
                    break;
                case "rec":
                    prefabName = "assets/bundled/prefabs/static/recycler_static.prefab";
                    break;
                case "sci":
                    prefabName = "assets/prefabs/npc/scientist/scientist.prefab";
                    break;
                case "tree1":
                    prefabName = "assets/bundled/prefabs/autospawn/resource/v2_temp_field_large/american_beech_a.prefab";
                    break;
                case "tree2":
                    prefabName = "assets/bundled/prefabs/autospawn/resource/v2_temp_forest/pine_c.prefab";
                    break;
                case "tree3":
                    prefabName = "assets/bundled/prefabs/autospawn/resource/v2_temp_forest/birch_big_temp.prefab";
                    break;
                case "tree4":
                    prefabName = "assets/bundled/prefabs/autospawn/resource/v2_temp_field_small/oak_f.prefab";
                    break;
                case "tree5":
                    prefabName = "assets/bundled/prefabs/autospawn/resource/v2_temp_field_large/oak_b.prefab";
                    break;
                case "sentry":
                    prefabName = "assets/content/props/sentry_scientists/sentry.scientist.static.prefab";
                    break;
                case "body":
                    prefabName = "assets/rust.ai/agents/npcplayer/npcplayertest.prefab";
                    break;
                case "ch47":
                    prefabName = "assets/prefabs/npc/ch47/ch47.entity.prefab";
                    break;
                case "pat":
                    prefabName = "assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab";
                    break;
                case "barrel":
                    prefabName = "assets/bundled/prefabs/autospawn/resource/loot/loot-barrel-1.prefab";
                    break;
                case "wolf":
                    prefabName = "assets/rust.ai/agents/wolf/wolf.prefab";
                    break;
                case "bear":
                    prefabName = "assets/rust.ai/agents/bear/bear.prefab";
                    break;


                default:
                    break;
            }



            try
            {

                var entity = GameManager.server.CreateEntity(prefabName);

                var e = GetEntityPlayerSees(player);

                entity.transform.position = player.transform.position + (player.eyes.BodyForward() * 2);

                //entity.transform.rotation = player.eyes.bodyRotation;
                //entity.transform.Rotate(player.transform.rotation.eulerAngles);

                entity.transform.position = new Vector3(entity.transform.position.x, player.transform.position.y, entity.transform.position.z);

                entity.Spawn();
                spawnedEntities.Add(entity);

            }
            catch (Exception ex)
            {
                PrintError($"ERROR: Error creating entity. {ex.Message}");
            }

        }

        [ChatCommand("realremove")]
        void realremove(BasePlayer player, string command, string[] args)
        {
            if (!CheckAccess(player, "realremove")) return;

            var e = GetEntityPlayerSees(player);
            if (e != null)
            {
                PrintToChat(player, $"Removing {e.PrefabName}");
                e.Kill(BaseNetworkable.DestroyMode.None);
            }
        }

        [ChatCommand("fastnight")]
        void fastnight(BasePlayer player, string command, string[] args)
        {
            if (!CheckAccess(player, "fastnight")) return;

            fastNight = !fastNight;
            PrintToChat(player, $"{(fastNight? "fastNight On." : "fastNight Off.")}");
        }

        [ChatCommand("holding")]
        void holding(BasePlayer player, string command, string[] args)
        {
            try
            {
                if (!CheckAccess(player, "holding")) return;

                int slot = 0;
                
                //var item0 = player.inventory.containerMain.GetSlot(slot);

                var item1 = player.inventory.containerBelt.GetSlot(slot);

                //var item2 = player.inventory.containerWear.GetSlot(slot);

                var item = item1;


                if (item != null)
                {
                    ItemDefinition definition = ItemManager.FindItemDefinition(item.info.itemid);
                    if (definition != null)
                    {
                        PrintToChat(player, $"You have a {item.info.itemid} ({definition.name}) in slot {slot}. it is {item.dirty} dirty?");
                        item.MarkDirty();
                    }
                    else
                        PrintToChat(player, $"bad item.info.itemid {item.info.itemid}");
                    
                }
                else
                    PrintToChat(player, $"Bad slot {slot}");

            }
            catch (Exception ex)
            {
                PrintToChat(player, "error in holding " + ex.StackTrace, player);
            }
        }

        [ChatCommand("zeroloot")]
		void zerzerolooto(BasePlayer player, string command, string[] args)
		{
            try
            {
                if (!CheckAccess(player, command)) return;

                killLootable();
                PrintToChat(player,"Lootables destroyed.");
            }
            catch (Exception ex)
            {
                PrintToChat(player, "error in zero " + ex.Message, player);
            }
		}

        [ChatCommand("animal")]
        void animal(BasePlayer player, string command, string[] args)
        {
            try
            {
                if (!CheckAccess(player, command)) return;

                var an = (args.Length > 0) ? args[0] : "chicken";                    
                var pos = player.transform.position + (player.eyes.BodyForward() * 5);
                var animal = spawnAnimal(an, pos, player, Vector2.zero);
            }
            catch (Exception ex)
            {
                PrintToChat(player, $"error in {command}." + ex.Message);
            }
        }

  //      [ChatCommand("daytime")]
		//void daytime(BasePlayer player, string command, string[] args)
		//{
  //          if (!CheckAccess(player, command)) return;
  //          //if (BasePlayer.activePlayerList.Count > 1) return;
            
  //          if (BasePlayer.activePlayerList.Count > 1 && !TimeChangeAsked)
  //          {
  //              PrintToChat(player, $"Daytime?? You're not the only one online. Type it again (within 30 seconds) so I know you mean it.");
  //              TimeChangeAsked = true;
  //              timer.Once(28f, () => { TimeChangeAsked = false; });
  //              return;
  //          }
  //          TOD_Sky.Instance.Cycle.Hour = 8f;
  //          //PrintToChat($"Daytime, presented to you from {player.displayName}, cause he afraid of the dark.");
  //      }

        //[ChatCommand("nighttime")]
        //void nighttime(BasePlayer player, string command, string[] args)
        //{
        //    if (((!CheckAccess(player, command))) && (BasePlayer.activePlayerList.Count > 1)) return;

        //    if (BasePlayer.activePlayerList.Count > 1 && !TimeChangeAsked)
        //    {
        //        PrintToChat(player, $"Nighttime?? You're not the only one online. Type it again (within 30 seconds) so I know you mean it.");
        //        TimeChangeAsked = true;
        //        timer.Once(28f, () => { TimeChangeAsked = false; });
        //        return;
        //    }
        //    fastNight = false;  
        //    TOD_Sky.Instance.Cycle.Hour = 19f;
        //    //PrintToChat($"Nighttime, presented to you from {player.displayName}, cause he prolly raiding yo ass!!");
        //}

        [ChatCommand("players")]
		void players(BasePlayer player, string command, string[] args)
		{
            if (!CheckAccess(player, command)) return;

            var tmpLst = BasePlayer.activePlayerList.OrderBy(x => Vector3.Distance(x.transform.position, ((BasePlayer)player).transform.position ));

            foreach (var p in tmpLst) {
                if (p != (BasePlayer)player) {
                    var pos = p.transform.position.ToString();
                    var dist = Vector3.Distance(p.transform.position, ((BasePlayer)player).transform.position);
                    //if (p!=player)
                    var message = $" is @ {pos} which is <color=#FF0000>{dist.ToString("0.0")} m.</color> from your position. Online for {p.secondsConnected} seconds.";
                    PrintToChat(player, $"<color=#00FF00>{p.displayName}</color> {message}");
                }
            }
        }

        [ChatCommand("sleepers")]
        void sleepers(BasePlayer player, string command, string[] args)
        {
            if (!CheckAccess(player, command)) return;

            if (BasePlayer.sleepingPlayerList.Count > 0)
                PrintToChat(player, $"<color=#000000>Sleepers:</color>");
            else
                PrintToChat(player, $"<color=#FF0000>No Sleepers?</color>");

            foreach (var p in BasePlayer.sleepingPlayerList)
            {
                var pos = p.transform.position.ToString();
                var dist = Vector3.Distance(p.transform.position, ((BasePlayer)player).transform.position);
                var message = $" is sleeping @ {pos} which is {dist.ToString("0.0")} m from your position.";
                var msg = message;

                PrintToChat(player, $"<color=#999999>{p.displayName} {msg }</color>");
            }

        }

        [ChatCommand("whatis")]
        void whatis(BasePlayer player, string command, string[] args)
        {
            if (!CheckAccess(player, command)) return;
            var e = GetEntityPlayerSees(player);
            if (e != null)
                PrintToChat(player, $"You're looking at {e.ShortPrefabName} ");
            else
            {
                PrintToChat(player, $"I see nothing here. Except...");
                var items = GameObject.FindObjectsOfType<BaseEntity>().Where(x => Vector3.Distance(x.transform.position, player.transform.position) < 10);
                foreach (BaseEntity i in items)
                {
                    PrintToChat(player, $"{i.PrefabName}");
                    PrintError($"{i.PrefabName}");
                }

            }

        }


        private Dictionary<BasePlayer, Vector3> recallPostitions = new Dictionary<BasePlayer, Vector3>();

        [ChatCommand("recall")]
        void Recall(BasePlayer player, string command, string[] args)
        {
            if (!CheckAccess(player, command)) return;

            if (recallPostitions.ContainsKey(player))
            {
                Teleport(player, recallPostitions[player]);
                recallPostitions.Remove(player);
            }
            else
            {
                recallPostitions.Add(player, player.transform.position);
            }

        }

        [ConsoleCommand("ppp")]
        void players(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (!CheckAccess(player, "ppp")) return;

            foreach (var p in BasePlayer.activePlayerList)
            {
                var pos = p.transform.position.ToString();
                var dist = Vector3.Distance(p.transform.position, ((BasePlayer)player).transform.position);
                //if (p!=player)
                var message = $" is @ {pos} which is {dist.ToString("0.0")} m from your position. Online for {p.Connection.connectionTime} seconds.";
                PrintToChat(player, $"<color=#00FF00>{p.displayName}</color> {message }");
            }
        }

        #endregion

        #region common stuff

        BaseEntity GetEntityPlayerSees(BasePlayer player)
        {
            //var layers = LayerMask.GetMask("Construction", "Default", "Deployed", "Resource", "Terrain", "Water", "World");
            var layers = LayerMask.GetMask("Construction", "Default", "Deployed", "Resource", "Terrain", "Water", "World");

            RaycastHit hit = new RaycastHit();

            if (Physics.Raycast(player.eyes.HeadRay(), out hit, float.MaxValue, layers))
            {
                var entity = hit.GetEntity();
                if (entity != null)
                    return entity;
            }
            return null;
        }

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
                PrintWarning($"WARNING: {this.Title}: makeItem: Error making item {shortname}");
                return null;
            }
            catch (Exception ex)
            {
                PrintError($"ERROR: {this.Title}: makeItem: Error making item {shortname} {ex.Message}");
                return null;
            }
        }

        Vector3 GetGroundPosition(Vector3 sourcePos)
        {
            RaycastHit hitInfo;

            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo, LayerMask.GetMask("Construction", "Deployed", "Resource", "Terrain", "Water", "World", "Default")))
            {
                sourcePos.y = hitInfo.point.y;
            }
            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
            return sourcePos;
        }

        Vector3 GetGroundPosition(Vector3 sourcePos, int Layer)
        {
            RaycastHit hitInfo;

            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo, ~Layer)) //LayerMask.GetMask("Construction", "Deployed", "Resource", "Terrain", "Water", "World", "Default")))
            {
                sourcePos.y = hitInfo.point.y;
            }
            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
            return sourcePos;
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
                "assets/rust.ai/agents/zombie/zombie.prefab",
                "assets/rust.ai/agents/npcplayer/npcplayer.prefab"
            };

            try
            {
                var prefabName = possibleAnimalPrefabs.Where(x => x.Contains(animal)).First();
                PrintWarning($"Spawning a {prefabName}");
                BaseEntity entity = GameManager.server.CreateEntity(prefabName, GetGroundPosition(position));
                if (entity == null)
                {
                    PrintError($"Error in spawnAnimal {prefabName}");
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
                PrintError("Error in spawnAnimal " + ex.Message);
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

        void killLootableAroundLocation(Vector3 location)
        {
            var spawns = Resources.FindObjectsOfTypeAll<LootContainer>()
                .Where(c => c.isActiveAndEnabled).
                OrderBy(c => c.transform.position.x).ThenBy(c => c.transform.position.z).ThenBy(c => c.transform.position.z)
                .ToList();

            foreach (var ent in spawns)
            {
                if (Vector3.Distance(location, ent.transform.position)  <= 1000) 
                    ent.Kill(BaseNetworkable.DestroyMode.None);
            }

        }

        void killLootable()
        {
            var spawns = Resources.FindObjectsOfTypeAll<LootContainer>()
                .Where(c => c.isActiveAndEnabled).
                OrderBy(c => c.transform.position.x).ThenBy(c => c.transform.position.z).ThenBy(c => c.transform.position.z)
                .ToList();

            foreach (var ent in spawns)
            {
                //if (Vector3.Distance(location, ent.transform.position) <= 1000)
                    ent.Kill(BaseNetworkable.DestroyMode.None);
            }

        }

        private bool CheckAccess(BasePlayer player, string command)
        {
            if (player == null) return true;

            if (!permission.UserHasPermission(player.UserIDString, usePermission))
            {
                PrintToChat(player, $"Unknown command: {command}");
                PrintWarning($"{player.displayName}/{player.userID} attempted {command}");
                return false;
            }
            return true;

        }


        private void sickPlayer(BasePlayer victim, string animal)
        {
            try
            {
                // default 5 meters behind victim
                var animalPosition = victim.transform.position - (victim.eyes.BodyForward() * 6);
                spawnAnimal(animal, animalPosition, victim, Vector3.zero);
            }
            catch (Exception ex)
            {
                PrintError("sickPlayer " + ex.Message);
            }
        }


        #endregion

        #region spawn buildings


        void Spawn1x2Box(Vector3 position, BasePlayer player, int strength = -1, BuildingGrade.Enum type = BuildingGrade.Enum.Wood)
        {

            position += new Vector3(0, 1, 0);

            SpawnStructure("assets/prefabs/building core/foundation/foundation.prefab",
                           position, Quaternion.AngleAxis(0, new Vector3(0, 1, 0)), type, strength);
            SpawnStructure("assets/prefabs/building core/wall/wall.prefab",
                           position + new Vector3(-1.5f, 0, 0), Quaternion.AngleAxis(180, new Vector3(0, 1, 0)), type, strength);
            SpawnStructure("assets/prefabs/building core/wall/wall.prefab",
                           position + new Vector3(0, 0, 1.5f), Quaternion.AngleAxis(-90, new Vector3(0, 1, 0)), type, strength);
            SpawnStructure("assets/prefabs/building core/wall.doorway/wall.doorway.prefab",
                            position + new Vector3(0, 0, -1.5f), Quaternion.AngleAxis(90, new Vector3(0, 1, 0)), type, strength);
            SpawnStructure("assets/prefabs/building core/floor/floor.prefab",
                           position + new Vector3(0, 3, 0), Quaternion.AngleAxis(0, new Vector3(0, 1, 0)), type, strength);


            //SpawnStructure("assets/prefabs/building core/wall/wall.prefab", // middle (doorway?)
            //               position + new Vector3(1.5f, 0, 0),Quaternion.AngleAxis(0, new Vector3(0, 1, 0)), type, strength);


            SpawnStructure("assets/prefabs/building core/foundation/foundation.prefab",
                           position + new Vector3(3f, 0, 0), Quaternion.AngleAxis(0, new Vector3(0, 1, 0)), type, strength);
            SpawnStructure("assets/prefabs/building core/wall/wall.prefab",
                            position + new Vector3(3f, 0, -1.5f), Quaternion.AngleAxis(90, new Vector3(0, 1, 0)), type, strength);
            SpawnStructure("assets/prefabs/building core/wall/wall.prefab",
                           position + new Vector3(4.5f, 0, 0), Quaternion.AngleAxis(0, new Vector3(0, 1, 0)), type, strength);
            SpawnStructure("assets/prefabs/building core/wall/wall.prefab",
                           position + new Vector3(3f, 0, 1.5f), Quaternion.AngleAxis(-90, new Vector3(0, 1, 0)), type, strength);
            SpawnStructure("assets/prefabs/building core/floor/floor.prefab",
                           position + new Vector3(3f, 3f, 0), Quaternion.AngleAxis(0, new Vector3(0, 1, 0)), type, 15);

            var door = SpawnDeployable("door.hinged.toptier",
                            position + new Vector3(0, 0, -1.5f),
                           Quaternion.AngleAxis(90, new Vector3(0, 1, 0)),
                           player) as Door;

            SpawnLock(door, player);

            var box = CreateBuildersBox(position + new Vector3(0.5f, 0.1f, 0.9f),
                player,
                Quaternion.AngleAxis(180, new Vector3(0, 1, 0)));

            var workbench = SpawnDeployable("workbench1",
                position + new Vector3(2.2f, 0.1f, 0.9f),
                Quaternion.AngleAxis(180, new Vector3(0, 1, 0)),
                player);

            var repbench = SpawnDeployable("research.table",
                            position + new Vector3(3.8f, 0.1f, 0f),
                            Quaternion.AngleAxis(-90, new Vector3(0, 1, 0)),
                            player) as ResearchTable;
            repbench.inventory.AddItem(ItemManager.FindItemDefinition("scrap"), 2500);

            var furnace = SpawnDeployable("furnace",
                position + new Vector3(-0.9f, 0.1f, 0.9f),
                Quaternion.AngleAxis(180, new Vector3(0, 1, 0)),
                player
                ) as BaseOven;
            furnace.inventory.AddItem(ItemManager.FindItemDefinition("wood"), 1000);
            furnace.SetFlag(BaseEntity.Flags.On, true);

            var chair = SpawnDeployable("chair",
                position + new Vector3(-0.9f, 0.1f, 0.0f),
                Quaternion.AngleAxis(90, new Vector3(0, 1, 0)),
                player
                );


            var campfire = SpawnDeployable("campfire",
                position + new Vector3(-0.9f, 0.1f, -1f),
                Quaternion.AngleAxis(180, new Vector3(0, 1, 0)),
                player
                ) as BaseOven;
            campfire.inventory.AddItem(ItemManager.FindItemDefinition("wood"), 1000);
            campfire.inventory.AddItem(ItemManager.FindItemDefinition("sulfur.ore"), 1000);
            campfire.inventory.AddItem(ItemManager.FindItemDefinition("metal.ore"), 1000);
            campfire.inventory.AddItem(ItemManager.FindItemDefinition("hq.metal.ore"), 25);
            campfire.SetFlag(BaseEntity.Flags.On, true);

            // assets/prefabs/deployable/campfire/campfire.prefab
            //SpawnLock(box, player, "6228");

            if (!AddItemToItemContainer("stones", 8000, ref box))
                return; // box full
            if (!AddItemToItemContainer("wood", 9000, ref box))
                return; // box full
            if (!AddItemToItemContainer("metal.fragments", 1000, ref box))
                return; // box full
            if (!AddItemToItemContainer("metal.refined", 50, ref box))
                return; // box full
            if (!AddItemToItemContainer("cloth", 500, ref box))
                return; // box full
            if (!AddItemToItemContainer("leather", 250, ref box))
                return; // box full
            if (!AddItemToItemContainer("lowgradefuel", 250, ref box))
                return; // box full
            if (!AddItemToItemContainer("bearmeat.cooked", 25, ref box))
                return; // box full
            if (!AddItemToItemContainer("scrap", 2500, ref box))
                return; // box full
            if (!AddItemToItemContainer("gears", 25, ref box))
                return; // box full
            if (!AddItemToItemContainer("metalpipe", 25, ref box))
                return; // box full
            if (!AddItemToItemContainer("metalspring", 25, ref box))
                return; // box full
            if (!AddItemToItemContainer("rope", 25, ref box))
                return; // box full
            if (!AddItemToItemContainer("metalblade", 25, ref box))
                return; // box full
            if (!AddItemToItemContainer("sheetmetal", 25, ref box))
                return; // box full
            if (!AddItemToItemContainer("tarp", 25, ref box))
                return; // box full
            if (!AddItemToItemContainer("syringe.medical", 10, ref box))
                return; // box full
            if (!AddItemToItemContainer("bandage", 10, ref box))
                return; // box full

            //"assets/prefabs/building core/foundation.triangle/foundation.triangle.prefab",
            //"assets/prefabs/building core/foundation.steps/foundation.steps.prefab",
            //"assets/prefabs/building core/foundation/foundation.prefab"
            //"assets/prefabs/building core/wall.frame/wall.frame.prefab",
            //"assets/prefabs/building core/wall.window/wall.window.prefab",
            //"assets/prefabs/building core/wall.doorway/wall.doorway.prefab",
            //"assets/prefabs/building core/wall/wall.prefab"
            //"assets/prefabs/building core/floor.frame/floor.frame.prefab",
            //"assets/prefabs/building core/floor.triangle/floor.triangle.prefab",
            //"assets/prefabs/building core/floor/floor.prefab"
            //"assets/prefabs/building core/roof/roof.prefab",
            //"assets/prefabs/building core/stairs.l/block.stair.lshape.prefab",
            //"assets/prefabs/building core/pillar/pillar.prefab",
            //"assets/prefabs/building core/stairs.u/block.stair.ushape.prefab"

        }


        void Spawn1x1Box(Vector3 position, BasePlayer player, int strength = 1, BuildingGrade.Enum type = BuildingGrade.Enum.Wood)
        {

            position += new Vector3(0, 1, 0);

            SpawnStructure("assets/prefabs/building core/foundation/foundation.prefab",
                           position,
                           Quaternion.AngleAxis(0, new Vector3(0, 1, 0)),
                           type, strength);
            SpawnStructure("assets/prefabs/building core/wall/wall.prefab",
                           position + new Vector3(-1.5f, 0, 0),
                           Quaternion.AngleAxis(180, new Vector3(0, 1, 0)),
                           type, strength);
            SpawnStructure("assets/prefabs/building core/wall/wall.prefab",
                           position + new Vector3(1.5f, 0, 0),
                           Quaternion.AngleAxis(0, new Vector3(0, 1, 0)),
                           type, strength);
            SpawnStructure("assets/prefabs/building core/wall/wall.prefab",
                           position + new Vector3(0, 0, 1.5f),
                           Quaternion.AngleAxis(-90, new Vector3(0, 1, 0)),
                           type, strength);
            SpawnStructure("assets/prefabs/building core/floor/floor.prefab",
                           position + new Vector3(0, 3, 0),
                           Quaternion.AngleAxis(0, new Vector3(0, 1, 0)),
                           type, strength);

            SpawnStructure("assets/prefabs/building core/wall.doorway/wall.doorway.prefab",
                            position + new Vector3(0, 0, -1.5f),
                           Quaternion.AngleAxis(90, new Vector3(0, 1, 0)),
                           type, strength);

            var door = SpawnDeployable("door.hinged.metal",
                            position + new Vector3(0, 0, -1.5f),
                           Quaternion.AngleAxis(90, new Vector3(0, 1, 0)),
                           player) as Door;

            //SpawnLock(door, player, "6228");

            var box = CreateBuildersBox(position + new Vector3(0.5f, 0.1f, 0.9f),
                player,
                Quaternion.AngleAxis(180, new Vector3(0, 1, 0)));


            var furnace = SpawnDeployable("furnace",
                position + new Vector3(-0.9f, 0.1f, 0.9f),
                Quaternion.AngleAxis(180, new Vector3(0, 1, 0)),
                player
                ) as BaseOven;
            furnace.inventory.AddItem(ItemManager.FindItemDefinition("wood"), 1000);
            furnace.SetFlag(BaseEntity.Flags.On, true);

            var campfire = SpawnDeployable("campfire",
                position + new Vector3(-0.9f, 0.1f, -1f),
                Quaternion.AngleAxis(180, new Vector3(0, 1, 0)),
                player
                ) as BaseOven;
            campfire.inventory.AddItem(ItemManager.FindItemDefinition("wood"), 1000);
            campfire.SetFlag(BaseEntity.Flags.On, true);

            // assets/prefabs/deployable/campfire/campfire.prefab
            //SpawnLock(box, player, "6228");

            if (!AddItemToItemContainer("stones", 8000, ref box))
                return; // box full
            if (!AddItemToItemContainer("wood", 9000, ref box))
                return; // box full
            if (!AddItemToItemContainer("metal.fragments", 1000, ref box))
                return; // box full
            if (!AddItemToItemContainer("bearmeat.cooked", 25, ref box))
                return; // box full

            //"assets/prefabs/building core/foundation.triangle/foundation.triangle.prefab",
            //"assets/prefabs/building core/foundation.steps/foundation.steps.prefab",
            //"assets/prefabs/building core/foundation/foundation.prefab"
            //"assets/prefabs/building core/wall.frame/wall.frame.prefab",
            //"assets/prefabs/building core/wall.window/wall.window.prefab",
            //"assets/prefabs/building core/wall.doorway/wall.doorway.prefab",
            //"assets/prefabs/building core/wall/wall.prefab"
            //"assets/prefabs/building core/floor.frame/floor.frame.prefab",
            //"assets/prefabs/building core/floor.triangle/floor.triangle.prefab",
            //"assets/prefabs/building core/floor/floor.prefab"
            //"assets/prefabs/building core/roof/roof.prefab",
            //"assets/prefabs/building core/stairs.l/block.stair.lshape.prefab",
            //"assets/prefabs/building core/pillar/pillar.prefab",
            //"assets/prefabs/building core/stairs.u/block.stair.ushape.prefab"

        }


        void Spawn1x1(Vector3 position, BasePlayer player)
        {

            BuildingGrade.Enum type = BuildingGrade.Enum.Wood;
            int strength = 1;

            position += new Vector3(0, 1, 0);

            SpawnStructure("assets/prefabs/building core/foundation/foundation.prefab",
                           position,
                           Quaternion.AngleAxis(0, new Vector3(0, 1, 0)),
                           type, strength);
            SpawnStructure("assets/prefabs/building core/wall/wall.prefab",
                           position + new Vector3(-1.5f, 0, 0),
                           Quaternion.AngleAxis(180, new Vector3(0, 1, 0)),
                           type, strength);
            SpawnStructure("assets/prefabs/building core/wall/wall.prefab",
                           position + new Vector3(1.5f, 0, 0),
                           Quaternion.AngleAxis(0, new Vector3(0, 1, 0)),
                           type, strength);
            SpawnStructure("assets/prefabs/building core/wall/wall.prefab",
                           position + new Vector3(0, 0, 1.5f),
                           Quaternion.AngleAxis(-90, new Vector3(0, 1, 0)),
                           type, strength);
            SpawnStructure("assets/prefabs/building core/floor/floor.prefab",
                           position + new Vector3(0, 3, 0),
                           Quaternion.AngleAxis(0, new Vector3(0, 1, 0)),
                           type, strength);

            SpawnStructure("assets/prefabs/building core/wall.doorway/wall.doorway.prefab",
                            position + new Vector3(0, 0, -1.5f),
                           Quaternion.AngleAxis(90, new Vector3(0, 1, 0)),
                           type, strength);

            var door = SpawnDeployable("door.hinged.metal",
                            position + new Vector3(0, 0, -1.5f),
                           Quaternion.AngleAxis(90, new Vector3(0, 1, 0)),
                           player) as Door;

            SpawnLock(door, player, "6228");

            //"assets/prefabs/building core/foundation.triangle/foundation.triangle.prefab",
            //"assets/prefabs/building core/foundation.steps/foundation.steps.prefab",
            //"assets/prefabs/building core/foundation/foundation.prefab"
            //"assets/prefabs/building core/wall.frame/wall.frame.prefab",
            //"assets/prefabs/building core/wall.window/wall.window.prefab",
            //"assets/prefabs/building core/wall.doorway/wall.doorway.prefab",
            //"assets/prefabs/building core/wall/wall.prefab"
            //"assets/prefabs/building core/floor.frame/floor.frame.prefab",
            //"assets/prefabs/building core/floor.triangle/floor.triangle.prefab",
            //"assets/prefabs/building core/floor/floor.prefab"
            //"assets/prefabs/building core/roof/roof.prefab",
            //"assets/prefabs/building core/stairs.l/block.stair.lshape.prefab",
            //"assets/prefabs/building core/pillar/pillar.prefab",
            //"assets/prefabs/building core/stairs.u/block.stair.ushape.prefab"

        }


        void SpawnStore(Vector3 position, BasePlayer player)
        {

            BuildingGrade.Enum type = BuildingGrade.Enum.TopTier;
            int strength = 0;

            position += new Vector3(0, 1, 0);

            SpawnStructure("assets/prefabs/building core/foundation/foundation.prefab",
                           position,
                           Quaternion.AngleAxis(0, new Vector3(0, 1, 0)),
                           type, strength);

            // -----
            SpawnStructure("assets/prefabs/building core/wall/wall.prefab",
                           position + new Vector3(-1.5f, 0, 0),
                           Quaternion.AngleAxis(180, new Vector3(0, 1, 0)),
                           type, strength);

            SpawnStructure("assets/prefabs/building core/wall/wall.prefab",
                           position + new Vector3(1.5f, 0, 0),
                           Quaternion.AngleAxis(0, new Vector3(0, 1, 0)),
                           type, strength);

            //SpawnStructure("assets/prefabs/building core/wall/wall.prefab",
            SpawnStructure("assets/prefabs/building core/wall.doorway/wall.doorway.prefab",
                           position + new Vector3(0, 0, 1.5f),
                           Quaternion.AngleAxis(-90, new Vector3(0, 1, 0)),
                           type, strength);

            var vendingM = SpawnDeployable("vending.machine",
                            position + new Vector3(0, 0, 1.5f),
                           Quaternion.AngleAxis(0, new Vector3(0, 1, 0)),
                           player) as VendingMachine;


            SpawnStructure("assets/prefabs/building core/wall.doorway/wall.doorway.prefab",
                            position + new Vector3(0, 0, -1.5f),
                           Quaternion.AngleAxis(90, new Vector3(0, 1, 0)),
                           type, strength);

            var door = SpawnDeployable("door.hinged.toptier",
                            position + new Vector3(0, 0, -1.5f),
                           Quaternion.AngleAxis(90, new Vector3(0, 1, 0)),
                           player) as Door;

            SpawnStructure("assets/prefabs/building core/floor/floor.prefab",
                           position + new Vector3(0, 3, 0),
                           Quaternion.AngleAxis(0, new Vector3(0, 1, 0)),
                           type, strength);


            //SpawnLock(door, player, "6228");

            var tool = SpawnDeployable("cupboard.tool",
                position + new Vector3(0.9f, 0, 0.5f),
                Quaternion.AngleAxis(-90, new Vector3(0, 1, 0)), player);



            var i = ItemManager.FindItemDefinition("stones");

            //vendingM.inventory.AddItem(ItemManager.FindItemDefinition("stones"), 1000);

            //"assets/prefabs/building core/foundation.triangle/foundation.triangle.prefab",
            //"assets/prefabs/building core/foundation.steps/foundation.steps.prefab",
            //"assets/prefabs/building core/foundation/foundation.prefab"
            //"assets/prefabs/building core/wall.frame/wall.frame.prefab",
            //"assets/prefabs/building core/wall.window/wall.window.prefab",
            //"assets/prefabs/building core/wall.doorway/wall.doorway.prefab",
            //"assets/prefabs/building core/wall/wall.prefab"
            //"assets/prefabs/building core/floor.frame/floor.frame.prefab",
            //"assets/prefabs/building core/floor.triangle/floor.triangle.prefab",
            //"assets/prefabs/building core/floor/floor.prefab"
            //"assets/prefabs/building core/roof/roof.prefab",
            //"assets/prefabs/building core/stairs.l/block.stair.lshape.prefab",
            //"assets/prefabs/building core/pillar/pillar.prefab",
            //"assets/prefabs/building core/stairs.u/block.stair.ushape.prefab"

        }

        private BuildingBlock SpawnStructure(string prefabname, Vector3 pos, Quaternion angles, BuildingGrade.Enum grade, float health = -1f)
        {
            GameObject prefab = GameManager.server.CreatePrefab(prefabname, pos, angles, true);
            if (prefab == null)
            {
                PrintError($"SpawnStructure: {prefab} no worky.");
                return null;
            }
            BuildingBlock block = prefab.GetComponent<BuildingBlock>();
            if (block == null) return null;
            block.transform.position = pos;
            block.transform.rotation = angles;
            block.gameObject.SetActive(true);
            block.blockDefinition = PrefabAttribute.server.Find<Construction>(block.prefabID);
            block.Spawn();
            block.SetGrade(grade);
            if (health <= 0f)
                block.health = block.MaxHealth();
            else
                block.health = health;

            block.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            spawnedBuildingBlocks.Add(block);
            return block;
        }

        private BaseEntity SpawnDeployable(string prefab, Vector3 pos, Quaternion angles, BasePlayer player)
        {
            //Item newItem = ItemManager.CreateByName(prefab, 1);
            Item newItem = makeItem(prefab, 1);

            if (newItem?.info.GetComponent<ItemModDeployable>() == null)
            {
                PrintToChat(player, $"SpawnDeployable: {prefab} no worky.");
                return null;
            }
            var deployable = newItem.info.GetComponent<ItemModDeployable>().entityPrefab.resourcePath;
            if (deployable == null)
            {
                return null;
            }
            var newBaseEntity = GameManager.server.CreateEntity(deployable, pos, angles, true);
            if (newBaseEntity == null)
            {
                return null;
            }

            newBaseEntity.SendMessage("SetDeployedBy", player, UnityEngine.SendMessageOptions.DontRequireReceiver);
            newBaseEntity.SendMessage("InitializeItem", newItem, UnityEngine.SendMessageOptions.DontRequireReceiver);
            newBaseEntity.Spawn();

            spawnedEntities.Add(newBaseEntity);
            return newBaseEntity;
        }

        private BaseEntity SpawnLock(BaseEntity door, BasePlayer player, string code = "")
        {
            var newCodeLock = CreateEntity("lock.code", Vector3.zero, new Quaternion());

            newCodeLock.gameObject.Identity();
            newCodeLock.SetParent(door, BaseEntity.Slot.Lock.ToString());
            newCodeLock.OnDeployed(door);
            newCodeLock.Spawn();

            //if (code != "")
            //{
            //    CodeLock codeLock = newCodeLock as CodeLock;
            //    codeLock.code = code;
            //    codeLock.SetFlag(BaseEntity.Flags.Locked, true);
            //}

            door.SetSlot(BaseEntity.Slot.Lock, newCodeLock);


            spawnedEntities.Add(newCodeLock);

            return newCodeLock;
        }

        private BaseEntity CreateEntity(Item item, Vector3 pos, Quaternion rot)
        {
            if (item?.info.GetComponent<ItemModDeployable>() == null)
            {
                PrintError($"CreateEntity: {item.ToString()}  no worky.");
                return null;
            }

            var deployable = item.info.GetComponent<ItemModDeployable>().entityPrefab.resourcePath;
            if (deployable == null)
            {
                return null;
            }
            var newBaseEntity = GameManager.server.CreateEntity(deployable, Vector3.zero, new Quaternion(), true);
            if (newBaseEntity == null)
            {
                return null;
            }
            return newBaseEntity;
        }

        private BaseEntity CreateEntity(string prefabName, Vector3 pos, Quaternion rot)
        {
            Item newItem = makeItem(prefabName, 1);
            if (newItem?.info.GetComponent<ItemModDeployable>() == null)
            {
                PrintError($"CreateEntity: no worky.");
                return null;
            }

            var deployable = newItem.info.GetComponent<ItemModDeployable>().entityPrefab.resourcePath;
            if (deployable == null)
            {
                return null;
            }
            var newBaseEntity = GameManager.server.CreateEntity(deployable, pos, rot, true);
            if (newBaseEntity == null)
            {
                return null;
            }
            return newBaseEntity;
        }

        public BaseEntity CreateBuildersBox(Vector3 position, BasePlayer Owner, Quaternion rotation)
        {
            try
            {
                var _woodenBoxName = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab";

                var _woodenBox = GameManager.server.CreateEntity(_woodenBoxName, position, rotation);
                _woodenBox.SendMessage("SetDeployedBy", Owner, UnityEngine.SendMessageOptions.DontRequireReceiver);
                _woodenBox.OwnerID = Owner.userID;
                _woodenBox.Spawn();

                spawnedEntities.Add(_woodenBox);

                return _woodenBox;
            }
            catch (Exception ex)
            {
                PrintError($"ERROR: CreateBuildersBox: Error creating box. {ex.Message}");
                return null;
            }
        }


        bool AddItemToItemContainer(string itemName, int amount, ref BaseEntity containerEntity)
        {

            var item = makeItem(itemName, amount);
            if (item == null)
            {
                PrintWarning("Cannot make " + itemName);
                return false;
            }

            var container = containerEntity.GetComponent<StorageContainer>();

            if (container.inventory.IsFull())
                return false;

            item.MoveToContainer(container.inventory);
            return true;
        }

        #endregion

        void plantGrid(Vector3 position, string prefabName = "assets/prefabs/plants/corn/corn.entity.prefab")
        {
            //var position2meters = player.transform.position + (player.eyes.BodyForward() * 5);
            for (int xx = 1; xx <= 5; xx++)
            {
                for (int zz = 1; zz <= 5; zz++)
                {
                    BaseEntity entity = GameManager.server.CreateEntity(prefabName, GetGroundPosition(position + new Vector3(xx, 0, zz)), default(Quaternion), true);
                    entity?.Spawn();
                }
            }
        }

        void Teleport(BasePlayer player, Vector3 position)
        {
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading");

            player.MovePosition(position);

            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", position);

            player.SendNetworkUpdate();

            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);

            player.UpdateNetworkGroup();

            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;

            try { player.ClearEntityQueue(null); } catch { }
            player.SendFullSnapshot();
        }

    }
}


