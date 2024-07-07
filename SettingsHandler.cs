using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIlimit;
using AILimit;

namespace dvize.AILimit
{
    internal class SettingsHandler
    {
        internal static void HandleMapDistanceChange(string mapName, float newValue)
        {
            switch (mapName)
            {
                case "factory4_day":
                case "factory4_night":
                    AILimitComponent.botDistance = AILimitPlugin.factoryDistance.Value;
                    break;
                case "bigmap":
                    AILimitComponent.botDistance = AILimitPlugin.customsDistance.Value;
                    break;
                case "sandbox":
                    AILimitComponent.botDistance = AILimitPlugin.groundZeroDistance.Value;
                    break;
                case "interchange":
                    AILimitComponent.botDistance = AILimitPlugin.interchangeDistance.Value;
                    break;
                case "rezervbase":
                    AILimitComponent.botDistance = AILimitPlugin.reserveDistance.Value;
                    break;
                case "laboratory":
                    AILimitComponent.botDistance = AILimitPlugin.laboratoryDistance.Value;
                    break;
                case "lighthouse":
                    AILimitComponent.botDistance = AILimitPlugin.lighthouseDistance.Value;
                    break;
                case "shoreline":
                    AILimitComponent.botDistance = AILimitPlugin.shorelineDistance.Value;
                    break;
                case "woods":
                    AILimitComponent.botDistance = AILimitPlugin.woodsDistance.Value;
                    break;
                case "tarkovstreets":
                    AILimitComponent.botDistance = AILimitPlugin.tarkovstreetsDistance.Value;
                    break;
                default:
                    AILimitComponent.botDistance = 200.0f;
                    break;
            }
    }
    }
}
