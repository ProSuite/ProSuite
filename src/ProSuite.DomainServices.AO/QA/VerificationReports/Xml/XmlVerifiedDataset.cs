using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	public class XmlVerifiedDataset : IEquatable<XmlVerifiedDataset>
	{
		private readonly VerifiedCategoriesBuilder _categoriesBuilder =
			new VerifiedCategoriesBuilder();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlVerifiedDataset"/> class.
		/// </summary>
		[UsedImplicitly]
		public XmlVerifiedDataset() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlVerifiedDataset"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="workspaceName">Name of the workspace.</param>
		public XmlVerifiedDataset([NotNull] string name, [CanBeNull] string workspaceName)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			Name = name;
			WorkspaceName = workspaceName;
		}

		#endregion

		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlAttribute("workspace")]
		public string WorkspaceName { get; set; }

		[XmlAttribute("warningCount")]
		public int WarningCount { get; set; }

		[XmlAttribute("errorCount")]
		public int ErrorCount { get; set; }

		[XmlAttribute("stopErrorCount")]
		public int StopErrorCount { get; set; }

		[XmlAttribute("geometryType")]
		public string GeometryType { get; set; }

		[XmlAttribute("verifiedConditionCount")]
		public int VerifiedConditionCount { get; set; }

		[XmlAttribute("coordinateSystem")]
		public string CoordinateSystem { get; set; }

		[XmlAttribute("tolerance")]
		public double Tolerance { get; set; }

		[XmlAttribute("resolution")]
		public double Resolution { get; set; }

		[XmlArray("VerifiedConditions")]
		[XmlArrayItem("Category")]
		[CanBeNull]
		public List<XmlVerifiedCategory> VerifiedCategories
			=> _categoriesBuilder.RootCategories.Count == 0
				   ? null
				   : _categoriesBuilder.RootCategories;

		public void AddVerifiedCondition(
			[NotNull] XmlVerifiedQualityCondition verifiedCondition)
		{
			VerifiedConditionCount++;

			_categoriesBuilder.AddVerifiedCondition(verifiedCondition);
		}

		public bool Equals(XmlVerifiedDataset other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(other.Name, Name) && Equals(other.WorkspaceName, WorkspaceName);
		}
	}
}
