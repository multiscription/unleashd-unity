namespace Multiscription.Unleashd
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using UnityEngine;

#pragma warning disable CS0162
    public class Unleashd : MonoBehaviour
    {
        private const string VERSION_INFO_CSHARP = "2.5.4";

        public delegate void PluginDelegate();
        public PluginDelegate OnReady;
        public PluginDelegate OnStateChanged;

        private static Unleashd instance;
        private AndroidJavaClass unleashdPlugin;
        private AndroidJavaObject applicationContext;
        private AndroidJavaObject currentActivity;
        private UnleashdAndroidJavaProxy unleashdProxy = new UnleashdAndroidJavaProxy();
        private bool isInitialized;
        private bool isReady;
        private string applicationId;
        private UnleashdConfig unleashdConfig;
        private Thread mainThread;
        private List<Action> mainThreadPending = new List<Action>();
        private Action[] mainThreadPendingArray = new Action[0];
        private DateTime trialEndDate = DateTime.Now;
        private SubscriptionState subscriptionState;
#if UNITY_EDITOR && UNITY_ANDROID
        private bool editorShowPurchaseDialog;
#endif

        /// <summary>
        /// First time it is called it will instantiate the Unleashd GameObject and return it. All subsequent call will return the already instantiated Unleashd GameObject. This method does not initialize the Unleashd SDK.
        /// </summary>
        /// <returns>Reference to the Unleashd GameObject instance.</returns>
        public static Unleashd Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject();
                    DontDestroyOnLoad(go);
                    go.name = "UnleashdManager";
                    instance = go.AddComponent<Unleashd>();
                    instance.subscriptionState = new SubscriptionState(false, false, false, false);
                }
                return instance;
            }
        }

        /// <summary>
        /// Initializes the Unleashd plugin.
        /// </summary>
        /// <param name="gameTrial">Amount of milliseconds for the duration of the ingame free trial. Pass TrialPeriod.NO_GAME_TRIAL for no trial.</param>
        /// <param name="benefitNotifications">Amount of milliseconds between benefit notifications. Pass BenefitFrequency.NO_BENEFIT_NOTIFICATION for no notifications. THIS FEATURE CURRENTLY NOT IMPLEMENTED.</param>
        /// <param name="applicationVersion">The version of your app. You should pass Application.version here.</param>
        /// <param name="sdkKeyAndroid">Your secret SDK key for Android found in the Unleashd Developer Portal.</param>
        /// <param name="applicationId">The id of your app. You should pass Application.identifier here.</param>
        [Obsolete("Init(long gameTrial, long benefitNotifications, string applicationVersion, string sdkKeyAndroid, string applicationId) is deprecated, please use Init() instead.")]
        public void Init(long gameTrial, long benefitNotifications, string applicationVersion, string sdkKeyAndroid, string applicationId)
        {
            Init(false);
        }

        /// <summary>
        /// Initializes the Unleashd plugin.
        /// </summary>
        public void Init()
        {
            Init(false);
        }

        /// <summary>
        /// Initialize the Unleashd SDK.
        /// <param name="disableIngameTrial">Can be used to disable ingame trials from code, e.g. when restoring save data from Google Play Games Service.</param>
        /// </summary>
        public void Init(bool disableIngameTrial)
        {
            if (!IsSupported())
            {
                Debug.LogError("Platform not supported");
                return;
            }

            UnleashdConfig loadedConfig = Resources.Load<UnleashdConfig>("Unleashd/UnleashdConfig");
            if (loadedConfig == null)
            {
                Debug.LogError("Resources/Unleashd/UnleashdConfig.asset could not be loaded");
                return;
            }

            if (isInitialized)
            {
                Debug.LogWarning("Unleashd already initialized");
                return;
            }

            unleashdConfig = loadedConfig;
            applicationId = Application.identifier;

#if UNITY_EDITOR
            isInitialized = true;
            subscriptionState.hasActiveSubscription = PlayerPrefs.GetInt("UnleashdSubscription") == 1;
            OnPluginReady();
            OnPluginStateChanged();
            return;
#endif

#if UNITY_ANDROID
            unleashdPlugin = new AndroidJavaClass("com.unleashd.sdk.Unleashd");
            if (unleashdPlugin == null)
            {
                Debug.LogError("unleashdPlugin is null");
                return;
            }

            unleashdProxy.OnPluginReady = OnPluginReady;
            unleashdProxy.OnPluginStateChanged = OnPluginStateChanged;

            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            if (unityPlayer == null)
            {
                Debug.LogError("unityPlayer is null");
                return;
            }

            currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            if (currentActivity == null)
            {
                Debug.LogError("currentActivity is null");
                return;
            }

            applicationContext = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
            if (applicationContext == null)
            {
                Debug.LogError("applicationContext is null");
            }

            long ingameTrialTime = unleashdConfig.trialDurationMinutes * TrialPeriod.ONE_MINUTE_GAME_TRIAL + unleashdConfig.trialDurationHours * TrialPeriod.ONE_HOUR_GAME_TRIAL + unleashdConfig.trialDurationDays * TrialPeriod.ONE_DAY_GAME_TRIAL;
            if (ingameTrialTime <= 0 || disableIngameTrial) ingameTrialTime = TrialPeriod.NO_GAME_TRIAL;
            unleashdPlugin.CallStatic("init", applicationContext, currentActivity, ingameTrialTime, BenefitFrequency.BENEFIT_NOTIFICATION_FREQUENCY_EVERY_DAY, Application.version, unleashdConfig.androidSDKKey, applicationId, unleashdProxy);

            isInitialized = true;
            return;
#endif
        }

        void Awake()
        {
            mainThread = Thread.CurrentThread;
        }

        void Update()
        {
            if (mainThreadPending.Count == 0) return;
            lock (mainThreadPending)
            {
                mainThreadPendingArray = mainThreadPending.ToArray();
                mainThreadPending.Clear();
            }
            foreach (Action action in mainThreadPendingArray)
            {
                action();
            }
        }

        private void RunOnMainThread(Action action)
        {
            if (Thread.CurrentThread == mainThread)
            {
                action();
            }
            else
            {
                lock (mainThreadPending)
                {
                    mainThreadPending.Add(action);
                }
            }
        }

        private void OnPluginReady()
        {
            isReady = true;
#if UNITY_ANDROID
            if (unleashdPlugin != null)
            {
                subscriptionState = GetSubscriptionState();
            }
#endif
            UpdateTrialEndDate();
            RunOnMainThread(() => { OnReady?.Invoke(); });
        }

        private void OnPluginStateChanged()
        {
#if UNITY_ANDROID
            if (unleashdPlugin != null)
            {
                SubscriptionState newPluginState = GetSubscriptionState();
                if (newPluginState.Equals(subscriptionState)) return;
                subscriptionState = newPluginState;
            }
#endif
            UpdateTrialEndDate();
            RunOnMainThread(() => { OnStateChanged?.Invoke(); });
        }

        private void UpdateTrialEndDate()
        {
#if UNITY_ANDROID
            if (unleashdPlugin != null)
            {
                long trialMsecsLeft = unleashdPlugin.CallStatic<long>("getTrialTimeLeft");
                trialEndDate = DateTime.Now.AddMilliseconds(trialMsecsLeft);
            }
#endif
        }

        private SubscriptionState GetSubscriptionState()
        {
            if (unleashdPlugin != null)
            {
                return new SubscriptionState(unleashdPlugin.CallStatic<bool>("hasActiveSubscription"), unleashdPlugin.CallStatic<bool>("isInGameTrialExpired"), unleashdPlugin.CallStatic<bool>("inGameTrialAllowed"), unleashdPlugin.CallStatic<bool>("isInGameTrial"));
            }
            else
            {
                return new SubscriptionState(false, false, false, false);
            }
        }

        /// <summary>
        /// Show the Unleashd popup, to start signup flow.
        /// </summary>
        public void ShowPurchaseDialog()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("Unleashd not initialized");
                return;
            }

