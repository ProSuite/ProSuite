using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Finder;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.DdxEditor.Content.QA.InstanceConfig;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Content.QA.TestDescriptors;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	public class QualityConditionPresenter :
		EntityItemPresenter<QualityCondition, IQualityConditionObserver, QualityCondition>,
		IQualityConditionObserver
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly QualityConditionItem _item;
		[NotNull] private readonly IQualityConditionView _view;
		[NotNull] private readonly IItemNavigation _itemNavigation;
		private bool _testFactoryHasError;

		[NotNull] private readonly BindingList<ParameterValueListItem> _paramValues
			= new BindingList<ParameterValueListItem>();

		[NotNull] private readonly SortableBindingList<QualitySpecificationReferenceTableRow>
			_qspecTableRows =
				new SortableBindingList<QualitySpecificationReferenceTableRow>();

		[NotNull] private readonly SortableBindingList<InstanceConfigurationReferenceTableRow>
			_issueFilterTableRows =
				new SortableBindingList<InstanceConfigurationReferenceTableRow>();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityConditionPresenter"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="view">The view.</param>
		/// <param name="itemNavigation">The item navigation service.</param>
		public QualityConditionPresenter([NotNull] QualityConditionItem item,
		                                 [NotNull] IQualityConditionView view,
		                                 [NotNull] IItemNavigation itemNavigation)
			: base(item, view)
		{
			Assert.ArgumentNotNull(itemNavigation, nameof(itemNavigation));

			_item = item;
			_view = view;
			_itemNavigation = itemNavigation;

			_view.Observer = this;
			_view.FindTestDescriptorDelegate = () => FindTestDescriptor(view);
		}

		#endregion

		void IQualityConditionObserver.OnTestDescriptorChanged()
		{
			QualityCondition qualityCondition = Assert.NotNull(_item.GetEntity());

			if (TestParameterValueUtils.SyncParameterValues(qualityCondition))
			{
				InitParameterData(qualityCondition);
			}

			EnableImportExport(qualityCondition);

			SetViewData();

			_testFactoryHasError = false;
		}

		void IQualityConditionObserver.SetTestParameterValues(
			IList<TestParameterValue> values)
		{
			QualityCondition qualityCondition = Assert.NotNull(_item.GetEntity());
			qualityCondition.ClearParameterValues();

			foreach (TestParameterValue value in values)
			{
				qualityCondition.AddParameterValue(value);
			}
		}

		ITestConfigurator IQualityConditionObserver.GetTestConfigurator()
		{
			return _item.GetConfigurator();
		}

		BindingList<ParameterValueListItem> IQualityConditionObserver.GetTestParameterItems()
		{
			GetTestParameterItems(Assert.NotNull(_item.GetEntity()));
			return _paramValues;
		}

		void IQualityConditionObserver.ExportQualityCondition(string exportFileName)
		{
			_item.ExportQualityCondition(exportFileName);
		}

		void IQualityConditionObserver.ImportQualityCondition(string importFileName)
		{
			_item.ImportQualityCondition(importFileName);

			InitParameterData(Assert.NotNull(_item.GetEntity()));
		}

		void IQualityConditionObserver.AssignToQualitySpecificationsClicked()
		{
			QualityCondition qualityCondition = Assert.NotNull(Item.GetEntity());

			IList<QualitySpecificationTableRow> specTableRows =
				_item.GetQualitySpecificationsToReference(qualityCondition, _view);

			if (specTableRows == null || specTableRows.Count == 0)
			{
				// nothing selected
				return;
			}

			bool anyChange = false;

			foreach (QualitySpecificationTableRow specTableRow in specTableRows)
			{
				QualitySpecification spec = specTableRow.QualitySpecification;

				if (spec.Contains(qualityCondition))
				{
					_msg.WarnFormat(
						"The quality condition {0} is already contained in quality specification {1}",
						qualityCondition.Name, spec.Name);
				}
				else
				{
					var element = spec.AddElement(qualityCondition);
					anyChange = true;
					_qspecTableRows.Add(new QualitySpecificationReferenceTableRow(
						                    spec, element));
				}
			}

			_view.BindToQualitySpecificationReferences(_qspecTableRows);
			_view.SelectQualitySpecifications(specTableRows.Select(r => r.QualitySpecification));

			RenderQualitySpecificationSummary();

			if (anyChange)
			{
				Item.NotifyChanged();
			}
		}

		void IQualityConditionObserver.RemoveFromQualitySpecificationsClicked()
		{
			// get selected targets
			IList<QualitySpecificationReferenceTableRow> selected =
				_view.GetSelectedQualitySpecificationReferenceTableRows();

			// remove them from the entity
			foreach (QualitySpecificationReferenceTableRow tableRow in selected)
			{
				tableRow.QualitySpecification.RemoveElement(
					tableRow.QualitySpecificationElement);

				_qspecTableRows.Remove(tableRow);
			}

			RenderQualitySpecificationSummary();

			Item.NotifyChanged();
		}

		void IQualityConditionObserver.QualitySpecificationSelectionChanged()
		{
			UpdateQualitySpecificationsAppearance();
		}

		public void IssueFilterSelectionChanged()
		{
			UpdateIssueFiltersAppearance();
		}

		public void AddIssueFilterClicked()
		{
			QualityCondition qualityCondition = Assert.NotNull(Item.GetEntity());

			IList<InstanceConfigurationTableRow> filterTableRows =
				_item.GetIssueFiltersToAdd(qualityCondition, _view);

			if (filterTableRows == null || filterTableRows.Count == 0)
			{
				// nothing selected
				return;
			}

			bool anyChange = false;

			foreach (InstanceConfigurationTableRow filterRow in filterTableRows)
			{
				IssueFilterConfiguration filterConfig =
					(IssueFilterConfiguration) filterRow.InstanceConfiguration;

				if (qualityCondition.IssueFilterConfigurations.Contains(filterConfig))
				{
					_msg.WarnFormat(
						"The quality condition {0} already uses the issue filter {1}",
						qualityCondition.Name, filterConfig.Name);
				}
				else
				{
					qualityCondition.AddIssueFilterConfiguration(filterConfig);
					anyChange = true;
					_issueFilterTableRows.Add(
						new InstanceConfigurationReferenceTableRow(filterConfig));
				}
			}

			_view.BindToIssueFilters(_issueFilterTableRows);
			_view.SelectIssueFilters(
				filterTableRows.Select(r => (IssueFilterConfiguration) r.InstanceConfiguration));

			if (anyChange)
			{
				Item.NotifyChanged();
			}
		}

		public void RemoveIssueFilterClicked()
		{
			// get selected targets
			IList<InstanceConfigurationReferenceTableRow> selected =
				_view.GetSelectedIssueFilterTableRows();

			QualityCondition qualityCondition = Assert.NotNull(Item.GetEntity());

			// remove them from the entity
			bool anyChange = false;
			foreach (InstanceConfigurationReferenceTableRow tableRow in selected)
			{
				var filterToRemove = (IssueFilterConfiguration) tableRow.InstanceConfig;

				qualityCondition.RemoveIssueFilterConfiguration(filterToRemove);

				_issueFilterTableRows.Remove(tableRow);

				anyChange = true;
			}

			if (anyChange)
			{
				Item.NotifyChanged();
			}
		}

		void IQualityConditionObserver.QualitySpecificationReferenceDoubleClicked(
			QualitySpecificationReferenceTableRow qualitySpecificationReferenceTableRow)
		{
			_itemNavigation.GoToItem(
				qualitySpecificationReferenceTableRow.QualitySpecification);
		}

		public void IssueFilterDoubleClicked(
			InstanceConfigurationReferenceTableRow filterConfigTableRow)
		{
			_itemNavigation.GoToItem(filterConfigTableRow.InstanceConfig);
		}

		void IQualityConditionObserver.GoToTestDescriptorClicked(
			TestDescriptor testDescriptor)
		{
			if (testDescriptor == null)
			{
				return;
			}

			_itemNavigation.GoToItem(testDescriptor);
		}

		void IQualityConditionObserver.OpenUrlClicked()
		{
			_item.OpenUrl();
		}

		void IQualityConditionObserver.NewVersionUuidClicked()
		{
			if (_view.Confirm(
				    "Are you sure to assign a new version UUID to this quality condition?" +
				    Environment.NewLine +
				    Environment.NewLine +
				    "This invalidates all exceptions defined for the Geoprocessing tool 'Quality Verification (xml-based)'",
				    "Assign new version UUID"))
			{
				_item.AssignNewVersionUuid();
				_view.UpdateScreen();
			}
		}

		public void DescriptorDocumentationLinkClicked()
		{
			_item.ExecuteWebHelpCommand();
		}

		public string GenerateName()
		{
			InstanceConfiguration instanceConfiguration = Assert.NotNull(_item.GetEntity());

			string generatedName = InstanceConfigurationUtils.GenerateName(instanceConfiguration);

			if (generatedName == null)
			{
				_msg.Warn("Test Descriptor or dataset parameter has not yet been configured. " +
				          "Cannot generate name.");
			}

			return generatedName;
		}

		protected override void OnBoundTo(QualityCondition qualityCondition)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			// called on initial load, on Save and on Discard (NOT: on add/remove qspecs)

			try
			{
				TestParameterValueUtils.SyncParameterValues(qualityCondition);
			}
			catch (Exception e)
			{
				_msg.WarnFormat(e.Message);
				_testFactoryHasError = true;
			}

			base.OnBoundTo(qualityCondition);

			InitParameterData(qualityCondition);
			EnableImportExport(qualityCondition);

			SetViewData();

			PopulateQualitySpecificationReferenceTableRows(_qspecTableRows);
			PopulateIssueFilterTableRows(_issueFilterTableRows);

			RenderQualitySpecificationReferences();

			RenderIssueFilters();

			_view.RenderCategory(qualityCondition.Category == null
				                     ? string.Empty
				                     : qualityCondition.Category.GetQualifiedName());
		}

		protected override void OnUnloaded()
		{
			_view.SaveState();

			base.OnUnloaded();
		}

		private void UpdateQualitySpecificationsAppearance()
		{
			_view.RemoveFromQualitySpecificationsEnabled =
				_view.HasSelectedQualitySpecificationReferences;
		}

		private void UpdateIssueFiltersAppearance()
		{
			_view.RemoveIssueFilterEnabled = _view.HasSelectedIssueFilter;
		}

		private void RenderQualitySpecificationReferences()
		{
			_view.BindToQualitySpecificationReferences(_qspecTableRows);

			RenderQualitySpecificationSummary();
		}

		private void RenderIssueFilters()
		{
			_view.BindToIssueFilters(_issueFilterTableRows);

			//RenderQualitySpecificationSummary();
		}

		private void RenderQualitySpecificationSummary()
		{
			var sb = new StringBuilder();

			foreach (QualitySpecificationReferenceTableRow row in _qspecTableRows)
			{
				if (sb.Length > 0)
				{
					sb.Append("; ");
				}

				sb.Append(row.QualitySpecificationName);
			}

			_view.QualitySpecificationSummary = sb.Length == 0
				                                    ? "<no quality specification uses this condition>"
				                                    : sb.ToString();
		}

		private void PopulateQualitySpecificationReferenceTableRows(
			[NotNull] ICollection<QualitySpecificationReferenceTableRow> tableRows)
		{
			Assert.ArgumentNotNull(tableRows, nameof(tableRows));

			tableRows.Clear();

			foreach (
				KeyValuePair<QualitySpecification, QualitySpecificationElement> pair in
				_item.GetQualitySpecificationReferences())
			{
				tableRows.Add(
					new QualitySpecificationReferenceTableRow(pair.Key, pair.Value));
			}
		}

		private void PopulateIssueFilterTableRows(
			ICollection<InstanceConfigurationReferenceTableRow> issueFilterTableRows)
		{
			QualityCondition qualityCondition = Assert.NotNull(_item.GetEntity());

			issueFilterTableRows.Clear();

			foreach (IssueFilterConfiguration issueFilterConfig in qualityCondition
				         .IssueFilterConfigurations)
			{
				issueFilterTableRows.Add(
					new InstanceConfigurationReferenceTableRow(issueFilterConfig));
			}
		}

		[CanBeNull]
		private TestDescriptor FindTestDescriptor(IWin32Window owner)
		{
			IList<TestDescriptorTableRow> list = _item.GetTestDescriptorTableRows();

			var finder = new Finder<TestDescriptorTableRow>();

			TestDescriptorTableRow tableRow = finder.ShowDialog(owner, list);

			return tableRow?.TestDescriptor;
		}

		private void InitParameterData([NotNull] QualityCondition qualityCondition)
		{
			GetTestParameterItems(qualityCondition);

#if NET6_0_OR_GREATER
			_view.TableViewControl.BindTo(qualityCondition);
#else
			_view.BindToParameterValues(_paramValues);
			_view.SetConfigurator(_testFactoryHasError
				                      ? null
				                      : _item.GetConfigurator());
#endif
		}

		private void GetTestParameterItems([NotNull] QualityCondition qualityCondition)
		{
			_paramValues.Clear();

			if (_testFactoryHasError)
			{
				return;
			}

			foreach (TestParameterValue paramValue in qualityCondition.ParameterValues)
			{
				_paramValues.Add(new ParameterValueListItem(paramValue));
			}
		}

		private void EnableImportExport([NotNull] QualityCondition qualityCondition)
		{
			if (! _testFactoryHasError)
			{
				TestFactory factory = TestFactoryUtils.CreateTestFactory(qualityCondition);
				if (factory != null)
				{
					Type factoryType = factory.GetType();

					_view.ExportEnabled = IsOverridden(factoryType,
					                                   nameof(factory.Export));
					_view.ImportEnabled = IsOverridden(factoryType,
					                                   nameof(factory.CreateQualityCondition));
				}

				return;
			}

			_view.ExportEnabled = false;
			_view.ImportEnabled = false;
		}

		private static bool IsOverridden([NotNull] Type type, [NotNull] string methodName)
		{
			Assert.ArgumentNotNull(type, nameof(type));
			Assert.ArgumentNotNullOrEmpty(methodName, nameof(methodName));

			MethodInfo thisMethod = type.GetMethod(methodName);

			Type baseType = type;
			while (baseType.BaseType != null &&
			       baseType.BaseType.GetMethod(methodName) != null)
			{
				baseType = baseType.BaseType;
			}

			//check if overriden
			Type thisType = thisMethod?.DeclaringType;
			bool overridden = baseType != thisType;
			return overridden;
		}

		private void SetViewData()
		{
			_view.SetTestDescription(_testFactoryHasError
				                         ? null
				                         : _item.GetTestDescription());

			IList<TestParameter> testParams = _testFactoryHasError
				                                  ? null
				                                  : _item.GetParameterDescription();

			_view.SetParameterDescriptions(testParams);

			TestDescriptor testDescriptor = _item.GetTestDescriptor();

			if (testDescriptor != null)
			{
				_view.IssueTypeDefault = testDescriptor.AllowErrors
					                         ? "Warning"
					                         : "Error";
				_view.StopOnErrorDefault = testDescriptor.StopOnError
					                           ? "Yes"
					                           : "No";
				_view.GoToTestDescriptorEnabled = true;
			}
			else
			{
				_view.IssueTypeDefault = string.Empty;
				_view.StopOnErrorDefault = string.Empty;
				_view.GoToTestDescriptorEnabled = false;
			}

			string html = _item.GetWebHelp(testDescriptor, out string title);
			_itemNavigation.UpdateItemHelp(title, html ?? string.Empty);
		}
	}
}
