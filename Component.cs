using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using AIlimit;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using UnityEngine;

namespace AILimit
{
    public class AILimitComponent : MonoBehaviour
    {
        private static float botDistance;
        private static int botCount;
        private static GameWorld gameWorld;

        private int frameCounter = 0;
        private List<botPlayer> disabledBotsLastFrame = new List<botPlayer>();


        private static Dictionary<int, PlayerInfo> playerInfoMapping = new Dictionary<int, PlayerInfo>();
        private static List<botPlayer> botList = new List<botPlayer>();

        private botPlayer bot;
        private Player player;

        private static BotSpawner botSpawnerClass;
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

        private void Start()
        {
            botSpawnerClass.OnBotCreated += OnPlayerAdded;
            botSpawnerClass.OnBotRemoved += OnPlayerRemoved;

            SetupBotDistanceForMap();

            //reset static vars to work with new raid
            playerInfoMapping = new Dictionary<int, PlayerInfo> {
            };

            botList = new List<botPlayer>
            {
            };

            Logger.LogDebug("Setup Bot Distance for Map: " + botDistance);
        }
        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<AILimitComponent>();

                //botspawner is wrong class. bots being enabled here will limit bots spawned.

                botSpawnerClass = (Singleton<IBotGame>.Instance).BotsController.BotSpawner;


                Logger.LogDebug("AILimit Enabled");
            }
        }

        private void SetupBotDistanceForMap()
        {
            string location = gameWorld.MainPlayer.Location.ToLower();
            Logger.LogDebug($"The location detected is: {location}");
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
                case "rezervbase":
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

        private void OnPlayerAdded(BotOwner botOwner)
        {
            if (!botOwner.GetPlayer.IsYourPlayer)
            {
                player = botOwner.GetPlayer;
                Logger.LogDebug("In OnPlayerAdded Method: " + player.gameObject.name);

                if (!playerInfoMapping.ContainsKey(player.Id))
                {
                    var playerInfo = new PlayerInfo
                    {
                        Player = player,
                        Bot = new botPlayer(player.Id)
                    };

                    playerInfoMapping.Add(player.Id, playerInfo);

                    // Add bot to the botList immediately
                    botList.Add(playerInfo.Bot);

                    Logger.LogDebug("Added: " + player.Profile.Info.Settings.Role + " - " + player.Profile.Nickname + " to botList");

                    bot = playerInfo.Bot;
                    bot.Distance = Vector3.SqrMagnitude(player.Position - gameWorld.MainPlayer.Position);


                    if (!bot.timer.Enabled && player.CameraPosition != null)
                    {
                        bot.timer.Enabled = true;
                        bot.timer.Start();
                    }
                }
            }
        }

        private void OnPlayerRemoved(BotOwner botOwner)
        {
            player = botOwner.GetPlayer;
            if (playerInfoMapping.ContainsKey(player.Id))
            {
                var playerInfo = playerInfoMapping[player.Id];
                if (botList.Contains(playerInfo.Bot))
                {
                    botList.Remove(playerInfo.Bot);
                }

                if (disabledBotsLastFrame.Contains(playerInfo.Bot))
                {
                    disabledBotsLastFrame.Remove(playerInfo.Bot);
                }

                playerInfoMapping.Remove(player.Id);
            }
        }

        private void Update()
        {
            if (AILimitPlugin.PluginEnabled.Value)
            {
                frameCounter++;

                if (frameCounter >= AILimitPlugin.FramesToCheck.Value)
                {
                    UpdateBots();
                    frameCounter = 0; // Reset the frame counter
                }
                else
                {
                    UpdateBotsWithDisabledList();
                }
            }
        }

        private void UpdateBots()
        {
            botCount = 0;
            disabledBotsLastFrame.Clear();

            botList.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            foreach (var bot in botList)
            {
                player = playerInfoMapping[bot.Id].Player;

                if (player == null || !player.HealthController.IsAlive)
                {
                    continue;
                }

                bot.Distance = Vector3.SqrMagnitude(player.Position - gameWorld.MainPlayer.Position);

                if (botCount < AILimitPlugin.BotLimit.Value &&
                    bot.Distance < botDistance * botDistance &&
                    bot.eligibleNow)
                {
                    player.gameObject.SetActive(true);
                    botCount++;
                }
                else if (bot.eligibleNow && !disabledBotsLastFrame.Contains(bot))
                {
                    // Clear AI decision queue so they don't do anything when they are disabled.
                    player.AIData.BotOwner.DecisionQueue.Clear();
                    player.AIData.BotOwner.Memory.GoalEnemy = null;
                    player.gameObject.SetActive(false);
                    disabledBotsLastFrame.Add(bot);
                }
            }
        }

        private void UpdateBotsWithDisabledList()
        {
            foreach (var bot in disabledBotsLastFrame)
            {
                player = playerInfoMapping[bot.Id].Player;

                if (player == null || !player.HealthController.IsAlive)
                {
                    continue;
                }

                if (bot.eligibleNow)
                {
                    player.AIData.BotOwner.DecisionQueue.Clear();
                    player.AIData.BotOwner.Memory.GoalEnemy = null;
                    player.gameObject.SetActive(false);
                }
            }
        }

        private static async Task<ElapsedEventHandler> EligiblePool(botPlayer botplayer)
        {
            //async while loop with await until bot actually in game
            while (playerInfoMapping[botplayer.Id].Player.CameraPosition == null)
            {
                await Task.Delay(1000);
            }

            botplayer.timer.Stop();
            botplayer.eligibleNow = true;
            Logger.LogDebug("Bot # " + playerInfoMapping[botplayer.Id].Player.gameObject.name + " is now eligible.");
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
            }
        }




    }
}
