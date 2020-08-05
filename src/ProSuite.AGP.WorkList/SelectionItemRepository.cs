using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.DomainModel.DataModel;

namespace ProSuite.AGP.WorkList
{
	internal class SelectionItemRepository : GdbItemRepository, ISelectionItemRepository
	{
		private readonly Dictionary<ISourceClass, List<long>> _oidsBySource =
			new Dictionary<ISourceClass, List<long>>();

		public SelectionItemRepository(
			IEnumerable<IWorkspaceContext> workspaces) : base(workspaces) { }

		void ISelectionItemRepository.RegisterDatasets(Dictionary<GdbTableReference, List<long>> selection)
		{
			foreach (ISourceClass dataset in RegisterDatasetsCore(selection.Keys))
			{
				_oidsBySource.Add(dataset, selection[dataset.Identity]);
			}
		}

		protected override IAttributeReader CreateAttributeReaderCore(
			FeatureClassDefinition definition)
		{
			return new AttributeReader(definition);
		}

		protected override IWorkItem CreateWorkItemCore(Row row, ISourceClass source)
		{
			int id = CreateItemIDCore(row, source);

			return new SelectionItem(id, row, source.AttributeReader);
		}

		protected override ISourceClass CreateSourceClassCore(GdbTableReference identity,
		                                                      IAttributeReader attributeReader,
		                                                      DatabaseStatusSchema statusSchema = null)
		{
			return new SelectionSourceClass(identity, attributeReader);
		}

		protected override IEnumerable<IWorkItem> GetItemsCore(ISourceClass sourceClass,
		                                                       QueryFilter filter, bool recycle)
		{
			Assert.True(_oidsBySource.TryGetValue(sourceClass, out List<long> oids),
			            "unexpected source class");

			if (filter == null)
			{
				filter = new QueryFilter {ObjectIDs = new ReadOnlyCollection<long>(oids)};
			}

			if (filter is SpatialQueryFilter spatialFilter)
			{
				spatialFilter.SearchOrder = SearchOrder.Attribute;
			}

			return base.GetItemsCore(sourceClass, filter, recycle);
		}
	}
}
