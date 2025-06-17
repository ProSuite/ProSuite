using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.AGP.WorkList.Test;

public class WorkListItemDatastoreMock : IWorkListItemDatastore
{
	public bool Validate(out string message)
	{
		throw new NotImplementedException();
	}

	public IEnumerable<Table> GetTables()
	{
		throw new NotImplementedException();
	}

	public Task<bool> TryPrepareSchema()
	{
		throw new NotImplementedException();
	}

	public Task<IList<Table>> PrepareTableSchema(IList<Table> dbTables)
	{
		throw new NotImplementedException();
	}

	public IAttributeReader CreateAttributeReader(TableDefinition definition, params Attributes[] attributes)
	{
		throw new NotImplementedException();
	}

	public DbSourceClassSchema CreateStatusSchema(TableDefinition tableDefinition)
	{
		throw new NotImplementedException();
	}

	public string SuggestWorkListName()
	{
		throw new NotImplementedException();
	}

	public bool ContainsSourceClass(ISourceClass sourceClass)
	{
		throw new NotImplementedException();
	}

	public IObjectDataset GetObjetDataset(TableDefinition tableDefinition)
	{
		throw new NotImplementedException();
	}
}
