using Multiscription.Unleashd;
using UnityEngine;
using UnityEngine.UI;

public class SampleIntegrationCode : MonoBehaviour
{
    [SerializeField]
    private Button unleashdButton;

    private Text unleashdButtonText;

    void Awake()
    {
        unleashdButtonText = unleashdButton.GetComponentInChildren<Text>();

        if (Unleashd.IsSupported())
        {
            unleashdButton.onClick.AddListener(OnButtonClickHandler);
            Unleashd.Instance.OnReady += UpdateUI;
            Unleashd.Instance.OnStateChanged += UpdateUI;
            if (!Unleashd.Instance.IsInitialized())
            {
                Unleashd.Instance.Init();
            }
        }

        UpdateUI();
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

    private void UpdateUI()
    {
        if (!Unleashd.IsSupported())
        {
            unleashdButtonText.text = "Platform not supported";
            unleashdButtonText.color = Color.red;
            unleashdButton.enabled = false;
            return;
        }

        if (!Unleashd.Instance.IsInitialized())
        {
            unleashdButtonText.text = "Plugin not initialized";
            unleashdButtonText.color = Color.red;
            unleashdButton.enabled = false;
            return;
        }

        if (!Unleashd.Instance.IsReady())
        {
            unleashdButtonText.text = "Plugin not ready";
            unleashdButtonText.color = Color.red;
            unleashdButton.enabled = false;
            return;
        }

        if (Unleashd.Instance.HasSubscriptionBenefits())
        {
            unleashdButtonText.text = "Plugin ready\nSubscription active";
            unleashdButtonText.color = new Color(0f, 0.7f, 0f);
            unleashdButton.enabled = true;
        }
        else
        {
            unleashdButtonText.text = "Plugin ready\nSubscription not active";
            unleashdButtonText.color = Color.black;
            unleashdButton.enabled = true;
        }
    }

    private void OnButtonClickHandler()
    {
        Unleashd.Instance.ShowPurchaseDialog();
    }
}
