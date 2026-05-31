using OntologicalStudio.Core.Models;
using OntologicalStudio.Localization.Services;

namespace OntologicalStudio.Desktop.Services;

public static class TypeLocalizationHelper
{
    public static EntityType Localize(EntityType entityType, ILocalizationService localization)
    {
        entityType.LocalizedName = localization.T(GetEntityTypeKey(entityType.Name));
        return entityType;
    }

    public static RelationshipType Localize(RelationshipType relationshipType, ILocalizationService localization)
    {
        relationshipType.LocalizedName = localization.T(GetRelationshipTypeKey(relationshipType.Name));
        return relationshipType;
    }

    public static string LocalizeEntityTypeName(string name, ILocalizationService localization)
        => localization.T(GetEntityTypeKey(name));

    public static string LocalizeRelationshipTypeName(string name, ILocalizationService localization)
        => localization.T(GetRelationshipTypeKey(name));

    public static bool MatchesRelationshipTypeInput(RelationshipType relationshipType, string? input, ILocalizationService localization)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var trimmed = input.Trim();
        return string.Equals(relationshipType.Name, trimmed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(LocalizeRelationshipTypeName(relationshipType.Name, localization), trimmed, StringComparison.OrdinalIgnoreCase);
    }

    public static bool MatchesEntityTypeInput(EntityType entityType, string? input, ILocalizationService localization)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var trimmed = input.Trim();
        return string.Equals(entityType.Name, trimmed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(LocalizeEntityTypeName(entityType.Name, localization), trimmed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(entityType.DisplayName, trimmed, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetEntityTypeKey(string name) => name switch
    {
        "Person" => "entityType.person",
        "CEO" => "entityType.ceo",
        "Founder" => "entityType.founder",
        "Manager" => "entityType.manager",
        "Team" => "entityType.team",
        "Competitor" => "entityType.competitor",
        "Customer" => "entityType.customer",
        "Stakeholder" => "entityType.stakeholder",
        "Belief" => "entityType.belief",
        "Fear" => "entityType.fear",
        "Goal" => "entityType.goal",
        "Habit" => "entityType.habit",
        "Trigger" => "entityType.trigger",
        "Conflict" => "entityType.conflict",
        _ => name
    };

    private static string GetRelationshipTypeKey(string name) => name switch
    {
        "influences" => "relationshipType.influences",
        "dependsOn" => "relationshipType.dependsOn",
        "resists" => "relationshipType.resists",
        "supports" => "relationshipType.supports",
        "contradicts" => "relationshipType.contradicts",
        "triggers" => "relationshipType.triggers",
        _ => name
    };
}