using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using AIlimit;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using LootingBots.Patch.Components;
using SAIN.Classes;
using SAIN.Components;
using UnityEngine;

namespace AILimit
{
    public class AILimitComponent : MonoBehaviour
    {
        private static float botDistanceLimit;
        private static int botCount;

        private static GameWorld gameWorld;

        private static Dictionary<int, PlayerInfo> playerInfoMapping = new Dictionary<int, PlayerInfo>();
        private static List<botPlayer> botList = new List<botPlayer>();
<<<<<<< Updated upstream
        private static List<int> deadPlayers = new List<int>();
=======

        private int frameCounter = 0;
        
        private static List<botPlayer> disabledBotsLastFrame = new List<botPlayer>();

>>>>>>> Stashed changes
        private botPlayer bot;
        private Player player;

        private List<botPlayer> disabledBotsList = new List<botPlayer>();
        private SortedSet<botPlayer> eligibleBotsQueue = new SortedSet<botPlayer>(new BotPlayerComparer());
        private float playerLastShotTime;
        private const float playerShotCooldown = 10f;
        private static bool useCustomDisabling = false;

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

        private void Start()
        {
            botSpawnerClass.OnBotCreated += OnPlayerAdded;
            botSpawnerClass.OnBotRemoved += OnPlayerRemoved;
            Singleton<GameWorld>.Instance.MainPlayer.OnDamageReceived += MainPlayer_OnDamageReceived;

            SetupBotDistanceForMap();
            Logger.LogDebug("Setup Bot Distance for Map: " + botDistanceLimit);

<<<<<<< Updated upstream
            checkSainandLootingBotDependencies();
        }

        private void checkSainandLootingBotDependencies()
        {
            string sainAssemblyName = "SAIN-3.5.8.dll";
            string lootingBotsAssemblyName = "skwizzy.LootingBots.dll";

            bool isSainLoaded = false;
            bool isLootingBotsLoaded = false;

            foreach (var pluginInfoEntry in BepInEx.Bootstrap.Chainloader.PluginInfos)
            {
                var pluginInfo = pluginInfoEntry.Value;
=======
            //reset static vars to work with new raid
            playerInfoMapping.Clear();
            botList.Clear();
            disabledBotsLastFrame.Clear();
>>>>>>> Stashed changes

                if (pluginInfo.Location.EndsWith(sainAssemblyName))
                {
                    isSainLoaded = true;
                }
                else if (pluginInfo.Location.EndsWith(lootingBotsAssemblyName))
                {
                    isLootingBotsLoaded = true;
                }
            }

            useCustomDisabling = isSainLoaded && isLootingBotsLoaded;
            if (useCustomDisabling)
            {
                Logger.LogDebug("Sain and LootingBots detected. Using custom component disable");
            }
            else
            {
                Logger.LogDebug("Sain and LootingBots not detected. Using setActive(false)");
            }
        }

        private void MainPlayer_OnDamageReceived(float damage, EBodyPart part, EDamageType type, float absorbed, MaterialType special)
        {
            playerLastShotTime = Time.time;
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
                    botDistanceLimit = AILimitPlugin.factoryDistance.Value;
                    break;
                case "bigmap":
                    botDistanceLimit = AILimitPlugin.customsDistance.Value;
                    break;
                case "interchange":
                    botDistanceLimit = AILimitPlugin.interchangeDistance.Value;
                    break;
                case "rezervbase":
                    botDistanceLimit = AILimitPlugin.reserveDistance.Value;
                    break;
                case "laboratory":
                    botDistanceLimit = AILimitPlugin.laboratoryDistance.Value;
                    break;
                case "lighthouse":
                    botDistanceLimit = AILimitPlugin.lighthouseDistance.Value;
                    break;
                case "shoreline":
                    botDistanceLimit = AILimitPlugin.shorelineDistance.Value;
                    break;
                case "woods":
                    botDistanceLimit = AILimitPlugin.woodsDistance.Value;
                    break;
                case "tarkovstreets":
                    botDistanceLimit = AILimitPlugin.tarkovstreetsDistance.Value;
                    break;
                default:
                    botDistanceLimit = 200.0f;
                    break;
            }
        }