#if UNITY_EDITOR
            editorShowPurchaseDialog = true;
            return;
#endif

#if UNITY_ANDROID
            if (unleashdPlugin != null)
            {
                unleashdPlugin.CallStatic("showPurchaseDialog", currentActivity, Application.productName, applicationId, unleashdConfig.androidSDKKey, (int)unleashdConfig.themeColor);
            }
            else
            {
                string bundleId = "com.multiscription.app";
                AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager");
                AndroidJavaObject launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", bundleId);
                if (launchIntent != null)
                {
                    ca.Call("startActivity", launchIntent);
                    up.Dispose();
                    ca.Dispose();
                    packageManager.Dispose();
                    launchIntent.Dispose();
                }
                else
                {
                    Application.OpenURL("https://play.google.com/store/apps/details?id=com.multiscription.app");
                }
            }
            return;
#endif
        }

        /// <summary>
        /// Show the Unleashd popup, to start signup flow.
        /// </summary>
        /// <param name="applicationName">Name of your app. You should pass Application.productName here.</param>
        /// <param name="themeColor">Color style of the Unleashd popup.</param>
        [Obsolete("ShowPurchaseDialog(string applicationName, ThemeColor themeColor) is deprecated, please use ShowPurchaseDialog() instead.")]
        public void ShowPurchaseDialog(string applicationName, ThemeColor themeColor)
        {
            ShowPurchaseDialog();
        }

        /// <summary>
        /// Close the Unleashd subscription dialog.
        /// </summary>
        public void DismissDialog()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("Unleashd not initialized");
                return;
            }
