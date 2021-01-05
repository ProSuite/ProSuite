using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class IntegerFieldDefinition : FieldDefinition
	{
		public IntegerFieldDefinition([NotNull] string name,
		                              [CanBeNull] string aliasName = null)
			: base(name, aliasName, esriFieldType.esriFieldTypeInteger) { }
	}
}
