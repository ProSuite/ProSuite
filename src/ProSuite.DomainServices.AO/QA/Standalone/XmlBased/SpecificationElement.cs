using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.QA.Xml;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased
{
	/// <summary>
	/// Data record for simplified information transfer in condition-list based
	/// quality specification building. Consider switching to a C# 9 record in the future.
	/// </summary>
	public class SpecificationElement
	{
		public SpecificationElement([NotNull] XmlQualityCondition xmlCondition,
		                            [CanBeNull] string categoryName)
		{
			XmlCondition = xmlCondition;
			CategoryName = categoryName;
		}

		[NotNull]
		public XmlQualityCondition XmlCondition { get; set; }

		[CanBeNull]
		public string CategoryName { get; set; }

		public bool AllowErrors { get; set; }

		public bool StopOnError { get; set; }
	}
}
