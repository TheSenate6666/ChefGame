using UnityEngine;

namespace ChefGame.Items
{
    /// <summary>Runtime identity for one physical item; keeps instance behavior separate from shared item data.</summary>
    [DisallowMultipleComponent]
    public class ItemInstance : MonoBehaviour
    {
        [SerializeField] private ItemDefinition definition;

        public ItemDefinition Definition => definition;
        public ItemType TypeId => definition != null ? definition.TypeId : ItemType.None;
    }
}
