using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain;

public abstract class DbStatusWorkList : WorkList
{
	private readonly List<WorkListFilterDefinition> _filterDefinitions;

	protected DbStatusWorkList([NotNull] IWorkItemRepository repository,
	                           [NotNull] Geometry areaOfInterest,
	                           [NotNull] string name,
	                           [NotNull] string displayName)
		: base(repository, areaOfInterest, name, displayName)
	{
		_filterDefinitions = new List<WorkListFilterDefinition>();
	}

	private GdbItemRepository GdbRepository => (GdbItemRepository) Repository;

	#region Filter Definitions

	/// <summary>
	/// Gets the available filter definitions for this work list.
	/// </summary>
	[NotNull]
	public IReadOnlyList<WorkListFilterDefinition> FilterDefinitions => _filterDefinitions;

	/// <summary>
	/// Gets or sets the currently selected filter definition.
	/// </summary>
	[CanBeNull]
	public WorkListFilterDefinition CurrentFilterDefinition
	{
		get => GdbRepository.CurrentFilterDefinition;
		set
		{
			if (GdbRepository.CurrentFilterDefinition != value)
			{
				GdbRepository.CurrentFilterDefinition = value;
			}
		}
	}

	#endregion

	public override bool CanSetStatus()
	{
		return base.CanSetStatus() && base.CanSetStatus();
	}

	protected override string GetFilterDisplayText()
	{
		return CurrentFilterDefinition != null
			       ? $"Filter: {CurrentFilterDefinition.Name}"
			       : base.GetFilterDisplayText();
	}

	protected virtual bool CanSetStatusCore()
	{
		return Project.Current?.IsEditingEnabled == true;
	}

	protected virtual IList<WorkListFilterDefinitionExpression> GetDefinitionExpressions(
		[NotNull] ISourceClass sourceClass)
	{
		if (CurrentFilterDefinition == null)
		{
			return null;
		}

		// Get definition expressions that match the current filter definition
		// Subclasses should override this to provide specific expressions for their source classes
		return null;
	}

	public void UpdateDefinitionExpressions()
	{
		var dbStatusRepository = (DbStatusWorkItemRepository) Repository;

		foreach (ISourceClass sourceClass in dbStatusRepository.SourceClasses)
		{
			DatabaseSourceClass dbSourceClass = (DatabaseSourceClass) sourceClass;

			IList<WorkListFilterDefinitionExpression> expressions =
				GetDefinitionExpressions(sourceClass);

			dbSourceClass.UpdateDefinitionFilterExpressions(expressions);

			foreach (var expression in expressions)
			{
				WorkListFilterDefinition referencedFilter = expression.FilterDefinition;

				if (! _filterDefinitions.Contains(referencedFilter))
				{
					_filterDefinitions.Add(referencedFilter);
				}
			}
		}

		if (CurrentFilterDefinition == null && _filterDefinitions.Count > 0)
		{
			// By convention, the first is the default:
			CurrentFilterDefinition = _filterDefinitions[0];
		}
	}
}
