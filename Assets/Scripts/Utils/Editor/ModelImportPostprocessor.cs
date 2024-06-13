using UnityEditor;

namespace Unity.Megacity.EditorTools
{
    public class ModelImportPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            var importer = assetImporter as ModelImporter;
            importer.isReadable = false;
        }
    }
}