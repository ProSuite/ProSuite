using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Finder;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.DdxEditor.Content.QA.TestDescriptors;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.QA;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public class InstanceConfigurationPresenter :
		EntityItemPresenter<InstanceConfiguration, IInstanceConfigurationObserver,
			InstanceConfiguration>,
		IInstanceConfigurationObserver
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly InstanceConfigurationItem _item;
		[NotNull] private readonly IInstanceConfigurationView _view;
		[NotNull] private readonly IItemNavigation _itemNavigation;
		private bool _testFactoryHasError;

		[NotNull] private readonly BindingList<ParameterValueListItem> _paramValues
			= new BindingList<ParameterValueListItem>();

		[NotNull] private readonly SortableBindingList<InstanceConfigurationReferenceTableRow>
			_qspecTableRows =
				new SortableBindingList<InstanceConfigurationReferenceTableRow>();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceConfigurationPresenter"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="view">The view.</param>
		/// <param name="itemNavigation">The item navigation service.</param>
		public InstanceConfigurationPresenter([NotNull] InstanceConfigurationItem item,
		                                      [NotNull] IInstanceConfigurationView view,
		                                      [NotNull] IItemNavigation itemNavigation)
			: base(item, view)
		{
			Assert.ArgumentNotNull(itemNavigation, nameof(itemNavigation));

			_item = item;
			_view = view;
			_itemNavigation = itemNavigation;

			_view.Observer = this;
			_view.FindInstanceDescriptorDelegate = () => FindInstanceDescriptor(view);
		}

		#endregion

		#region IInstanceConfigurationObserver implementation

		void IInstanceConfigurationObserver.OnInstanceDescriptorChanged()
		{
			InstanceConfiguration instanceConfig = Assert.NotNull(_item.GetEntity());
			InstanceFactory instanceFactory = InstanceFactoryUtils.CreateFactory(instanceConfig);

			if (TestParameterValueUtils.SyncParameterValues(instanceConfig, instanceFactory))
			{
				InitParameterData(instanceConfig);
			}

			SetViewData();

			_testFactoryHasError = false;
		}

		void IInstanceConfigurationObserver.SetTestParameterValues(
			IList<TestParameterValue> values)
		{
			InstanceConfiguration instanceConfiguration = Assert.NotNull(_item.GetEntity());
			instanceConfiguration.ClearParameterValues();

			foreach (TestParameterValue value in values)
			{
				instanceConfiguration.AddParameterValue(value);
			}
		}

		BindingList<ParameterValueListItem> IInstanceConfigurationObserver.GetTestParameterItems()
		{
			GetTestParameterItems(Assert.NotNull(_item.GetEntity()));
			return _paramValues;
		}

		void IInstanceConfigurationObserver.InstanceReferenceDoubleClicked(
			InstanceConfigurationReferenceTableRow qualitySpecificationReferenceTableRow)
		{
			_itemNavigation.GoToItem(
				qualitySpecificationReferenceTableRow.InstanceConfig);
		}

		void IInstanceConfigurationObserver.InstanceDescriptorLinkClicked(
			InstanceDescriptor instanceDescriptor)
		{
			if (instanceDescriptor == null)
			{
				return;
			}

			_itemNavigation.GoToItem(instanceDescriptor);
		}

		void IInstanceConfigurationObserver.OpenUrlClicked()
		{
			_item.OpenUrl();
		}

		#endregion

		protected override void OnBoundTo(InstanceConfiguration instanceConfiguration)
		{
			Assert.ArgumentNotNull(instanceConfiguration, nameof(instanceConfiguration));
			// called on initial load, on Save and on Discard

			try
			{
				InstanceConfiguration instanceConfig = Assert.NotNull(_item.GetEntity());
				InstanceFactory instanceFactory =
					InstanceFactoryUtils.CreateFactory(instanceConfig);

				TestParameterValueUtils.SyncParameterValues(instanceConfig, instanceFactory);
			}
			catch (Exception e)
			{
				_msg.WarnFormat(e.Message);
				_testFactoryHasError = true;
			}

			base.OnBoundTo(instanceConfiguration);

			InitParameterData(instanceConfiguration);

			SetViewData();

			PopulateQualityConditionReferenceTableRows(_qspecTableRows);

			RenderQualitySpecificationReferences();

			_view.RenderCategory(instanceConfiguration.Category == null
				                     ? string.Empty
				                     : instanceConfiguration.Category.GetQualifiedName());
		}

		protected override void OnUnloaded()
		{
			_view.SaveState();

			base.OnUnloaded();
		}

		private void RenderQualitySpecificationReferences()
		{
			_view.BindToQualityConditionReferences(_qspecTableRows);

			RenderQualitySpecificationSummary();
		}

		private void RenderQualitySpecificationSummary()
		{
			var sb = new StringBuilder();

			foreach (InstanceConfigurationReferenceTableRow row in _qspecTableRows)
			{
				if (sb.Length > 0)
				{
					sb.Append("; ");
				}

				sb.Append(row.InstanceConfig.Name);
			}

			_view.ReferenceingInstancesSummary = sb.Length == 0
				                                     ? "<no quality condition, transformer or filter uses this instance>"
				                                     : sb.ToString();
		}

		private void PopulateQualityConditionReferenceTableRows(
			[NotNull] ICollection<InstanceConfigurationReferenceTableRow> tableRows)
		{
			Assert.ArgumentNotNull(tableRows, nameof(tableRows));

			tableRows.Clear();

			foreach (InstanceConfiguration instanceConfig in _item.GetReferencingInstances())
			{
				tableRows.Add(new InstanceConfigurationReferenceTableRow(instanceConfig));
			}
		}

		[CanBeNull]
		private InstanceDescriptor FindInstanceDescriptor(IWin32Window owner)
		{
			IList<InstanceDescriptorTableRow> list = _item.GetInstanceDescriptorTableRows();

			var finder = new Finder<InstanceDescriptorTableRow>();

			InstanceDescriptorTableRow tableRow = finder.ShowDialog(owner, list);

			return tableRow?.InstanceDescriptor;
		}

		private void InitParameterData([NotNull] InstanceConfiguration instanceConfig)
		{
			GetTestParameterItems(instanceConfig);

			_view.BindToParameterValues(_paramValues);

#if NET6_0
			_view.TableViewControl.BindTo(instanceConfig);
#endif
		}

		private void GetTestParameterItems([NotNull] InstanceConfiguration qualityCondition)
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

		private void SetViewData()
		{
			_view.SetDescription(_testFactoryHasError
				                     ? null
				                     : _item.GetInstanceDescription());

			IList<TestParameter> testParams = _testFactoryHasError
				                                  ? null
				                                  : _item.GetParameterDescription();

			_view.SetParameterDescriptions(testParams);

			InstanceDescriptor instanceDescriptor = _item.GetInstanceDescriptor();

			_view.InstanceDescriptorLinkEnabled = instanceDescriptor != null;
		}
	}
}
