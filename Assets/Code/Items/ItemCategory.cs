namespace ChefGame.Items
{
    /// <summary>Broad gameplay category used as an item-data equivalent of a tag.</summary>
    public enum ItemCategory
    {
        Ingredient,
        Utensil
    }

    /// <summary>Stable gameplay identity for a specific item definition.</summary>
    public enum ItemType
    {
        None = 0,
        Tomato = 1,
        TomatoHalf = 2,
        Knife = 3
    }
}