#if UNITY_EDITOR && UNITY_ANDROID
            editorShowPurchaseDialog = false;
            return;
#endif
            if (unleashdPlugin != null)
            {
                unleashdPlugin.CallStatic("dismiss");
            }
        }

        /// <summary>
        /// Checks if the player has an active subscription.
        /// </summary>
        /// <returns>True if the player has an active subscription. False otherwise.</returns>
        public bool HasActiveSubscription()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("Unleashd not initialized");
                return false;
            }
            return subscriptionState.hasActiveSubscription;
        }

        /// <summary>
        /// Checks if the player have had a ingame trial that is now expired.
        /// </summary>
        /// <returns>True if the player have had a ingame trial that is expired. False otherwise.</returns>
        public bool IsInGameTrialExpired()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("Unleashd not initialized");
                return false;
            }
            return subscriptionState.isInGameTrialExpired;
        }

        /// <summary>
        /// Checks if the game allows having an ingame trial.
        /// </summary>
        /// <returns>True if the game allows having an ingame trial. False otherwise.</returns>
        public bool InGameTrialAllowed()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("Unleashd not initialized");
                return false;
            }
            return subscriptionState.inGameTrialAllowed;
        }

        /// <summary>
        /// Checks if the player has an active ingame trial.
        /// </summary>
        /// <returns>True if the player has an active ingame trial. False otherwise.</returns>
        public bool IsInGameTrial()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("Unleashd not initialized");
                return false;
            }
            return subscriptionState.isInGameTrial;
        }

        /// <summary>
        /// Get time left of active ingame trial, if the player has an active ingame trial.
        /// </summary>
        /// <returns>Time left of active trial as an English text.</returns>
        [Obsolete("GetTrialTrialLeftFormatted() is deprecated, please use GetTrialTimeLeftFormatted() instead.")]
        public string GetTrialTrialLeftFormatted()
        {
            return GetTrialTimeLeftFormatted();
        }

        /// <summary>
        /// Get time left of active ingame trial, if the player has an active ingame trial.
        /// </summary>
        /// <returns>Time left of active trial as an English text.</returns>
        public string GetTrialTimeLeftFormatted()
        {
            if (unleashdPlugin != null)
            {
                return unleashdPlugin.CallStatic<string>("getTrialTrialLeftFormatted");
            }
            return "";
        }

        /// <summary>
        /// Get the time when the trial ends, if the player has an active ingame trial.
        /// </summary>
        /// <returns>DateTime holding the time when the ingame trial ends.</returns>
        public DateTime GetTrialEndDate()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("Unleashd not initialized");
                return DateTime.Now;
            }
            return trialEndDate;
        }

        /// <summary>
        /// Get monthly price of a single-user Unleashd subscription, in the local currency.
        /// </summary>
        /// <returns>String with monthly price of subscription and currency indication.</returns>
        public string GetLocalizedSubscriptionPrice()
        {
            if (unleashdPlugin != null)
            {
                return unleashdPlugin.CallStatic<string>("getLocalizedSubscriptionPrice");
            }
            return "";
        }

        /// <summary>
        /// Shortcut to check if the player should have access to Unleashd benefits.
        /// </summary>
        /// <returns>True if the player should have access to Unleashd benefits. False otherwise.</returns>
        [Obsolete("CanPlay() is deprecated, please use HasSubscriptionBenefits() instead.")]
        public bool CanPlay()
        {
            return HasSubscriptionBenefits();
        }

        /// <summary>
        /// Shortcut to check if the player should have access to Unleashd benefits.
        /// </summary>
        /// <returns>True if the player should have access to Unleashd benefits. False otherwise.</returns>
        public bool HasSubscriptionBenefits()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("Unleashd not initialized");
                return false;
            }
            return isReady && (HasActiveSubscription() || (InGameTrialAllowed() && IsInGameTrial() && !IsInGameTrialExpired()));
        }

        /// <summary>
        /// Checks if the Unleashd SDK has been initialized. This does not imply that the SDK is ready for use (use OnReady callback or IsReady method for that).
        /// </summary>
        /// <returns>True if the SDK has been initialized. False otherwise.</returns>
        public bool IsInitialized()
        {
            return isInitialized;
        }

        /// <summary>
        /// Checks if the Unleashd SDK is ready for use. This implies that the rest of the SDK is ready for use.
        /// </summary>
        /// <returns>True if the SDK has been initialized and OnReady callback has been invoked. False otherwise.</returns>
        public bool IsReady()
        {
            return isReady;
        }

        /// <summary>
        /// Checks if the Unleashd SDK is supported on the current runtime platform.
        /// </summary>
        /// <returns>True if this platform supports Unleashd. False otherwise.</returns>
        public static bool IsSupported()
        {
#if UNITY_EDITOR && UNITY_ANDROID
            return true;
#endif
            return Application.platform == RuntimePlatform.Android;
        }

        /// <summary>
        /// Get version number of the SDK.
        /// </summary>
        /// <returns>Version number of the C# part of the SDK followed by version number of the native part of the SDK.</returns>
        public static string GetVersionNumber()
        {
            string nativeVersion = "NA";
            if (instance != null && instance.unleashdPlugin != null)
            {
                nativeVersion = instance.unleashdPlugin.CallStatic<string>("getVersionNumber");
            }
            return VERSION_INFO_CSHARP + "_" + nativeVersion;
        }

