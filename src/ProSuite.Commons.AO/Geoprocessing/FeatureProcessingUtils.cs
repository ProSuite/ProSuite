using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geoprocessing
{
	public static class FeatureProcessingUtils
	{
		public static bool Cancelled(ITrackCancel trackCancel)
		{
			return trackCancel != null && ! trackCancel.Continue();
		}

		[NotNull]
		public static IList<IFeature> GetSelectedFeatures(
			[NotNull] ISelectionSet selectionSet,
			[NotNull] IGeometry processingArea,
			out IWorkspace editWorkspace)
		{
			return GetSelectedFeatures(new List<ISelectionSet> { selectionSet }, processingArea,
			                           out editWorkspace);
		}

		[NotNull]
		public static IList<IFeature> GetSelectedFeatures(
			[NotNull] IEnumerable<ISelectionSet> selectionSets,
			[NotNull] IGeometry processingArea,
			out IWorkspace editWorkspace)
		{
			Assert.ArgumentNotNull(selectionSets, nameof(selectionSets));

			editWorkspace = null;

			var result = new List<IFeature>();

			foreach (ISelectionSet selectionSet in selectionSets)
			{
				var selectionTarget = (IFeatureClass) selectionSet.Target;

				IWorkspace workspace = DatasetUtils.GetWorkspace(selectionTarget);

				if (editWorkspace == null)
				{
					editWorkspace = workspace;
				}
				else
				{
					Assert.True(
						WorkspaceUtils.IsSameWorkspace(editWorkspace, workspace,
						                               WorkspaceComparison.Exact),
						"Source features must all come from the same workspace");
				}

				IQueryFilter filter =
					GdbQueryUtils.CreateSpatialFilter(selectionTarget, processingArea);

				result.AddRange(GetSelectedObjects<IFeature>(selectionSet, filter, false));
			}

			return result;
		}

		/// <summary>
		/// Copy from SelectionUtils. Consider moving parts of SelectionUtils to Commons.AO.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="selectionSet"></param>
		/// <param name="queryFilter"></param>
		/// <param name="recycle"></param>
		/// <returns></returns>
		[NotNull]
		private static IEnumerable<T> GetSelectedObjects<T>(
			[NotNull] ISelectionSet selectionSet,
			[CanBeNull] IQueryFilter queryFilter,
			bool recycle) where T : IObject
		{
			// TODO: Copy from SelectionUtils. Consider moving parts of SelectionUtils to Commons.AO.
			Assert.ArgumentNotNull(selectionSet, nameof(selectionSet));

			ICursor cursor;
			selectionSet.Search(queryFilter, recycle, out cursor);

			try
			{
				for (IRow row = cursor.NextRow();
				     row != null;
				     row = cursor.NextRow())
				{
					yield return (T) row;
				}
			}
			finally
			{
				Marshal.ReleaseComObject(cursor);
			}
		}
	}
}
