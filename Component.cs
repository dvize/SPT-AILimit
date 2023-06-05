using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using System.Collections.Generic;
using UnityEngine;
using System.Timers;
using System;
using BepInEx.Logging;
using AIlimit;
using HarmonyLib;

namespace AILimit
{
    public class AILimitComponent : MonoBehaviour
    {
        private static bool pluginEnabled;
        private static int botLimit;
        private static float botDistance;
        private static float timeAfterSpawn;

        private static GameWorld gameWorld;

        private static Dictionary<int, Player> playerMapping = new Dictionary<int, Player>();
        private static Dictionary<int, botPlayer> botMapping = new Dictionary<int, botPlayer>();
        private static List<botPlayer> botList = new List<botPlayer>();
        private static botPlayer bot;
        private static Player player;
        private static BotControllerClass botControllerClass;
        private static BotSpawnerClass botSpawnerClass;
        protected static ManualLogSource Logger
        {
            get; private set;
        }

        public AILimitComponent()
        {
            if (Logger == null)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(AILimitComponent));
            }
        }

        public void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<AILimitComponent>();

                botSpawnerClass = (Singleton<IBotGame>.Instance).BotsController.BotSpawner;
                botSpawnerClass.OnBotCreated += OnPlayerAdded;
                botSpawnerClass.OnBotRemoved += OnPlayerRemoved;


                SetupBotDistanceForMap();
                pluginEnabled = AILimitPlugin.PluginEnabled.Value;
                botLimit = AILimitPlugin.BotLimit.Value; 
                timeAfterSpawn = AILimitPlugin.TimeAfterSpawn.Value;
            }
        }

        private static void SetupBotDistanceForMap()
        {
            string location = gameWorld.MainPlayer.Location;

            switch (location)
            {
                case "factory4_day":
                case "factory4_night":
                    botDistance = AILimitPlugin.factoryDistance.Value;
                    break;
                case "bigmap":
                    botDistance = AILimitPlugin.customsDistance.Value;
                    break;
                case "interchange":
                    botDistance = AILimitPlugin.interchangeDistance.Value;
                    break;
                case "reservbase":
                    botDistance = AILimitPlugin.reserveDistance.Value;
                    break;
                case "laboratory":
                    botDistance = AILimitPlugin.laboratoryDistance.Value;
                    break;
                case "lighthouse":
                    botDistance = AILimitPlugin.lighthouseDistance.Value;
                    break;
                case "shoreline":
                    botDistance = AILimitPlugin.shorelineDistance.Value;
                    break;
                case "woods":
                    botDistance = AILimitPlugin.woodsDistance.Value;
                    break;
                case "tarkovstreets":
                    botDistance = AILimitPlugin.tarkovstreetsDistance.Value;
                    break;
                default:
                    botDistance = 200.0f;
                    break;
            }
        }

        private static void OnPlayerAdded(BotOwner botOwner)
        {
            player = botOwner.GetPlayer;
            if (!player.IsYourPlayer && (!botMapping.ContainsKey(player.Id)) && (!playerMapping.ContainsKey(player.Id)))
            {
                playerMapping.Add(player.Id, player);
                var tempbotplayer = new botPlayer(player.Id);
                botMapping.Add(player.Id, tempbotplayer);

                // Add bot to the botList immediately
                botList.Add(tempbotplayer);
            }
            else if (!playerMapping.ContainsKey(player.Id))
            {
                playerMapping.Add(player.Id, player);
            }

            if (botMapping.ContainsKey(player.Id))
            {
                bot = botMapping[player.Id];
                bot.Distance = Vector3.Distance(player.Position, gameWorld.MainPlayer.Position);

                if (!bot.timer.Enabled && player.CameraPosition != null)
                {
                    bot.timer.Enabled = true;
                    bot.timer.Start();
                }
            }

            return;
        }



        private static void OnPlayerRemoved(BotOwner botOwner)
        {
            player = botOwner.GetPlayer;

            if (playerMapping.ContainsKey(player.Id))
            {
                playerMapping.Remove(player.Id);
            }

            if (botMapping.ContainsKey(player.Id))
            {
                bot = botMapping[player.Id];

                if (botList.Contains(bot))
                {
                    botList.Remove(bot);
                }

                botMapping.Remove(player.Id);
            }
        }

        private void Update()
        {
            if (AILimitPlugin.PluginEnabled.Value)
            {
                try
                {
                    UpdateBots();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }

        private void UpdateBots()
        {
            int botCount = 0;

            botList.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            for (int i = 0; i < botList.Count; i++)
            {
                botList[i].Distance = Vector3.Distance(playerMapping[botList[i].Id].Position, gameWorld.MainPlayer.Position);

                if (botCount < AILimitPlugin.BotLimit.Value &&
                    botList[i].Distance < botDistance &&
                    botList[i].eligibleNow)
                {
                    if (playerMapping.ContainsKey(botList[i].Id))
                    {
                        playerMapping[botList[i].Id].enabled = true;
                        botCount++;
                    }
                }
                else if (botList[i].eligibleNow)
                {
                    if (playerMapping.ContainsKey(botList[i].Id))
                    {
                        playerMapping[botList[i].Id].enabled = false;
                    }
                }
            }
        }



        private static ElapsedEventHandler EligiblePool(botPlayer botplayer)
        {
            botplayer.timer.Stop();
            botplayer.eligibleNow = true;

            return null;
        }

        private class botPlayer
        {
            public int Id
            {
                get; set;
            }
            public float Distance
            {
                get; set;
            }
            public bool eligibleNow
            {
                get; set;
            }

            public Timer timer;

            public botPlayer(int newID)
            {
                Id = newID;
                eligibleNow = false;

                timer = new Timer(AILimitPlugin.TimeAfterSpawn.Value * 1000);
                timer.Enabled = false;
                timer.AutoReset = false;
                timer.Elapsed += (sender, e) => EligiblePool(this);

                Player registeredplayer = playerMapping[Id];
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

