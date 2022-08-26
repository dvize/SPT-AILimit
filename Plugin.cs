using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;


namespace dvize.AILimit
{
    [BepInPlugin("com.dvize.ailimit", "dvize.ailimit", "1.2.0")]

    public class Plugin : BaseUnityPlugin
    {
        internal static ConfigEntry<bool> PluginEnabled;
        internal static ConfigEntry<int> BotLimit;
        internal static ConfigEntry<int> BotDistance;
        public Transform _mainCameraTransform;
        public List<AIDistance> distList = new List<AIDistance>();
        public Vector3 cameraPosition;
        public AIDistance inLoopElement = new AIDistance();
        bool initAlready = false;

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

        private async void Update()
        {
            if (Plugin.PluginEnabled.Value)
            {
                if (!Singleton<GameWorld>.Instantiated)
                {
                    StopAllCoroutines();
                    initAlready = false;
                    return;
                }

                if (_mainCameraTransform == null)
                {

                    if (Camera.main != null)
                    {
                        _mainCameraTransform = Camera.main.transform;
                    }

                    return;
                }


                if (!initAlready)
                {
                    StartCoroutine(CheckBotDistance());
                    StartCoroutine(DisableBots());
                    initAlready = true;
                }
                

            }



        }

        private IEnumerator CheckBotDistance()
        {
            if (!(Singleton<GameWorld>.Instance.RegisteredPlayers.IsNullOrEmpty()))
            {
                distList.Clear();
                cameraPosition = _mainCameraTransform.position;

                for (int i = 0; i < Singleton<GameWorld>.Instance.RegisteredPlayers.Count; i++)
                {
                    //allplayers contains AI apparently. filter for player and AI

                    if (!Singleton<GameWorld>.Instance.RegisteredPlayers[i].IsYourPlayer)
                    {
                        //find distance to player add to list
                        inLoopElement.Element = i;
                        inLoopElement.Distance = Vector3.Distance(cameraPosition, Singleton<GameWorld>.Instance.RegisteredPlayers[i].Position);

                        distList.Add(inLoopElement);
                        Singleton<GameWorld>.Instance.RegisteredPlayers[i].enabled = false;
                    }
                }
            }
            yield return new WaitForSeconds(1f);
        }

        private IEnumerator DisableBots()
        {
            //need to sort list for the distances closest to player and pick the first up to bot limit
            //list only contains bot distance and element in allplayereelement.
            
            if (!distList.IsNullOrEmpty())
            {
                int botCount = 0;
                
                distList.Sort((x, y) => x.Distance.CompareTo(y.Distance));

                for (int i = 0; i < distList.Count; i++)
                {
                    if ((distList[i].Distance <= Plugin.BotDistance.Value) && (botCount <= Plugin.BotLimit.Value))
                    {

                        //player.enabled = true;

                        Singleton<GameWorld>.Instance.RegisteredPlayers[distList[i].Element].enabled = true;
                        botCount++;
                    }
                }   
                
            }

            yield return new WaitForSeconds(1f);
        }

    }
    public class AIDistance
    {
        public int Element { get; set; }
        public float Distance { get; set; }
    }

}