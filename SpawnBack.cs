using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SpawnBack", "CARNY666", "1.0.0")]
    class SpawnBack : RustPlugin
    {

        Dictionary<string, ItemContainer> userStorage = new Dictionary<string, ItemContainer>();

        class itemDef
        {
            public int itemId { get; set; }
            public int amount { get; set; }
        }

        [ChatCommand("paste")]
        void pas(BasePlayer player, string command, string[] args)
        {

            

            if (userStorage.ContainsKey(player.displayName))
            {                
                ItemContainer container = userStorage[player.displayName];

                
                PrintToChat($"{container.itemList.Count()} Items");

                foreach (Item i in container.itemList)
                {
                    PrintToChat($"{i.amount} x {i.info.displayName}");
                    i.MoveToContainer(player.inventory.containerMain);
                }
            }
        }

        [ChatCommand("copy")]
        void cop(BasePlayer player, string command, string[] args)
        {
            var tmp = ItemsInInventory(player);
            PrintToChat($"{tmp.Count} items.");

            //var box = CreateBuildersBox(GetGroundPosition(player.transform.position + player.transform.forward), player);
            //var container = box.GetComponent<StorageContainer>();
            if (userStorage.ContainsKey(player.displayName))
                userStorage.Remove(player.displayName);

            var container = new ItemContainer();

            foreach (var s in tmp)
            {
                var item = MakeItem(s.itemId, s.amount);
                PrintToChat($"Adding {item.amount} x {item.info.displayName}");
                item.MoveToContainer(container);// container.inventory);
            }

            PrintToChat($"{container.itemList.Count()} Items");
            userStorage.Add(player.displayName, container);
        }


        void copyToBox(BasePlayer player)
        {
            var tmp = ItemsInInventory(player);
            PrintToChat($"{tmp.Count} items.");
            var box = CreateBuildersBox(GetGroundPosition(player.transform.position + player.transform.forward), player);
            var container = box.GetComponent<StorageContainer>();

            foreach (var s in tmp)
            {
                var item = MakeItem(s.itemId, s.amount);
                item.MoveToContainer(container.inventory);
            }

        }

        List<itemDef> ItemsInInventory(BasePlayer player)
        {
            List<itemDef> retval = new List<itemDef>();
            foreach(Item p in player.inventory.AllItems())
                if (!p.dirty)
                    retval.Add(new itemDef { itemId = p.info.itemid, amount = p.amount } );

            return retval;
        }

        private void OnLootEntity(BasePlayer looter, BaseEntity entity)
        {
            //if (looter == null || entity == null || !entity.IsValid() || !IsValidType(entity)) return;
            PrintToChat($"{looter.displayName} looting {entity.PrefabName} ");
        }

        object CanDeployItem(BasePlayer player, Deployer deployer, uint entityId)
        {
            PrintToChat("CanDeployItem works!");
            return null;
        }

        BaseEntity CreateBuildersBox(Vector3 position, BasePlayer Owner)
        {
            try
            {
                var _woodenBoxName = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab";

                var _woodenBox = GameManager.server.CreateEntity(_woodenBoxName, position);
                _woodenBox.SendMessage("SetDeployedBy", Owner, UnityEngine.SendMessageOptions.DontRequireReceiver);
                _woodenBox.OwnerID = Owner.userID;
                _woodenBox.Spawn();

                return _woodenBox;
            }
            catch (Exception ex)
            {
                PrintError($"ERROR: CreateBox: Error creating box. {ex.Message}");
                return null;
            }
        }

        Vector3 GetGroundPosition(Vector3 sourcePos)
        {
            RaycastHit hitInfo;

            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo))
                sourcePos.y = hitInfo.point.y;

            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
            return sourcePos;
        }

        Item MakeItem(int itemid, int amount)
        {
            return ItemManager.CreateByItemID((int)itemid, amount, 0);
        }

        Item MakeItem(string shortname, int amount)
        {
            try
            {
                var definition = ItemManager.FindItemDefinition(shortname);
                if (definition != null)
                {
                    Item item = MakeItem(definition.itemid, amount);
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

        bool AddItemToItemContainer(string itemName, int amount, ref BaseEntity containerEntity)
        {

            var item = MakeItem(itemName, amount);
            if (item == null)
            {
                PrintWarning("Cannot make " + itemName);
                return false;
            }

            return true;
        }

    }
}
