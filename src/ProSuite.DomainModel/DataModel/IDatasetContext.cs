using System;
using ArcGIS.Core.Data;

namespace ProSuite.DomainModel.DataModel
{
	public interface IDatasetContext : IDisposable
	{
		FeatureClass OpenFeatureClass(string name);
		Table OpenTable(string name);
	}
}
