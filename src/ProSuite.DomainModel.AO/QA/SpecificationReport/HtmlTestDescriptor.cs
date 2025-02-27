using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.DomainModel.AO.QA.SpecificationReport
{
	public class HtmlTestDescriptor
	{
		[CanBeNull] private readonly string _obsoleteMessage;

		[NotNull] private readonly List<HtmlTestParameter> _parameters =
			new List<HtmlTestParameter>();

		[NotNull] private readonly Dictionary<string, HtmlTestParameter>
			_testParametersByName = new Dictionary<string, HtmlTestParameter>();

		[NotNull] private readonly List<HtmlQualitySpecificationElement>
			_referencingElements = new List<HtmlQualitySpecificationElement>();

		private bool _referencingElementsDirty;

		[NotNull] private readonly List<IssueCode> _issueCodes;
		[NotNull] private readonly List<string> _testCategories;

		internal HtmlTestDescriptor([NotNull] TestDescriptor testDescriptor)
		{
			Assert.ArgumentNotNull(testDescriptor, nameof(testDescriptor));

			IInstanceInfo instanceInfo =
				Assert.NotNull(InstanceDescriptorUtils.GetInstanceInfo(testDescriptor));

			Name = testDescriptor.Name;
			Description = StringUtils.IsNotEmpty(testDescriptor.Description)
				              ? testDescriptor.Description
				              : null;

			TestDescription = instanceInfo.TestDescription;
			Signature = InstanceUtils.GetTestSignature(instanceInfo);

			Type testType;
			if (testDescriptor.TestClass != null)
			{
				testType = testDescriptor.TestClass.GetInstanceType();
				ConstructorId = testDescriptor.TestConstructorId;
				UsesConstructor = true;
				IsObsolete = InstanceUtils.IsObsolete(testType, ConstructorId,
				                                      out _obsoleteMessage);
			}
			else if (testDescriptor.TestFactoryDescriptor != null)
			{
				testType = testDescriptor.TestFactoryDescriptor.GetInstanceType();
				ConstructorId = -1;
				UsesConstructor = false;
				IsObsolete = InstanceUtils.IsObsolete(testType, out _obsoleteMessage);
			}
			else
			{
				throw new ArgumentException("Invalid test descriptor");
			}

			AssemblyName = Path.GetFileName(testType.Assembly.Location);
			ClassName = testType.FullName;

			_issueCodes = IssueCodeUtils.GetIssueCodes(testType).ToList();
			_testCategories = instanceInfo.TestCategories.OrderBy(c => c).ToList();

			foreach (TestParameter testParameter in instanceInfo.Parameters)
			{
				var htmlTestParameter = new HtmlTestParameter(testParameter);

				_parameters.Add(htmlTestParameter);
				_testParametersByName.Add(testParameter.Name, htmlTestParameter);
			}
		}

		[CanBeNull]
		[UsedImplicitly]
		public string Description { get; private set; }

		[CanBeNull]
		[UsedImplicitly]
		public string TestDescription { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public string Name { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public string Signature { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public string AssemblyName { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public string ClassName { get; private set; }

		[UsedImplicitly]
		public bool UsesConstructor { get; private set; }

		[UsedImplicitly]
		public int ConstructorId { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public List<IssueCode> IssueCodes => _issueCodes;

		[UsedImplicitly]
		public bool IsObsolete { get; private set; }

		[CanBeNull]
		[UsedImplicitly]
		public string ObsoleteMessage => _obsoleteMessage;

		[NotNull]
		[UsedImplicitly]
		public List<string> TestCategories => _testCategories;

		[NotNull]
		[UsedImplicitly]
		public List<HtmlTestParameter> Parameters => _parameters;

		[UsedImplicitly]
		public bool HasReferencingElements => _referencingElements.Count > 0;

		[NotNull]
		[UsedImplicitly]
		public List<HtmlQualitySpecificationElement> ReferencingElements
		{
			get
			{
				if (_referencingElementsDirty)
				{
					_referencingElements.Sort(new HtmlQualitySpecificationElementComparer());
					_referencingElementsDirty = false;
				}

				return _referencingElements;
			}
		}

		[CanBeNull]
		internal HtmlTestParameter GetParameter([NotNull] string parameterName)
		{
			HtmlTestParameter result;
			return ! _testParametersByName.TryGetValue(parameterName, out result)
				       ? null // test does not (anymore) now about the parameter
				       : result;
		}

		internal void AddReferencingElement(
			[NotNull] HtmlQualitySpecificationElement htmlElement)
		{
			Assert.ArgumentNotNull(htmlElement, nameof(htmlElement));

			_referencingElementsDirty = true;
			_referencingElements.Add(htmlElement);
		}
	}
}
