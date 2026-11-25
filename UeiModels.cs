using System.Collections.Generic;
using UnityEngine;

internal enum UeiEntryKind
{
    Item,
    Liquid,
    Entity,
}

internal enum UeiRecipeRelation
{
    ProducedBy,
    UsedIn,
}

internal sealed class UeiEntry
{
    public UeiEntryKind Kind;
    public string Id = string.Empty;
    public string EntryKey = string.Empty;
    public string DisplayName = string.Empty;
    public string Description = string.Empty;
    public string AcquisitionAnnotation = string.Empty;
    public string AcquisitionSupplement = string.Empty;
    public string AcquisitionText = string.Empty;
    public string CategoryKey = string.Empty;
    public string CategoryLabel = string.Empty;
    public string SearchText = string.Empty;
    public Sprite? Icon;
    public Color IconColor = Color.white;
    public ItemInfo? ItemInfo;
    public LiquidType? LiquidInfo;
    public readonly List<string> Tags = new();
    public readonly List<CraftingQuality> Qualities = new();

    public bool IsFavorite => UeiFavorites.Contains(EntryKey);
}

internal sealed class UeiRecipeLink
{
    public int RecipeIndex;
    public UeiRecipeRelation Relation;
    public bool Visible;
    public bool HasMadeBefore;

    public Recipe? Recipe
    {
        get
        {
            if (Recipes.recipes == null || RecipeIndex < 0 || RecipeIndex >= Recipes.recipes.Count)
            {
                return null;
            }

            return Recipes.recipes[RecipeIndex];
        }
    }
}
