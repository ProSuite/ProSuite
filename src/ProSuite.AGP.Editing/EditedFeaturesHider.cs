using System;
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
/// selection colours opaque and does not apply the display filter, so the selection colours of
/// a stereo map's layers are left unchanged. Hidden by a definition
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

	// The layers that any hider hides now, once per hiding (MCT only). Their own query or
	// filter is no leftover: removing it would leave the hider to restore a query that is
	// gone, which ArcGIS Pro refuses ("choice list is empty").
	private static readonly List<BasicFeatureLayer> _layersHiddenNow = new();

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
	/// features stuck hidden. A layer that a hider hides now keeps its filter.
	/// Must be called on the MCT.
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
			if (_layersHiddenNow.Contains(layer))
			{
				continue;
			}

			if (RemoveOwnDefinitionQuery(layer))
			{
				_msg.Debug($"Removed stale '{_filterName}' definition query from layer " +
				           $"{layer.Name}");
			}

			if (layer.GetDefinition() is not CIMFeatureLayer cimLayer)
			{
				continue;
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

		_hiddenLayers.Add(new HiddenLayerState(layer, cimLayer, _useDefinitionQuery));
		_layersHiddenNow.Add(layer);

		if (_useDefinitionQuery)
		{
			// Hides the selection too.
			ActivateOwnDefinitionQuery(layer, whereClause);
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
		// selection fill of polygon interiors fully transparent. Not in a stereo map, whose view
		// draws them opaque (black) instead of the normal selection colour.
		if (layer.Map?.DefaultViewingMode != MapViewingMode.MapStereo)
		{
			cimLayer.SelectionColor = CreateTransparentColor();
			cimLayer.PolygonSelectionFillColor = CreateTransparentColor();
		}

		SetDefinitionWithoutUndo(layer, cimLayer);
	}

	/// <summary>
	/// Makes a definition query of its own the active one: the active query of the layer, if
	/// any, and the where clause. Not through the layer's CIM definition: ArcGIS Pro clears the
	/// current editing template on every <see cref="BasicFeatureLayer.SetDefinition"/>, even
	/// with an unchanged definition.
	/// </summary>
	private static void ActivateOwnDefinitionQuery([NotNull] BasicFeatureLayer layer,
	                                               [NotNull] string whereClause)
	{
		string active = layer.DefinitionQuery;

		string expression = string.IsNullOrWhiteSpace(active)
			                    ? whereClause
			                    : $"({active}) AND {whereClause}";

		ExecuteWithoutUndo(
			layer,
			() => layer.InsertDefinitionQuery(new DefinitionQuery(_filterName, expression), true));
	}

	/// <summary>
	/// Removes the definition query of this class, making the query it was made from the
	/// active one again where it can be found among the remaining ones. Not through the
	/// layer's CIM definition, which would clear the current editing template.
	/// </summary>
	/// <returns>Whether the layer had the definition query of this class.</returns>
	/// <remarks>Must be called on the MCT.</remarks>
	private static bool RemoveOwnDefinitionQuery([NotNull] BasicFeatureLayer layer)
	{
		DefinitionQuery ownQuery =
			layer.DefinitionQueries.FirstOrDefault(q => q.Name == _filterName);

		if (ownQuery == null)
		{
			return false;
		}

		DefinitionQuery original = null;

		if (layer.ActiveDefinitionQuery?.Name == _filterName)
		{
			// The own query is "(<the query it was made from>) AND <where clause>".
			string ownExpression = ownQuery.WhereClause ?? string.Empty;

			original = layer.DefinitionQueries.FirstOrDefault(
				q => q.Name != _filterName &&
				     ownExpression.StartsWith($"({q.WhereClause}) AND "));
		}

		ExecuteWithoutUndo(layer, () =>
		{
			if (original != null)
			{
				layer.SetActiveDefinitionQuery(original.Name);
			}

			layer.RemoveDefinitionQueries(new[] { _filterName });
		});

		return true;
	}

	/// <summary>
	/// Restores the hidden features' visibility before the project is saved, so the temporary
	/// display filter is never written into the .aprx (which would leave the features invisible
	/// even after a normal close and reopen).
	/// </summary>
	private async Task OnProjectSavingAsync(ProjectEventArgs args)
	{
		// An ArcGIS Pro event handler: nothing may escape into the save.
		try
		{
			await QueuedTask.Run(Restore);
		}
		catch (Exception ex)
		{
			_msg.Warn("Could not show the hidden features again before saving the project", ex);
		}
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
		ExecuteWithoutUndo(layer, () => layer.SetDefinition(definition));
	}

	/// <remarks>Must be called on the MCT.</remarks>
	private static void ExecuteWithoutUndo([NotNull] BasicFeatureLayer layer,
	                                       [NotNull] Action layerChange)
	{
		OperationManager operationManager = layer.Map?.OperationManager;

		var operationsBefore = operationManager?.FindUndoOperations(_ => true);

		layerChange();

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

		private readonly bool _hiddenByDefinitionQuery;

		private readonly bool _enableDisplayFilters;
		[CanBeNull] private readonly CIMDisplayFilter[] _displayFilters;

		[CanBeNull] private readonly string _activeDefinitionQueryName;

		// null: the default from the selection options
		[CanBeNull] private readonly CIMColor _selectionColor;
		[CanBeNull] private readonly CIMColor _polygonSelectionFillColor;

		public HiddenLayerState([NotNull] BasicFeatureLayer layer,
		                        [NotNull] CIMFeatureLayer cimLayer,
		                        bool hiddenByDefinitionQuery)
		{
			_layer = layer;
			_hiddenByDefinitionQuery = hiddenByDefinitionQuery;
			_enableDisplayFilters = cimLayer.EnableDisplayFilters;
			_displayFilters = cimLayer.DisplayFilters;
			_activeDefinitionQueryName = layer.ActiveDefinitionQuery?.Name;
			_selectionColor = cimLayer.SelectionColor;
			_polygonSelectionFillColor = cimLayer.PolygonSelectionFillColor;
		}

		public void Restore()
		{
			_layersHiddenNow.Remove(_layer);

			if (_hiddenByDefinitionQuery)
			{
				if (_layer.DefinitionQueries.All(q => q.Name != _filterName))
				{
					// Removed behind the hider's back, e.g. by the layer properties: the
					// layer shows its features already.
					_msg.Debug($"Layer {_layer.Name} no longer has the '{_filterName}' " +
					           "definition query; nothing to restore");
					return;
				}

				ExecuteWithoutUndo(_layer, () =>
				{
					_layer.SetActiveDefinitionQuery(_activeDefinitionQueryName);
					_layer.RemoveDefinitionQueries(new[] { _filterName });
				});
				return;
			}

			if (_layer.GetDefinition() is not CIMFeatureLayer cimLayer)
			{
				return;
			}

			cimLayer.EnableDisplayFilters = _enableDisplayFilters;
			cimLayer.DisplayFilters = _displayFilters;
			cimLayer.SelectionColor = _selectionColor;
			cimLayer.PolygonSelectionFillColor = _polygonSelectionFillColor;

			SetDefinitionWithoutUndo(_layer, cimLayer);
		}
	}
}
