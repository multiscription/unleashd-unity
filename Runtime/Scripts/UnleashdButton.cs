namespace Multiscription.Unleashd
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;
#if UNITY_EDITOR
    using System.Reflection;
    using UnityEditor.PackageManager;
#endif

    [SelectionBase]
    public class UnleashdButton : MonoBehaviour
    {
        [SerializeField]
        private ButtonDesign buttonDesign;
        [SerializeField]
        private ButtonColor buttonColor;
        [SerializeField]
        private ButtonTextColor buttonTextColor;
        [SerializeField]
        [HideInInspector]
        private AnimationCurve animationCurve;

        private Button unleashdButton;
        private RectTransform unleashdButtonRectTransform;
        private Text unleashdButtonText;

        private const int maxTrialTimeComponents = 2;
        private const float animationTime = 5;

        void Awake()
        {
            unleashdButton = gameObject.GetComponentInChildren<Button>();
            unleashdButtonRectTransform = unleashdButton.GetComponent<RectTransform>();
            unleashdButtonText = gameObject.GetComponentInChildren<Text>();

            if (Unleashd.IsSupported())
            {
                unleashdButton.onClick.AddListener(OpenUnleashd);
                Unleashd.Instance.OnReady += UpdateUI;
                Unleashd.Instance.OnStateChanged += UpdateUI;
                if (!Unleashd.Instance.IsInitialized())
                {
                    Unleashd.Instance.Init();
                }

             UpdateUI();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
#endif
            if (Unleashd.IsSupported())
            {
                Unleashd.Instance.OnReady -= UpdateUI;
                Unleashd.Instance.OnStateChanged -= UpdateUI;
            }
        }

        void OnApplicationFocus(bool focus)
        {
            if (focus)
            {
                UpdateUI();
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            PackageInfo packageInfo = PackageInfo.FindForAssembly(assembly);
            string spritesPath = "Assets/Unleashd/Sprites/Buttons/";
            if (packageInfo != null)
            {
                spritesPath = "Packages/com.multiscription.unleashd/SDK/Sprites/Buttons/";
            }
            Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(spritesPath + buttonDesign.ToString() + "_" + buttonColor.ToString() + ".png");
            Button button = gameObject.GetComponentInChildren<Button>();
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage.sprite != sprite)
            {
                button.GetComponent<Image>().sprite = sprite;
                UnityEditor.EditorUtility.SetDirty(buttonImage);
            }

            bool markTextDirty = false;
            Text text = gameObject.GetComponentInChildren<Text>();
            if (buttonTextColor == ButtonTextColor.WHITE)
            {
                if (text.color != Color.white)
                {
                    text.color = Color.white;
                    markTextDirty = true;
                }
            }
            else
            {
                if (text.color != Color.black)
                {
                    text.color = Color.black;
                    markTextDirty = true;
                }
            }
            if (buttonDesign == ButtonDesign.ROUND)
            {
                if (text.rectTransform.anchoredPosition != new Vector2(0f, 17f))
                {
                    text.rectTransform.anchoredPosition = new Vector2(0f, 17f);
                    markTextDirty = true;
                }
            }
            else
            {
                if (text.rectTransform.anchoredPosition != new Vector2(0f, 11f))
                {
                    text.rectTransform.anchoredPosition = new Vector2(0f, 11f);
                    markTextDirty = true;
                }
            }
            if (markTextDirty)
            {
                UnityEditor.EditorUtility.SetDirty(text);
                UnityEditor.EditorUtility.SetDirty(text.rectTransform);
            }
        }
#endif

        void UpdateUI()
        {
            if (Unleashd.Instance.HasActiveSubscription())
            {
                unleashdButtonText.text = "Unleashd";
                unleashdButtonRectTransform.localScale = new Vector3(1f, 1f, unleashdButtonRectTransform.localScale.z);
            }
            else if (Unleashd.Instance.IsInGameTrialExpired())
            {
                unleashdButtonText.text = "Upgrade";
            }
            else if (Unleashd.Instance.IsInGameTrial())
            {
                UpdateTrialTimeLeft();
                unleashdButtonRectTransform.localScale = new Vector3(1f, 1f, unleashdButtonRectTransform.localScale.z);
            }
            else if (!Unleashd.Instance.HasActiveSubscription() && !Unleashd.Instance.InGameTrialAllowed())
            {
                unleashdButtonText.text = "Upgrade";
            }
            else if (!Unleashd.Instance.HasActiveSubscription() && Unleashd.Instance.InGameTrialAllowed())
            {
                unleashdButtonText.text = buttonDesign == ButtonDesign.ROUND ? "Free\nTrial" : "Free Trial";
            }
            else if (!Unleashd.Instance.HasActiveSubscription())
            {
                unleashdButtonText.text = "Upgrade";
            }
            else
            {
                unleashdButtonText.text = "";
                unleashdButtonRectTransform.localScale = new Vector3(1f, 1f, unleashdButtonRectTransform.localScale.z);
                return;
            }
        }

        private void Update()
        {
            if (!Unleashd.Instance.HasActiveSubscription() && !Unleashd.Instance.IsInGameTrial())
            {
                float scale = animationCurve.Evaluate(Time.timeSinceLevelLoad / animationTime);
                unleashdButtonRectTransform.localScale = new Vector3(scale, scale, unleashdButtonRectTransform.localScale.z);
            }
        }

        void UpdateTrialTimeLeft()
        {
            if (Unleashd.Instance.IsInGameTrial())
            {
                if (!Unleashd.Instance.HasActiveSubscription())
                {
                    unleashdButtonText.text = FormatTrialTime();
                }
                CancelInvoke("UpdateTrialTimeLeft");
                Invoke("UpdateTrialTimeLeft", 0.2f);
            }
        }

        string FormatTrialTime()
        {
            TimeSpan trialTimeSpan = Unleashd.Instance.GetTrialEndDate() - DateTime.Now;
            string trialTime = "";
            string space = buttonDesign == ButtonDesign.ROUND ? "\n" : " ";
            int trialTimeComponentCount = 0;

            if (trialTimeSpan.Days > 0 && trialTimeComponentCount < maxTrialTimeComponents)
            {
                trialTime += trialTimeSpan.Days + "d";
                trialTimeComponentCount++;
            }
            if (trialTimeSpan.Hours > 0 && trialTimeComponentCount < maxTrialTimeComponents)
            {
                trialTime += (trialTimeComponentCount >= 1 ? space : "") + trialTimeSpan.Hours + "h";
                trialTimeComponentCount++;
            }
            if (trialTimeSpan.Minutes > 0 && trialTimeComponentCount < maxTrialTimeComponents)
            {
                trialTime += (trialTimeComponentCount >= 1 ? space : "") + trialTimeSpan.Minutes + "m";
                trialTimeComponentCount++;
            }
            if ((trialTimeSpan.Seconds > 0 && trialTimeComponentCount < maxTrialTimeComponents) ||
                (trialTimeSpan.Seconds == 0 && trialTimeSpan.Minutes == 0 && trialTimeSpan.Hours == 0 && trialTimeSpan.Days == 0))
            {
                trialTime += (trialTimeComponentCount >= 1 ? space : "") + trialTimeSpan.Seconds + "s";
            }
            return trialTime;
        }


        void OpenUnleashd()
        {
            Unleashd.Instance.ShowPurchaseDialog();
        }

        public enum ButtonDesign
        {
            ROUND = 0,
            RECTANGULAR = 1,
        }

        public enum ButtonColor
        {
            GOLD = 0,
            PINK = 1,
            GREEN = 2,
            BLUE = 3
        }

        public enum ButtonTextColor
        {
            WHITE = 0,
            BLACK = 1
        }
    }
}
