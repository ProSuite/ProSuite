using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.QA.ServiceManager.Interfaces;

namespace ProSuite.AGP.Solution.QA
{
	public class XmlVerificationEnvironment : IQualityVerificationEnvironment
	{
		private readonly IQASpecificationProvider _specificationProvider;

		public XmlVerificationEnvironment(
			IQASpecificationProvider specificationProvider)
		{
			_specificationProvider = specificationProvider;

			QualitySpecifications = new List<QualitySpecificationReference>();

			GetSpecifications();
		}

		public QualitySpecificationReference CurrentQualitySpecification { get; set; }

		public IList<QualitySpecificationReference> QualitySpecifications { get; }

		public void RefreshQualitySpecifications()
		{
			QualitySpecifications.Clear();
		}

		public event EventHandler QualitySpecificationsRefreshed;

		public string BackendDisplayName { get; }

		public SpatialReference SpatialReference { get; }

		public Task<ServiceCallStatus> VerifyExtent(Envelope extent,
		                                            QualityVerificationProgressTracker progress,
		                                            string resultsPath)
		{
			throw new NotImplementedException();
		}

		public Task<ServiceCallStatus> VerifyExtent(Envelope extent)
		{
			throw new NotImplementedException();
		}

		private void GetSpecifications()
		{
			IList<string> list = _specificationProvider.GetQASpecificationNames();

			for (var i = 0; i < list.Count; i++)
			{
				QualitySpecifications.Add(new QualitySpecificationReference(i, list[i]));
			}
		}
	}
}
