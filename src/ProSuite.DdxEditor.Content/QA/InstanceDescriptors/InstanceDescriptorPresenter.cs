using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.DdxEditor.Content.QA.TestDescriptors;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	internal class InstanceDescriptorPresenter :
		EntityItemPresenter<InstanceDescriptor, IInstanceDescriptorObserver, InstanceDescriptor>,
		IInstanceDescriptorObserver
	{
		[NotNull] private readonly IInstanceDescriptorView _view;
		[NotNull] private readonly IItemNavigation _itemNavigation;
		[NotNull] private readonly InstanceDescriptorItem _item;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly SortableBindingList<ReferencingInstanceConfigurationTableRow>
			_configurationTableRows =
				new SortableBindingList<ReferencingInstanceConfigurationTableRow>();

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceDescriptorPresenter"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="view">The view.</param>
		/// <param name="findTestClass">The find test class delegate.</param>
		/// <param name="itemNavigation">The item navigation helper.</param>
		public InstanceDescriptorPresenter(
			[NotNull] InstanceDescriptorItem item,
			[NotNull] IInstanceDescriptorView view,
			[NotNull] ClassDescriptorProvider findTestClass,
			[NotNull] IItemNavigation itemNavigation)
			: base(item, view)
		{
			Assert.ArgumentNotNull(itemNavigation, nameof(itemNavigation));

			_item = item;
			_view = view;
			_itemNavigation = itemNavigation;

			_view.Observer = this;

			_view.FindClassDelegate =
				() => findTestClass(_view,
				                    Assert.NotNull(_item.GetEntity()).Class);
		}

		protected override void OnBoundTo(InstanceDescriptor entity)
		{
			base.OnBoundTo(entity);

			SetViewData();

			PopulateInstanceConfigurationTableRows(_configurationTableRows);

			RenderInstanceConfigurations();
		}

		protected override void OnUnloaded()
		{
			_view.SaveState();

			base.OnUnloaded();
		}

		void IInstanceDescriptorObserver.NotifyFactoryChanged()
		{
			_view.RefreshFactoryElements();
			SetViewData();
		}

		public void InstanceConfigurationDoubleClicked(
			ReferencingInstanceConfigurationTableRow referencingConfigurationTableRow)
		{
			_itemNavigation.GoToItem(referencingConfigurationTableRow.InstanceConfiguration);
		}

		private void PopulateInstanceConfigurationTableRows(
			ICollection<ReferencingInstanceConfigurationTableRow> tableRows)
		{
			Assert.ArgumentNotNull(tableRows, nameof(tableRows));

			tableRows.Clear();

			foreach (InstanceConfiguration config in _item.GetInstanceConfigurations())
			{
				// TODO:
				tableRows.Add(new ReferencingInstanceConfigurationTableRow(config));
			}
		}

		private void RenderInstanceConfigurations()
		{
			_view.BindToInstanceConfigurations(_configurationTableRows);
		}

		private void SetViewData()
		{
			// TODO handle validation/valid state in item

			string instanceDescription;
			string[] instanceCategories;
			IList<TestParameter> testParameters;

			try
			{
				testParameters =
					_item.GetTestParameters(out instanceDescription, out instanceCategories);
			}
			catch (Exception e)
			{
				_msg.WarnFormat(e.Message);

				instanceDescription = string.Empty;
				instanceCategories = Array.Empty<string>();
				testParameters = new List<TestParameter>();
			}

			_view.RenderInstanceDescription(instanceDescription);
			_view.RenderInstanceCategories(instanceCategories);
			_view.RenderTestParameters(testParameters);
		}
	}
}
