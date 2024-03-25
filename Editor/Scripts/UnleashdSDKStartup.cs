namespace Multiscription.Unleashd
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    [InitializeOnLoad]
    public class UnleashdSDKStartup
    {
        static UnleashdSDKStartup()
        {
            EditorApplication.update += CheckProjectSettings;
        }

        static void CheckProjectSettings()
        {
            EditorApplication.update -= CheckProjectSettings;
            if (!SessionState.GetBool("UnleashdSDKStartupDone", false))
            {
                UnleashdConfig config = Resources.Load<UnleashdConfig>("Unleashd/UnleashdConfig");
                if (config == null)
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    {
                        AssetDatabase.CreateFolder("Assets", "Resources");
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                    if (!AssetDatabase.IsValidFolder("Assets/Resources/Unleashd"))
                    {
                        AssetDatabase.CreateFolder("Assets/Resources", "Unleashd");
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                    AssetDatabase.CreateAsset(ScriptableObject.CreateInstance(typeof(UnleashdConfig)), "Assets/Resources/Unleashd/UnleashdConfig.asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Debug.LogWarning("Resources/Unleashd/UnleashdConfig.asset created");
                }

                SessionState.SetBool("UnleashdSDKStartupDone", true);
            }
        }
    }
}
