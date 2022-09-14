using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;


namespace dvize.AILimit
{
    [BepInPlugin("com.dvize.ailimit", "dvize.ailimit", "1.0.0")]

    public class Plugin : BaseUnityPlugin
    {
        internal static ConfigEntry<bool> PluginEnabled;
        internal static ConfigEntry<int> BotLimit;
        internal static ConfigEntry<int> BotDistance;
        private Transform _mainCameraTransform;
        private void Awake()
        {
            PluginEnabled = Config.Bind(
                "Main Settings",
                "Plugin on/off",
                true,
                "");

            BotDistance = Config.Bind(
                "Main Settings",
                "Bot Distance",
                400,
                "Set Max Distance to activate bots");

            BotLimit = Config.Bind(
                "Main Settings",
                "Bot Limit (At Distance)",
                7,
                "Based on your distance selected, limits up to this many # of bots moving at one time");

        }

        private void FixedUpdate()
        {
            if (Plugin.PluginEnabled.Value)
            {
                var gameWorld = Singleton<GameWorld>.Instance;

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

                Vector3 cameraPosition = this._mainCameraTransform.position;

                var distList = new List<AIDistance>();

                //check list of bots for distance to player

                for (int i = 0; i < gameWorld.RegisteredPlayers.Count; i++)
                {
                    //allplayers contains AI apparently. filter for player and AI
                    Player player = gameWorld.RegisteredPlayers[i];

                    if (!player.IsYourPlayer)
                    {
                        //find distance to player add to list
                        var tempElement = new AIDistance();
                        tempElement.element = i;
                        tempElement.distance = Vector3.Distance(cameraPosition, player.Position);

                        distList.Add(tempElement);
                        player.enabled = false;

                    }

                }

                //need to sort list for the distances closest to player and pick the first up to bot limit
                //list only contains bot distance and element in allplayereelement.

                distList.Sort((x, y) => x.distance.CompareTo(y.distance));

                int botCount = 0;

                for (int i = 0; i < distList.Count; i++)
                {
                    if (botCount > Plugin.BotLimit.Value)
                    {
                        break;
                    }

                    if ((distList[i].distance <= Plugin.BotDistance.Value) && (botCount <= Plugin.BotLimit.Value))
                    {
                        Player player = gameWorld.RegisteredPlayers[distList[i].element];
                        player.enabled = true;
                        botCount++;
                    }

                }

            }

        }

    }
    public class AIDistance
    {
        public int element { get; set; }
        public float distance { get; set; }
    }

}