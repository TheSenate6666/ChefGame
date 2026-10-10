using UnityEngine;

namespace ChefGame.Items
{
    /// <summary>A tomato half is still an ingredient with its own name, but can never be cut again.</summary>
    [CreateAssetMenu(fileName = "Tomato Half", menuName = "Chef Game/Items/Ingredients/Tomato Half")]
    public sealed class TomatoHalfDefinition : TomatoDefinition
    {
        public override ItemType TypeId => ItemType.TomatoHalf;
        public override bool CanBeCut => false;
    }
}
