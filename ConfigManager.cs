using System;
using AIlimit;

namespace dvize.AILimit
{
    internal static class ConfigManager
    {
        public static event Action<float> OnFactoryDistanceChanged;
        public static event Action<float> OnGroundZeroDistanceChanged;
        public static event Action<float> OnInterchangeDistanceChanged;
        public static event Action<float> OnLaboratoryDistanceChanged;
        public static event Action<float> OnLighthouseDistanceChanged;
        public static event Action<float> OnReserveDistanceChanged;
        public static event Action<float> OnShorelineDistanceChanged;
        public static event Action<float> OnWoodsDistanceChanged;
        public static event Action<float> OnCustomsDistanceChanged;
        public static event Action<float> OnTarkovStreetsDistanceChanged;

        public static void Initialize()
        {
            // Subscribe to the SettingChanged event for each ConfigEntry
            AILimitPlugin.factoryDistance.SettingChanged += (sender, e) => OnFactoryDistanceChanged?.Invoke(AILimitPlugin.factoryDistance.Value);
            AILimitPlugin.groundZeroDistance.SettingChanged += (sender, e) => OnGroundZeroDistanceChanged?.Invoke(AILimitPlugin.groundZeroDistance.Value);
            AILimitPlugin.interchangeDistance.SettingChanged += (sender, e) => OnInterchangeDistanceChanged?.Invoke(AILimitPlugin.interchangeDistance.Value);
            AILimitPlugin.laboratoryDistance.SettingChanged += (sender, e) => OnLaboratoryDistanceChanged?.Invoke(AILimitPlugin.laboratoryDistance.Value);
            AILimitPlugin.lighthouseDistance.SettingChanged += (sender, e) => OnLighthouseDistanceChanged?.Invoke(AILimitPlugin.lighthouseDistance.Value);
            AILimitPlugin.reserveDistance.SettingChanged += (sender, e) => OnReserveDistanceChanged?.Invoke(AILimitPlugin.reserveDistance.Value);
            AILimitPlugin.shorelineDistance.SettingChanged += (sender, e) => OnShorelineDistanceChanged?.Invoke(AILimitPlugin.shorelineDistance.Value);
            AILimitPlugin.woodsDistance.SettingChanged += (sender, e) => OnWoodsDistanceChanged?.Invoke(AILimitPlugin.woodsDistance.Value);
            AILimitPlugin.customsDistance.SettingChanged += (sender, e) => OnCustomsDistanceChanged?.Invoke(AILimitPlugin.customsDistance.Value);
            AILimitPlugin.tarkovstreetsDistance.SettingChanged += (sender, e) => OnTarkovStreetsDistanceChanged?.Invoke(AILimitPlugin.tarkovstreetsDistance.Value);
        }
    }
}
