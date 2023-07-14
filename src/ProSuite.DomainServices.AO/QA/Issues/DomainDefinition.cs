using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	// TODO: Consider making Field definitions and Domain definitions AO-free and use them
	//       also in Pro issue persistence.
	public class DomainDefinition
	{
		public DomainDefinition(string name, esriFieldType fieldType)
		{
			Name = name;
			FieldType = fieldType;
		}

		public string Name { get; }

		public esriFieldType FieldType { get; }
	}

	public class CodedValueDomainDefinition : DomainDefinition
	{
		public IList<CodedValue> CodedValues { get; }

		public CodedValueDomainDefinition([NotNull] string name,
		                                  esriFieldType fieldType,
		                                  IList<CodedValue> codedValues) : base(name, fieldType)
		{
			CodedValues = codedValues;
		}
	}
}
