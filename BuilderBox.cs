using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Rust;
using Oxide.Core.Configuration;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("BuilderBox", "CARNY666", "1.1.2")]
    [Description("Builder's Boxes.")]
    class BuilderBox : RustPlugin
    {
        private List<BaseEntity> spawnedEntities = new List<BaseEntity>();

        #region events

        private const string usePermission = "builderbox.use"; // oxide.grant user/group builderbox.use

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

            foreach (UserData s in BuilderBoxUsers)
                foreach (var t in s.boxes)
                    t.Kill(BaseNetworkable.DestroyMode.None);

        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            var user = GetUser(player, true);

            if (user != null)
                foreach (var b in user.boxes)
                    b.Kill(BaseNetworkable.DestroyMode.None);
        }
        #endregion

        #region commands
        [ChatCommand("box")]
        void Box(BasePlayer player, string command, string[] args)
        {
            if (!CheckAccess(player, "box")) return;

            var user = GetUser(player, false);

            if (args.Count() > 0)
            {
                var boxnum = 0;

                #region kill
                if (args[0].ToLower() == "kill")
                {
                    PrintToChat(player, $"{user.boxes.Count} boxes will be removed.");
                    if (user.boxes.Count > 0)
                        foreach (var b in user.boxes)
                            b.Kill(BaseNetworkable.DestroyMode.None);

                    user.boxes.Clear();
                    return;
                }
                #endregion

                #region all
                if (args[0].ToLower() == "all")
                {
                    if (args.Count() > 1)
                        BuildersBoxes(player, args[1].ToString());
                    else
                        BuildersBoxes(player);
                    return;
                }
                #endregion


                #region box id  (by id)
                var id = ItemManager.itemList.Where(item => item.itemid.ToString() == args[0].ToLower()).ToList();
                if (id.Count > 0)
                {                    
                    var box = CreateBuildersBox(new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z + boxnum++), player);
                    user.boxes.Add(box);

                    var definition = ItemManager.FindItemDefinition(id[0].shortname);
                    PrintToChat(player, $"<color=blue>One exact match: </color> '{id[0].shortname}'; {id[0].itemid}; {definition.displayName.english}\r\n");
                    while (AddItemToItemContainer(player, id[0].shortname, definition.stackable, ref box)) ;

                    PrintToChat(player, $"Type <color=green>/box kill</color> to remove any old boxes.\r\n\r\n");
                    return;
                }                
                #endregion
                

                var matchingList = ItemManager.itemList.Where(item => item.shortname.ToLower().Contains(args[0].ToLower())).ToList();
                if (matchingList.Count < 1)
                {
                    PrintToChat(player, $"<color=red>Nothing Found</color> with '{args[0].ToLower()}' winthin it.");
                    return;
                }
                else
                {
                    // show list
                    var outp = "";
                    foreach (ItemDefinition s in matchingList)
                        outp += $"{clrIt(args[0].ToLower(), s.shortname.ToLower())}; {s.itemid.ToString()};  {s.displayName.english}\r\n ";
                    PrintToChat(player, $"<color=red>Found</color> <color=yellow>{matchingList.Count} matching entries</color>:\r\n {outp}\r\n\r\n");
                }

                var exactList = ItemManager.itemList.Where(item => item.shortname.ToLower() == args[0].ToLower()).ToList();
                if (exactList.Count == 1)
                {
                    var box = CreateBuildersBox(new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z + boxnum++), player);
                    user.boxes.Add(box);

                    var definition = ItemManager.FindItemDefinition(exactList[0].shortname);
                    PrintToChat(player, $"<color=blue>One exact match: </color> '{exactList[0].shortname}'; {exactList[0].itemid}; {definition.displayName.english}\r\n");
                    while (AddItemToItemContainer(player, exactList[0].shortname, definition.stackable, ref box)) ;

                    PrintToChat(player, $"Type <color=green>/box kill</color> to remove any old boxes.\r\n\r\n");

                    return;
                }

                var box2 = CreateBuildersBox(new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z + boxnum++), player);
                user.boxes.Add(box2);

                foreach (var s in matchingList)
                {
                    var definition = ItemManager.FindItemDefinition(s.shortname);
                    if (!AddItemToItemContainer(player, s.shortname, definition.stackable, ref box2))
                    {
                        box2 = CreateBuildersBox(new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z + boxnum++), player);
                        user.boxes.Add(box2);

                        if (matchingList.Count > 1) // add a box of them
                            AddItemToItemContainer(player, s.shortname, definition.stackable, ref box2);
                        else
                        {
                            while (AddItemToItemContainer(player, s.shortname, definition.stackable, ref box2));
                        }
                    }
                }
            }
            else
            {
                PrintToChat(player, "Try: <color=yellow>/box metal</color> or <color=yellow>/box 1882709339</color>, also <color=red>/box kill</color> and <color=green>/box all</color>.");
            }
        }
        string clrIt(string searchTerm, string textIn, string tcolor = "yellow", string dcolor = "green")
        {
            if (!textIn.ToLower().Contains(searchTerm.ToLower())) return "";
            return $"<color=\"{dcolor}\">{textIn.Replace(searchTerm, $"<color=\"{tcolor}\">{searchTerm}</color>")}</color>";
        }

        #endregion

        #region item manip
        bool IsComponent(ItemDefinition item)
        {
            var retVal = false;
            if (item.shortname.ToLower() == "smgbody")
                retVal = true;
            if (item.shortname.ToLower() == "riflebody")
                retVal = true;
            if (item.shortname.ToLower() == "semibody")
                retVal = true;
            if (item.shortname.ToLower() == "scrap")
                retVal = true;
            if (item.shortname.ToLower() == "fuse")
                retVal = true;
            if (item.shortname.ToLower() == "propanetank")
                retVal = true;
            if (item.shortname.ToLower() == "gears")
                retVal = true;
            if (item.shortname.ToLower() == "metalblade")
                retVal = true;
            if (item.shortname.ToLower() == "metalpipe")
                retVal = true;
            if (item.shortname.ToLower() == "metalspring")
                retVal = true;
            if (item.shortname.ToLower() == "roadsigns")
                retVal = true;
            if (item.shortname.ToLower() == "rope")
                retVal = true;
            if (item.shortname.ToLower() == "sewingkit")
                retVal = true;
            if (item.shortname.ToLower() == "sheetmetal")
                retVal = true;
            if (item.shortname.ToLower() == "tarp")
                retVal = true;
            if (item.shortname.ToLower() == "techparts")
                retVal = true;

            return retVal;
        }

        bool IsResoruce(ItemDefinition item)
        {
            var retVal = false;
            if (item.shortname.ToLower() == "stones")
                retVal = true;
            if (item.shortname.ToLower() == "wood")
                retVal = true;
            if (item.shortname.ToLower() == "bone.fragments")
                retVal = true;
            if (item.shortname.ToLower() == "metal.fragments")
                retVal = true;
            if (item.shortname.ToLower() == "metal.refined")
                retVal = true;
            if (item.shortname.ToLower() == "lowgradefuel")
                retVal = true;
            if (item.shortname.ToLower() == "metal.ore")
                retVal = true;
            if (item.shortname.ToLower() == "sulfur.ore")
                retVal = true;
            if (item.shortname.ToLower() == "hq.metal.ore")
                retVal = true;

            return retVal;

        }

        Item MakeItem(string shortname, int amount)
        {
            try
            {
                var definition = ItemManager.FindItemDefinition(shortname);
                if (definition != null)
                {
                    Item item = ItemManager.CreateByItemID((int)definition.itemid, amount, 0);
                    return item;
                }

                PrintWarning($"WARNING: makeItem: Error making item {shortname}");
                return null;
            }
            catch (Exception ex)
            {
                PrintError($"ERROR: makeItem: Error making item {shortname} {ex.Message}");
                return null;
            }
        }

        bool AddItemToItemContainer(BasePlayer player, string itemName, int amount, ref BaseEntity containerEntity)
        {

            var item = MakeItem(itemName, amount);
            if (item == null)
            {
                PrintToChat(player, "Cannot make " + itemName);
                return false;
            }

            var container = containerEntity.GetComponent<StorageContainer>();

            if (container.inventory.IsFull())
                return false;

            item.MoveToContainer(container.inventory);
            return true;
        }

        #endregion

        void BuildersBoxes(BasePlayer player, string itemType = "", int boxCount = 0,  bool givePlayerBasics = false)
        {
            try
            {
                UserData u;
                if (BuilderBoxUsers.Where(x => x.player == player).Count() < 1)
                {
                    u = new UserData(player);
                    BuilderBoxUsers.Add(u);
                }
                else
                {
                    u = BuilderBoxUsers.Where(x => x.player == player).First();
                }

                var boxnum = 1;
                var boxPos = player.transform.position;

                BaseEntity boxEntity = CreateBuildersBox(GetGroundPosition(boxPos), player);
                u.boxes.Add(boxEntity);

                //foreach (var resources in ItemManager.itemList.ToList())

                var list = new List<ItemDefinition>();

                if (itemType == "")
                    list = ItemManager.itemList.ToList();
                else if (itemType == "wearable")
                    list = ItemManager.itemList.Where(x => x.isWearable).ToList();
                else if (itemType == "usable")
                    list = ItemManager.itemList.Where(x => x.isUsable).ToList();
                else if (itemType == "holdable")
                    list = ItemManager.itemList.Where(x => x.isHoldable).ToList();
                else if (itemType.ToLower().StartsWith("amm"))
                    list = ItemManager.itemList.Where(x => x.name.ToString().ToLower().Contains("ammo")).ToList();
                else if (itemType.ToLower().StartsWith("compo"))
                {
                    foreach(var i in ItemManager.itemList)
                    {
                        if (IsComponent(i))
                            list.Add(i);
                    }                    
                }
                else if (itemType.ToLower().StartsWith("res"))
                {
                    foreach (var i in ItemManager.itemList)
                    {
                        if (IsResoruce(i))
                            list.Add(i);
                    }
                }
                else if (itemType.ToLower().StartsWith("ele"))
                {                    
                    
                }
                else if (itemType.ToLower().StartsWith("match:"))
                {
                    var match = itemType.ToLower().Replace("match:", string.Empty);
                    list = ItemManager.itemList.Where(x => x.shortname.ToLower().Contains(match)).ToList();
                }
                else
                    list = ItemManager.itemList.Where(x => !x.isHoldable).Where(x => !x.isUsable).Where(x => !x.isWearable).ToList();

                //foreach (var resources in ItemManager.itemList.Where(x =>x.isWearable) .ToList())
                foreach (var resources in list)
                {
                    if (!AddItemToItemContainer(player, resources.shortname, resources.stackable, ref boxEntity))
                    {                        
                        //var definition = ItemManager.FindItemDefinition(resources.shortname);
                        if ((boxCount > 0 ) && (u.boxes.Count() >= boxCount)) break;
                        boxEntity = CreateBuildersBox(GetGroundPosition( new Vector3(boxPos.x, boxPos.y, boxPos.z + boxnum++)), player);
                        u.boxes.Add(boxEntity);
                        AddItemToItemContainer(player, resources.shortname, resources.stackable, ref boxEntity);
                        
                    }
                }

                player.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                PrintToChat(player, $"{boxnum} box{ (boxnum>1?"es":"") } created..");

            }
            catch (Exception ex)
            {
                PrintError($"ERROR: box: {ex.Message}");
            }
        }

        BaseEntity CreateBuildersBox(Vector3 position, BasePlayer Owner) 
        {
            try
            {
                var _woodenBoxName = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab";

                var _woodenBox = GameManager.server.CreateEntity(_woodenBoxName,  position);
                _woodenBox.SendMessage("SetDeployedBy", Owner, UnityEngine.SendMessageOptions.DontRequireReceiver);
                _woodenBox.OwnerID = Owner.userID;
                _woodenBox.Spawn();

                spawnedEntities.Add(_woodenBox);

                return _woodenBox;
            }
            catch (Exception ex)
            {
                PrintError($"ERROR: CreateBox: Error creating box. {ex.Message}");
                return null;
            }
        }

        BaseEntity CreateBuildersBox(string pfefab, Vector3 position, BasePlayer Owner, Quaternion rotation)
        {
            try
            {
                var prefabName = pfefab; //"assets/prefabs/deployable/large wood storage/box.wooden.large.prefab";

                var newEnt = GameManager.server.CreateEntity(prefabName, position, rotation);
                newEnt.SendMessage("SetDeployedBy", Owner, UnityEngine.SendMessageOptions.DontRequireReceiver);
                newEnt.OwnerID = Owner.userID;
                newEnt.Spawn();

                spawnedEntities.Add(newEnt);

                return newEnt;
            }
            catch (Exception ex)
            {
                PrintError($"ERROR: CreateBuildersBox: Error creating box. {ex.Message}");
                return null;
            }
        }

        #region common
        Vector3 GetGroundPosition(Vector3 sourcePos)
        {
            RaycastHit hitInfo;

            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo))
                sourcePos.y = hitInfo.point.y;

            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
            return sourcePos;
        }

        bool CheckAccess(BasePlayer player, string command)
        {
            if (!permission.UserHasPermission(player.UserIDString, usePermission))
            {
                PrintToChat(player, $"Unknown command: {command}");
                PrintWarning($"{player.displayName}/{player.userID} attempted {command}");
                return false;
            }
            return true;
        }


        #endregion

        #region user man
        private List<UserData> BuilderBoxUsers = new List<UserData>();

        UserData GetUser(BasePlayer player, bool destroyBoxes = false)
        {
            UserData u;
            if (BuilderBoxUsers.Where(x => x.player == player).Count() < 1)
            {
                u = new UserData(player);
                BuilderBoxUsers.Add(u);
            }
            else
            {
                u = BuilderBoxUsers.Where(x => x.player == player).First();
            }

            if (destroyBoxes)
            {
                if (u.boxes.Count > 0)
                {
                    var c = u.boxes.Count();
                    foreach (BaseEntity b in u.boxes)
                    {
                        StorageContainer container = b.GetComponent<StorageContainer>();
                        b.Kill();
                    }

                    u.boxes.Clear();
                    PrintToChat(player, $"{c} box{ (c > 1 ? "es" : "") } killed..");
                }
            }

            return u;
        }

        class UserData
        {
            public BasePlayer player;
            public List<BaseEntity> boxes;

            public UserData(BasePlayer player)
            {
                this.player = player;
                boxes = new List<BaseEntity>();
            }
        }

        #endregion
    }


}

