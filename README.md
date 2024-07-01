# Unity SDK Integration
Visit the [Unleashd Developer Portal](https://developer.unleashd.com/my/sdk/docs/latest/android) for more detailed instructions.

## Support
We support Unity 2021.3 LTS, Unity 2022.3 LTS and Unity 2023.

We support Android version 5.0 (API level 21) and up.

## Step 1 : Install External Dependency Manager for Unity
We are using External Dependency Manager for Unity (EDM4U) to manage native Android dependencies. If you already are using EDM4U, then you don't need to install it again and you can go directly to **Step 2** below.
If you're not using External Dependency Manager for Unity already, you can download it [here](https://developers.google.com/unity/archive#external_dependency_manager_for_unity).

Install by going to **Assets** -> **Import Package** -> **Custom Package** in Unity Editor.
It's recommended at this step to verify that your game builds and runs after installing EDM4U.

## Step 2: Verify External Dependency Manager for Unity settings
Open **Assets** -> **External Dependency Manager** -> **Android Resolver** -> **Settings** in Unity Editor.

Verify that **Use Jetifier** is **enabled**.

## Step 3: Install Unleashd SDK
There are two methods of installing the Unleashd SDK:
- The first method is by utilizing Unity Package Manager. Proceed to **step 3a**.
- The second method is a manual installation. Proceed to **step 3b**.

## Step 3a: Unity Package Manager installation
Copy the following git repository link: https://github.com/multiscription/unleashd-unity.git

Install by going to **Window** -> **Package Manager** -> Add sign -> **Add package from GIT Url...** -> Insert GIT repository link -> **Add** in Unity Editor.

## Step 3b: Manual installation
Download the SDK from the [Unleashd Developer Portal](https://developer.unleashd.com/my/sdk/latest).

Install by going to **Assets** -> **Import Package** -> **Custom Package** in Unity Editor.


## Step 4: Configure SDK
The SDK is configured in a Scriptable Object called UnleashdConfig which you can find in the **Assets/Resources/Unleashd/**.
This Scriptable Object will be automatically created after the Unleashd SDK is successfully imported into your game.

When you click to edit it you can configure:
- The SDK color theme
- In-game trial duration
- The SDK game for your project

The default trial duration is set to 3 minutes. This short duration is for testing purposes. When the integration is fully completed (after our final approval), the trial duration should be set to a higher duration as agreed with us.

Your **custom SDK key** can be found on the [Unleashd Developer Portal](https://developer.unleashd.com/projects) in your game project and is used to connect the SDK to our back-end. Copy this key and paste it into the scriptable object **UnleashdConfig** Android SDK Key field.

## Step 5: Integrate Unleashd SDK into game
There are two methods to start the integration with the Unleashd SDK. First method is to use the UnleashdButton prefab, which is what we recommend. The second method is to do the entire integration from your C# code. For more information about SDK integration visit the [Unleashd Developer Portal](https://developer.unleashd.com/my/sdk/docs/latest/android).

## Step 6: Finalization
Ensure that Android dependencies are properly updated by going to:

 **Assets** -> **External Dependency Manager** -> **Android Resolver** -> **Force Resolve** in Unity Editor.

## Import Samples
If you used Unity Package Manager SDK installation:

Import samples by going to **Window** -> **Package Manager** -> **Unleashd** -> **Samples** -> **Import**

They will appear in the folder **Assets/Samples/Unleashd/SDK_Version/Samples/** in your Unity project.

If you used manual SDK installation:

You can find examples of both types of integration in the folder **Assets/Unleashd/Examples/** in your Unity project.

## Update Unleashd SDK
Receive new updates for Unleashd SDK by updating it in the package manager to get the latest version.

Update by going to **Window** -> **Package Manager** -> **Unleashd** -> **Update**

## Trouble Shooting
If you encounter problems building or running your Unity game with the Unleashd SDK, here's a list of things to check:

**Utilize IL2CPP:** Check under **Edit** -> **Project Settings** -> **Player** -> **Other Settings** that Scripting Backend is set to IL2CPP.

**Resolve dependencies:** Ensure that Android dependencies are properly resolved by going to **Assets** -> **External Dependency Manager** -> **Android Resolver** -> **Force Resolve** in Unity Editor. This step must finish without errors.

**Callbacks:** When subscribing to the Unleashd C# callbacks, ensure you use **OnStateChanged += MyCallbackHandler** instead of **OnStateChanged = MyCallbackHandler**. Same goes for the OnReady callback.

**Restart Unity:** When installing and updating native plugins, restarting Unity may be needed occasionally.

**Project State:** If your game builds and runs fine, but the Unleashd popup fails to open, please ensure project state is changed from **DRAFT** to **DEVELOPMENT** in the [Unleashd Developer Portal](https://developer.test.unleashd.com).