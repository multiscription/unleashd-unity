namespace Multiscription.Unleashd
{
    using UnityEngine;

    /// <summary>
    /// ScriptableObject for holding SDK keys and identifiers for all supported platforms.
    /// </summary>
    public class UnleashdConfig : ScriptableObject
    {
        [Header("General configuration:")]
        [Tooltip("SDK color theme")]
        public Unleashd.ThemeColor themeColor = Unleashd.ThemeColor.GOLD;
        [Tooltip("Ingame trial duration (days)")]
        [Range(0, 99)]
        public int trialDurationDays = 0;
        [Tooltip("Ingame trial duration (hours)")]
        [Range(0, 23)]
        public int trialDurationHours = 0;
        [Tooltip("Ingame trial duration (minutes)")]
        [Range(0, 59)]
        public int trialDurationMinutes = 3;

        [Header("Android configuration:")]
        [Tooltip("SDK Key for Android\nFind in Unleashd Developer Portal")]
        public string androidSDKKey = "88ef1e355b5d462e9a81eb4d6bdb8900";
    }
}
