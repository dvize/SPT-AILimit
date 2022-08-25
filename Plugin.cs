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
    [BepInPlugin("com.dvize.ailimit", "dvize.ailimit", "1.1.0")]

    public class Plugin : BaseUnityPlugin
    {
        internal static ConfigEntry<bool> PluginEnabled;
        internal static ConfigEntry<int> BotLimit;
        internal static ConfigEntry<int> BotDistance;
        private Transform _mainCameraTransform;
        private List<AIDistance> distList = new List<AIDistance>();
        private Vector3 cameraPosition;
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

                distList.Clear();
                cameraPosition = this._mainCameraTransform.position;

                //check list of bots for distance to player


                CheckBotDistance(distList, cameraPosition);

                DisableBots(distList);

            }
            
        }

        private void CheckBotDistance(List<AIDistance> distList,Vector3 cameraPosition )
        {

            var gameWorld = Singleton<GameWorld>.Instance;


            for (int i = 0; i < gameWorld.RegisteredPlayers.Count; i++)
            {
                //allplayers contains AI apparently. filter for player and AI
                    
                if (!gameWorld.RegisteredPlayers[i].IsYourPlayer)
                {
                    //find distance to player add to list
                    var tempElement = new AIDistance
                    {
                        Element = i,
                        Distance = Vector3.Distance(cameraPosition, gameWorld.RegisteredPlayers[i].Position)
                    };

                    distList.Add(tempElement);
                    gameWorld.RegisteredPlayers[i].enabled = false;
                }
            }

            return;
        }

        private void DisableBots(List<AIDistance> distList)
        {
            //need to sort list for the distances closest to player and pick the first up to bot limit
            //list only contains bot distance and element in allplayereelement.

            if (!distList.IsNullOrEmpty())
            {
                distList.Sort((x, y) => x.Distance.CompareTo(y.Distance));

                int botCount = 0;

                for (int i = 0; i < distList.Count; i++)
                {
                    if (botCount > Plugin.BotLimit.Value)
                    {
                        break;
                    }

                    if ((distList[i].Distance <= Plugin.BotDistance.Value) && (botCount <= Plugin.BotLimit.Value))
                    {
                        
                        //player.enabled = true;

                        Singleton<GameWorld>.Instance.RegisteredPlayers[distList[i].Element].enabled = true;
                        botCount++;
                    }
                }
            }
            
            return;
        }

    }
    public class AIDistance
    {
        public int Element { get; set; }
        public float Distance { get; set; }
    }

}