#if UNITY_EDITOR && UNITY_ANDROID
        void OnGUI()
        {
            if (editorShowPurchaseDialog)
            {
                float itemWidth = Screen.width * 0.8f;
                float itemHeight = Screen.height * 0.1f;
                float itemHorizontalPosition = (Screen.width - itemWidth) / 2f;
                float itemVerticalPosition = Screen.height * 0.02f;
                GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                int fontSize = Mathf.Min(Mathf.FloorToInt(Screen.width * 40 / 1000f), Mathf.FloorToInt(Screen.height * 40 / 1000f));
                labelStyle.fontSize = fontSize;
                labelStyle.fontStyle = FontStyle.Bold;
                labelStyle.alignment = TextAnchor.MiddleCenter;
                GUI.Label(new Rect(itemHorizontalPosition, Screen.height - (itemVerticalPosition + itemHeight) * 3, itemWidth, itemHeight), "Unleashd debug menu\n" + "Subscription status: " + (subscriptionState.hasActiveSubscription ? "Active" : "Not active"), labelStyle);
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.fontSize = fontSize;
                if (GUI.Button(new Rect(itemHorizontalPosition, Screen.height - (itemVerticalPosition + itemHeight) * 2, itemWidth, itemHeight), subscriptionState.hasActiveSubscription ? "Deactivate Unleashd subscription" : "Activate Unleashd subscription", buttonStyle))
                {
                    subscriptionState.hasActiveSubscription = !subscriptionState.hasActiveSubscription;
                    PlayerPrefs.SetInt("UnleashdSubscription", subscriptionState.hasActiveSubscription ? 1 : 0);
                    editorShowPurchaseDialog = false;
                    OnPluginStateChanged();
                }
                if (GUI.Button(new Rect(itemHorizontalPosition, Screen.height - (itemVerticalPosition + itemHeight) * 1, itemWidth, itemHeight), "Close", buttonStyle))
                {
                    editorShowPurchaseDialog = false;
                }
            }
        }
