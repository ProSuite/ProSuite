
using System.Collections.Generic;

namespace ESRI.ArcGIS.Geodatabase
{
	public interface IObjectClass : IClass
	{
		long ObjectClassID { get; }

		IEnumerable<IRelationshipClass> get_RelationshipClasses(esriRelRole role);

		string AliasName { get; }
	}
}
