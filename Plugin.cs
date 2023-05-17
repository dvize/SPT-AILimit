using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using UnityEngine;

namespace dvize.AILimit
{
    [BepInPlugin("com.dvize.AIlimit", "dvize.AIlimit", "1.4.6")]

    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> PluginEnabled;
        public static ConfigEntry<int> BotLimit;
        public static ConfigEntry<float> BotDistance;
        public static ConfigEntry<float> TimeAfterSpawn;

        public void Awake()
        {
            PluginEnabled = Config.Bind(
                "Main Settings",
                "Plugin on/off",
                true,
                "");

            BotDistance = Config.Bind(
                "Main Settings",
                "Bot Distance",
                200f,
                "Set Max Distance to activate bots");

            BotLimit = Config.Bind(
                "Main Settings",
                "Bot Limit (At Distance)",
                10,
                "Based on your distance selected, limits up to this many # of bots moving at one time");

            TimeAfterSpawn = Config.Bind(
                "Main Settings",
                "Time After Spawn",
                10f,
                "Time (sec) to wait before disabling");
        }

        public void Update()
        {
            if (PluginEnabled.Value)
            {
                if (!Singleton<GameWorld>.Instantiated)
                {
                    return;
                }

                GameWorld gameWorld = Singleton<GameWorld>.Instance;
                try
                {
                    UpdateBots(gameWorld);
                }
                catch (Exception e)
                {
                    Logger.LogInfo(e);
                }
            }
        }

        public static SortedList<botPlayer, float> botList = new SortedList<botPlayer, float>();
        public static Player player;
        public static botPlayer bot;

        public void UpdateBots(GameWorld gameWorld)
        {
            int botCount = 0;

            var players = gameWorld.RegisteredPlayers;
            var bots = players
                .Where(p => !p.IsYourPlayer)
                .Select(p => new botPlayer(p, Vector3.Distance(p.Position, gameWorld.MainPlayer.Position)))
                .ToDictionary(b => b, b => b.Distance);

            botList.Clear();
            foreach (var bot in bots)
            {
                botList.Add(bot.Key, bot.Value);
            }

            foreach (var bot in botList)
            {
                var botplayer = gameWorld.RegisteredPlayers.FirstOrDefault(x => x == bot.Key.Player);

                if (botCount < Plugin.BotLimit.Value && bot.Value < Plugin.BotDistance.Value)
                {
                    //find the player in the gameWorld Registered Players list and set enabled to true
                    
                    botplayer.enabled = true;
                    botCount++;
                }

                if (bot.Key.eligibleNow)
                {
                    botplayer.enabled = false;
                }
            }
        }

        public class botPlayer
        {
            public float Distance
            {
                get; set;
            }
            public bool eligibleNow
            {
                get; set;
            }

            public Player Player
            {
            get; set; 
            
            }

            private System.Timers.Timer timer;
            public botPlayer(Player player, float distance)
            {
                Player = player;
                Distance = distance;
                eligibleNow = false;

                timer = new System.Timers.Timer(Plugin.TimeAfterSpawn.Value * 1000);
                timer.Elapsed += (sender, timerargs) =>
                {
                    eligibleNow = true;
                };
                timer.Start();

                player.OnPlayerDeadOrUnspawn += (deadArgs) =>
                {
                    //check if botList contains a botPlayer with the same player
                    var botValue = botList.FirstOrDefault(x => x.Key.Player == player).Key;

                    if (botValue != null)
                    {
                        botList.Remove(botValue);
                    }
                };
            }
        }

    }

}



