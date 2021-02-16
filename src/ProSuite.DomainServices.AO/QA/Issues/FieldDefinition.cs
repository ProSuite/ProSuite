using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
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
		private string AliasName { get; }

		private esriFieldType Type { get; }

		private int Length { get; }

		[NotNull]
		public IField CreateField()
		{
			return Type == esriFieldType.esriFieldTypeString
				       ? FieldUtils.CreateTextField(Name, Length, AliasName)
				       : FieldUtils.CreateField(Name, Type, AliasName);
		}
	}
}
