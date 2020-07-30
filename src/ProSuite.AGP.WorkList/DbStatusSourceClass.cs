using System;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.DomainModel.DataModel;

namespace ProSuite.AGP.WorkList
{
	[CLSCompliant(false)]
	public class DbStatusSourceClass
	{
		public IObjectDataset Dataset { get; }
		public DbStatusSchema StatusSchema { get; }

		public DbStatusSourceClass(IObjectDataset dataset, DbStatusSchema statusSchema)
		{
			Dataset = dataset;
			StatusSchema = statusSchema;
		}
	}
}
