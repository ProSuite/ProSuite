using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using EsriDE.ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.AGP.WorkList.Contracts
{
	[CLSCompliant(false)]
	public interface IWorkItemRepository
	{
		IEnumerable<KeyValuePair<IWorkItem, IReadOnlyList<Coordinate3D>>> GetItems(
			QueryFilter filter, bool recycle);

		IEnumerable<IWorkItem> GetAll();

		void Register(IObjectDataset dataset, DbStatusSchema statusSchema = null);
	}
}
