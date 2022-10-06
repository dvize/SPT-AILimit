using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.UI;
using EFT.UI.Health;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;


namespace dvize.BulletTime
{
    [BepInPlugin("com.dvize.BulletTime", "dvize.BulletTime", "1.0.0")]

    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> PluginEnabled;
        public static ConfigEntry<float> BulletTimeScale;
        public static ConfigEntry<int> MaxBulletTimeSeconds;
        public static ConfigEntry<int> CooldownPeriodSeconds;
        public static ConfigEntry<KeyboardShortcut> KeyBulletTime;
        public float CoolDownElapsedSecond = 0f;
        public float BulletTimeElapsedSecond = 0f;
        public bool startBulletTime = false;
        public bool firstTimeTriggered = false;

        internal void Awake()
        {
            PluginEnabled = Config.Bind(
                "Main Settings",
                "Plugin on/off",
                true,
                "");

            BulletTimeScale = Config.Bind(
                "Main Settings",
                "Bullet Time Scale",
                0.40f,
                "Set how slow the Bullet Time Scale goes to");

            MaxBulletTimeSeconds = Config.Bind(
                "Main Settings",
                "Set the Maximum Bullet Time per Session",
                10,
                "Set how much your stamina drains per second. No bullet time if no stamina");

            CooldownPeriodSeconds = Config.Bind(
                "Main Settings",
                "Set the cooldown period before being able to trigger Bullet Time",
                120,
                "Set how much your stamina drains per second. No bullet time if no stamina");

            KeyBulletTime = Config.Bind(
                "Hotkey for Bullet Time", 
                "Use Bullet Time", 
                new KeyboardShortcut(KeyCode.Mouse4),
                "Key for Bullet Time toggle");
        }

        private void Update()
        {
            if (Plugin.PluginEnabled.Value)
            {
                
                if (!Singleton<GameWorld>.Instantiated)
                {
                    return;
                }


                var player = Singleton<GameWorld>.Instance.AllPlayers[0];
                CoolDownElapsedSecond += Time.unscaledDeltaTime;

                if (Plugin.KeyBulletTime.Value.IsUp())
                {
                    if (!firstTimeTriggered)
                    {
                        //Logger.LogInfo("BulletTime: First Key Press - Activate Bullet Time");
                        firstTimeTriggered = true;
                        startBulletTime = true;
                    }
                    else if (firstTimeTriggered)
                    {
                        //Logger.LogInfo("BulletTime: Second Key Press - Deactivate Bullet Time");
                        startBulletTime = false;
                        firstTimeTriggered = false;
                        BulletTimeElapsedSecond = 0f;
                        CoolDownElapsedSecond = 0f;
                        Time.timeScale = 1.0f;
                    }
                }

               
                if (startBulletTime)
                {
                    //as this is per frame, sufficient stamina only needs to be above 0
                    //bool staminasufficient = player.Physical.Stamina.Current > 0;
                    bool cooldowndone = CoolDownElapsedSecond >= Plugin.CooldownPeriodSeconds.Value;

                    if (cooldowndone)
                    {
                        player.enabled = false;
                        Time.timeScale = Plugin.BulletTimeScale.Value;
                        player.enabled = true;
                        BulletTimeElapsedSecond += Time.unscaledDeltaTime;
                        //Time.fixedDeltaTime = 0.02f * Time.timeScale;
                        //player.Physical.Stamina.Current -= (elapsedSecond / Plugin.StaminaDrainPerSecond.Value);
                    }

                    if (BulletTimeElapsedSecond >= Plugin.MaxBulletTimeSeconds.Value)
                    {
                        //Logger.LogInfo("BulletTime: Stamina Ran Out");
                        Time.timeScale = 1.0f;
                        BulletTimeElapsedSecond = 0f;
                        CoolDownElapsedSecond = 0f;
                        startBulletTime = false;
                        firstTimeTriggered = false; //reset key incase stamina runs out
                    }
                }

                
            }

        }

    }
}