using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Deathmatch", "CARNY666", "1.4.15")]
    class Deathmatch : RustPlugin
    {

        #region constants
        const string dataFileName = "DeathmatchDatafile";
        const string loadoutFileName = "DeathmatchLoudoutfile";
        const string adminPriv = "Deathmatch.admin";
        const string playPriv = "Deathmatch.play";

        const int bodyDecayInSeconds = 2;
        #endregion

        #region properties
        bool displayDeathNotes = true;
        bool gameInProgress = false;
        bool finders_keepers = false;
        bool doPrintToNonPlayers = true;

        SaveableData data;

        string dmLoadout = "default";
        Loadouts loadouts = new Loadouts();
        List<PlayerLoadout> dmLoadouts = new List<PlayerLoadout>();

        int loadoutIndex = 0;

        List<ModPlayers> dmPlayers = new List<ModPlayers>();
        List<BaseEntity> createdEntities = new List<BaseEntity>();
        #endregion

        #region events

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            try
            {

                if (entity == null || info == null) return null;

                if (!isWithinSpawnPosition(entity.transform.position, float.Parse(Config["maxDistanceFromSpawnPoints"].ToString()))) // outside jurisdiction...
                    return null;

                var ignore = new[] { "BaseNPC", "LootContainer", "Landmine", "PlayerCorpse" };
                var doNotDamage = new[] { "ShopFront", "PlayerCorpse", "ReactiveTarget", "MiningQuarry", "DecorDeployable", "StabilityEntity", "Workbench", "Locker", "BaseFuelLightSource", "Barricade", "BuildingBlock", "Door", "Signage", "SimpleBuildingBlock", "RepairBench", "BaseOven", "CeilingLight", "BoxStorage", "BaseLadder", "SearchLight", "RescourceExtractorFuelStorage" };

                if (ignore.Contains(entity.GetType().ToString()))
                    return null;

                // dont damage some things in the zone
                if (doNotDamage.Contains(entity.GetType().ToString()))
                {
                    info.damageTypes.ScaleAll(0.0f);
                    return false;
                }

                // if its a player
                if (entity is BasePlayer)
                {
                    // get player data
                    var player = entity.ToPlayer();
                    if (player == null) return null;

                    if (!dmPlayers.Any(x => x.player == player)) return null;
                    var victim = dmPlayers?.Where(x => x.player == player).First();
                    if (victim == null) return null;

                    // get initiator player data (enemy)                    
                    var initiator = dmPlayers?.Where(x => x.player == info.InitiatorPlayer).First();

                    // add score based on damage
                    Interface.Oxide.NextTick(() =>
                    {
                        if (initiator != null)
                        {
                            // last initiator for bleed outs
                            victim.lastInitiator = initiator;
                            if ((victim.Health - player.health) > 0)
                            {
                                initiator.score += victim.Health - player.health;
                                PrintToChat(initiator.player, $"<color=red>-{victim.Health - player.health}</color>");
                            }
                        }
                        victim.Health = player.health; // record current health
                    });

                    return null;
                }

                PrintError($"Unhandled entity of type {entity.GetType().ToString()} is being damaged. Notify support. Add '{entity.GetType().ToString()}' to doNotDamage array in OnEntityTakeDamage in DeathMatch mod code.");
                return null;
            }
            catch (Exception ex)
            {
                PrintError($"Error OnEntityTakeDamage {ex.StackTrace} in DeathMatch");
                return null;
            }

        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file for DeathMatch.");

            Config.Clear();

            Config["maxDistanceFromSpawnPoints"] = 100.0f;
            Config["disarmDeadBodies"] = true;

            SaveConfig();
        }

        void Init()
        {
            PrintWarning($"{this.Title} {this.Version} Initialized @ {DateTime.Now.ToLongTimeString()}...");
        }

        void Loaded()
        {
            try
            {
                loadouts = Interface.Oxide.DataFileSystem.ReadObject<Loadouts>(loadoutFileName);
                dmLoadouts = loadouts.loadouts.Where(x => x.category.ToString().ToLower() == dmLoadout).ToList<PlayerLoadout>();

                PrintWarning($"{loadouts.loadouts.Count()} weapon loadout configurations loaded.");

                data = Interface.Oxide.DataFileSystem.ReadObject<SaveableData>(dataFileName);
                PrintWarning($"{data.positions.Count()} spawn positions available.");

                permission.RegisterPermission(adminPriv, this);
                PrintWarning(adminPriv + " privilidge is registered.");

                permission.RegisterPermission(playPriv, this);
                PrintWarning(playPriv + " privilidge is registered.");

            }
            catch (Exception ex)
            {
                PrintError($"Error in Loaded, {ex.Message}");
            }
        }

        void Unload()
        {
            try
            {
                foreach (ModPlayers player in dmPlayers)
                {                    
                    QuitPlayer(player.player, false);
                }
            }
            catch (Exception ex)
            {
                PrintError($"Error in Unload, {ex.Message}");
            }

        }

        public bool isPlayerInDeathmatch(BasePlayer player)
        {
            if (!gameInProgress) return false;
            if (dmPlayers.Select(x => x.player == player).Any()) return true;
            return false;
        }

        void OnPlayerWound(BasePlayer player) // control instant death
        {
            try
            {
                if (!gameInProgress) return;

                if (!player || !player.gameObject || player.IsDestroyed)
                    return;

                var dmPlayer = dmPlayers.Where(x => x.player == player).First();

                if (dmPlayer != null)
                    dmPlayer.player.DieInstantly();
            }
            catch (Exception ex)
            {
                PrintError($"Error in OnPlayerWound, {ex.Message}");
            }


        }

        void OnItemDropped(Item item, BaseEntity entity) // control dropping stuff within DM arena
        {
            try
            {
                if (!gameInProgress) return;
                
                var maxDistance = (data.positions.Count() > 1) ? float.Parse(Config["maxDistanceFromSpawnPoints"].ToString()) : 50000f;

                if (isWithinSpawnPosition(entity.transform.position, maxDistance))
                    item.Remove(0f);
            }
            catch (Exception ex)
            {
                PrintError($"Error in OnItemDropped, {ex.Message}");
            }


        }

        void OnPlayerRespawned(BasePlayer player) // control player respawnings
        {
            try
            {
                if (!gameInProgress) return;

                // test is player in game
                if (!dmPlayers.Select(x => x.player == player).Any()) return;

                var dmPlayer = dmPlayers.Where(x => x.player == player).First();

                dmPlayer.lastInitiator = null;

                var dmPosition = data.positions[UnityEngine.Random.Range(0, data.positions.Count)];
                Teleport(player, dmPosition.GetVector3());

                if (dmLoadouts.Count() > 1)
                    dmPlayer.currentLoadoutIndex++;

                if (dmPlayer.currentLoadoutIndex > dmLoadouts.Count() - 1)
                    dmPlayer.currentLoadoutIndex = 0;

                EquipPlayer(dmPlayer.player);

                player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            }
            catch (Exception ex)
            {
                PrintError($"Error in OnPlayerRespawned, {ex.Message}");
            }


        }

        void OnPlayerDisconnected(BasePlayer player, string reason) // removes player from dm
        {
            try
            {
                QuitPlayer(player);
            }
            catch (Exception ex)
            {
                PrintError($"Error in OnPlayerDisconnected, {ex.Message}");
            }

        }

        void OnEntityDeath(BaseEntity entity, HitInfo info) // control deaths 
        {
            try
            {
                if (!gameInProgress) return;

                if (entity is BasePlayer)
                {
                    // test is player in game
                    if (!dmPlayers.Select(x => x.player == entity.ToPlayer()).Any()) return;

                    var victim = dmPlayers.Any(x => x.player == entity.ToPlayer()) ? dmPlayers.First(x => x.player == entity.ToPlayer()) : null;
                    if (victim != null)
                    {
                        if ((bool)Config["disarmDeadBodies"])
                            RemoveItemsFromPlayer(victim.player);

                        victim.deaths++;
                    }
                    else
                        return; // no victim?

                    var killer = dmPlayers.Any(x => x.player == info.Initiator.ToPlayer()) ? dmPlayers.First(x => x.player == info.Initiator.ToPlayer()) : null;

                    if (killer == null)
                        killer = victim.lastInitiator;

                    if (killer != null)
                    {
                        if (killer != victim)
                        {
                            killer.kills++;

                            if (info.isHeadshot)
                            {
                                killer.score += 100;
                                killer.headShotsDealt++;
                                victim.headShotsRecieved++;
                                PrintToChat(killer.player, $"<color=red>headshotKill+100</color>");
                            }
                            else
                            {
                                killer.score += 50;
                                PrintToChat(killer.player, $"<color=red>kill+50</color>");
                            }

                            if (displayDeathNotes)
                                PrintToPlayers($"{victim.player.displayName} was killed by {killer.player.displayName}. {(info.isHeadshot ? " In the head" : "")} With a {(info.Weapon.name)}");
                        }
                        else
                        {

                            if (displayDeathNotes)
                                PrintToPlayers($"{victim.player.displayName} died as of a self inflicted wound, suicide. Nobody's worth it.");
                            victim.currentLoadoutIndex = 0; // reset weapon pack to suicide pack..
                            return;
                        }
                    }
                    else
                    {
                        if (displayDeathNotes)
                            PrintToPlayers($"{victim.player.displayName} died as a result of [insert reason here].");
                    }

                }
            }
            catch (Exception ex)
            {
                PrintError($"Error in OnEntityDeath, {ex.Message}");
            }
        }

        #endregion

        #region methods / functions
        void PrintToPlayers(string message)
        {
            try
            {
                foreach (ModPlayers player in dmPlayers)
                    PrintToChat(player.player, message);
            }
            catch (Exception ex)
            {
                PrintError($"Error in PrintToPlayers, {ex.Message}");
            }

        }

        void PrintToNonPlayers(string message)
        {

            if (!doPrintToNonPlayers)
            {
                PrintToPlayers("> " + message);
                return;
            }

            try
            {
                foreach (BasePlayer b in BasePlayer.activePlayerList)
                    if (!dmPlayers.Where(x=>x.player.displayName == b.displayName).Any())
                        PrintToChat(b, message);
            }
            catch (Exception ex)
            {
                PrintError($"Error in PrintToNonPlayers, {ex.Message}");
            }
        }

        void DistanceRestrictionSystem(int iTime = 20)
        {

            try
            {
                if (!gameInProgress) return; // no game timer not required.

                foreach (var p in dmPlayers)
                {
                    // remove non active players
                    if (!BasePlayer.activePlayerList.Contains(p.player))
                    {
                        PrintToPlayers($"{p.player.displayName} has been removed from the game.");
                        dmPlayers.Remove(p);
                        break;
                    }

                    float lastDistance = 0;
                    bool toofar = true;

                    foreach (var d in data.positions)
                    {
                        p.outOfBounds = false;
                        toofar = false;
                        lastDistance = Vector3.Distance(p.player.transform.position, (d.GetVector3()));
                        if (lastDistance <= float.Parse(Config["maxDistanceFromSpawnPoints"].ToString()))
                            break;

                        toofar = true;
                    }

                    if (toofar)
                    {
                        p.outOfBounds = true;
                        PrintToPlayers($"{p.player.displayName} has gone too far from the spawn points.");
                        var dmPosition = RandomSpawnPoint();
                        p.player.transform.rotation = dmPosition.GetQuaternion();
                        Teleport(p.player, dmPosition.GetVector3());
                    }
                }

                timer.Once(iTime, () =>
                {
                    DistanceRestrictionSystem();
                });
            }
            catch (Exception ex)
            {
                PrintError($"Error in DistanceRestrictionSystem, {ex.Message}");
            }


        }

        void DespawnBackpackSystem(int iTimeSec = 20)
        {
            try
            {
                if (!gameInProgress) return; // no game timer not required.

                var distance = float.Parse(Config["maxDistanceFromSpawnPoints"].ToString());

                foreach (var d in data.positions)
                    DespawnObjectsWithinDistance(d.GetVector3(), "backpack", distance);

                timer.Once(iTimeSec, () =>
                {
                    DespawnBackpackSystem();
                });
            }
            catch (Exception ex)
            {
                PrintError($"Error in DespawnBackpackSystem, {ex.Message}");
            }

        }

        void DespawnObjectsWithinDistance(Vector3 position, string partialName ,float  distance)
        {
            try
            {
                var items = GameObject.FindObjectsOfType<BaseEntity>().Where(x => Vector3.Distance(x.transform.position, position) < distance);
                foreach (var obj in items)
                {                    
                    if (obj.ToString().ToLower().Contains(partialName))
                        obj.Kill(BaseNetworkable.DestroyMode.None);
                }
            }
            catch (Exception ex)
            {
                PrintError($"Error in despawnObjectsWIthinDistance, {ex.Message}");
            }


        }

        //todo: add to ModPlayer as Restore()
        //void InventoryToPlayer(BasePlayer player, ModPlayerInventory inventory)
        //{
        //    try
        //    {

        //        foreach (ModItem e in inventory.containerWear)
        //        {
        //            player.inventory.GiveItem(e.Item(), player.inventory.containerWear);
        //            player.SendNetworkUpdate();
        //        }

        //        foreach (ModItem e in inventory.containerMain)
        //        {
        //            player.inventory.GiveItem(e.Item(), player.inventory.containerMain);
        //            player.SendNetworkUpdate();
        //        }

        //        foreach (ModItem e in inventory.containerBelt)
        //        {
        //            player.inventory.GiveItem(e.Item(), player.inventory.containerBelt);
        //            player.SendNetworkUpdate();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        PrintError($"Error in InventoryToPlayer, {ex.Message}");
        //    }


        //}

        void EquipPlayer(BasePlayer player, bool removeItems = false)
        {
            try
            {
                var dmPlayer = dmPlayers.Where(x => x.player == player).First();
                dmPlayer.player.SendNetworkUpdate();

                // unlock wear container
                //dmPlayer.player.inventory.containerWear.SetFlag(ItemContainer.Flag.IsLocked, false);
                //dmPlayer.player.inventory.containerMain.SetFlag(ItemContainer.Flag.IsLocked, false);
                //dmPlayer.player.SendNetworkUpdate();

                if (removeItems)
                    RemoveItemsFromPlayer(dmPlayer.player);

                // food / water health
                dmPlayer.player.metabolism.calories.Add(1000);
                dmPlayer.player.metabolism.hydration.Add(1000);
                dmPlayer.player.health = 100;
                dmPlayer.player.UpdateRadiation(0);
                dmPlayer.Health = dmPlayer.player.health; // last health
                dmPlayer.player.SendNetworkUpdate();

                dmLoadouts[dmPlayer.currentLoadoutIndex].SupplyLoadout(dmPlayer.player);


                // lock up the wear container
                //dmPlayer.player.inventory.containerWear.SetFlag(ItemContainer.Flag.IsLocked, true);
                //dmPlayer.player.inventory.containerMain.SetFlag(ItemContainer.Flag.IsLocked, true);
                //dmPlayer.player.SendNetworkUpdate();

                dmPlayer.player.SendNetworkUpdate();

                PrintToChat(player, $"You now have the {dmLoadouts[dmPlayer.currentLoadoutIndex].name} loadout from the {dmLoadouts[dmPlayer.currentLoadoutIndex].category} set.");
            }
            catch (Exception ex)
            {
                PrintError($"Error in EquipPlayer, {ex.Message}");
            }


        }

        void RemoveItemsFromPlayer(BasePlayer player)
        {
            try
            {
                foreach (Item i in player.inventory.AllItems())
                {
                    i.Remove(0f);
                    player.SendNetworkUpdate();
                }
                //i.Drop(player.transform.position, -player.transform.up);
            }
            catch (Exception ex)
            {
                PrintError($"Error in RemoveItemsFromPlayer, {ex.Message}");
            }


        }

        ModSpawnPositions RandomSpawnPoint()
        {
            try
            {
                return data.positions[UnityEngine.Random.Range(0, data.positions.Count)];
            }
            catch (Exception ex)
            {
                PrintError($"Error in Unload, {ex.Message}");
                throw new Exception($"Error in Unload, {ex.Message}", ex);
            }
        }

        bool QuitPlayer(BasePlayer player, bool sleeping = true, int reasonCd = 0)
        {
            try
            {
                if (dmPlayers.Any(x => x.player == player))
                {
                    var dmPlayer = dmPlayers.Where(x => x.player == player).First();
                    
                    Teleport(dmPlayer.player, dmPlayer.originalPosition);
                    RemoveItemsFromPlayer(dmPlayer.player);
                    dmPlayer.RestoreInventory();

                    var score = dmPlayer.score + dmPlayer.bonusScrap;

                    if (score > 0)
                    {
                        var scraps = new ModItem("scrap", ModItem.ModItemType.Item, (int)score);
                        dmPlayer.player.GiveItem(scraps.Item(), BaseEntity.GiveItemReason.PickedUp);
                        PrintToPlayers($"<color=yellow>{dmPlayer.player.displayName}</color> has been awarded {score.ToString("0")} in scrap for their valor and bravery. It would be a shame if someone robbed them on their way home.");
                    }

                    if (reasonCd == 0)
                    {
                        PrintToPlayers($"<color=yellow>{dmPlayer.player.displayName}</color> has left Deathmatch with {dmPlayer.kills} kill{ ((dmPlayer.kills > 1) ? "s" : "") } and {dmPlayer.deaths} death{ ((dmPlayer.deaths > 1) ? "s" : "") }.");
                    }
                    else if (reasonCd == 1)
                    {
                        PrintToPlayers($"<color=yellow>{dmPlayer.player.displayName}</color> was kicked from the deathmatch because they were caught playing with themselves. Which is okay, just not here.");
                    }

                    dmPlayers.Remove(dmPlayer);

                    if (dmPlayers.Count <= 0)
                        gameInProgress = false;

                    return true;
                }
                return false;
            }

            catch (Exception ex)
            {
                PrintError($"Error in Unload, {ex.Message}");
                throw new Exception($"Error in QuitPlayer, {ex.Message}", ex);
            }

        }

        bool JoinPlayer(BasePlayer player, bool sleeping = true)
        {
            try
            {
                var dmPosition = RandomSpawnPoint();

                var p = new ModPlayers(player, player.transform.position, true);
                p.currentLoadoutIndex = 0;
                dmPlayers.Add(p);

                var dmPlayer = dmPlayers.Where(x => x.player == player).First();

                dmPlayer.player.SendNetworkUpdate();

                Teleport(dmPlayer.player, dmPosition.GetVector3());
                EquipPlayer(dmPlayer.player);

                dmPlayer.player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, sleeping);

                PrintToNonPlayers($"<color=yellow>{dmPlayer.player.displayName}</color> has joined DeathMatch.. to join them type '/dm' in chat. Gear is provided and you'll be returned to your original state. Unless you win, you'll be returned with some extra scrap.");

                if (!gameInProgress && dmPlayers.Count > 0)
                {
                    gameInProgress = true;

                    DistanceRestrictionSystem(20);
                    DespawnBackpackSystem(20);
                    //AutoLightingInit();

                    //timer.Once(30, () =>
                    //{
                    //    if (dmPlayers.Count < 2)
                    //        QuitPlayer(player, false, 1);
                    //});
                }

                return true;
            }
            catch (Exception ex)
            {
                PrintError($"Error in JoinPlayer, {ex.Message}");
                return false;
            }
        }

        Vector3 GetGroundPosition(Vector3 sourcePos)
        {
            try
            {
                RaycastHit hitInfo;

                if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo))
                    sourcePos.y = hitInfo.point.y;
                sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));

                return sourcePos;
            }
            catch (Exception ex)
            {
                PrintError($"Error in GetGroundPosition, {ex.Message}");
                throw new Exception($"Error in GetGroundPosition, {ex.Message}", ex);
            }

        }

        bool isWithinSpawnPosition(Vector3 location, float maxDistance)
        {
            try
            {
                foreach (var d in data.positions)
                {
                    if (Vector3.Distance(location, d.GetVector3()) <= maxDistance)
                        return true;
                }
            }
            catch (Exception ex)
            {
                PrintError($"Error in isWithinSpawnPosition, {ex.Message}");
            }
            return false;

        }

        #endregion

        #region ChatCommands
        //[ConsoleCommand("spawnsc")]
        //void spawnsc(ConsoleSystem.Arg arg)
        //{

        //    var prefabName = "assets/prefabs/npc/scientist/scientist.prefab";

        //    try
        //    {
        //        var entity = GameManager.server.CreateEntity(prefabName);
        //        var dmPosition = RandomSpawnPoint();
        //        entity.transform.position = dmPosition.GetVector3();
        //        entity.Spawn();

        //    }
        //    catch (Exception ex)
        //    {
        //        PrintError($"ERROR: Error creating entity. {ex.Message}");
        //    }

        //}


        [ChatCommand("dm")]
        void dmCommand(BasePlayer player, string command, string[] args)
        {
            try
            {
                if (!permission.UserHasPermission(player.UserIDString, playPriv))
                {
                    PrintToChat(player, "You do not have permission to play deathmatch.");
                    return;
                }

                #region player join/quit
                if (args.Length == 0)
                {
                    if (data.positions.Count() < 1)
                    {
                        PrintToChat(player, $"There are no positions created/saved. DeathMatch is not set up.");
                        return;
                    }

                    if (dmPlayers.Where(x => x.player == player).Any()) // already a player QUIT
                        QuitPlayer(player);
                    else
                    {
                        if (player.playerFlags != BasePlayer.PlayerFlags.Wounded)
                            JoinPlayer(player);
                    }

                    if (dmPlayers.Count() > 1)
                        PrintToPlayers($"There are now {dmPlayers.Count()} Deathmatch Players.");
                    else
                        PrintToPlayers($"There is only ONE Deathmatch Player.");

                    return;
                }
                #endregion

                if (!permission.UserHasPermission(player.UserIDString, adminPriv))
                {
                    PrintToChat(player, "You do not have permission to admin deathmatch.");
                    return;
                }

                if (args.Length < 1) return;

                if ((args[0].ToLower() == "?") || (args[0].ToLower() == "help"))
                {
                    PrintToChat(player, "/dm timer - toggles loadout mode.");
                    PrintToChat(player, "/dm set - sets spawn point.");
                    PrintToChat(player, "/dm clear - clears spawn points.");

                    return;
                }


                #region set positoin

                if (args[0].ToLower() == "loadout")
                {
                    if (args.Length > 1)
                    {
                        dmLoadout = args[1].ToLower();
                        dmLoadouts = loadouts.loadouts.Where(x => x.category.ToString().ToLower() == dmLoadout).ToList<PlayerLoadout>();
                        PrintToChat(player, $"DM loadout set to {dmLoadout}");
                    }
                    else
                        PrintToChat(player, $"usage: /dm loadout \"Lodout Name\"");
                    return;

                }

                #endregion


                #region set positoin

                if (args[0].ToLower() == "set")
                {
                    //var DropPosition = GetGroundPosition(player.transform.position + (player.eyes.BodyForward() * 2));
                    var s = new ModSpawnPositions(player.transform.position);
                    s.SetQuaternion(player.transform.rotation);

                    if (data.positions == null)
                        data.positions = new List<ModSpawnPositions>();

                    data.positions.Add(s);
                    Interface.Oxide.DataFileSystem.WriteObject(dataFileName, data);
                    PrintToChat(player, $"Position {player.transform.position}, {data.positions.Count()} Deathmatch  positions.");
                    return;
                }
                #endregion

                #region clear positoins
                if (args[0] == "clear")
                {
                    data.positions.Clear();
                    Interface.Oxide.DataFileSystem.WriteObject(dataFileName, data);
                    PrintToChat(player, "DM Positions cleared.");
                    return;
                }
                #endregion

                PrintToChat(player, $"{args[0]} is not a command.");

            }
            catch (Exception ex)
            {
                PrintError($"Error in dmCommand, {ex.Message}");
            }


        }
        #endregion

        #region Teleport

        public void TeleportToPlayer(BasePlayer player, BasePlayer target) => Teleport(player, target.transform.position);

        public void TeleportToPosition(BasePlayer player, float x, float y, float z) => Teleport(player, new Vector3(x, y, z));

        public void Teleport(BasePlayer player, Vector3 position)
        {
            //SaveLocation(player);
            //teleporting.Add(player.userID);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading");
            StartSleeping(player);
            player.MovePosition(position);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", position);
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            //player.UpdatePlayerCollider(true, false);
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            //TODO temporary for potential rust bug
            try
            {
                player.ClearEntityQueue(null);
            }
            catch
            {
            }
            player.SendFullSnapshot();
        }

        private void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping())
                return;

            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);

            if (!BasePlayer.sleepingPlayerList.Contains(player))
                BasePlayer.sleepingPlayerList.Add(player);

            player.CancelInvoke("InventoryUpdate");

            //player.inventory.crafting.CancelAll(true);
            //player.UpdatePlayerCollider(true, false);
        }

        #endregion

        #region despawn dead bodies 
        private void OnEntitySpawned(BaseEntity entity) => ResetCorpseTime(entity, bodyDecayInSeconds);

        private void OnEntityTakeDamage(BaseEntity entity, HitInfo info) => ResetCorpseTime(entity, bodyDecayInSeconds);

        private void OnLootEntityEnd(BasePlayer looter, BaseEntity entity) => ResetCorpseTime(entity, bodyDecayInSeconds);

        private void ResetCorpseTime(BaseEntity entity, float Duration)
        {
            try
            {
                if (!gameInProgress) return; // no game timer not required.

                var corpse = entity as BaseCorpse;

                if (!corpse) return;
                if (!(corpse is PlayerCorpse) && !corpse?.parentEnt?.ToPlayer()) return;
                if (!dmPlayers.Select(x => x.player == corpse?.parentEnt?.ToPlayer()).Any()) return;

                timer.Once(1, () =>
                {
                    if (!corpse.IsDestroyed)
                        corpse.ResetRemovalTime(Duration);
                });
            }
            catch (Exception ex)
            {
                PrintError($"Error ResetCorpseTime " + ex.StackTrace);
            }

        }
        #endregion

        #region spawning barrels
        void SpawnBarrel(Vector3 position)
        {
            try
            {
                var p = $"assets/bundled/prefabs/autospawn/resource/loot/{RandBarrel()}";
                if (position == Vector3.zero)
                {
                    var dmPosition = RandomSpawnPoint().GetVector3();
                    var flt = 20; // with x meters of spawnpoint.
                    position = GetGroundPosition(new Vector3(
                        UnityEngine.Random.Range(dmPosition.x - flt, dmPosition.x + flt),
                        dmPosition.y,
                        UnityEngine.Random.Range(dmPosition.z - flt, dmPosition.z + flt)
                    ));
                }

                var entity = GameManager.server.CreateEntity(p, position);
                
                entity.Spawn();

                bool addLoot = false;

                if (addLoot)
                {
                    var lootContainer = entity as LootContainer;

                    //lootContainer.inventory.Clear(); <- error now?
                    lootContainer.inventory.itemList.Clear();

                    lootContainer.inventory.AddItem(ItemManager.FindItemDefinition(RandHealth()), 2);
                    lootContainer.inventory.AddItem(ItemManager.FindItemDefinition(RandAmmo()), 12);

                    if (UnityEngine.Random.Range(0, 100) > 75)
                        lootContainer.inventory.AddItem(ItemManager.FindItemDefinition(RandAddon()), 1);
                }

                //if ((dmPlayers.Count() > 0) && (BasePlayer.activePlayerList.Count() > 0))
                //    timer.Once(UnityEngine.Random.Range(1, 30), () => { entity.Kill(BaseNetworkable.DestroyMode.None); SpawnBarrel(Vector3.zero); });

            }
            catch (Exception ex)
            {
                PrintError($"ERROR: Error creating barrel entity. {ex.Message}");
            }
        }

        string RandAmmo()
        {
            List<string> items = new List<string>();

            items.AddRange(new[] { "ammo.handmade.shell", "ammo.nailgun.nails", "ammo.pistol", "ammo.pistol.fire", "ammo.pistol.hv", "ammo.rifle", "ammo.rifle.explosive", "ammo.rifle.hv", "ammo.rifle.incendiary", "ammo.shotgun", "ammo.shotgun.slug", "arrow.wooden", "arrow.hv" });
            // rockets: "ammo.rocket.basic", "ammo.rocket.fire", "ammo.rocket.hv", "ammo.rocket.smoke"
            return items[UnityEngine.Random.Range(0, items.Count())];

        }

        string RandHealth()
        {
            string[] items = { "bandage", "syringe.medical", "largemedkit" };
            return items[UnityEngine.Random.Range(0, items.Count())];

        }

        string RandBarrel()
        {
            string[] items = { "loot-barrel-1.prefab", "loot-barrel-2.prefab", "trash-pile-1.prefab" };
            return items[UnityEngine.Random.Range(0, items.Count())];
        }

        string RandAddon()
        {
            string[] items = { "weapon.mod.flashlight", "weapon.mod.holosight", "weapon.mod.lasersight", "weapon.mod.muzzleboost", "weapon.mod.muzzlebrake", "weapon.mod.silencer", "weapon.mod.simplesight", "weapon.mod.small.scope" };
            return items[UnityEngine.Random.Range(0, items.Count())];
        }
        #endregion

        #region control ovens and lights
        void AutoLightingInit()
        {
            try
            {
                timer.Repeat(30f, 0, () =>
                {
                    try
                    {
                        if (dmPlayers.Count <= 0) return;

                        string[] lighting = { "lantern", "searchlight", "tunalight", "ceilinglight", "furnace", "furnace.large", "skull_fire_pit", "campfire" };

                        var hour = TimeSpan.FromHours(ConVar.Env.time).Hours;

                        if (hour >= 7 && hour < 19) // daytime.. 7am and 7pm 
                        {
                            foreach (var entry in GameObject.FindObjectsOfType<BaseOven>())
                                if (lighting.Any(entry.PrefabName.Contains))
                                    if (isWithinSpawnPosition(entry.transform.position, float.Parse(Config["maxDistanceFromSpawnPoints"].ToString())))
                                        entry.SetFlag(BaseEntity.Flags.On, false);

                            //changeSearchlightOwner(false);
                        }
                        else                        // nighttime..
                        {
                            foreach (var entry in GameObject.FindObjectsOfType<BaseOven>())
                                if (lighting.Any(entry.PrefabName.Contains))
                                    if (isWithinSpawnPosition(entry.transform.position, float.Parse(Config["maxDistanceFromSpawnPoints"].ToString())))
                                        entry.SetFlag(BaseEntity.Flags.On, true);

                            //changeSearchlightOwner(true);
                        }
                    }
                    catch (Exception ex)
                    {
                        PrintError("Error AutoLightingInit:repeat  " + ex.Message);
                    }

                });
            }
            catch (Exception ex)
            {
                PrintError($"Error AutoLightingInit {ex.StackTrace}");
            }
        }
        #endregion

        #region Loadouts (aka custom Kits)

        class Loadouts
        {
            public List<PlayerLoadout> loadouts;

            public Loadouts()
            {
                loadouts = new List<PlayerLoadout>();
            }
                
            public bool AddPlayersLoadout(BasePlayer player, string Name, string Category = "default")
            {
                try
                {
                    loadouts.Add(new PlayerLoadout(player) { name = Name, category = Category });
                    return true;
                }
                catch (Exception ex)
                {
                    throw new Exception("Loadouts Error: AddPlayersLoadout:" + ex.StackTrace);
                }
            }

            public bool ReplacePlayersLoadout(BasePlayer player, string Name, string Category = "default")
            {
                try
                {
                    if (loadouts.Any(x => x.name.ToString() == Name && x.category.ToString() == Category))
                    {
                        var index = loadouts.IndexOf(loadouts.First(x => x.name.ToString() == Name && x.category.ToString() == Category));
                        loadouts.RemoveAt(index);
                        loadouts.Insert(index, new PlayerLoadout(player) { name = Name, category = Category });
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    throw new Exception("Loadouts Error: AddPlayersLoadout:" + ex.StackTrace);
                }
            }

            public bool LoadoutExistByName(string Name, string Category = "default")
            {
                return (loadouts.Any(x => x.category.ToString() == Category && x.name.ToString() == Name));
            }

        }

        class PlayerLoadout
        {
            public object name { get; set; }
            public object category { get; set; }
            public List<ModItem> containerWear = new List<ModItem>();
            public List<ModItem> containerBelt = new List<ModItem>();
            public List<ModItem> containerMain = new List<ModItem>();

            public PlayerLoadout()
            {
                name = "";
                category = "default";
                containerWear = new List<ModItem>();
                containerBelt = new List<ModItem>();
                containerMain = new List<ModItem>();
            }

            public PlayerLoadout(string Name)
            {
                name = Name;

                containerWear = new List<ModItem>();
                containerBelt = new List<ModItem>();
                containerMain = new List<ModItem>();
            }

            public PlayerLoadout(BasePlayer player)
            {
                try
                {
                    containerWear = new List<ModItem>();
                    containerBelt = new List<ModItem>();
                    containerMain = new List<ModItem>();

                    foreach (var i in player.inventory.containerWear.itemList)
                    {
                        if (i != null)
                        {
                            var ii = new ModItem(i);
                            containerWear.Add(ii);
                            player.SendNetworkUpdate();
                        }
                    }

                    foreach (var i in player.inventory.containerMain.itemList)
                    {
                        if (i != null)
                        {
                            var ii = new ModItem(i);
                            containerMain.Add(ii);
                            player.SendNetworkUpdate();
                        }
                    }

                    foreach (var i in player.inventory.containerBelt.itemList)
                    {
                        if (i != null)
                        {
                            var ii = new ModItem(i);
                            containerBelt.Add(ii);
                            player.SendNetworkUpdate();
                        }
                    }

                }
                catch (Exception ex)
                {
                    throw new Exception("Error PlayerLoadout:", ex);
                }
            }

            public bool SupplyLoadout(BasePlayer Player)
            {
                try
                {
                    foreach (Item i in Player.inventory.AllItems())
                    {
                        i.Remove(0f);
                        Player.SendNetworkUpdate();
                    }

                    //Player.inventory.containerMain.Clear();
                    //Player.inventory.containerWear.Clear();
                    //Player.inventory.containerBelt.Clear();

                    Player.inventory.containerMain.itemList.Clear();
                    Player.inventory.containerWear.itemList.Clear();
                    Player.inventory.containerBelt.itemList.Clear();


                    foreach (ModItem e in containerWear)
                    {
                        var tmp = e.Item();
                        tmp.MarkDirty();

                        Player.inventory.GiveItem(tmp, Player.inventory.containerWear);
                        Player.SendNetworkUpdate();
                    }

                    foreach (ModItem e in containerMain)
                    {
                        var tmp = e.Item();
                        tmp.MarkDirty();


                        Player.inventory.GiveItem(tmp, Player.inventory.containerMain);
                        Player.SendNetworkUpdate();
                    }

                    foreach (ModItem e in containerBelt)
                    {
                        var tmp = e.Item();
                        tmp.MarkDirty();

                        Player.inventory.GiveItem(tmp, Player.inventory.containerBelt);
                        Player.SendNetworkUpdate();
                    }
                    return true;    
                }
                catch (Exception ex)
                {
                    throw new Exception("Error ArenaPlayer:SupplyLoadout:", ex);
                }
            }


        }

        class ModItem
        {
            public string equipName;
            public int amount;
            public ulong skinId;

            public ModItemType etype;

            public List<string> addons = new List<string>();

            public ModItem()
            {
                addons = new List<string>();
            }

            public ModItem(string EquipName, ModItemType type, int Amount = 1)
            {
                equipName = EquipName;
                amount = Amount;
                etype = type;
                addons = new List<string>();
            }

            public ModItem(string EquipName, string[] Addons, int AmmoAmount = -1)
            {
                equipName = EquipName;
                amount = 1;
                etype = ModItem.ModItemType.Weapon;
                addons = new List<string>();
                foreach (string s in Addons)
                    addons.Add(s);
            }

            public ModItem(Item item, int Amount)
            {
                try
                {
                    var id = ItemManager.FindItemDefinition(item.info.itemid);
                    equipName = id.shortname;
                    amount = Amount;
                    skinId = item.skin;

                    if (item.contents != null)
                    {
                        foreach (Item i in item.contents.itemList)
                        {
                            var iid = ItemManager.FindItemDefinition(i.info.itemid);
                            addons.Add(iid.shortname);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error ModItem(Item item, int Amount):{ex.Message}", ex);
                }
            }

            public ModItem(Item item)
            {
                try
                {
                    var id = ItemManager.FindItemDefinition(item.info.itemid);
                    equipName = id.shortname;
                    amount = item.amount;
                    skinId = item.skin;

                    if (item.contents != null)
                    {
                        foreach (Item i in item.contents.itemList)
                        {
                            var iid = ItemManager.FindItemDefinition(i.info.itemid);
                            addons.Add(iid.shortname);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error ModItem(Item item):", ex);
                }

            }

            public Item Item()
            {
                try
                {
                    var definition = ItemManager.FindItemDefinition(this.equipName);

                    if (definition != null)
                    {
                        Item item = ItemManager.CreateByItemID((int)definition.itemid, this.amount, skinId);

                        if (this.etype == ModItem.ModItemType.Weapon)
                        {
                            // If weapon fill magazine to capacity
                            var weapon = item.GetHeldEntity() as BaseProjectile;
                            if (weapon != null)
                            {
                                (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = weapon.primaryMagazine.capacity;
                            }

                            foreach (var a in this.addons)
                            {
                                var addonDef = ItemManager.FindItemDefinition(a);
                                Item addonItem = ItemManager.CreateByItemID((int)addonDef.itemid, 1);
                                item.contents.AddItem(addonItem.info, 1);
                            }
                        }
                        return item;
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error ModItem:Item:{ex.Message}", ex);
                }
            }
            public enum ModItemType
            {
                Weapon,
                Item
            }

        }

        //[ChatCommand("testcmd")]
        //void testcmd(BasePlayer player, string command, string[] args)
        //{
        //    PrintToChat(player, $"{args.Count()}");
        //}

        int IndexOfLoadout(string name, string category = "default")
        {
            var retval = -1;
            retval = loadouts.loadouts.IndexOf(loadouts.loadouts.Where(x => x.name.ToString().ToLower() == name.ToLower()).First());
            return retval;
        }

        [ChatCommand("loadout")]
        void chatCommandLoadout(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, adminPriv))
                return;

            int index = -1;
            string category = "default";
            string name = "";

            if (args.Count() == 0) // list loadouts
            {
                PrintToChat(player, $"<size=12><color=yellow>usage:</color></size>");
                PrintToChat(player, $"<size=12><color=yellow>        /loadout</color> - <color=white>Lists Loadout Categories.</color></size>");
                PrintToChat(player, $"<size=12><color=yellow>        /loadout \"category\"</color> - <color=white>Lists items in Loadout Category.</color></size>");
                PrintToChat(player, $"<size=12><color=yellow>        /loadout add \"Name\" \"category\"</color> - <color=white>Adds an item to a category.</color></size>");
                PrintToChat(player, $"<size=12><color=yellow>        /loadout rem \"Name\" \"category\"</color> - <color=white>Removes an item from a category.</color></size>");
                PrintToChat(player, $"<size=12><color=yellow>        /loadout use \"Name\" \"category\"</color> - <color=white>Removes an item.</color></size>");
                PrintToChat(player, $"<size=12><color=yellow>        /loadout clear [\"category\"]</color></size>");

                PrintToChat(player, $"<size=16><color=red>Listing Loadout Categories:</color></size>");
                var xx = loadouts.loadouts.GroupBy(x => x.category).Select(x => x.FirstOrDefault()).Select(x=>x.category.ToString()).ToArray();
                foreach (var l in xx)
                    PrintToChat(player, $"<size=12><color=yellow>{l}</color></size>");
                return;
            }

            if (args[0].ToLower().StartsWith("rem")) // remove
            {
                if (args.Count() < 2)
                {
                    PrintToChat(player, $"<size=12><color=yellow>usage:  /loadout {args[0].ToLower()} \"Name\" \"category\"</color></size>");
                    return;
                }

                if (args.Count() > 1)
                    name = args[1];

                if (args.Count() > 2)
                    category = args[2];

                index = IndexOfLoadout(name, category);
                if (index < 0)
                {
                    PrintToChat(player, $"<size=12><color=yellow>Did not find {name} in {category}.</color></size>");
                    return;
                }

                loadouts.loadouts.RemoveAt(index);
                Interface.Oxide.DataFileSystem.WriteObject(loadoutFileName, loadouts);
                PrintToChat(player, $"<size=12><color=orange>Loadout removed {name} from {category}.</color></size>");
                return;
            }

            if (args[0].ToLower() == "add")
            {
                if (args.Count() < 2)
                {
                    PrintToChat(player, $"<size=12><color=yellow>usage:  /loadout {args[0].ToLower()} \"Name\" \"category\"</color></size>");
                    return;
                }

                if (args.Count() > 1)
                    name = args[1];
                if (args.Count() > 2)
                    category = args[2];

                if (loadouts.LoadoutExistByName(name, category))
                {
                    PrintToChat(player, $"<size=12><color=orange>Copying your attire, inventory and hotbar and replacing {name} in the {category} category..</color></size>");
                    loadouts.ReplacePlayersLoadout(player, name, category);
                }
                else
                {
                    PrintToChat(player, $"<size=12><color=orange>Copying your attire, inventory and hotbar to {name} in the {category} category..</color></size>");
                    loadouts.AddPlayersLoadout(player, name, category);
                }

                Interface.Oxide.DataFileSystem.WriteObject(loadoutFileName, loadouts);
                return;
            }

            if (args[0].ToLower() == "use")
            {
                if (args.Count() < 2)
                {
                    PrintToChat(player, $"<size=12><color=yellow>usage:  /loadout {args[0].ToLower()} \"Name\" \"category\"</color></size>");
                    return;
                }

                if (args.Count() > 1)
                    name = args[1];
                if (args.Count() > 2)
                    category = args[2];

                index = IndexOfLoadout(name, category);
                if (index < 0)
                {
                    //PrintToChat(player, $"<size=12><color=yellow>Did not find {name} in {category}.</color></size>");
                    PrintToChat(player, $"Did not find {name} in {category}.");
                    return;
                }

                loadouts.loadouts[index].SupplyLoadout(player);
                //PrintToChat(player, $"<size=12><color=orange>Fits perfect..</color></size>");
                PrintToChat(player, $"Fits perfect..");
                return;
            }

            if (args[0].ToLower() == "clear")
            {

                if (args.Count() == 1)
                    loadouts.loadouts.Clear();
                else {
                    category = args[1];

                    index = loadouts.loadouts.IndexOf(loadouts.loadouts.Where(x => x.category.ToString().ToLower() == category).First());

                    if (index == -1)
                    {
                        PrintToChat(player, $"<size=12><color=red>No Loadouts category called {category}..</color></size>");
                        return;
                    }

                    while (index != -1)
                    {
                        loadouts.loadouts.RemoveAt(index);
                        index = loadouts.loadouts.IndexOf(loadouts.loadouts.Where(x => x.category.ToString().ToLower() == category).First());
                    }
                }
                Interface.Oxide.DataFileSystem.WriteObject(loadoutFileName, loadouts);

                PrintToChat(player, $"<size=12><color=red>Loadouts cleared for {category}..</color></size>");
                return;
            }

            if (loadouts.loadouts.Where(x => x.category.ToString().ToLower() == args[0].ToLower()).AsEnumerable<PlayerLoadout>().Count() > 0)
            {
                PrintToChat(player, $"<size=16><color=red>Listing {args[0]} loadouts:</color></size>");

                foreach (var l in loadouts.loadouts.Where(x => x.category.ToString().ToLower() == args[0].ToLower()).AsEnumerable<PlayerLoadout>())
                    PrintToChat(player, $"<size=12><color=white>{l.name}</color></size>");
            }

        }

        #endregion

        #region classes enums

        class SaveableData
        {
            public List<ModSpawnPositions> positions;
            public SaveableData()
            {
                positions = new List<ModSpawnPositions>();
            }
        }

        class ModSpawnPositions
        {
            // I cannot save Vector3/Quaternion in a config file so I made my own.
            //private vector3 spawnPosition;
            //private quaternion rotation;
            public vector3 spawnPosition { get; set; }
            public quaternion rotation { get; set; }

            public ModSpawnPositions(Vector3 Drop)
            {
                spawnPosition = new vector3 { x = Drop.x, y = Drop.y, z = Drop.z };
                rotation = new quaternion { x = 0, y = 0, z = 0, w = 0 };
            }
            public ModSpawnPositions()
            {
                spawnPosition = new vector3 { x = 0, y = 0, z = 0 };
                rotation = new quaternion { x = 0, y = 0, z = 0, w = 0 };
            }

            public override string ToString()
            {
                return $"Position {spawnPosition.ToString()}, Rotation {rotation.ToString()}";
            }

            public Vector3 GetVector3()
            {
                try
                {
                    return new Vector3(spawnPosition.x, spawnPosition.y, spawnPosition.z);

                }
                catch (Exception e)
                {
                    throw new Exception("ModSpawnPositions:GetVector3", e);
                }
            }
            public void SetVector3(Vector3 value)
            {
                spawnPosition = new vector3() { x = value.x, y = value.y, z = value.z };
            }

            public Quaternion GetQuaternion()
            {
                return new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
            }
            public void SetQuaternion(Quaternion value)
            {
                rotation = new quaternion() { w = value.w, x = value.x, y = value.y, z = value.z };
            }

            public class quaternion
            {
                public float x;
                public float y;
                public float z;
                public float w;
                public quaternion()
                {
                }
                public override string ToString()
                {
                    return $"(x:{x}, y:{y}, z:{z}, w:{w})";
                }
            }
            public class vector3
            {
                public float x;
                public float y;
                public float z;
                public vector3()
                {

                }
                public override string ToString()
                {
                    return $"(x:{x}, y:{y}, z:{z})";
                }
            }

        }

        class ModPlayers
        {
            public Vector3 originalPosition;
            public ModPlayerInventory originalInventory;
            public BasePlayer player;
            public ModPlayers lastInitiator;
            public int kills = 0;
            public int deaths = 0;
            public int currentLoadoutIndex = 1;
            public int headShotsRecieved = 0;
            public int headShotsDealt = 0;
            public bool warnedAboutLeavingTheArea = false;
            public bool outOfBounds = false;
            public float Health = 0f;
            public float score = 0f;


            //finders_keepers
            public long bonusScrap = 0;


            public void RestoreInventory()
            {
                try
                {
                    var inventory = originalInventory;

                    foreach (ModItem e in inventory.containerWear)
                    {
                        player.inventory.GiveItem(e.Item(), player.inventory.containerWear);
                        player.SendNetworkUpdate();
                    }

                    foreach (ModItem e in inventory.containerMain)
                    {
                        player.inventory.GiveItem(e.Item(), player.inventory.containerMain);
                        player.SendNetworkUpdate();
                    }

                    foreach (ModItem e in inventory.containerBelt)
                    {
                        player.inventory.GiveItem(e.Item(), player.inventory.containerBelt);
                        player.SendNetworkUpdate();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error in RestoreInventory", ex);
                }

            }

            public ModPlayers(BasePlayer basePlayer, Vector3 OriginalPosition, bool copyInventory = false)
            {
                try
                {

                    player = basePlayer;
                    originalPosition = OriginalPosition;
                    originalInventory = new ModPlayerInventory("originalInventory");
                    if (copyInventory)
                        originalInventory.PlayerToInventory(player);

                    Health = player.health;
                }
                catch (Exception ex)
                {
                    throw new Exception("Error ModPlayers:", ex);
                }
            }
        }

        class ModPlayerInventory
        {
            public object name { get; set; }
            public List<ModItem> containerWear = new List<ModItem>();
            public List<ModItem> containerBelt = new List<ModItem>();
            public List<ModItem> containerMain = new List<ModItem>();

            public ModPlayerInventory(object packName = null)
            {
                if (packName != null)
                    name = packName;
                else
                    name = "";

                containerWear = new List<ModItem>();
                containerBelt = new List<ModItem>();
                containerMain = new List<ModItem>();
            }

            public void PlayerToInventory(BasePlayer player)
            {
                try
                {

                    containerWear = new List<ModItem>();
                    containerBelt = new List<ModItem>();
                    containerMain = new List<ModItem>();

                    foreach (var i in player.inventory.containerWear.itemList)
                    {
                        containerWear.Add(new ModItem(i));
                        player.SendNetworkUpdate();
                    }

                    foreach (var i in player.inventory.containerMain.itemList)
                    {
                        containerMain.Add(new ModItem(i));
                        player.SendNetworkUpdate();
                    }

                    foreach (var i in player.inventory.containerBelt.itemList)
                    {
                        containerBelt.Add(new ModItem(i));
                        player.SendNetworkUpdate();
                    }

                }
                catch (Exception ex)
                {
                    throw new Exception("Error CopyInventory:", ex);
                }
            }

        }

        #endregion

        #region finders_keepers


        #endregion




    }
}

