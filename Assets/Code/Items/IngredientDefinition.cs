using System.Collections.Generic;
using UnityEngine;

namespace ChefGame.Items
{
    /// <summary>Shared data and cut behavior for all ingredient types.</summary>
    public abstract class IngredientDefinition : ItemDefinition
    {
        [Header("Ingredient")]
        [SerializeField] private bool canBeCut = true;

        [Header("Cut results")]
        [SerializeField] private GameObject cutEffectPrefab;
        [SerializeField] private List<GameObject> cutResultPrefabs = new List<GameObject>();

        public override ItemCategory Category => ItemCategory.Ingredient;
        public virtual bool CanBeCut => canBeCut;
        public GameObject CutEffectPrefab => cutEffectPrefab;
        public IReadOnlyList<GameObject> CutResultPrefabs => cutResultPrefabs;

        /// <summary>Returns the designer-authored ingredient name for systems such as stove recipes.</summary>
        public string GetName() => CustomName;

        public bool HasCutResults
        {
            get
            {
                if (cutResultPrefabs == null)
                    return false;

                for (int i = 0; i < cutResultPrefabs.Count; i++)
                {
                    if (cutResultPrefabs[i] != null)
                        return true;
                }

                return false;
            }
        }

        /// <summary>Spawns the configured cut effect and result prefabs, then removes the ingredient instance.</summary>
        public bool GettingCut(GameObject ingredientInstance)
        {
            return GettingCut(ingredientInstance, cutResultPrefabs);
        }

        /// <summary>As above, with an optional per-call result list; null uses this asset's configured list.</summary>
        public bool GettingCut(GameObject ingredientInstance, IReadOnlyList<GameObject> prefabsToSpawn)
        {
            if (!canBeCut || ingredientInstance == null)
                return false;

            IReadOnlyList<GameObject> results = prefabsToSpawn ?? cutResultPrefabs;
            bool hasValidResult = false;
            for (int i = 0; results != null && i < results.Count; i++)
            {
                if (results[i] != null)
                {
                    hasValidResult = true;
                    break;
                }
            }

            // Keep the original intact until cut-result prefabs are configured.
            if (!hasValidResult)
                return false;

            Vector3 position = ingredientInstance.transform.position;
            Quaternion rotation = ingredientInstance.transform.rotation;

            if (cutEffectPrefab != null)
                Instantiate(cutEffectPrefab, position, rotation);

            if (results != null)
            {
                for (int i = 0; i < results.Count; i++)
                {
                    if (results[i] != null)
                        Instantiate(results[i], position, rotation);
                }
            }

            Destroy(ingredientInstance);
            return true;
        }
    }
}
