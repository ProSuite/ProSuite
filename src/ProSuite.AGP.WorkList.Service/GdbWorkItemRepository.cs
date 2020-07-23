using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using EsriDE.ProSuite.DomainModel.Core.DataModel;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AG.Gdb;
using ProSuite.Commons.AG.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.DataModel;
using GeometryType = ArcGIS.Core.Geometry.GeometryType;

namespace ProSuite.AGP.WorkList.Service
{
	// Note maybe all SDK code, like open workspace, etc. should be in here. Not in DbStatusSourceClass for instance.
	[CLSCompliant(false)]
	public abstract class GdbWorkItemRepository : IWorkItemRepository
	{
		private readonly List<DbStatusSourceClass> _sourceClasses = new List<DbStatusSourceClass>();
		private readonly IWorkspaceContext _workspaceContext;

		protected GdbWorkItemRepository(IWorkspaceContext workspaceContext)
		{
			_workspaceContext = workspaceContext;
		}

		//public GdbWorkItemRepository(IWorkItemFactory factory)
		//{
		//	_workItems = _features.Select(f => factory.CreateWorkItem(f)).ToList();
		//}

		public void Register(IObjectDataset dataset, DbStatusSchema statusSchema = null)
		{
			if (dataset is IVectorDataset vectorDataset)
			{
				// todo daro: determine FeatureClassDefinition here? geodatabase.GetDefinition<FeatureClassDefinition>("LocalGovernment.GDB.FireStation")
				_sourceClasses.Add(CreateStatusSourceClass(vectorDataset, statusSchema));
			}
		}

		// todo daro: return tuple or keyvaluepair IWorkItem, Geometry (Array of points), Coordinate3D?
		public IEnumerable<KeyValuePair<IWorkItem, IReadOnlyList<Coordinate3D>>> GetItems(
			QueryFilter filter, bool recycle)
		{
			foreach (DbStatusSourceClass sourceClass in _sourceClasses)
			{
				// todo daro: revise
				var featureClass = _workspaceContext.OpenFeatureClass((IVectorDataset)sourceClass.Dataset);

				// todo daro: determine geometry type on DbStatusSourceClass?
				GeometryType type = GeometryUtils.GetGeometryType(featureClass);

				//QueryFilter queryFilter = filter;
				QueryFilter queryFilter = null;

				// Todo daro: check recycle
				foreach (Feature feature in GdbRowUtils.GetRows<Feature>(featureClass, queryFilter, recycle: false))
				{
					Geometry geometry = feature.GetShape();
					IReadOnlyList<Coordinate3D> coordinates = GeometryUtils.GetCoordinates(geometry, type);
					WorkItem item = CreateWorkItemCore(feature);

					// todo daro: cache?
					yield return new KeyValuePair<IWorkItem, IReadOnlyList<Coordinate3D>>(item, coordinates);
				}
			}
		}

		public IEnumerable<IWorkItem> GetAll()
		{
			foreach (DbStatusSourceClass sourceClass in _sourceClasses)
			{
				// todo daro: revise
				var featureClass = _workspaceContext.OpenFeatureClass((IVectorDataset) sourceClass.Dataset);

				// Todo daro: check recycle
				foreach (Feature feature in GdbRowUtils.GetRows<Feature>(featureClass, filter : null, recycle : false))
				{
					yield return CreateWorkItemCore(feature);
				}
			}
		}

		[NotNull]
		protected abstract WorkItem CreateWorkItemCore([NotNull] Row row);

		private DbStatusSourceClass CreateStatusSourceClass(IVectorDataset dataset, DbStatusSchema statusSchema)
		{
			return new DbStatusSourceClass(dataset, statusSchema);
		}
	}
}
