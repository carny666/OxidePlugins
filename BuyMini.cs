using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Rust;
using Oxide.Core.Configuration;
using Oxide.Core;

namespace Oxide.Plugins

{
    [Info("BuyMini", "CARNY666", "1.0.0")]
    [Description("Buy a minicopter and some fuel.")]
    class BuyMini : RustPlugin
    {
        const float aboveGoundPosition = 0.75f;
        const int miniCost = 500;
        const float lgfcost = 0.5f;

        private const string AllowPermission = "BuyMini.allow";
        private const string AdminPermission = "BuyMini.admin";

        BuyMiniData data;

        void Init()
        {
            data = Config.ReadObject<BuyMiniData>();
            PrintWarning($"{this.Title} {this.Version} Initialized @ {DateTime.Now.ToLongTimeString()}...");
        }

        void Loaded()
        {
            try
            {
                permission.RegisterPermission(AllowPermission, this);
                permission.RegisterPermission(AdminPermission, this);
            }
            catch (Exception ex)
            {
                PrintWarning($"BuyMini:Loaded: {ex.StackTrace}");
            }
        }

        protected override void LoadDefaultConfig()
        {
            var tmp = new BuyMiniData
            {
                userData = new List<userData>(),
                //MyCopters = new List<BaseEntity>()
            };
            Config.WriteObject(tmp, true);
        }

        [ChatCommand("buymini")]
        void BuyMiniChatCommand(BasePlayer player, string command, string[] args)
        {
            try
            {
                PrintWarning($"{player.displayName} using buymini.");
                if (!permission.UserHasPermission(player.UserIDString, AllowPermission)) return;
                //if (!permission.UserHasPermission(player.UserIDString, AdminPermission)) return;

                if (player.IsBuildingBlocked())
                {
                    PrintToChat(player, "Not while building blocked.");
                    return;
                }


                if (args.Length == 0)
                {
                    PrintToChat(player, $"<color=#00e673>Usage:</color>");
                    PrintToChat(player, $"<color=#ffff00> /buymini [0-n]</color>");
                    PrintToChat(player, $"<color=#00e673>Where <color=#FFFFFF>[0-n]</color> is how much lgf you'd like to purchase.</color>");
                    PrintToChat(player, $"<color=#00e673>The cost is <color=#FFFFFF>{miniCost}</color> scrap per copter and <color=#FFFFFF>{lgfcost}</color> scrap per lgf unit (rounded up).</color>");
                    PrintToChat(player, $"<color=#00e673>Have the scrap in one stack in your inventory.</color>");
                    PrintToChat(player, $"<color=#1ac6ff>Examples: /buymini 0</color>");
                    PrintToChat(player, $"<color=#00e673>buys one copter no fuel costs {miniCost} scrap.</color>");
                    PrintToChat(player, $"<color=#1ac6ff>          /buymini 100</color>");
                    PrintToChat(player, $"<color=#00e673>buys one copter 100 fuel costs {(int)(100 * lgfcost)} scrap.</color>");
                    return;
                }

                var fuelPurchase = int.Parse(args[0]);

                var cost = miniCost + (fuelPurchase * lgfcost);

                foreach (Item item in player.inventory.AllItems())
                {
                    if (item.info.displayName.english == "Scrap")
                    {
                        if (item.amount < cost) // not enough scrap for copter and lgf.
                        {
                            PrintToChat(player, $"<color=red>What is this, you don't have that kind of scrap. Get it together in one slot.</color>");
                            return;
                        }

                        if (data.userData?.Where(x => x.UserName == player.displayName).FirstOrDefault() == null)
                            data.userData.Add(new userData { UserName = player.displayName, CopterPurchases = 1, fuelPurchases = fuelPurchase });

                        var d = data.userData?.Where(x => x.UserName == player.displayName).FirstOrDefault();
                        d.UserName = player.displayName;
                        d.fuelPurchases += fuelPurchase;
                        d.CopterPurchases++;

                        Config.WriteObject(data, true);

                        spawnCopter(player, fuelPurchase);
                        PrintToChat(player, $"You have purchased a Mini Copter{ ((fuelPurchase>0)?$" and {fuelPurchase} low grade fuel":"")} for the low price of {cost} scrap. Pleasure doing business with you.");
                        PrintWarning($"{player.displayName} purchased {fuelPurchase} lgf.");
                        item.amount -= (int)cost;
                        player.SendNetworkUpdate();

                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                PrintError($"BuyMini: {ex.StackTrace}");
            }
        }

        void spawnCopter(BasePlayer player, int fuel = 0)
        {
            var p = "assets/content/vehicles/minicopter/minicopter.entity.prefab";
            
            try
            {
                var _entityName = p;

                var entity = GameManager.server.CreateEntity(_entityName, GetGroundPosition(player.transform.position + (player.eyes.BodyForward() * 3)));
                entity.Spawn();

                //data.MyCopters.Add(entity);
                //Config.WriteObject(data, true);

                if (fuel > 0)
                {
                    var addonDef = ItemManager.FindItemDefinition("lowgradefuel");
                    var item = ItemManager.CreateByItemID(addonDef.itemid, fuel, 0);
                    player.inventory.GiveItem(item, player.inventory.containerMain);
                    player.SendNetworkUpdate();
                }

            }
            catch (Exception)
            {
                //PrintError($"ERROR: Error creating entity. {ex.Message}");
            }

        }


        [ConsoleCommand("bm")]
        void consoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            BasePlayer player = arg.Player();

            //if (!permission.UserHasPermission(player.UserIDString, AllowPermission)) return;
            //if (!permission.UserHasPermission(player.UserIDString, AdminPermission)) return;

            foreach(var p in data.userData)
                PrintToChat(player, $"{p.CopterPurchases} copters purchased with {p.fuelPurchases} fuel by {p.UserName}");
        }

        Vector3 GetGroundPosition(Vector3 sourcePos)
        {

            RaycastHit hitInfo;
            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo))
                sourcePos.y = hitInfo.point.y;

            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos)) + aboveGoundPosition;

            return sourcePos;
        }


        class BuyMiniData
        {
            public List<userData> userData { get; set; }
            //public List<BaseEntity> MyCopters { get; set; }
        }

        class userData
        {
            public string UserName { get; set; }
            public double CopterPurchases { get; set; }
            public double fuelPurchases { get; set; }
        }

    }

}
