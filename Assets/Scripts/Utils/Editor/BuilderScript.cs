using System.Collections;
using System.Collections.Generic;
using Unity.NetCode.Hybrid;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class BuilderScript : MonoBehaviour
{
    [MenuItem("Tools/Builder/Build Android")]
    static void BuildAndroid()
    {
        //enforcing the il2cpp backend
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        //enforcing Vulkan
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new []{GraphicsDeviceType.Vulkan});
        PlayerSettings.SetArchitecture(BuildTargetGroup.Android,1);
        AssetDatabase.SaveAssets();
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/Menu.unity","Assets/Scenes/Main.unity" };
        buildPlayerOptions.target = BuildTarget.Android;
        // SubTarget expects an integer.
        buildPlayerOptions.locationPathName = "./build/players/Megacity.apk";
      
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
    
    [MenuItem("Tools/Builder/Build Dedicated Server Windows")]
    static void BuildDedicatedServerWindows()
    {
        //enforcing the il2cpp backend
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        AssetDatabase.SaveAssets();

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/Menu.unity","Assets/Scenes/Main.unity" };
        buildPlayerOptions.target = BuildTarget.StandaloneWindows;
        // SubTarget expects an integer.
        buildPlayerOptions.subtarget = (int)StandaloneBuildSubtarget.Server;
        //this is needed for profiling 
        //buildPlayerOptions.options = BuildOptions.Development;
        buildPlayerOptions.locationPathName = "./build/Server-Win/Server.exe";

        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
    
    [MenuItem("Tools/Builder/Build Dedicated Server Linux")]
    static void BuildDedicatedServerLinux()
    {
        //enforcing the il2cpp backend
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        AssetDatabase.SaveAssets();

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/Menu.unity","Assets/Scenes/Main.unity" };
        buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
        // SubTarget expects an integer.
        buildPlayerOptions.subtarget = (int)StandaloneBuildSubtarget.Server;
        //this is needed for profiling 
        //buildPlayerOptions.options = BuildOptions.Development;
        buildPlayerOptions.locationPathName = "./build/Server/Server.x86_64";

        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
    
    [MenuItem("Tools/Builder/Build Standalone Windows")]
    static void BuildStandAloneWindows()
    {
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        NetCodeClientSettings.instance.ClientTarget = NetCodeClientTarget.Client;
        AssetDatabase.SaveAssets();
        
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/Menu.unity","Assets/Scenes/Main.unity" };
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        //this is needed for profiling 
        buildPlayerOptions.options = BuildOptions.Development | BuildOptions.ShowBuiltPlayer;
        buildPlayerOptions.locationPathName = "./build/Megacity-Win-Client/Megacity.exe";
        buildPlayerOptions.extraScriptingDefines = new[] { "NETCODE_DEBUG", "UNITY_CLIENT" };
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
}