#endif

        private struct SubscriptionState
        {
            public bool hasActiveSubscription;
            public bool isInGameTrialExpired;
            public bool inGameTrialAllowed;
            public bool isInGameTrial;

            public SubscriptionState(bool hasActiveSubscription, bool isInGameTrialExpired, bool inGameTrialAllowed, bool isInGameTrial)
            {
                this.hasActiveSubscription = hasActiveSubscription;
                this.isInGameTrialExpired = isInGameTrialExpired;
                this.inGameTrialAllowed = inGameTrialAllowed;
                this.isInGameTrial = isInGameTrial;
            }
        }

        /// <summary>
        /// Available theme colors when opening the Unleashd subscription dialog.
        /// </summary>
        public enum ThemeColor
        {
            GOLD = 0,
            PINK = 1,
            GREEN = 2,
            BLUE = 3
        }

    }
#pragma warning restore CS0162

    public class UnleashdAndroidJavaProxy : AndroidJavaProxy
    {
        public UnleashdAndroidJavaProxy() : base("com.unleashd.sdk.PluginCallback") { }

        public delegate void PluginDelegate();
        public PluginDelegate OnPluginReady;
        public PluginDelegate OnPluginStateChanged;

        public void pluginReady()
        {
            OnPluginReady();
        }

        public void pluginStateChanged()
        {
            OnPluginStateChanged();
        }
    }

    /// <summary>
    /// Constants for defining the length of the ingame trial period. Use NO_GAME_TRIAL to not have ingame trial.
    /// </summary>
    public static class TrialPeriod
    {
        public static long NO_GAME_TRIAL = -1;
        public static long ONE_SECOND_GAME_TRIAL = 1000;
        public static long ONE_MINUTE_GAME_TRIAL = ONE_SECOND_GAME_TRIAL * 60;
        public static long ONE_HOUR_GAME_TRIAL = ONE_MINUTE_GAME_TRIAL * 60;
        public static long ONE_DAY_GAME_TRIAL = ONE_HOUR_GAME_TRIAL * 24;
        public static long ONE_WEEK_GAME_TRIAL = ONE_DAY_GAME_TRIAL * 7;
    }

    /// <summary>
    /// Constants for defining the frequency of the benefit notifications on the device. Use NO_BENEFIT_NOTIFICATION to not have notifications.
    /// </summary>
    public static class BenefitFrequency
    {
        public static long NO_BENEFIT_NOTIFICATION = TrialPeriod.NO_GAME_TRIAL;
        public static long BENEFIT_NOTIFICATION_FREQUENCY_EVERY_DAY = TrialPeriod.ONE_DAY_GAME_TRIAL;
        public static long BENEFIT_NOTIFICATION_FREQUENCY_EVERY_3_DAYS = TrialPeriod.ONE_DAY_GAME_TRIAL * 3;
        public static long BENEFIT_NOTIFICATION_FREQUENCY_EVERY_WEEK = TrialPeriod.ONE_DAY_GAME_TRIAL * 7;
        public static long BENEFIT_NOTIFICATION_FREQUENCY_EVERY_MONTH = TrialPeriod.ONE_DAY_GAME_TRIAL * 30;
    }
}
