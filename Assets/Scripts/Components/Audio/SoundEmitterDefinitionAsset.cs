using System;
using Unity.Entities;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Megacity.Audio
{
    /// <summary>
    /// Defines the ECSound emitter type and parameters
    /// such as distance, volume, and probability
    /// </summary>

    public class SoundEmitterDefinitionAsset : ScriptableObject
    {
        public ECSoundEmitterDefinition data;

        [NonSerialized]
        private Entity definitionEntity;

#if UNITY_EDITOR
        [MenuItem("Assets/Create/Sound Emitter Definition Asset")]
        public static void CreateAsset()
        {
            var asset = CreateInstance<SoundEmitterDefinitionAsset>();

            asset.data.volume = 0.5f;
            asset.data.maxDist = 100.0f;

            AssetDatabase.CreateAsset(asset, "Assets/SoundEmitterDefinition.asset");
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();

            Selection.activeObject = asset;
        }
#endif
    }
}
