using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.DataModel;

namespace ProSuite.AGP.WorkList.Service
{
	// Note maybe all SDK code, like open workspace, etc. should be in here. Not in DbStatusSourceClass for instance.
	[CLSCompliant(false)]
	public abstract class GdbWorkItemRepository : IWorkItemRepository
	{
		private readonly List<DbStatusSourceClass> _sourceClasses = new List<DbStatusSourceClass>();
		private readonly IWorkspaceContext _workspaceContext;

		protected GdbWorkItemRepository([NotNull] IWorkspaceContext workspaceContext,
		                                bool isQueryLanguageSupported = false)
		{
			Assert.ArgumentNotNull(workspaceContext, nameof(workspaceContext));

			_workspaceContext = workspaceContext;
			IsQueryLanguageSupported = isQueryLanguageSupported;
		}

		public void Register(IObjectDataset dataset, DbStatusSchema statusSchema = null)
		{
			if (dataset is IVectorDataset vectorDataset)
			{
				// todo daro: determine FeatureClassDefinition here? geodatabase.GetDefinition<FeatureClassDefinition>("LocalGovernment.GDB.FireStation")
				_sourceClasses.Add(CreateStatusSourceClass(vectorDataset, statusSchema));
			}
		}

		// todo daro: return tuple or keyvaluepair IWorkItem, Geometry (Array of points), Coordinate3D?
		public bool IsQueryLanguageSupported { get; }

		public int GetCount(QueryFilter filter = null)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<PluginField> GetFields(IEnumerable<string> fieldNames = null)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(
			[CanBeNull] QueryFilter filter, bool recycle)
		{
			foreach (DbStatusSourceClass sourceClass in _sourceClasses)
			{
				// todo daro: revise
				FeatureClass featureClass =
					sourceClass.OpenFeatureClass(_workspaceContext.Geodatabase);

				// Todo daro: check recycle
				if (featureClass == null)
				{
					continue;
				}

				foreach (Feature feature in GdbRowUtils.GetRows<Feature>(
					featureClass, filter, false))
				{
					Geometry geometry = feature.GetShape();
					IWorkItem item = CreateWorkItemCore(feature);

					// todo daro: cache?
					yield return new KeyValuePair<IWorkItem, Geometry>(item, geometry);
				}
			}
		}

		public IEnumerable<IWorkItem> GetAll()
		{
			foreach (DbStatusSourceClass sourceClass in _sourceClasses)
			{
				// todo daro: revise
				FeatureClass featureClass =
					_workspaceContext.OpenFeatureClass((IVectorDataset)sourceClass.Dataset);

				// Todo daro: check recycle
				foreach (Feature feature in GdbRowUtils.GetRows<Feature>(featureClass, null, false))
				{
					yield return CreateWorkItemCore(feature);
				}
			}
		}

		[NotNull]
		protected abstract IWorkItem CreateWorkItemCore([NotNull] Row row);

		protected abstract DbStatusSourceClass CreateStatusSourceClass(IVectorDataset dataset, DbStatusSchema statusSchema);
	}
}
