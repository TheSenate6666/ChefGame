using UnityEngine;

namespace ChefGame.Items
{
    /// <summary>Shared designer-authored data for an item type.</summary>
    public abstract class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string customName = "New Item";
        [SerializeField, TextArea] private string description;

        public string CustomName => customName;
        public string Description => description;
        public abstract ItemType TypeId { get; }
        public abstract ItemCategory Category { get; }
    }
}
