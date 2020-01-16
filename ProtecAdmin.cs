using Oxide.Core;
using Oxide.Core.Libraries;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ProtecAdmin", "CARNY666", "1.0.1")]
    class ProtecAdmin : RustPlugin
    {
        const string usePermission = "ProtecAdmin.lights";
        string protec = "76561198974369133"; // Bubbba_louie

        UserData userData;

        void Init()
        {
            userData = Config.ReadObject<UserData>();

            PrintWarning($"{this.Title} {this.Version} Initialized @ {DateTime.Now.ToLongTimeString()}...");

            timer.Repeat(30f, 0, () =>
            {
                var hour = TimeSpan.FromHours(ConVar.Env.time).Hours;

                if (hour >= 8 && hour < 18) // daytime.. 7am and 6pm 
                {
                    changeLighingState(false);
                }
                else                        // nighttime..
                {
                    changeLighingState(true);
                }

            });
        }
        protected override void LoadDefaultConfig()
        {
            var tmp = new UserData 
            {
                UserName = new List<string>(),
            };
            Config.WriteObject(tmp, true);
        }

        void Loaded()
        {
            permission.RegisterPermission(usePermission, this);
        }

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            try
            {

                if (entity == null || info == null) return null;

                var ignore = new[] { "BaseNPC", "LootContainer", "Landmine", "PlayerCorpse" };
                //var doNotDamage = new[] { "ShopFront", "PlayerCorpse", "ReactiveTarget", "MiningQuarry", "DecorDeployable", "StabilityEntity", "Workbench", "Locker", "BaseFuelLightSource", "Barricade", "BuildingBlock", "Door", "Signage", "SimpleBuildingBlock", "RepairBench", "BaseOven", "CeilingLight", "BoxStorage", "BaseLadder", "SearchLight", "RescourceExtractorFuelStorage" };

                if (ignore.Contains(entity.GetType().ToString()))
                    return null;

                // dont damage some things in the zone
                if (entity.OwnerID.ToString() == protec)  // && info.InitiatorPlayer.ToPlayer().userID)
                {
                    if (info.InitiatorPlayer != null)
                        PrintToChat(info.InitiatorPlayer, "Server owned buildings and deployables cannot be damaged.");

                    info.damageTypes.ScaleAll(0.0f);
                    return false;
                }

                if (info.InitiatorPlayer != null)
                {

                    //if (entity.OwnerID.ToString() != info.InitiatorPlayer.UserIDString)
                    //{
                    //    var raidee = BasePlayer.FindByID(entity.OwnerID);
                    //    var raider = info.InitiatorPlayer;

                    //    webrequest.Enqueue("http://" + $"everythingtoday.org/rust/_DiscordWebHook.ashx?eventType=OLR&raider={raider.displayName}&raidee={raidee.displayName}", null, (code, response) =>
                    //    {
                    //        if (code != 200 || response == null)
                    //        {
                    //            PrintError($"No answer from OLR.");
                    //            return;
                    //        }
                    //        Puts($"OLR answered: {response}");
                    //    }, this, RequestMethod.PUT);
                    //}

                }




                //PrintError($"Unhandled entity of type {entity.GetType().ToString()} is being damaged. Notify support. Add '{entity.GetType().ToString()}' to doNotDamage array in OnEntityTakeDamage in ProtecAmin mod code.");
                return null;
            }
            catch (Exception ex)
            {
                PrintError($"Error OnEntityTakeDamage {ex.StackTrace} in ProtecAmin.");
                return null;
            }

        }

        void OnConsumeFuel(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            if (oven.OwnerID.ToString() == protec) // constant fuel.
                fuel.amount = 1;
        }

        //[ChatCommand("addnoprotec")]
        //void NoProtect(BasePlayer player, string command, string[] args)
        //{
        //    if (player.userID.ToString() != protec) return;

        //    if (userData.UserName.Contains(x => x.StartsWith(args[0]) ).
                

        //    if (userData.UserName.Where(x => x.StartsWith(args[0])
        //        .Contains(args[0]))
        //    {
        //        PrintToChat(player, "User already in list.");
        //        return;
        //    }
        //    userData.UserName.Add(args[0]);

        //    Config.WriteObject(userData, true);
        //}


        [ChatCommand("sss")]
        void Box(BasePlayer player, string command, string[] args)
        {
            PrintToChat(player, $"{player.UserIDString}, {player.userID}");
        }

        void OnLoseCondition(Item item, ref float amount)
        {
            //if (item.GetOwnerPlayer().displayName.ToUpper() == "Vethra")
            //{
            //    amount *= 3;
            //    PrintWarning($"{item.info.displayName.translated } {item.GetOwnerPlayer().displayName} damaged {amount}");
            //}
        }

        void changeLighingState(bool bOn)
        {
            string[] lighting = { "lantern", "searchlight", "tunalight", "ceilinglight" };


            foreach (BaseOven entry in GameObject.FindObjectsOfType<BaseOven>())
                if (entry.OwnerID.ToString() == protec || permission.UserHasPermission(entry.OwnerID.ToString(), usePermission))
                {
                    if (lighting.Any(entry.PrefabName.Contains))
                    {
                        if (bOn)
                        {
                            //entry.ConsumeFuel()
                            entry.SetFlag(BaseEntity.Flags.On, true);
                        }
                        else
                            entry.SetFlag(BaseEntity.Flags.On, false);
                    }
                }

            
        }


        class UserData
        {
            public List<string> UserName { get; set; }
        }
    }
}

