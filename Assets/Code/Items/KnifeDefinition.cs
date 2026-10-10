using UnityEngine;

namespace ChefGame.Items
{
    [CreateAssetMenu(fileName = "Knife", menuName = "Chef Game/Items/Utensils/Knife")]
    public sealed class KnifeDefinition : UtensilDefinition
    {
        [SerializeField] private bool cutsIngredients = true;

        public bool CutsIngredients => cutsIngredients;
    }
}
