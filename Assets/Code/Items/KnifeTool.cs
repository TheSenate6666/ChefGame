using UnityEngine;

namespace ChefGame.Items
{
    /// <summary>Put this on the knife's trigger-zone object. It cuts each cuttable ingredient once on contact.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class KnifeTool : MonoBehaviour
    {
        [SerializeField] private string itemTag = "Item";

        private Collider cuttingZone;
        private KnifeDefinition definition;

        private void Awake()
        {
            cuttingZone = GetComponent<Collider>();
            if (!cuttingZone.isTrigger)
                Debug.LogError($"{nameof(KnifeTool)} on '{name}' requires its Collider to have Is Trigger enabled.", this);

            ItemInstance itemInstance = GetComponentInParent<ItemInstance>();
            definition = itemInstance != null ? itemInstance.Definition as KnifeDefinition : null;
            if (definition == null)
                Debug.LogError($"{nameof(KnifeTool)} on '{name}' needs a parent {nameof(ItemInstance)} assigned a {nameof(KnifeDefinition)} asset.", this);
        }

        private void OnTriggerEnter(Collider other) => TryCutIngredient(other);
        private void OnTriggerStay(Collider other) => TryCutIngredient(other);

        private void TryCutIngredient(Collider other)
        {
            if (definition == null || other == null)
                return;

            IngredientInstance ingredient = other.GetComponentInParent<IngredientInstance>();
            if (ingredient == null || !ingredient.CompareTag(itemTag))
                return;

            if (!definition.CanCut(ingredient.GetName()))
                return;

            ingredient.TryGetCut();
        }
    }
}
