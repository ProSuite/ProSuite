using System;
using System.Collections.Generic;
using System.Linq;
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
				var relatedByHandle = new Dictionary<IntPtr, RelatedTableRows>();

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

					CollectRelatedObjects(table, oids, perimeter, relatedByHandle);
				}

				if (relatedByHandle.Count == 0)
				{
					return;
				}

				// SelectRows always adds to the current selection, so clear it first
				// unless the user requested to add to the existing selection.
				if (! addToSelection)
				{
					map.ClearSelection();
				}

				foreach (RelatedTableRows related in relatedByHandle.Values)
				{
					relatedSelected += (int) SelectionUtils.SelectRows(
						mapView,
						displayTable => IsSameTable(displayTable, related.Table),
						related.GetObjectIds());
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
		[NotNull] Dictionary<IntPtr, RelatedTableRows> relatedByHandle)
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

				AddRelatedRows(relatedRows, perimeter, relatedByHandle);
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
		[NotNull] Dictionary<IntPtr, RelatedTableRows> relatedByHandle)
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

			if (! relatedByHandle.TryGetValue(handle, out RelatedTableRows related))
			{
				related = new RelatedTableRows(relatedTable);
				relatedByHandle[handle] = related;
			}

			related.Add(relatedRow.GetObjectID());
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

	/// <summary>
	/// Accumulates the distinct object IDs of related rows that belong to a single table.
	/// </summary>
	private sealed class RelatedTableRows
	{
		private readonly HashSet<long> _objectIds = new();

		public RelatedTableRows([NotNull] Table table)
		{
			Table = table;
		}

		[NotNull]
		public Table Table { get; }

		public void Add(long oid)
		{
			_objectIds.Add(oid);
		}

		[NotNull]
		public IReadOnlyList<long> GetObjectIds()
		{
			return _objectIds.ToList();
		}
	}
}
