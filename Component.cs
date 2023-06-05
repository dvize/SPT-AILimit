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

        private static Dictionary<int, PlayerInfo> playerInfoMapping = new Dictionary<int, PlayerInfo>();
        private static List<botPlayer> botList = new List<botPlayer>();
        private static botPlayer bot;
        private static Player player;
        private static PlayerInfo playerInfo;
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

            if (!playerInfoMapping.ContainsKey(player.Id))
            {
                playerInfo = new PlayerInfo
                {
                    Player = player,
                    Bot = new botPlayer(player.Id)
                };
                playerInfoMapping.Add(player.Id, playerInfo);

                // Add bot to the botList immediately
                botList.Add(playerInfo.Bot);
            }

            bot = playerInfo.Bot;
            bot.Distance = Vector3.Distance(player.Position, gameWorld.MainPlayer.Position);

            if (!bot.timer.Enabled && player.CameraPosition != null)
            {
                bot.timer.Enabled = true;
                bot.timer.Start();
            }

            return;
        }



        private static void OnPlayerRemoved(BotOwner botOwner)
        {
            player = botOwner.GetPlayer;

            if (playerInfoMapping.ContainsKey(player.Id))
            {
                var playerInfo = playerInfoMapping[player.Id];

                if (botList.Contains(playerInfo.Bot))
                {
                    botList.Remove(playerInfo.Bot);
                }
                playerInfoMapping.Remove(player.Id);
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

            foreach (var bot in botList)
            {
                bot.Distance = Vector3.Distance(playerInfoMapping[bot.Id].Player.Position, gameWorld.MainPlayer.Position);

                if (botCount < AILimitPlugin.BotLimit.Value &&
                    bot.Distance < botDistance &&
                    bot.eligibleNow)
                {
                    var player = playerInfoMapping[bot.Id].Player;
                    player.enabled = true;
                    botCount++;
                }
                else if (bot.eligibleNow)
                {
                    var player = playerInfoMapping[bot.Id].Player;
                    player.enabled = false;
                }
            }
        }



        private static ElapsedEventHandler EligiblePool(botPlayer botplayer)
        {
            botplayer.timer.Stop();
            botplayer.eligibleNow = true;

            return null;
        }

        private class PlayerInfo
        {
            public Player Player
            {
                get; set;
            }
            public botPlayer Bot
            {
                get; set;
            }
        }

        private class botPlayer
        {
            public int Id
            {
                get;
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

                Player registeredplayer = playerInfoMapping[Id].Player;
                registeredplayer.OnPlayerDeadOrUnspawn += (deadArgs) =>
                {
                    var botValue = playerInfoMapping.ContainsKey(deadArgs.Id) ? playerInfoMapping[deadArgs.Id].Bot : null;

                    if (botList.Contains(botValue))
                    {
                        botList.Remove(botValue);
                    }

                    playerInfoMapping.Remove(deadArgs.Id);
                };
            }
        }




    }
}
