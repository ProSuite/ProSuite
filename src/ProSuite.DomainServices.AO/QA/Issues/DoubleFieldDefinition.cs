using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class DoubleFieldDefinition : FieldDefinition
	{
		public DoubleFieldDefinition([NotNull] string name,
		                             [CanBeNull] string aliasName = null)
			: base(name, aliasName, esriFieldType.esriFieldTypeDouble) { }
	}
}
