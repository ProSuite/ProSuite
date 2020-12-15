using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.AO.QA.SpecificationReport
{
	public class HtmlQualitySpecification
	{
		[NotNull] private readonly List<HtmlDataQualityCategory> _categories;
		[NotNull] private readonly List<HtmlQualitySpecificationElement> _elements;
		[NotNull] private readonly List<HtmlTestDescriptor> _testDescriptors;
		[NotNull] private readonly List<HtmlDataQualityCategory> _rootCategories;
		[NotNull] private readonly List<HtmlDataModel> _dataModels;

		internal HtmlQualitySpecification(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] IEnumerable<HtmlDataQualityCategory> categories,
			[NotNull] IEnumerable<HtmlQualitySpecificationElement> elements,
			[NotNull] IEnumerable<HtmlTestDescriptor> testDescriptors,
			[NotNull] IEnumerable<HtmlDataModel> dataModels,
			DateTime reportCreationDate)
		{
			Name = qualitySpecification.Name;
			Description = StringUtils.IsNotEmpty(qualitySpecification.Description)
				              ? qualitySpecification.Description
				              : null;
			Uuid = qualitySpecification.Uuid;
			ReportCreationDate = reportCreationDate;

			string url = qualitySpecification.Url;

			if (url != null && StringUtils.IsNotEmpty(url))
			{
				UrlText = url;
				UrlLink = SpecificationReportUtils.GetCompleteUrl(url);
			}

			if (qualitySpecification.TileSize != null)
			{
				HasTileSize = true;
				TileSize = qualitySpecification.TileSize.Value;
			}
			else
			{
				HasTileSize = false;
				TileSize = -1;
			}

			// collections are assumed to be ordered already
			_categories = categories.ToList();
			_elements = elements.ToList();
			_testDescriptors = testDescriptors.ToList();
			_rootCategories = _categories.Where(c => c.IsRoot)
			                             .ToList();
			_dataModels = dataModels.ToList();
		}

		[UsedImplicitly]
		public bool ShowQualityConditionUuids { get; set; }

		[UsedImplicitly]
		public string Name { get; private set; }

		[UsedImplicitly]
		public string Uuid { get; private set; }

		[UsedImplicitly]
		public string UrlText { get; private set; }

		[UsedImplicitly]
		public string UrlLink { get; private set; }

		[UsedImplicitly]
		public string Description { get; private set; }

		[UsedImplicitly]
		public bool HasTileSize { get; private set; }

		[UsedImplicitly]
		public double TileSize { get; private set; }

		[UsedImplicitly]
		public DateTime ReportCreationDate { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public List<HtmlDataQualityCategory> RootCategories
		{
			get { return _rootCategories; }
		}

		[NotNull]
		[UsedImplicitly]
		public List<HtmlDataQualityCategory> Categories
		{
			get { return _categories; }
		}

		[NotNull]
		[UsedImplicitly]
		public List<HtmlQualitySpecificationElement> Elements
		{
			get { return _elements; }
		}

		[NotNull]
		[UsedImplicitly]
		public List<HtmlTestDescriptor> TestDescriptors
		{
			get { return _testDescriptors; }
		}

		[NotNull]
		[UsedImplicitly]
		public List<HtmlDataModel> DataModels
		{
			get { return _dataModels; }
		}
	}
}
