using UnityEngine;

namespace ChefGame.Items
{
    /// <summary>Runtime state for a physical ingredient instance.</summary>
    [DisallowMultipleComponent]
    public sealed class IngredientInstance : ItemInstance
    {
        [SerializeField, HideInInspector] private bool hasBeenCut;
        private bool warnedMissingCutResults;

        public IngredientDefinition IngredientData => Definition as IngredientDefinition;
        public bool HasBeenCut => hasBeenCut;

        /// <summary>Returns this ingredient's definition name, or an empty string if no definition is assigned.</summary>
        public string GetName() => IngredientData != null ? IngredientData.GetName() : string.Empty;

        /// <returns>True only when cut results were spawned and this ingredient was scheduled for removal.</returns>
        public bool TryGetCut()
        {
            IngredientDefinition ingredientData = IngredientData;
            if (hasBeenCut || ingredientData == null || !ingredientData.CanBeCut)
                return false;

            if (!ingredientData.HasCutResults)
            {
                if (!warnedMissingCutResults)
                {
                    Debug.LogWarning($"'{name}' uses ingredient definition '{ingredientData.CustomName}', but no cut-result prefabs are configured. The ingredient was left intact.", this);
                    warnedMissingCutResults = true;
                }

                return false;
            }

            if (!ingredientData.GettingCut(gameObject))
                return false;

            hasBeenCut = true;
            return true;
        }
    }
}
