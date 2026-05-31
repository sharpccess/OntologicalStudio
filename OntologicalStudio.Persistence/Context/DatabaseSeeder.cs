using Microsoft.EntityFrameworkCore;
using OntologicalStudio.Core.Models;
using System.Text.Json;

namespace OntologicalStudio.Persistence.Context;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Apply migrations automatically
        await context.Database.MigrateAsync();

        var requiredEntityTypes = new List<EntityType>
        {
            new() { Name = "Person", Description = "Individual person - supports personal, family, team and stakeholder modelling", IsDefaultTemplate = true, SuggestedHydrationFields = JsonSerializer.Serialize(new[] { "personality", "motivations", "fears", "values", "communicationStyle", "relationships" }) },
            new() { Name = "CEO", Description = "Chief Executive Officer - Executive leadership role", IsDefaultTemplate = true, SuggestedHydrationFields = JsonSerializer.Serialize(new[] { "authorityStyle", "keyIncentives", "stressTriggers" }) },
            new() { Name = "Founder", Description = "Company Founder - Often represents historical values and direct authority", IsDefaultTemplate = true, SuggestedHydrationFields = JsonSerializer.Serialize(new[] { "attachmentLevel", "strategicVision", "blindSpots" }) },
            new() { Name = "Manager", Description = "Middle Manager - Coordinates teams and translates strategies to operations", IsDefaultTemplate = true, SuggestedHydrationFields = JsonSerializer.Serialize(new[] { "spanOfControl", "incentiveAlignment" }) },
            new() { Name = "Team", Description = "Operational Unit / Team - Executes operational tasks", IsDefaultTemplate = true, SuggestedHydrationFields = JsonSerializer.Serialize(new[] { "moraleLevel", "skillGaps", "turnoverRisk" }) },
            new() { Name = "Competitor", Description = "External competitor - Market player affecting strategy", IsDefaultTemplate = true, SuggestedHydrationFields = JsonSerializer.Serialize(new[] { "marketShare", "competitiveAdvantage" }) },
            new() { Name = "Customer", Description = "Target audience or consumer - Source of demand", IsDefaultTemplate = true, SuggestedHydrationFields = JsonSerializer.Serialize(new[] { "satisfactionLevel", "retentionRate" }) },
            new() { Name = "Stakeholder", Description = "Shareholder or board member - General interested party", IsDefaultTemplate = true, SuggestedHydrationFields = JsonSerializer.Serialize(new[] { "equityPercentage", "influenceLevel" }) },
            new() { Name = "Belief", Description = "Core Belief - Internalized conviction guiding behavior", IsDefaultTemplate = true, SuggestedHydrationFields = JsonSerializer.Serialize(new[] { "origin", "rigidity", "impactOnDecisions" }) },
            new() { Name = "Fear", Description = "Fear / Blocker - Emotional obstacle preventing action", IsDefaultTemplate = true, SuggestedHydrationFields = JsonSerializer.Serialize(new[] { "underlyingThreat", "copingMechanism" }) },
            new() { Name = "Goal", Description = "Strategic or Personal Goal - Desired future state", IsDefaultTemplate = true, SuggestedHydrationFields = JsonSerializer.Serialize(new[] { "targetDate", "priority", "motivationSource" }) },
            new() { Name = "Habit", Description = "Behavioral Habit - Repeated automated action pattern", IsDefaultTemplate = true, SuggestedHydrationFields = JsonSerializer.Serialize(new[] { "cue", "routine", "reward", "frequency" }) },
            new() { Name = "Trigger", Description = "Emotional Trigger - Internal or external event triggering specific reactions", IsDefaultTemplate = true, SuggestedHydrationFields = JsonSerializer.Serialize(new[] { "stimulusType", "immediateReaction" }) },
            new() { Name = "Conflict", Description = "Internal Conflict - Contradictory desires or dualities", IsDefaultTemplate = true, SuggestedHydrationFields = JsonSerializer.Serialize(new[] { "competingDesires", "tensionLevel" }) }
        };

        var existingTypeNames = await context.EntityTypes
            .Select(x => x.Name)
            .ToListAsync();

        var missingTypes = requiredEntityTypes
            .Where(x => !existingTypeNames.Contains(x.Name))
            .ToList();

        if (missingTypes.Count > 0)
        {
            await context.EntityTypes.AddRangeAsync(missingTypes);
        }

        // Seed RelationshipTypes if empty
        if (!await context.RelationshipTypes.AnyAsync())
        {
            var relationshipTypes = new List<RelationshipType>
            {
                new() { Name = "influences", Description = "The source entity affects or guides the behavior/state of the target", Bidirectional = false },
                new() { Name = "dependsOn", Description = "The source entity requires the target entity to function properly", Bidirectional = false },
                new() { Name = "resists", Description = "The source entity opposes, limits, or slows down the target entity", Bidirectional = false },
                new() { Name = "supports", Description = "The source entity strengthens or facilitates the target entity", Bidirectional = false },
                new() { Name = "contradicts", Description = "A tension or logical conflict exists between the two entities", Bidirectional = true },
                new() { Name = "triggers", Description = "The source entity directly causes the occurrence or activation of the target", Bidirectional = false }
            };

            await context.RelationshipTypes.AddRangeAsync(relationshipTypes);
        }

        await context.SaveChangesAsync();
    }
}