        private void OnPlayerAdded(BotOwner botOwner)
        {

            if (!botOwner.GetPlayer.IsYourPlayer)
            {
                player = botOwner.GetPlayer;
                //Logger.LogDebug("In OnPlayerAdded Method: " + player.gameObject.name);

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
                bot.Distance = Vector3.Distance(player.Position, gameWorld.MainPlayer.Position);

                if (!bot.timer.Enabled && player.CameraPosition != null)
                {
                    bot.timer.Enabled = true;
                    bot.timer.Start();
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
                    frameCounter = 0;
                }
                else
                {
                    UpdateBotsWithDisabledList();
                }
            }
        }
        private void UpdateBots()
        {
            bool playerInBattle = (Time.time - playerLastShotTime) <= playerShotCooldown;
            botCount = 0;
<<<<<<< Updated upstream
=======

            //reset disabled bots for next set of 60 frames
            disabledBotsLastFrame.Clear();

>>>>>>> Stashed changes
            botList.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            deadPlayers.Clear();

            foreach (var bot in botList)
            {
<<<<<<< Updated upstream
                if (playerInfoMapping.ContainsKey(bot.Id) && (!playerInfoMapping[bot.Id].Player.HealthController.IsAlive || playerInfoMapping[bot.Id].Player == null))
                {
                    deadPlayers.Add(bot.Id);
=======
                player = playerInfoMapping[bot.Id].Player;

                if (player == null || !player.HealthController.IsAlive)
                {
>>>>>>> Stashed changes
                    continue;
                }

                bot.Distance = Vector3.Distance(playerInfoMapping[bot.Id].Player.Position, gameWorld.MainPlayer.Position);

                //if bot meets conditions and in distance and not during a battle, keep them activated.
                if (botCount < AILimitPlugin.BotLimit.Value && bot.Distance < botDistanceLimit && bot.eligibleNow)
                {
<<<<<<< Updated upstream
                    player = playerInfoMapping[bot.Id].Player;
                    if (useCustomDisabling)
                    {
                        DisableComponents(player, false);
                    }
                    else
                    {
                        player.gameObject.SetActive(true);
                    }
                    botCount++;
                }
                //if player inbattle and is not eligiblenow
                else if (!bot.eligibleNow && playerInBattle)
                {
                    player = playerInfoMapping[bot.Id].Player;
                    if (useCustomDisabling)
                    {
                        DisableComponents(player, true);
                        StopAllCoroutines();
                    }
                    else
                    {
                        player.gameObject.SetActive(false);
                    }
                }
                else if ((botCount >= AILimitPlugin.BotLimit.Value || bot.Distance >= botDistanceLimit) && bot.eligibleNow)
                {
                    player = playerInfoMapping[bot.Id].Player;
                    if (useCustomDisabling)
                    {
                        DisableComponents(player, true);
                    }
                    else
                    {
                        player.gameObject.SetActive(false);
                    }
                }
            }

            foreach (var deadPlayerId in deadPlayers)
            {
                if (playerInfoMapping.ContainsKey(deadPlayerId))
                {
                    var playerInfo = playerInfoMapping[deadPlayerId];
                    botList.Remove(playerInfo.Bot);
                    playerInfoMapping.Remove(deadPlayerId);
                }
            }
        }

        //create a list of classes and components to disable if found on gameObject
        List<Type> componentTypes = new List<Type>
        {
            typeof(SAINComponent),
            typeof(CoverFinderComponent),
            typeof(FlashLightComponent),
            typeof(SquadClass),
            typeof(BotEquipmentClass),
            typeof(BotInfoClass),
            typeof(SAINBotUnstuck),
            typeof(HearingSensorClass),
            typeof(BotTalkClass),
            typeof(DecisionClass),
            typeof(CoverClass),
            typeof(SelfActionClass),
            typeof(SteeringClass),
            typeof(BotGrenadeClass),
            typeof(SAIN_Mover),
            typeof(NoBushESP),
            typeof(EnemyController),
            typeof(SoundsController),
            typeof(LootingBrain)
        };
        private void DisableComponents(Player player, bool setInactive)
        {
            if (setInactive)
            {
                //disable specific component types as well as the default behaviour
                foreach (Type componentType in componentTypes)
                {
                    Component foundComponent = player.gameObject.GetComponent(componentType);

                    if (foundComponent != null)
                    {
                        ((Behaviour)foundComponent).enabled = false;
                        PauseAllComponentCoroutines(foundComponent);
                    }
                }

                player.enabled = false;
            }
            else
            {
                //re-enable all the specific component types and the base player
                foreach (Type componentType in componentTypes)
                {
                    Component foundComponent = player.gameObject.GetComponent(componentType);

                    if (foundComponent != null)
                    {
                        ResumeAllComponentCoroutines(foundComponent);
                        ((Behaviour)foundComponent).enabled = true;
                    }
                }

                player.enabled = true;
            }
        }

        private class BotPlayerComparer : IComparer<botPlayer>
        {
            public int Compare(botPlayer x, botPlayer y)
            {
                if (x == null || y == null)
                    throw new ArgumentException("At least one object must implement IComparable.");

                return x.Distance.CompareTo(y.Distance);
=======
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
>>>>>>> Stashed changes
            }
        }

        private static async Task<ElapsedEventHandler> EligiblePool(botPlayer botplayer)
        {
            //async while loop with await until bot actually in game
            while (playerInfoMapping[botplayer.Id].Player.CameraPosition == null)
            {
                await Task.Delay(500);
            }

            botplayer.timer.Stop();
            botplayer.eligibleNow = true;
            Logger.LogDebug("Bot # " + playerInfoMapping[botplayer.Id].Player.gameObject.name + " is now eligible.");
            return null;
        }

        private void PauseAllComponentCoroutines(Component component)
        {
            // Get the private 'm_Coroutines' field of the component
            var coroutinesField = typeof(Component).GetField("m_Coroutines", BindingFlags.NonPublic | BindingFlags.Instance);

            if (coroutinesField != null)
            {
                // Get the value of the 'm_Coroutines' field
                var coroutines = (IEnumerator[])coroutinesField.GetValue(component);

                // Create a copy of the coroutines array to store the original coroutines
                var originalCoroutines = new IEnumerator[coroutines.Length];
                coroutines.CopyTo(originalCoroutines, 0);

                // Stop each coroutine by setting it to null
                for (int i = 0; i < coroutines.Length; i++)
                {
                    coroutines[i] = null;
                }

                // Store the original coroutines in a separate array
                component.gameObject.AddComponent<CoroutineContainer>().OriginalCoroutines = originalCoroutines;
            }
        }

        private void ResumeAllComponentCoroutines(Component component)
        {
            // Get the CoroutineContainer attached to the component's GameObject
            var container = component.gameObject.GetComponent<CoroutineContainer>();

            if (container != null)
            {
                // Get the private 'm_Coroutines' field of the component
                var coroutinesField = typeof(Component).GetField("m_Coroutines", BindingFlags.NonPublic | BindingFlags.Instance);

                if (coroutinesField != null)
                {
                    // Get the value of the 'm_Coroutines' field
                    var coroutines = (IEnumerator[])coroutinesField.GetValue(component);

                    // Assign back the original coroutines from the CoroutineContainer
                    container.OriginalCoroutines.CopyTo(coroutines, 0);

                    // Remove the CoroutineContainer from the GameObject
                    GameObject.Destroy(container);
                }
            }
        }

        private class CoroutineContainer : MonoBehaviour
        {
            public IEnumerator[] OriginalCoroutines
            {
                get; set;
            }
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
            public bool isDisabled
            {
                get; set;
            }

            public Timer timer;

            public botPlayer(int newID)
            {
                Id = newID;
                eligibleNow = false;
                isDisabled = false; // Initialize isDisabled to false

                timer = new Timer(AILimitPlugin.TimeAfterSpawn.Value * 1000);
                timer.Enabled = false;
                timer.AutoReset = false;
                timer.Elapsed += (sender, e) => EligiblePool(this);
            }
        }

    }
}
