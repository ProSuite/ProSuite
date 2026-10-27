using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Events;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing;

/// <summary>
/// Temporarily hides the features being edited by a tool, so that their (old) symbolized
/// geometry does not visually interfere with the sketch. Each layer gets a display filter that
/// excludes the features' object IDs, and transparent selection colours, as the display filter
/// does not hide the selection. The previous layer state is restored in <see cref="Restore"/>.
/// </summary>
/// <remarks>
/// The display filter works in 2D map views only: the stereo view draws the transparent
/// selection colours opaque and does not apply the display filter. Hidden by a definition
/// query instead (see the constructor), the features are hidden in stereo too, selected or not.
/// </remarks>
public class EditedFeaturesHider
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private const string _filterName = "ProSuite_HideEditedFeatures";

	// Used by earlier versions of the Destroy and Rebuild tool, still removed as leftover.
	private const string _legacyFilterName = "DestroyAndRebuild_HideEditedFeature";

	private readonly bool _useDefinitionQuery;

	private readonly List<HiddenLayerState> _hiddenLayers = new();

	[CanBeNull] private SubscriptionToken _projectSavingToken;

	/// <param name="useDefinitionQuery">Whether the features are hidden by a definition query
	/// of their layer instead of a display filter. The stereo view applies the definition
	/// query, also to selected features; but the layer then no longer finds the features,
	/// so the caller must restore before it selects them.</param>
	public EditedFeaturesHider(bool useDefinitionQuery = false)
	{
		_useDefinitionQuery = useDefinitionQuery;
	}

	public bool IsHiding => _hiddenLayers.Count > 0;

	/// <summary>
	/// Hides the specified features, replacing the currently hidden ones.
	/// Must be called on the MCT.
	/// </summary>
	public void Hide([NotNull] IEnumerable<KeyValuePair<BasicFeatureLayer, List<long>>> oidsByLayer)
	{
		Assert.ArgumentNotNull(oidsByLayer, nameof(oidsByLayer));

		// Restore first so we never capture our own filter as the state to restore to.
		Restore();

		foreach (KeyValuePair<BasicFeatureLayer, List<long>> pair in oidsByLayer)
		{
			HideFeatures(pair.Key, pair.Value);
		}

		if (IsHiding)
		{
			_projectSavingToken ??= ProjectSavingEvent.Subscribe(OnProjectSavingAsync);
		}
	}

	/// <summary>
	/// Restores the display-filter and selection-colour state that was overridden by
	/// <see cref="Hide"/>, making the hidden features visible again.
	/// Must be called on the MCT.
	/// </summary>
	public void Restore()
	{
		if (_projectSavingToken != null)
		{
			ProjectSavingEvent.Unsubscribe(_projectSavingToken);
			_projectSavingToken = null;
		}

		try
		{
			foreach (HiddenLayerState hiddenLayer in _hiddenLayers)
			{
				hiddenLayer.Restore();
			}
		}
		finally
		{
			_hiddenLayers.Clear();
		}
	}

	/// <summary>
	/// Removes any leftover display filter of this class (identified by its well-known name)
	/// from the feature layers of the given map. This guards against a filter that a previous
	/// session persisted without restoring, e.g. after a crash, which would otherwise leave
	/// features stuck hidden. Must be called on the MCT.
	/// </summary>
	public static void RemoveLeftovers([CanBeNull] Map map)
	{
		if (map == null)
		{
			return;
		}

		foreach (BasicFeatureLayer layer in
		         map.GetLayersAsFlattenedList().OfType<BasicFeatureLayer>())
		{
			if (layer.GetDefinition() is not CIMFeatureLayer cimLayer)
			{
				continue;
			}

			if (RemoveOwnDefinitionQuery(cimLayer.FeatureTable))
			{
				SetDefinitionWithoutUndo(layer, cimLayer);

				_msg.Debug($"Removed stale '{_filterName}' definition query from layer " +
				           $"{layer.Name}");
			}

			CIMDisplayFilter[] filters = cimLayer.DisplayFilters;

			if (filters == null || ! filters.Any(IsOwnFilter))
			{
				continue;
			}

			CIMDisplayFilter[] remaining = filters.Where(f => ! IsOwnFilter(f)).ToArray();

			cimLayer.DisplayFilters = remaining.Length > 0 ? remaining : null;

			if (remaining.Length == 0)
			{
				cimLayer.EnableDisplayFilters = false;
			}

			// The transparent selection colours are always set together with the filter. Their
			// original values are lost, fall back to the default selection colours.
			if (IsTransparent(cimLayer.SelectionColor))
			{
				cimLayer.SelectionColor = null;
			}

			if (IsTransparent(cimLayer.PolygonSelectionFillColor))
			{
				cimLayer.PolygonSelectionFillColor = null;
			}

			SetDefinitionWithoutUndo(layer, cimLayer);

			_msg.Debug($"Removed stale '{_filterName}' display filter from layer {layer.Name}");
		}
	}

	private void HideFeatures([NotNull] BasicFeatureLayer layer, [NotNull] List<long> oids)
	{
		if (oids.Count == 0 || layer.GetDefinition() is not CIMFeatureLayer cimLayer)
		{
			return;
		}

		string oidField = layer.GetTable().GetDefinition().GetObjectIDField();
		string whereClause = $"{oidField} NOT IN ({string.Join(",", oids)})";

		_hiddenLayers.Add(new HiddenLayerState(layer, cimLayer));

		if (_useDefinitionQuery)
		{
			// Hides the selection too.
			AddDefinitionQuery(Assert.NotNull(cimLayer.FeatureTable), whereClause);

			SetDefinitionWithoutUndo(layer, cimLayer);
			return;
		}

		cimLayer.DisplayFilters = new[]
		                          {
			                          new CIMDisplayFilter
			                          {
				                          Name = _filterName,
				                          WhereClause = whereClause
			                          }
		                          };
		cimLayer.EnableDisplayFilters = true;

		// The display filter does not hide the selection: make the selection colour and the
		// selection fill of polygon interiors fully transparent.
		cimLayer.SelectionColor = CreateTransparentColor();
		cimLayer.PolygonSelectionFillColor = CreateTransparentColor();

		SetDefinitionWithoutUndo(layer, cimLayer);
	}

	/// <summary>
	/// Makes a definition query of its own the active one: the active query of the layer, if
	/// any, and the where clause.
	/// </summary>
	private static void AddDefinitionQuery([NotNull] CIMFeatureTable featureTable,
	                                       [NotNull] string whereClause)
	{
		string active = featureTable.DefinitionExpression;

		string expression = string.IsNullOrWhiteSpace(active)
			                    ? whereClause
			                    : $"({active}) AND {whereClause}";

		var ownQuery = new CIMDefinitionFilter
		               {
			               Name = _filterName,
			               DefinitionExpression = expression
		               };

		featureTable.DefinitionFilterChoices =
			(featureTable.DefinitionFilterChoices ?? Enumerable.Empty<CIMDefinitionFilter>())
			.Append(ownQuery)
			.ToArray();
		featureTable.DefinitionExpressionName = _filterName;
		featureTable.DefinitionExpression = expression;
	}

	/// <summary>
	/// Removes the definition query of this class, making the query it was made from the
	/// active one again where it can be found among the remaining ones.
	/// </summary>
	/// <returns>Whether the layer had the definition query of this class.</returns>
	private static bool RemoveOwnDefinitionQuery([CanBeNull] CIMFeatureTable featureTable)
	{
		CIMDefinitionFilter ownQuery =
			featureTable?.DefinitionFilterChoices?.FirstOrDefault(q => q?.Name == _filterName);

		if (ownQuery == null)
		{
			return false;
		}

		CIMDefinitionFilter[] remaining =
			featureTable.DefinitionFilterChoices.Where(q => q != ownQuery).ToArray();

		featureTable.DefinitionFilterChoices = remaining.Length > 0 ? remaining : null;

		if (featureTable.DefinitionExpressionName != _filterName)
		{
			return true;
		}

		// The own query is "(<the query it was made from>) AND <where clause>".
		string ownExpression = ownQuery.DefinitionExpression ?? string.Empty;

		CIMDefinitionFilter original =
			remaining.FirstOrDefault(
				q => ownExpression.StartsWith($"({q?.DefinitionExpression}) AND "));

		featureTable.DefinitionExpressionName = original?.Name;
		featureTable.DefinitionExpression = original?.DefinitionExpression;

		return true;
	}

	/// <summary>
	/// Restores the hidden features' visibility before the project is saved, so the temporary
	/// display filter is never written into the .aprx (which would leave the features invisible
	/// even after a normal close and reopen).
	/// </summary>
	private async Task OnProjectSavingAsync(ProjectEventArgs args)
	{
		await QueuedTask.Run(Restore);
	}

	private static bool IsOwnFilter([CanBeNull] CIMDisplayFilter filter)
	{
		return filter?.Name == _filterName || filter?.Name == _legacyFilterName;
	}

	private static CIMColor CreateTransparentColor()
	{
		return ColorFactory.Instance.CreateRGBColor(0, 0, 0, 0);
	}

	private static bool IsTransparent([CanBeNull] CIMColor color)
	{
		return color != null && color.Alpha <= 0;
	}

	/// <summary>
	/// Applies a layer definition change without leaving an entry on the map's undo/redo stack.
	/// The temporary display filter used to hide the edited features is transient tool state and
	/// must not be undoable: undoing/redoing it would re-apply or drop the filter behind the tool's
	/// back and could leave features stuck hidden.
	/// </summary>
	/// <remarks>Must be called on the MCT.</remarks>
	private static void SetDefinitionWithoutUndo([NotNull] BasicFeatureLayer layer,
	                                             [NotNull] CIMBaseLayer definition)
	{
		OperationManager operationManager = layer.Map?.OperationManager;

		var operationsBefore = operationManager?.FindUndoOperations(_ => true);

		layer.SetDefinition(definition);

		if (operationManager == null)
		{
			return;
		}

		// Remove any operation the SetDefinition call just pushed onto the undo stack.
		foreach (var operation in operationManager.FindUndoOperations(_ => true))
		{
			if (operationsBefore == null || ! operationsBefore.Contains(operation))
			{
				operationManager.RemoveUndoOperation(operation);
			}
		}
	}

	/// <summary>
	/// The display-filter, definition-query and selection-colour state of a layer before its
	/// features were hidden.
	/// </summary>
	private class HiddenLayerState
	{
		[NotNull] private readonly BasicFeatureLayer _layer;

		private readonly bool _enableDisplayFilters;
		[CanBeNull] private readonly CIMDisplayFilter[] _displayFilters;

		[CanBeNull] private readonly string _definitionExpression;
		[CanBeNull] private readonly string _definitionExpressionName;
		[CanBeNull] private readonly CIMDefinitionFilter[] _definitionFilterChoices;

		// null: the default from the selection options
		[CanBeNull] private readonly CIMColor _selectionColor;
		[CanBeNull] private readonly CIMColor _polygonSelectionFillColor;

		public HiddenLayerState([NotNull] BasicFeatureLayer layer,
		                        [NotNull] CIMFeatureLayer cimLayer)
		{
			_layer = layer;
			_enableDisplayFilters = cimLayer.EnableDisplayFilters;
			_displayFilters = cimLayer.DisplayFilters;
			_definitionExpression = cimLayer.FeatureTable?.DefinitionExpression;
			_definitionExpressionName = cimLayer.FeatureTable?.DefinitionExpressionName;
			_definitionFilterChoices = cimLayer.FeatureTable?.DefinitionFilterChoices;
			_selectionColor = cimLayer.SelectionColor;
			_polygonSelectionFillColor = cimLayer.PolygonSelectionFillColor;
		}

		public void Restore()
		{
			if (_layer.GetDefinition() is not CIMFeatureLayer cimLayer)
			{
				return;
			}

			cimLayer.EnableDisplayFilters = _enableDisplayFilters;
			cimLayer.DisplayFilters = _displayFilters;

			if (cimLayer.FeatureTable != null)
			{
				cimLayer.FeatureTable.DefinitionExpression = _definitionExpression;
				cimLayer.FeatureTable.DefinitionExpressionName = _definitionExpressionName;
				cimLayer.FeatureTable.DefinitionFilterChoices = _definitionFilterChoices;
			}

			cimLayer.SelectionColor = _selectionColor;
			cimLayer.PolygonSelectionFillColor = _polygonSelectionFillColor;

			SetDefinitionWithoutUndo(_layer, cimLayer);
		}
	}
}
