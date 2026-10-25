using UnityEngine;

namespace Mundo3D.Inventario {
    public enum InventoryItemCategory {
        AccessCard,
        CharacterCV,
        DeveloperCard,
        OfficeNote,
        Consumable,
        Evidence,
        Certificate,
        Other
    }

    [CreateAssetMenu(
        fileName = "NewInventoryItem",
        menuName = "Mundo 3D/Inventario/Definicion de objeto"
    )]
    public sealed class InventoryItemDefinition : ScriptableObject {
        [Header("Identificación")]
        [SerializeField]
        private string id;

        [SerializeField]
        private string displayName;

        [SerializeField]
        private InventoryItemCategory category;

        [Header("Descripción")]
        [TextArea(3, 6)]
        [SerializeField]
        private string description;

        [Header("Información ampliada")]
        [SerializeField, TextArea(5, 12)]
        private string detailedDescription;

        [Header("Interfaz")]
        [SerializeField]
        private Sprite icon;

        public string Id => id;
        public string DisplayName => displayName;
        public InventoryItemCategory Category => category;
        public string Description => description;

        public string DetailedDescription => detailedDescription;
        public Sprite Icon => icon;

#if UNITY_EDITOR
        private void OnValidate() {
            if (id != null)
                id = id.Trim();
        }
#endif
    }
}
