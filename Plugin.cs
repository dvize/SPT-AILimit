
using Aki.Reflection.Patching;
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

namespace dvize.AILimit
{
    [BepInPlugin("com.dvize.AIlimit", "dvize.AIlimit", "1.4.0")]

    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> PluginEnabled;
        public static ConfigEntry<int> BotLimit;
        public static ConfigEntry<int> BotDistance;
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
                400,
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

        List<AIDistance> distList = new List<AIDistance>();
        Transform _mainCameraTransform;
        GameWorld gameWorld = new GameWorld();
        Vector3 cameraPosition = new Vector3();
        AIDistance tempElement = new AIDistance();
        AIDistance searchElement = new AIDistance();
        bool isdead;
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

                try
                {

                    gameWorld = Singleton<GameWorld>.Instance;
                    cameraPosition = this._mainCameraTransform.position;

                    //distList.Clear();


                    //check list of bots for distance to player

                    for (int i = 0; i < gameWorld.RegisteredPlayers.Count; i++)
                    {
                        //determine if at least camera is spawned to potentially add to list
                        if ((!gameWorld.RegisteredPlayers[i].IsYourPlayer) && (gameWorld.RegisteredPlayers[i].CameraPosition != null))
                        {
                            //figure out if its in the list
                            searchElement = distList.Find(x => x.id == gameWorld.RegisteredPlayers[i].Id);
                            if (searchElement != null)
                            {
                                //increment Timer for in raid
                                searchElement.timesincespawn += Time.deltaTime;

                                //check to make sure timer exceeded before able to disable.
                                if (searchElement.timesincespawn > Plugin.TimeAfterSpawn.Value)
                                {
                                    gameWorld.RegisteredPlayers[i].enabled = false;
                                }
                                
                            }
                            else
                            {                             
                                //its not found in list so should be new spawn
                                tempElement = new AIDistance
                                {
                                    id = gameWorld.RegisteredPlayers[i].Id,     //use Id instead so can track without clearing distList
                                    distance = Vector3.Distance(cameraPosition, gameWorld.RegisteredPlayers[i].Position), //find distance to player add to list
                                    timesincespawn = 0f
                                   
                                };

                                //create handler for this player to remove him from the distlist on death.
                                
                                gameWorld.RegisteredPlayers[i].OnPlayerDeadOrUnspawn += (deadArgs) =>
                                {
                                    distList.Remove(distList.Find(x => x.id == deadArgs.Id));
                                };
                                
                                distList.Add(tempElement);
                            }
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

                        if ((distList[i].distance <= Plugin.BotDistance.Value) && (botCount < Plugin.BotLimit.Value) && (distList[i].timesincespawn >= Plugin.TimeAfterSpawn.Value))
                        {
                            gameWorld.RegisteredPlayers.ForEach(_player =>
                            {
                                if (_player.Id == distList[i].id)
                                {
                                    _player.enabled = true;
                                    botCount++;
                                    
                                }
                            });

                        }
                    }
                }
                catch
                {

                }

            }

        }

    }
    
public class AIDistance
    {
        public int id { get; set; }
        public float distance { get; set; }
        public float timesincespawn { get; set; }

    }

}