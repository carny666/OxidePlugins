using Oxide.Core;
using Oxide.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BarrelDamage", "CARNY666", "1.0.5")]
    class BarrelDamage : RustPlugin
    {

        void Init()
        {
            Config.Load();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");

            Config.Clear();
            Config["rock"] = 2.0f;

            SaveConfig();
        }

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (!entity.ShortPrefabName.ToLower().Contains("barrel")) return null;

            if (Config[info.Weapon.GetItem().info.displayName.english.ToLower()] != null)
                info.damageTypes.ScaleAll(float.Parse(Config[info.Weapon.GetItem().info.displayName.english.ToLower()].ToString()));
            else
            {
                Config[info.Weapon.GetItem().info.displayName.english.ToLower()] = 1.0f;
                PrintWarning($"Added {info.Weapon.GetItem().info.displayName.english.ToLower()} with a default damage rate of x1 to BarrelDamage's config.");
                SaveConfig();
            }
            return null;
        }
    }
}
