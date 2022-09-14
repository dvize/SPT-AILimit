using System;
using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using UnityEngine;

namespace Nexus.AIDisabler
{
	[BepInPlugin("com.pandahhcorp.aidisabler", "AIDisabler", "1.0.0")]
	public class AIDisablerPlugin : BaseUnityPlugin
	{
		private ConfigEntry<Single> _configRange;
		private Transform _mainCameraTransform;

		private void Awake()
		{
			this.Logger.LogInfo("Loading: AIDisabler");
			this._configRange =
				this.Config.Bind("General", "Range", 100f, "All AI outside of this range will be disabled");
			this.Logger.LogInfo("Loaded: AIDisabler");
		}

		private void FixedUpdate()
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

			Vector3 cameraPosition = this._mainCameraTransform.position;
			foreach (Player player in Singleton<GameWorld>.Instance.RegisteredPlayers)
			{
				if (!player.IsYourPlayer)
				{
					player.enabled = Vector3.Distance(cameraPosition, player.Position) <= this._configRange.Value;
				}
			}
		}
	}
}