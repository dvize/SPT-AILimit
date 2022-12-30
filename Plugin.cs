using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using System.Collections.Generic;
using UnityEngine;
using System.Timers;
using System;

namespace dvize.AILimit
{
    [BepInPlugin("com.dvize.AIlimit", "dvize.AIlimit", "1.4.1")]

    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> PluginEnabled;
        public static ConfigEntry<int> BotLimit;
        public static ConfigEntry<float> BotDistance;
        public static ConfigEntry<float> TimeAfterSpawn;
        internal void Awake()
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

        public static GameWorld gameWorld = new GameWorld();
        private void Update()
        {
            if (Plugin.PluginEnabled.Value)
            {
                if (!Singleton<GameWorld>.Instantiated)
                {
                    return;
                }

                gameWorld = Singleton<GameWorld>.Instance;
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

        public static Dictionary<int, Player> playerMapping = new Dictionary<int, Player>();
        public static Dictionary<int, botPlayer> botMapping = new Dictionary<int, botPlayer>();
        public static List<botPlayer> botList = new List<botPlayer>();
        public static Player player;
        public static botPlayer bot;
        public void UpdateBots(GameWorld gameWorld)
        {

            int botCount = 0;

            for (int i = 0; i < gameWorld.RegisteredPlayers.Count; i++)
            {
                player = gameWorld.RegisteredPlayers[i];
                if (!player.IsYourPlayer)
                {
                    if (!botMapping.ContainsKey(player.Id) && (!playerMapping.ContainsKey(player.Id)))
                    {
                        playerMapping.Add(player.Id, player);
                        var tempbotplayer = new botPlayer(player.Id);
                        botMapping.Add(player.Id, tempbotplayer);
                    }
                    else if (!playerMapping.ContainsKey(player.Id))
                    {
                        playerMapping.Add(player.Id, player);
                    }
                    
                    if (botMapping.ContainsKey(player.Id))
                    {
                        bot = botMapping[player.Id];
                        bot.Distance = Vector3.Distance(player.Position, gameWorld.RegisteredPlayers[0].Position);

                        //add bot if eligible
                        if (bot.eligibleNow && !botList.Contains(bot))
                        {
                            botList.Add(bot);
                        }

                        if (!bot.timer.Enabled && player.CameraPosition != null)
                        {
                            bot.timer.Enabled = true;
                            bot.timer.Start();
                        }
                    }

                }
            }

            //add sort by distance
            if (botList.Count > 1)
            {
                //botList = botList.OrderBy(o => o.Distance).ToList();
                for (int i = 1; i < botList.Count; i++)
                {
                    botPlayer current = botList[i];
                    int j = i - 1;
                    while (j >= 0 && botList[j].Distance > current.Distance)
                    {
                        botList[j + 1] = botList[j];
                        j--;
                    }
                    botList[j + 1] = current;
                }
            }
            
            for (int i = 0; i < botList.Count; i++)
            {
                if (botCount < BotLimit.Value && botList[i].Distance < BotDistance.Value)
                {
                    if (playerMapping.ContainsKey(botList[i].Id))
                    {
                        playerMapping[botList[i].Id].enabled = true;
                        //playerMapping[botList[i].Id].gameObject.SetActive(true);
                        
                        botCount++;
                    }
                }
                else
                {
                    if (playerMapping.ContainsKey(botList[i].Id))
                    {
                        playerMapping[botList[i].Id].enabled = false;
                        //playerMapping[botList[i].Id].gameObject.SetActive(false);
                    }
                }
            }
        }
        public static ElapsedEventHandler EligiblePool(botPlayer botplayer)
        {
            botplayer.timer.Stop();
            botplayer.eligibleNow = true;
            
            return null;
        }
        public class botPlayer
        {
            public int Id { get; set; }
            public float Distance { get; set; }
            public bool eligibleNow { get; set; }

            public Timer timer = new Timer(Plugin.TimeAfterSpawn.Value * 1000);
            public botPlayer(int newID)
            {
                this.Id = newID;
                this.eligibleNow = false;
                //this.timer.Start();
                this.timer.Enabled = false;
                this.timer.AutoReset = false;
                this.timer.Elapsed += Plugin.EligiblePool(this);

                //create handler for this player to remove him from the distlist on death.
                Player registeredplayer = playerMapping[this.Id];
                registeredplayer.OnPlayerDeadOrUnspawn += (deadArgs) =>
                {
                    botPlayer botValue = null;

                    if (botMapping.ContainsKey(deadArgs.Id))
                    {
                        botValue = botMapping[deadArgs.Id];
                        botMapping.Remove(deadArgs.Id);
                    }
                    
                    if (botList.Contains(botValue))
                    {
                        botList.Remove(botValue);
                    }
                    
                    if (playerMapping.ContainsKey(deadArgs.Id))
                    {
                        playerMapping.Remove(deadArgs.Id);
                    }
                    
                };
            }

        }
    }
    

}

