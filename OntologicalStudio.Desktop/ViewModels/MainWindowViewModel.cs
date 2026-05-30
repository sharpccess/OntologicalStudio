using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Application.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace OntologicalStudio.Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IUniverseService _universeService;
    private readonly IEntityService _entityService;
    private readonly IRelationshipService _relationshipService;
    private readonly IScenarioService _scenarioService;
    private readonly IAIHydrationService _hydrationService;
    private readonly IReportService _reportService;
    private readonly IEntityTypeRepository _entityTypeRepository;
    private readonly IRelationshipTypeRepository _relationshipTypeRepository;

    private ObservableCollection<Universe> _universes = new();
    private Universe? _selectedUniverse;

    private ObservableCollection<EntityViewModel> _entities = new();
    private ObservableCollection<RelationshipViewModel> _relationships = new();
    private ObservableCollection<ScenarioViewModel> _scenarios = new();

    private List<EntityType> _entityTypes = new();
    private List<RelationshipType> _relationshipTypes = new();

    private object? _selectedObject;
    private EntityType? _selectedEntityTypeForNewEntity;
    private RelationshipType? _selectedRelationshipTypeForNewRelationship;
    private EntityViewModel? _relationshipSource;
    private EntityViewModel? _relationshipTarget;

    private string _newUniverseName = string.Empty;
    private string _newUniverseDescription = string.Empty;
    
    private string _aiResultText = string.Empty;
    private bool _isAiBusy;

    public ObservableCollection<Universe> Universes
    {
        get => _universes;
        set => this.RaiseAndSetIfChanged(ref _universes, value);
    }

    public Universe? SelectedUniverse
    {
        get => _selectedUniverse;
        set
        {
            if (this.RaiseAndSetIfChanged(ref _selectedUniverse, value))
            {
                _ = LoadUniverseDetailsAsync(value);
            }
        }
    }

    public ObservableCollection<EntityViewModel> Entities
    {
        get => _entities;
        set => this.RaiseAndSetIfChanged(ref _entities, value);
    }

    public ObservableCollection<RelationshipViewModel> Relationships
    {
        get => _relationships;
        set => this.RaiseAndSetIfChanged(ref _relationships, value);
    }

    public ObservableCollection<ScenarioViewModel> Scenarios
    {
        get => _scenarios;
        set => this.RaiseAndSetIfChanged(ref _scenarios, value);
    }

    public List<EntityType> EntityTypes
    {
        get => _entityTypes;
        set => this.RaiseAndSetIfChanged(ref _entityTypes, value);
    }

    public List<RelationshipType> RelationshipTypes
    {
        get => _relationshipTypes;
        set => this.RaiseAndSetIfChanged(ref _relationshipTypes, value);
    }

    public object? SelectedObject
    {
        get => _selectedObject;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedObject, value);
            this.RaisePropertyChanged(nameof(SelectedEntity));
            this.RaisePropertyChanged(nameof(SelectedRelationship));
            this.RaisePropertyChanged(nameof(SelectedScenario));
        }
    }

    public EntityViewModel? SelectedEntity => SelectedObject as EntityViewModel;
    public RelationshipViewModel? SelectedRelationship => SelectedObject as RelationshipViewModel;
    public ScenarioViewModel? SelectedScenario => SelectedObject as ScenarioViewModel;

    public EntityType? SelectedEntityTypeForNewEntity
    {
        get => _selectedEntityTypeForNewEntity;
        set => this.RaiseAndSetIfChanged(ref _selectedEntityTypeForNewEntity, value);
    }

    public RelationshipType? SelectedRelationshipTypeForNewRelationship
    {
        get => _selectedRelationshipTypeForNewRelationship;
        set => this.RaiseAndSetIfChanged(ref _selectedRelationshipTypeForNewRelationship, value);
    }

    public EntityViewModel? RelationshipSource
    {
        get => _relationshipSource;
        set => this.RaiseAndSetIfChanged(ref _relationshipSource, value);
    }

    public EntityViewModel? RelationshipTarget
    {
        get => _relationshipTarget;
        set => this.RaiseAndSetIfChanged(ref _relationshipTarget, value);
    }

    public string NewUniverseName
    {
        get => _newUniverseName;
        set => this.RaiseAndSetIfChanged(ref _newUniverseName, value);
    }

    public string NewUniverseDescription
    {
        get => _newUniverseDescription;
        set => this.RaiseAndSetIfChanged(ref _newUniverseDescription, value);
    }

    public string AiResultText
    {
        get => _aiResultText;
        set => this.RaiseAndSetIfChanged(ref _aiResultText, value);
    }

    public bool IsAiBusy
    {
        get => _isAiBusy;
        set => this.RaiseAndSetIfChanged(ref _isAiBusy, value);
    }

    // Commands
    public ReactiveCommand<Unit, Unit> CreateUniverseCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteUniverseCommand { get; }
    public ReactiveCommand<Unit, Unit> AddEntityCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteEntityCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateRelationshipCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteRelationshipCommand { get; }
    public ReactiveCommand<Unit, Unit> AddScenarioCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteScenarioCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveSelectedObjectCommand { get; }
    public ReactiveCommand<Unit, Unit> HydrateEntityCommand { get; }
    public ReactiveCommand<Unit, Unit> RunReasoningCommand { get; }
    public ReactiveCommand<string, Unit> ExportPdfReportCommand { get; }

    public MainWindowViewModel(
        IUniverseService universeService,
        IEntityService entityService,
        IRelationshipService relationshipService,
        IScenarioService scenarioService,
        IAIHydrationService hydrationService,
        IReportService reportService,
        IEntityTypeRepository entityTypeRepository,
        IRelationshipTypeRepository relationshipTypeRepository)
    {
        _universeService = universeService;
        _entityService = entityService;
        _relationshipService = relationshipService;
        _scenarioService = scenarioService;
        _hydrationService = hydrationService;
        _reportService = reportService;
        _entityTypeRepository = entityTypeRepository;
        _relationshipTypeRepository = relationshipTypeRepository;

        // Initialize commands
        CreateUniverseCommand = ReactiveCommand.CreateFromTask(CreateUniverseAsync);
        DeleteUniverseCommand = ReactiveCommand.CreateFromTask(DeleteUniverseAsync);
        AddEntityCommand = ReactiveCommand.CreateFromTask(AddEntityAsync);
        DeleteEntityCommand = ReactiveCommand.CreateFromTask(DeleteEntityAsync);
        CreateRelationshipCommand = ReactiveCommand.CreateFromTask(CreateRelationshipAsync);
        DeleteRelationshipCommand = ReactiveCommand.CreateFromTask(DeleteRelationshipAsync);
        AddScenarioCommand = ReactiveCommand.CreateFromTask(AddScenarioAsync);
        DeleteScenarioCommand = ReactiveCommand.CreateFromTask(DeleteScenarioAsync);
        SaveSelectedObjectCommand = ReactiveCommand.CreateFromTask(SaveSelectedObjectAsync);
        HydrateEntityCommand = ReactiveCommand.CreateFromTask(HydrateEntityAsync);
        RunReasoningCommand = ReactiveCommand.CreateFromTask(RunReasoningAsync);
        ExportPdfReportCommand = ReactiveCommand.CreateFromTask<string>(ExportPdfReportAsync);

        _ = LoadInitialDataAsync();
    }

    private async Task LoadInitialDataAsync()
    {
        try
        {
            var universesList = await _universeService.GetAllAsync();
            Universes = new ObservableCollection<Universe>(universesList);

            var types = await _entityTypeRepository.GetAllAsync();
            EntityTypes = types.ToList();
            SelectedEntityTypeForNewEntity = EntityTypes.FirstOrDefault();

            var relTypes = await _relationshipTypeRepository.GetAllAsync();
            RelationshipTypes = relTypes.ToList();
            SelectedRelationshipTypeForNewRelationship = RelationshipTypes.FirstOrDefault();

            if (Universes.Any())
            {
                SelectedUniverse = Universes.First();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading initial data: {ex.Message}");
        }
    }

    private async Task LoadUniverseDetailsAsync(Universe? universe)
    {
        Entities.Clear();
        Relationships.Clear();
        Scenarios.Clear();
        SelectedObject = null;

        if (universe == null) return;

        try
        {
            // Load Entities
            var domainEntities = await _entityService.GetByUniverseAsync(universe.Id);
            var entityVms = domainEntities.Select(e => new EntityViewModel(e)).ToList();
            Entities = new ObservableCollection<EntityViewModel>(entityVms);

            // Load Scenarios
            var domainScenarios = await _scenarioService.GetByUniverseAsync(universe.Id);
            var scenarioVms = domainScenarios.Select(s => new ScenarioViewModel(s)).ToList();
            Scenarios = new ObservableCollection<ScenarioViewModel>(scenarioVms);

            // Load Relationships (need to map source and target to the generated VMs)
            var relVms = new List<RelationshipViewModel>();
            foreach (var entityVm in Entities)
            {
                var sourceRelationships = await _relationshipService.GetBySourceEntityAsync(entityVm.Id);
                foreach (var rel in sourceRelationships)
                {
                    if (relVms.Any(r => r.Id == rel.Id)) continue;

                    var targetVm = Entities.FirstOrDefault(e => e.Id == rel.TargetEntityId);
                    if (targetVm != null)
                    {
                        relVms.Add(new RelationshipViewModel(rel, entityVm, targetVm));
                    }
                }
            }
            Relationships = new ObservableCollection<RelationshipViewModel>(relVms);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading universe details: {ex.Message}");
        }
    }

    private async Task CreateUniverseAsync()
    {
        if (string.IsNullOrWhiteSpace(NewUniverseName)) return;

        var universe = await _universeService.CreateAsync(NewUniverseName, NewUniverseDescription);
        Universes.Add(universe);
        SelectedUniverse = universe;

        NewUniverseName = string.Empty;
        NewUniverseDescription = string.Empty;
    }

    private async Task DeleteUniverseAsync()
    {
        if (SelectedUniverse == null) return;

        await _universeService.DeleteAsync(SelectedUniverse.Id);
        Universes.Remove(SelectedUniverse);
        SelectedUniverse = Universes.FirstOrDefault();
    }

    private async Task AddEntityAsync()
    {
        if (SelectedUniverse == null || SelectedEntityTypeForNewEntity == null) return;

        try
        {
            // Create in DB
            var entity = await _entityService.CreateAsync(
                $"{SelectedEntityTypeForNewEntity.Name} Node", 
                "Description", 
                SelectedEntityTypeForNewEntity.Id, 
                SelectedUniverse.Id
            );

            // Default position on canvas
            entity.PositionX = 150 + (Entities.Count * 20) % 300;
            entity.PositionY = 150 + (Entities.Count * 25) % 200;
            
            await _entityService.UpdateAsync(entity);

            var vm = new EntityViewModel(entity);
            Entities.Add(vm);
            SelectedObject = vm;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding entity: {ex.Message}");
        }
    }

    private async Task DeleteEntityAsync()
    {
        if (SelectedEntity == null) return;

        try
        {
            var entityId = SelectedEntity.Id;

            // Remove associated relationship VMs
            var associatedRels = Relationships.Where(r => r.Source.Id == entityId || r.Target.Id == entityId).ToList();
            foreach (var rel in associatedRels)
            {
                Relationships.Remove(rel);
            }

            await _entityService.DeleteAsync(entityId);
            Entities.Remove(SelectedEntity);
            SelectedObject = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting entity: {ex.Message}");
        }
    }

    private async Task CreateRelationshipAsync()
    {
        if (RelationshipSource == null || RelationshipTarget == null || SelectedRelationshipTypeForNewRelationship == null) return;

        try
        {
            var rel = await _relationshipService.CreateAsync(
                RelationshipSource.Id, 
                RelationshipTarget.Id, 
                SelectedRelationshipTypeForNewRelationship.Id
            );

            // Seed relationship type details
            rel.RelationshipType = SelectedRelationshipTypeForNewRelationship;

            var vm = new RelationshipViewModel(rel, RelationshipSource, RelationshipTarget);
            Relationships.Add(vm);
            SelectedObject = vm;

            RelationshipSource = null;
            RelationshipTarget = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating relationship: {ex.Message}");
        }
    }

    private async Task DeleteRelationshipAsync()
    {
        if (SelectedRelationship == null) return;

        try
        {
            await _relationshipService.DeleteAsync(SelectedRelationship.Id);
            Relationships.Remove(SelectedRelationship);
            SelectedObject = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting relationship: {ex.Message}");
        }
    }

    private async Task AddScenarioAsync()
    {
        if (SelectedUniverse == null) return;

        try
        {
            var scenario = await _scenarioService.CreateAsync("New Scenario / Problem", "Describe the situation and challenges here", SelectedUniverse.Id);
            
            // Add all current entities to this scenario by default
            foreach (var entity in Entities)
            {
                await _scenarioService.AddEntityToScenarioAsync(scenario.Id, entity.Id, "Stakeholder");
            }

            var vm = new ScenarioViewModel(scenario);
            Scenarios.Add(vm);
            SelectedObject = vm;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding scenario: {ex.Message}");
        }
    }

    private async Task DeleteScenarioAsync()
    {
        if (SelectedScenario == null) return;

        try
        {
            await _scenarioService.DeleteAsync(SelectedScenario.Id);
            Scenarios.Remove(SelectedScenario);
            SelectedObject = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting scenario: {ex.Message}");
        }
    }

    public async Task SaveSelectedObjectAsync()
    {
        if (SelectedObject == null) return;

        try
        {
            if (SelectedEntity != null)
            {
                await _entityService.UpdateAsync(SelectedEntity.Model);
            }
            else if (SelectedRelationship != null)
            {
                await _relationshipService.UpdateAsync(SelectedRelationship.Model);
            }
            else if (SelectedScenario != null)
            {
                await _scenarioService.UpdateAsync(SelectedScenario.Model);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving changes: {ex.Message}");
        }
    }

    private async Task HydrateEntityAsync()
    {
        if (SelectedEntity == null) return;

        IsAiBusy = true;
        AiResultText = "Synthesizing entity dimensions...";

        try
        {
            var options = new HydrationOptions
            {
                AutoApprove = false,
                Temperature = 0.7f,
                DetailLevel = 3
            };

            var result = await _hydrationService.HydrateEntityAsync(SelectedEntity.Id, options);
            
            // Show suggestions to the user
            AiResultText = $"### AI Suggestions for {SelectedEntity.Name}\n\n" +
                           $"**Confidence Score:** {result.ConfidenceScore}/100\n" +
                           $"**Completeness Score:** {result.CompletenessScore}/100\n\n" +
                           $"**Suggested Attributes:**\n{result.SuggestedPropertiesJson}\n\n" +
                           $"**Suggested Notes:**\n{result.AnalysisNotes}\n\n" +
                           $"_Review changes and save to apply._";

            // Map suggestions back to selected entity for editing
            SelectedEntity.ConfidenceLevel = result.ConfidenceScore;
            SelectedEntity.CompletenessScore = result.CompletenessScore;
            
            if (!string.IsNullOrWhiteSpace(result.AnalysisNotes))
            {
                SelectedEntity.Notes = string.IsNullOrEmpty(SelectedEntity.Notes) 
                    ? result.AnalysisNotes 
                    : $"{SelectedEntity.Notes}\n\n-- AI SUGGESTION --\n{result.AnalysisNotes}";
            }

            if (!string.IsNullOrWhiteSpace(result.SuggestedPropertiesJson))
            {
                SelectedEntity.Model.Properties = result.SuggestedPropertiesJson;
            }

            await _entityService.UpdateAsync(SelectedEntity.Model);
        }
        catch (Exception ex)
        {
            AiResultText = $"Hydration failed: {ex.Message}";
        }
        finally
        {
            IsAiBusy = false;
        }
    }

    private async Task RunReasoningAsync()
    {
        if (SelectedScenario == null) return;

        IsAiBusy = true;
        AiResultText = "Analyzing problem scenario dynamics...";

        try
        {
            string analysis = await _hydrationService.AnalyzeScenarioAsync(SelectedScenario.Id);
            SelectedScenario.Model.Results = analysis;
            await _scenarioService.UpdateAsync(SelectedScenario.Model);

            AiResultText = analysis;
        }
        catch (Exception ex)
        {
            AiResultText = $"Analysis failed: {ex.Message}";
        }
        finally
        {
            IsAiBusy = false;
        }
    }

    private async Task ExportPdfReportAsync(string filePath)
    {
        if (SelectedUniverse == null) return;

        try
        {
            await _reportService.GeneratePdfReportAsync(SelectedUniverse.Id, filePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error exporting PDF: {ex.Message}");
        }
    }
}
