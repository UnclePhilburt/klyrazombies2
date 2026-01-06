namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem.Editor
{
    using Opsive.Shared.Editor.Utility;
    using UnityEngine.UIElements;

    public static class CharacterInventoryStyles
    {
        public static StyleSheet StyleSheet =>
            EditorUtility.LoadAsset<StyleSheet>("21fb5aa2f5b9ab944a492c3ff83de1c0");

        public static readonly string s_StatesReorderableList_Header = "states-reorderable-list__header";
        public static readonly string s_StatesReorderableListElement = "states-reorderable-list-element";
    }
}