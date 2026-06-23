using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Keyboard;
using SelectionUtils = ProSuite.Commons.AGP.Selection.SelectionUtils;

namespace ProSuite.AGP.Editing.Selection;

/// <summary>
/// Button to select features related to the current selection via relationship classes.
/// <para>
/// Hold the add-modifier key (Shift) to add the related features to the current
/// selection instead of replacing it. Hold the perimeter-modifier key (Control) to
/// restrict the related features to a perimeter (see <see cref="GetPerimeter"/>).
/// </para>
/// </summary>
public abstract class SelectRelatedFeaturesButtonBase : ButtonCommandBase
{
	private const Keys _addKey = Keys.Shift;
	private const Keys _perimeterKey = Keys.Control;

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	/// <summary>
	/// Gets the perimeter geometry to which related features are restricted when the
	/// perimeter-modifier key is pressed. Related features whose shape does not
	/// intersect this geometry are excluded. The default returns the map view extent.
	/// </summary>
	[CanBeNull]
	protected virtual Geometry GetPerimeter([NotNull] MapView mapView)
	{
		return mapView.Extent;
	}

	protected override async Task<bool> OnClickAsyncCore()
	{
		MapView mapView = MapView.Active;
		if (mapView is null)
		{
			return false;
		}

		Map map = mapView.Map;
		if (map is null)
		{
			return false;
		}

		bool addToSelection = KeyboardUtils.IsModifierPressed(_addKey);
		bool restrictToPerimeter = KeyboardUtils.IsModifierPressed(_perimeterKey);

		Geometry perimeter = restrictToPerimeter ? GetPerimeter(mapView) : null;

		SelectionCombinationMethod combinationMethod = addToSelection
			                                               ? SelectionCombinationMethod.Add
			                                               : SelectionCombinationMethod.New;

		var relatedSelected = 0;

		await QueuedTask.Run(() =>
		{
			Dictionary<MapMember, List<long>> selection = SelectionUtils.GetSelection(map);

			if (selection.Count == 0)
			{
				return;
			}

			using (_msg.IncrementIndentation(
				       "Getting related objects for current selection..."))
			{
				var relatedOidsByTableHandle =
					new Dictionary<IntPtr, List<long>>();
				var tableByHandle = new Dictionary<IntPtr, Table>();

				foreach (KeyValuePair<MapMember, List<long>> pair in selection)
				{
					Table table = GetTable(pair.Key);

					if (table is null)
					{
						continue;
					}

					List<long> oids = pair.Value;
					if (oids.Count == 0)
					{
						continue;
					}

					CollectRelatedObjects(table, oids, perimeter,
					                      relatedOidsByTableHandle,
					                      tableByHandle);
				}

				if (relatedOidsByTableHandle.Count == 0)
				{
					return;
				}

				if (combinationMethod == SelectionCombinationMethod.New)
				{
					map.ClearSelection();
				}

				foreach (KeyValuePair<IntPtr, List<long>> pair in
				         relatedOidsByTableHandle)
				{
					List<long> relatedOids = pair.Value;

					if (relatedOids.Count == 0)
					{
						continue;
					}

					Table relatedTable = tableByHandle[pair.Key];

					relatedSelected += (int) SelectionUtils.SelectRows(
						mapView,
						displayTable => IsSameTable(displayTable, relatedTable),
						relatedOids);
				}
			}
		});

		if (relatedSelected == 0)
		{
			_msg.Warn("No related objects found.");
		}
		else
		{
			_msg.InfoFormat("{0} related object(s) selected.", relatedSelected);
		}

		return relatedSelected > 0;
	}

	protected override void OnUpdateCore()
	{
		Enabled = MapView.Active?.Map?.SelectionCount > 0;
	}

	private static void CollectRelatedObjects(
		[NotNull] Table table,
		[NotNull] List<long> oids,
		[CanBeNull] Geometry perimeter,
		[NotNull] Dictionary<IntPtr, List<long>> relatedOidsByTableHandle,
		[NotNull] Dictionary<IntPtr, Table> tableByHandle)
	{
		Datastore datastore = table.GetDatastore();
		if (datastore is not Geodatabase geodatabase)
		{
			return;
		}

		string tableName = table.GetName();

		Predicate<RelationshipClassDefinition> predicate =
			def => string.Equals(def.GetOriginClass(), tableName,
			                     StringComparison.OrdinalIgnoreCase) ||
			       string.Equals(def.GetDestinationClass(), tableName,
			                     StringComparison.OrdinalIgnoreCase);

		foreach (RelationshipClass relClass in
		         RelationshipClassUtils.GetRelationshipClasses(geodatabase,
		                                                       predicate))
		{
			try
			{
				RelationshipClassDefinition relClassDef =
					relClass.GetDefinition();

				bool isOrigin = string.Equals(
					relClassDef.GetOriginClass(), tableName,
					StringComparison.OrdinalIgnoreCase);

				IReadOnlyList<Row> relatedRows = isOrigin
					                                 ? relClass.GetRowsRelatedToOriginRows(oids)
					                                 : relClass.GetRowsRelatedToDestinationRows(
						                                 oids);

				AddRelatedRows(relatedRows, perimeter,
				               relatedOidsByTableHandle, tableByHandle);
			}
			finally
			{
				relClass.Dispose();
			}
		}
	}

	private static void AddRelatedRows(
		[NotNull] IReadOnlyList<Row> relatedRows,
		[CanBeNull] Geometry perimeter,
		[NotNull] Dictionary<IntPtr, List<long>> relatedOidsByTableHandle,
		[NotNull] Dictionary<IntPtr, Table> tableByHandle)
	{
		foreach (Row relatedRow in relatedRows)
		{
			if (perimeter != null && relatedRow is Feature feature)
			{
				Geometry shape = feature.GetShape();
				if (shape is null ||
				    ! GeometryEngine.Instance.Intersects(perimeter, shape))
				{
					continue;
				}
			}

			Table relatedTable = relatedRow.GetTable();
			IntPtr handle = relatedTable.Handle;
			long oid = relatedRow.GetObjectID();

			if (! relatedOidsByTableHandle.TryGetValue(
				    handle, out List<long> existingOids))
			{
				existingOids = new List<long>();
				relatedOidsByTableHandle[handle] = existingOids;
				tableByHandle[handle] = relatedTable;
			}

			if (! existingOids.Contains(oid))
			{
				existingOids.Add(oid);
			}
		}
	}

	[CanBeNull]
	private static Table GetTable([NotNull] MapMember mapMember)
	{
		if (mapMember is IDisplayTable displayTable)
		{
			return LayerUtils.GetTable(displayTable, true);
		}

		return null;
	}

	private static bool IsSameTable([NotNull] IDisplayTable displayTable,
	                                [NotNull] Table table)
	{
		Table displayTableTable = displayTable.GetTable();
		if (displayTableTable is null)
		{
			return false;
		}

		return displayTableTable.Handle == table.Handle;
	}
}
