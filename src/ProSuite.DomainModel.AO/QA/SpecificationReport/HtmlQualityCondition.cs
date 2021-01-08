using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.AO.QA.SpecificationReport
{
	public class HtmlQualityCondition
	{
		[NotNull] private readonly QualityCondition _qualityCondition;

		[NotNull] private readonly List<HtmlTestParameterValue> _parameterValues =
			new List<HtmlTestParameterValue>();

		internal HtmlQualityCondition([NotNull] QualityCondition qualityCondition,
		                              [NotNull] HtmlTestDescriptor testDescriptor,
		                              [NotNull] HtmlDataQualityCategory category)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			_qualityCondition = qualityCondition;
			TestDescriptor = testDescriptor;
			Category = category;

			Description = StringUtils.IsNotEmpty(qualityCondition.Description)
				              ? qualityCondition.Description
				              : null;
			Uuid = qualityCondition.Uuid;
			VersionUuid = qualityCondition.VersionUuid;

			string url = qualityCondition.Url;

			if (url != null && StringUtils.IsNotEmpty(url))
			{
				UrlText = url;
				UrlLink = SpecificationReportUtils.GetCompleteUrl(url);
			}

			foreach (TestParameterValue value in qualityCondition.ParameterValues)
			{
				HtmlTestParameter parameter = testDescriptor.GetParameter(value.TestParameterName);

				if (parameter == null)
				{
					// test parameter was deleted/renamed -> ignore value
					continue;
				}

				_parameterValues.Add(new HtmlTestParameterValue(value, parameter));
			}
		}

		[NotNull]
		[UsedImplicitly]
		public string Name
		{
			get { return _qualityCondition.Name; }
		}

		[CanBeNull]
		[UsedImplicitly]
		public string Description { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public string Uuid { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public HtmlDataQualityCategory Category { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public string VersionUuid { get; private set; }

		[UsedImplicitly]
		public bool IsAllowable
		{
			get { return _qualityCondition.AllowErrors; }
		}

		[CanBeNull]
		[UsedImplicitly]
		public string UrlText { get; private set; }

		[CanBeNull]
		[UsedImplicitly]
		public string UrlLink { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public HtmlTestDescriptor TestDescriptor { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public List<HtmlTestParameterValue> ParameterValues
		{
			get { return _parameterValues; }
		}
	}
}