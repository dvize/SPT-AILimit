
using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using System.Collections.Generic;
using UnityEngine;


namespace dvize.AILimit
{
    [BepInPlugin("com.dvize.AIlimit", "dvize.AIlimit", "1.2.0")]

    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> PluginEnabled;
        public static ConfigEntry<int> BotLimit;
        public static ConfigEntry<int> BotDistance;

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
                400,
                "Set Max Distance to activate bots");

            BotLimit = Config.Bind(
                "Main Settings",
                "Bot Limit (At Distance)",
                7,
                "Based on your distance selected, limits up to this many # of bots moving at one time");

        }

        List<AIDistance> distList = new List<AIDistance>();
        Transform _mainCameraTransform;
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

                var gameWorld = Singleton<GameWorld>.Instance;
                Vector3 cameraPosition = this._mainCameraTransform.position;
                distList.Clear();


                //check list of bots for distance to player
                
                for (int i = 0; i < gameWorld.RegisteredPlayers.Count; i++)
                {

                    if (!gameWorld.RegisteredPlayers[i].IsYourPlayer)
                    {
                        //find distance to player add to list
                        var tempElement = new AIDistance
                        {
                            element = i,
                            distance = Vector3.Distance(cameraPosition, gameWorld.RegisteredPlayers[i].Position)
                        };

                        distList.Add(tempElement);
                        //gameWorld.RegisteredPlayers[i].enabled = false;
                        
                        gameWorld.RegisteredPlayers[i].gameObject.SetActive(false);
                        gameWorld.RegisteredPlayers[i].enabled = false;
                    }

                }

                //need to sort list for the distances closest to player and pick the first up to bot limit

                distList.Sort((x, y) => x.distance.CompareTo(y.distance));

                int botCount = 0;

                for (int i = 0; i < distList.Count; i++)
                {
                    if ((botCount >= Plugin.BotLimit.Value) || (distList[i].distance > Plugin.BotDistance.Value))
                    {
                        break;
                    }

                    if ((distList[i].distance <= Plugin.BotDistance.Value) && (botCount < Plugin.BotLimit.Value))
                    {
                        gameWorld.RegisteredPlayers[distList[i].element].gameObject.SetActive(true);
                        gameWorld.RegisteredPlayers[distList[i].element].enabled = true;

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