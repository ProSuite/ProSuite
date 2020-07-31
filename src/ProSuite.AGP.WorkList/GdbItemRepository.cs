using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.DataModel;

namespace ProSuite.AGP.WorkList
{
	// Note maybe all SDK code, like open workspace, etc. should be in here. Not in DatabaseSourceClass for instance.
	public abstract class GdbItemRepository : IWorkItemRepository
	{
		private readonly List<DatabaseSourceClass> _sourceClasses = new List<DatabaseSourceClass>();
		private readonly IWorkspaceContext _workspaceContext;

		protected GdbItemRepository([NotNull] IWorkspaceContext workspaceContext)
		{
			Assert.ArgumentNotNull(workspaceContext, nameof(workspaceContext));

			_workspaceContext = workspaceContext;
		}

		public IEnumerable<IWorkItem> GetItems(QueryFilter filter = null, bool recycle = true)
		{
			foreach (DatabaseSourceClass sourceClass in _sourceClasses)
			{
				FeatureClass featureClass = OpenFeatureClass(sourceClass.Name);

				if (featureClass == null)
				{
					continue;
				}

				// Todo daro: check recycle
				foreach (Feature feature in GdbQueryUtils.GetRows<Feature>(
					featureClass, filter, recycle))
				{
					yield return CreateWorkItemCore(feature, sourceClass.AttributeReader);
				}
			}
		}

		public void Register(string tableName)
		{
			// todo daro: _workspaceContext.GetDefinition(tableName)
			var definition = _workspaceContext.Geodatabase.GetDefinition<FeatureClassDefinition>(tableName);

			IAttributeReader attributeReader = CreateAttributeReaderCore(definition);

			DatabaseStatusSchema statusSchema = CreateStatusSchemaCore();

			DatabaseSourceClass sourceClass = CreateSourceClassCore(tableName, statusSchema, attributeReader);

			_sourceClasses.Add(sourceClass);
		}

		[NotNull]
		protected abstract DatabaseStatusSchema CreateStatusSchemaCore();

		protected abstract IAttributeReader CreateAttributeReaderCore(FeatureClassDefinition definition);

		[NotNull]
		protected abstract IWorkItem CreateWorkItemCore([NotNull] Row row, IAttributeReader reader);

		[NotNull]
		protected abstract DatabaseSourceClass CreateSourceClassCore(
			string name,
			DatabaseStatusSchema statusSchema,
			IAttributeReader attributeReader);

		[CanBeNull]
		private FeatureClass OpenFeatureClass(string name)
		{
			// todo daro: revise
			return _workspaceContext.OpenFeatureClass(name);
		}

		#region unused

		public int GetCount(QueryFilter filter = null)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<PluginField> GetFields(IEnumerable<string> fieldNames = null)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
