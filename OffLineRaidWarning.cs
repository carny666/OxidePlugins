using Oxide.Core;
using Oxide.Core.Libraries;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("OffLineRaidWarning", "SCUPPER", "1.0.1")]
    class OffLineRaidWarning : RustPlugin
    {
        List<mydata> data = new List<mydata>();

        private string GetName(string id)
        {
            if (id == "0") return "[SERVERSPAWN]";
            //string color = GetPlayerColor(ulong.Parse(id));
            return $"{covalence.Players.FindPlayerById(id)?.Name}";
        }


        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (entity == null || info == null) return null;
                if (info.InitiatorPlayer == null) return null;

                var victim = GetName(entity.OwnerID.ToString());
                var raider = GetName(info.InitiatorPlayer.userID.ToString());


                if (raider != "[SERVERSPAWN]" && victim != "[SERVERSPAWN]")
                {
                    recordRaid(raider, victim, info.damageTypes.Total());
                    PrintError($"Damages: {raider} aganst {victim} damages={info.damageTypes.Total()}");
                }
                return null;
            }
            catch (Exception ex)
            {
                PrintError($"{ex.Message}");
                return null;
            }

        }

        void sendDiscord(string raider, string raidee)
        {

            webrequest.Enqueue("http://" + $"www.everythingtoday.org/rust/_DiscordWebHook.ashx?eventType=OLR&raider={raider}&raidee={raidee}", null, (code, response) =>
            {
                if (code != 200 || response == null)
                {
                    PrintError($"No answer from OLR.");
                    return;
                }
                Puts($"OLR answered: {response}");
            }, this, RequestMethod.PUT);

        }

        void recordRaid(string raider, string raidee, float damage)
        {
            var damageTimeMinutes = 5;
            var damageToReport = 100f;

            if (!data.Any(x=>x.Raider == raider && x.Raidee == raidee))
                data.Add(new mydata { Raidee = raidee, Raider = raider, TotalDamage = 0, LastDamage = DateTime.Now, LastWarning = DateTime.Now.AddMinutes(-10)});

            var tmp = data.Where(x => x.Raider == raider && x.Raidee == raidee).First();
            tmp.TotalDamage += damage;

            if (tmp.TotalDamage > damageToReport && tmp.LastWarning.AddMinutes(5) < DateTime.Now)
            {
                tmp.TotalDamage = 0;
                tmp.LastWarning = DateTime.Now;                
                PrintError($"{raider} is raiding {raidee}, discord?");
                sendDiscord(raider, raidee);
            }

            for(int ii = 0; ii < data.Count; ii++)

                if (data[ii].LastDamage <= DateTime.Now.AddMinutes(-damageTimeMinutes))
                    data.RemoveAt(ii);

        }


        class mydata
        {
            public string Raider { get; set; }
            public string Raidee { get; set; }
            public float TotalDamage { get; set; }
            public DateTime LastDamage { get; set; }
            public DateTime LastWarning { get; set; }
        }


    }
}

