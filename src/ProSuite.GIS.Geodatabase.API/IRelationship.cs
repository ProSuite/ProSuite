using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ESRI.ArcGIS.Geodatabase
{
	public interface IRelationship
	{
		IRelationshipClass RelationshipClass {get; }

		IObject OriginObject {  get; }

		IObject DestinationObject { get; }
	}
}
