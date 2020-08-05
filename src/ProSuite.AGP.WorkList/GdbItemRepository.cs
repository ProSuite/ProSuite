using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.DataModel;

namespace ProSuite.AGP.WorkList
{
	// Note maybe all SDK code, like open workspace, etc. should be in here. Not in DatabaseSourceClass for instance.
	public abstract class GdbItemRepository : IWorkItemRepository
	{
		private readonly IEnumerable<IWorkspaceContext> _workspaces;

		private readonly Dictionary<ISourceClass, IWorkspaceContext> _workspacesBySourceClass
			= new Dictionary<ISourceClass, IWorkspaceContext>();

		protected GdbItemRepository(IEnumerable<IWorkspaceContext> workspaces)
		{
			_workspaces = workspaces;
		}

		public IEnumerable<IWorkItem> GetItems(QueryFilter filter = null, bool recycle = true)
		{
			foreach (ISourceClass sourceClass in _workspacesBySourceClass.Keys)
			{
				foreach (IWorkItem workItem in GetItemsCore(sourceClass, filter, recycle))
				{
					yield return workItem;
				}
			}
		}

		IEnumerable<ISourceClass> IWorkItemRepository.RegisterDatasets(ICollection<GdbTableIdentity> datasets)
		{
			return RegisterDatasetsCore(datasets);
		}

		protected IEnumerable<ISourceClass> RegisterDatasetsCore(ICollection<GdbTableIdentity> datasets)
		{
			foreach (IWorkspaceContext workspace in _workspaces)
			{
				using (Geodatabase geodatabase = workspace.OpenGeodatabase())
				{
					var definitions = geodatabase.GetDefinitions<FeatureClassDefinition>().ToLookup(d => d.GetName());
					
					foreach (GdbTableIdentity dataset in datasets.Where(d => workspace.Contains(d)))
					{
						// definition names should be unique
						FeatureClassDefinition definition = definitions[dataset.Name].FirstOrDefault();

						ISourceClass result = CreateSourceClass(dataset, definition);

						_workspacesBySourceClass.Add(result, workspace);

						yield return result;
					}
				}
			}
		}

		protected virtual IEnumerable<IWorkItem> GetItemsCore([NotNull] ISourceClass sourceClass, [CanBeNull] QueryFilter filter, bool recycle)
		{
			FeatureClass featureClass = OpenFeatureClass(sourceClass);

			if (featureClass == null)
			{
				yield break;
			}
			
			// Todo daro: check recycle
			foreach (Feature feature in GdbQueryUtils.GetRows<Feature>(
				featureClass, filter, recycle))
			{
				yield return CreateWorkItemCore(feature, sourceClass);
			}
		}

		[CanBeNull]
		protected virtual DatabaseStatusSchema CreateStatusSchemaCore()
		{
			return null;
		}

		[NotNull]
		protected abstract IAttributeReader CreateAttributeReaderCore([NotNull] FeatureClassDefinition definition);

		[NotNull]
		protected abstract IWorkItem CreateWorkItemCore([NotNull] Row row, ISourceClass source);

		[NotNull]
		protected abstract ISourceClass CreateSourceClassCore(GdbTableIdentity identity,
		                                                      [NotNull] IAttributeReader attributeReader,
		                                                      [CanBeNull] DatabaseStatusSchema statusSchema = null);

		[CanBeNull]
		private FeatureClass OpenFeatureClass([NotNull] ISourceClass sourceClass)
		{

			return _workspacesBySourceClass.TryGetValue(sourceClass, out IWorkspaceContext workspace)
				       ? workspace.OpenFeatureClass(sourceClass.Name)
				       : null;
		}

		private ISourceClass CreateSourceClass(GdbTableIdentity table, FeatureClassDefinition definition)
		{
			IAttributeReader attributeReader = CreateAttributeReaderCore(definition);

			DatabaseStatusSchema statusSchema = CreateStatusSchemaCore();

			ISourceClass sourceClass = CreateSourceClassCore(table, attributeReader, statusSchema);

			return sourceClass;
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

		protected virtual int CreateItemIDCore(Row row, ISourceClass source)
		{
			long oid = row.GetObjectID();

			// oid = 666, tableId = 42 => 42666
			return (int) (Math.Pow(10, Math.Floor(Math.Log10(oid) + 1)) * source.Identity.Id + oid);
		}
	}
}
