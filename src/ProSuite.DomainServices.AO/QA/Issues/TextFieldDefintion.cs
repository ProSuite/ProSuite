using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class TextFieldDefintion : FieldDefinition
	{
		public TextFieldDefintion([NotNull] string name,
		                          int length,
		                          [CanBeNull] string aliasName = null)
			: base(name, aliasName, esriFieldType.esriFieldTypeString, length) { }
	}
}
