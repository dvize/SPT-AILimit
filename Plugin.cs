using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System.Configuration;
using EFT.UI;
using HarmonyLib;
using System;
using System.Timers;
using System.Linq;
using System.Net;
using static Streamer;

namespace dvize.AILimit
{
    [BepInPlugin("com.dvize.AIlimit", "dvize.AIlimit", "1.4.0")]

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
                400f,
                "Set Max Distance to activate bots");

            BotLimit = Config.Bind(
                "Main Settings",
                "Bot Limit (At Distance)",
                10,
                "Based on your distance selected, limits up to this many # of bots moving at one time");

            TimeAfterSpawn = Config.Bind(
                "Main Settings",
                "Time After Spawn",
                20f,
                "Time (sec) to wait before disabling");
        }

        public static List<botPlayer> distListofBots = new List<botPlayer>();
        Transform _mainCameraTransform;
        GameWorld gameWorld = new GameWorld();
        private void Update()
        {
            if (Plugin.PluginEnabled.Value)
            {
                if (!Singleton<GameWorld>.Instantiated)
                {
                    return;
                }

                if (this._mainCameraTransform == null)
                {
                    Camera camera = Camera.main;
                    if (camera != null)
                    {
                        this._mainCameraTransform = camera.transform;
                    }

                    return;
                }

                gameWorld = Singleton<GameWorld>.Instance;


                AddBotsInSync(gameWorld);
                startTimers();
                AddBotDistance();
                DisableBots(gameWorld);
                SortTheList();

                EnableBots(gameWorld);
                
            }

        }

        public void startTimers()
        {
            foreach (botPlayer bot in distListofBots)
            {
                if (bot.timer.Enabled == false)
                {
                    bot.timer.Enabled = true;
                    bot.timer.Start();
                }             
            }
        }
        public void AddBotsInSync(GameWorld gameWorld)
        {
            foreach (Player player in gameWorld.RegisteredPlayers)
            {
                if (!player.IsYourPlayer)
                {
                    if (!distListofBots.Any(x => x.Id == player.Id))
                    {
                        distListofBots.Add(new botPlayer(player));
                        continue;
                    }
                }
            }
        }
        public void AddBotDistance()
        {
            foreach (Player player in gameWorld.RegisteredPlayers)
            {
                if (player == null)
                {
                    continue;
                }
                
                //need AI check so it doesn't pull player in.
                if (!player.IsYourPlayer)
                {
                    //check if they on list.. if they are then add distance
                    foreach (botPlayer bot in distListofBots)
                    {
                        if (bot.Id == player.Id)
                        {
                            bot.Distance = Vector3.Distance(player.Transform.position, _mainCameraTransform.position);
                            break;
                        }
                    }
                }
            }
        }
        public void EnableBots(GameWorld gameWorld)
        {
            int botCount = 0;
            
            for (int i = 0; i < distListofBots.Count; i++)
            {
                if (distListofBots[i].Distance > Plugin.BotDistance.Value)
                {
                    break;
                }

                //no need to check for time as they won't be on list unless timer elapsed
                if ((distListofBots[i].Distance <= Plugin.BotDistance.Value) && (botCount < Plugin.BotLimit.Value) &&
                    distListofBots[i].eligibleNow)
                {
                        foreach(Player _player in gameWorld.RegisteredPlayers)
                        {
                            if (_player.Id == distListofBots[i].Id)
                            {
                                _player.enabled = true;
                                //_player.gameObject.SetActive(true);
                                botCount++;
                                break;
                            }
                        }
                    }
                }
        }

        public void SortTheList()
        {
            distListofBots.Sort((x, y) => x.Distance.CompareTo(y.Distance));

            /*for (int i = 1; i < distListofBots.Count; i++)
            {
                int j = i - 1;
                botPlayer temp = distListofBots[i];

                while (j >= 0 && distListofBots[j].Distance > temp.Distance)
                {
                    distListofBots[j + 1] = distListofBots[j];
                    j--;
                }

                distListofBots[j + 1] = temp;
            }*/
        }

        //should only disable on distList
        public void DisableBots(GameWorld gameWorld)
        {
            for (int i = 0; i < distListofBots.Count; i++)
            {
                if (distListofBots[i].eligibleNow)
                {
                    //find id in registeredplayers and disable
                    foreach (Player _player in gameWorld.RegisteredPlayers)
                    {
                        if ((_player.Id == distListofBots[i].Id) && (_player.CameraPosition != null))
                        {
                            _player.enabled = false;
                            //_player.gameObject.SetActive(false);
                            break;
                        }
                    }
                }
            }
        }
        
        static public ElapsedEventHandler AddBotToEligiblePool(botPlayer sender)
        {
            var botplayer = sender as botPlayer;
            var registeredplayer = Singleton<GameWorld>.Instance.RegisteredPlayers.Find(x => x.Id == botplayer.Id);
            var distListPlayer = distListofBots.Find(x => x.Id == botplayer.Id);

            //create handler for this player to remove him from the distlist on death.
            registeredplayer.OnPlayerDeadOrUnspawn += (deadArgs) =>
            {
                distListofBots.Remove(distListofBots.Find(x => x.Id == deadArgs.Id));
            };
            //var gameWorld = Singleton<GameWorld>.Instance;
            botplayer.timer.Stop();
            botplayer.eligibleNow = true;
            
            //this adding seems circular .. can't add to list 
            //distListofBots.Add(new botPlayer(gameWorld.RegisteredPlayers.Find(x => x.Id == botplayer.Id)));
            return null;
        }
        
        public class botPlayer
        {
            public int Id { get; set; }
            public float Distance { get; set; }
            public bool eligibleNow { get; set; }

            public Timer timer = new Timer(Plugin.TimeAfterSpawn.Value * 1000);
            public botPlayer(Player player)
            {
                this.Id = player.Id;
                this.eligibleNow = false;
                //this.timer.Start();
                this.timer.Enabled = false;
                this.timer.AutoReset = false;
                this.timer.Elapsed += Plugin.AddBotToEligiblePool(this);
            }

        }
        
        
    }
    
}

