using System.Reflection;
using AILimit;
using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using EFT;

namespace AIlimit
{
    [BepInPlugin("com.dvize.AILimit", "dvize.AILimit", "1.5.0")]
    public class AILimitPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> PluginEnabled;
        public static ConfigEntry<int> BotLimit;
        public static ConfigEntry<float> BotDistance;
        public static ConfigEntry<float> TimeAfterSpawn;


        public static ConfigEntry<float> factoryDistance;
        public static ConfigEntry<float> interchangeDistance;
        public static ConfigEntry<float> laboratoryDistance;
        public static ConfigEntry<float> lighthouseDistance;
        public static ConfigEntry<float> reserveDistance;
        public static ConfigEntry<float> shorelineDistance;
        public static ConfigEntry<float> woodsDistance;
        public static ConfigEntry<float> customsDistance;
        public static ConfigEntry<float> tarkovstreetsDistance;
        private void Awake()
        {
            PluginEnabled = Config.Bind(
                "Main Settings",
                "Plugin on/off",
                true,
                "");

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


            factoryDistance = Config.Bind(
                "Map Related",
                "factory",
                60.0f,
                "Distance after which bots are disabled.");

            customsDistance = Config.Bind(
                "Map Related",
                "customs",
                200.0f,
                "Distance after which bots are disabled.");

            interchangeDistance = Config.Bind(
                "Map Related",
                "interchange",
                200.0f,
                "Distance after which bots are disabled.");

            laboratoryDistance = Config.Bind(
                "Map Related",
                "labs",
                150.0f,
                "Distance after which bots are disabled.");

            lighthouseDistance = Config.Bind(
                "Map Related",
                "lighthouse",
                200.0f,
                "Distance after which bots are disabled.");

            reserveDistance = Config.Bind(
                "Map Related",
                "reserve",
                200.0f,
                "Distance after which bots are disabled.");

            shorelineDistance = Config.Bind(
                "Map Related",
                "shoreline",
                200.0f,
                "Distance after which bots are disabled.");

            woodsDistance = Config.Bind(
                "Map Related",
                "woods",
                200.0f,
                "Distance after which bots are disabled.");

            tarkovstreetsDistance = Config.Bind(
                "Map Related",
                "streets",
                200.0f,
                "Distance after which bots are disabled.");


            new NewGamePatch().Enable();
        }

    }

    //re-initializes each new game
    internal class NewGamePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

        [PatchPrefix]
        public static void PatchPrefix()
        {
            AILimitComponent.Enable();
        }
    }
}
