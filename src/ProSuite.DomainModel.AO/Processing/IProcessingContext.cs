using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.Processing;

namespace ProSuite.DomainModel.AO.Processing
{
	public interface IProcessingContext
	{
		#region Replacements for WorkContext and MapContext

		// TODO try NOT using Model - strive for model independence!
		// TODO in the long run, have custom "MapContext" interface (see Pro trial impl)

		[NotNull]
		DdxModel Model { get; }

		[NotNull]
		IWorkspaceContext WorkspaceContext { get; }

		double ReferenceScale { get; }

		ISpatialReference SpatialReference { get; }

		double PointsToMapUnits(double pointDistance);
		double MapUnitsToPoints(double mapDistance);

		#endregion

		ProcessExecutionType ExecutionType { get; }
		ProcessSelectionType SelectionType { get; set; }

		[NotNull]
		IDomainTransactionManager DomainTransactionManager { get; }

		[CanBeNull]
		string DefaultRepresentation { get; }

		void SetSystemFields(IRowBuffer row, IObjectClass objectClass);

		/// <summary>
		/// Get the perimeter where features should be processed,
		/// considering the context's <see cref="SelectionType"/>.
		/// The result may be <c>null</c>, meaning that no perimeter limits
		/// processing. This occurs, for example, when the selection type
		/// is "selected features" or "all features" without an implicit
		/// edit perimeter.
		/// </summary>
		/// <returns>An <see cref="IPolygon"/> instance or <c>null</c></returns>
		[CanBeNull]
		IPolygon GetProcessingPerimeter();

		bool CanExecute(IGdbProcess gdbProcess);

		[NotNull]
		IGdbTransaction GetTransaction();

		[CanBeNull]
		Func<IObjectClass, IObjectClass, bool, IDisposable> GetTransactionWrapperFunc();

		[NotNull]
		IWorkspace GetWorkspace();

		/// <remarks>Honors context's <see cref="SelectionType"/></remarks>
		IEnumerable<IFeature> GetFeatures(
			[NotNull] IFeatureClass featureClass, [CanBeNull] string whereClause,
			bool recycling = false);

		IEnumerable<IFeature> GetFeatures(
			[NotNull] IFeatureClass featureClass, [CanBeNull] string whereClause,
			[CanBeNull] IGeometry extent, bool recycling = false);

		/// <remarks>Honors context's <see cref="SelectionType"/></remarks>
		int CountFeatures(
			[NotNull] IFeatureClass featureClass, [CanBeNull] string whereClause);

		int CountFeatures(
			[NotNull] IFeatureClass featureClass, [CanBeNull] string whereClause,
			[CanBeNull] IGeometry extent);

		bool AllowModification([NotNull] IFeature feature, [NotNull] out string reason);

		// TODO consider the signature below to efficiently ask about _all_ derived features:
		//bool AllowModification([NotNull] IFeature originFeature, DerivedDatasetAssociation dda, out string reason);
	}
}
