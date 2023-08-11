using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public abstract class FieldDefinition
	{
		protected FieldDefinition([NotNull] string name,
		                          [CanBeNull] string aliasName,
		                          esriFieldType type,
		                          int length = 0)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			Assert.ArgumentCondition(length > 0 || type != esriFieldType.esriFieldTypeString,
			                         "Invalid length for text field");
			Assert.ArgumentCondition(length == 0 || type == esriFieldType.esriFieldTypeString,
			                         "Invalid length for non-text field");

			Name = name;
			AliasName = aliasName ?? name;
			Type = type;
			Length = length;
		}

		[NotNull]
		public string Name { get; }

		[NotNull]
		public string AliasName { get; }

		public esriFieldType Type { get; }

		public int Length { get; }

		[CanBeNull]
		public DomainDefinition Domain { get; set; }
	}
}
