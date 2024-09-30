using System.Collections.Generic;

namespace ProSuite.GIS.Geodatabase.API
{
	public interface IObjectClass : IClass
	{
		long ObjectClassID { get; }

		IEnumerable<IRelationshipClass> get_RelationshipClasses(esriRelRole role);

		string AliasName { get; }
	}
}
