using UnityEngine;

namespace ChefGame.Items
{
    /// <summary>Concrete tomato item type; cutting behavior and editable cut outputs come from IngredientDefinition.</summary>
    [CreateAssetMenu(fileName = "Tomato", menuName = "Chef Game/Items/Ingredients/Tomato")]
    public class TomatoDefinition : IngredientDefinition
    {
        public override ItemType TypeId => ItemType.Tomato;
    }
}
