using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	internal class TestDescriptorPresenter :
		EntityItemPresenter<TestDescriptor, ITestDescriptorObserver, TestDescriptor>,
		ITestDescriptorObserver
	{
		[NotNull] private readonly ITestDescriptorView _view;
		[NotNull] private readonly IItemNavigation _itemNavigation;
		[NotNull] private readonly TestDescriptorItem _item;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly SortableBindingList<ReferencingQualityConditionTableRow>
			_qconTableRows =
				new SortableBindingList<ReferencingQualityConditionTableRow>();

		/// <summary>
		/// Initializes a new instance of the <see cref="TestDescriptorPresenter"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="view">The view.</param>
		/// <param name="findTestFactory">The find test factory delegate.</param>
		/// <param name="findTestClass">The find test class delegate.</param>
		/// <param name="findTestConfigurator">The find test configurator delegate.</param>
		/// <param name="itemNavigation">The item navigation helper.</param>
		public TestDescriptorPresenter(
			[NotNull] TestDescriptorItem item,
			[NotNull] ITestDescriptorView view,
			[NotNull] ClassDescriptorProvider findTestFactory,
			[NotNull] ClassDescriptorProvider findTestClass,
			[NotNull] ClassDescriptorProvider findTestConfigurator,
			[NotNull] IItemNavigation itemNavigation)
			: base(item, view)
		{
			Assert.ArgumentNotNull(itemNavigation, nameof(itemNavigation));

			_item = item;
			_view = view;
			_itemNavigation = itemNavigation;

			_view.Observer = this;

			_view.FindTestFactoryDelegate =
				() => findTestFactory(_view,
				                      Assert
					                      .NotNull(_item.GetEntity())
					                      .TestFactoryDescriptor);

			_view.FindTestClassDelegate =
				() => findTestClass(_view,
				                    Assert.NotNull(_item.GetEntity()).TestClass);

			_view.FindTestConfiguratorDelegate =
				() => findTestConfigurator(_view,
				                           Assert
					                           .NotNull(_item.GetEntity())
					                           .TestConfigurator);
		}

		protected override void OnBoundTo(TestDescriptor entity)
		{
			base.OnBoundTo(entity);

			SetViewData();

			PopulateQualityConditionTableRows(_qconTableRows);

			RenderQualityConditions();
		}

		protected override void OnUnloaded()
		{
			_view.SaveState();

			base.OnUnloaded();
		}

		void ITestDescriptorObserver.NotifyFactoryChanged()
		{
			_view.RefreshFactoryElements();
			SetViewData();
		}

		void ITestDescriptorObserver.QualityConditionDoubleClicked(
			ReferencingQualityConditionTableRow referencingQualityConditionTableRow)
		{
			_itemNavigation.GoToItem(referencingQualityConditionTableRow
				                         .QualityCondition);
		}

		private void PopulateQualityConditionTableRows(
			ICollection<ReferencingQualityConditionTableRow> tableRows)
		{
			Assert.ArgumentNotNull(tableRows, nameof(tableRows));

			tableRows.Clear();

			foreach (QualityCondition qualityCondition in _item.GetQualityConditions())
			{
				tableRows.Add(new ReferencingQualityConditionTableRow(qualityCondition));
			}
		}

		private void RenderQualityConditions()
		{
			_view.BindToQualityConditions(_qconTableRows);
		}

		private void SetViewData()
		{
			// TODO handle validation/valid state in item

			string testDescription;
			string[] testCategories;
			IList<TestParameter> testParameters;

			try
			{
				testParameters = _item.GetTestParameters(out testDescription, out testCategories);
			}
			catch (Exception e)
			{
				_msg.WarnFormat(e.Message);

				testDescription = string.Empty;
				testCategories = Array.Empty<string>();
				testParameters = new List<TestParameter>();
			}

			_view.RenderTestDescription(testDescription);
			_view.RenderTestCategories(testCategories);
			_view.RenderTestParameters(testParameters);
		}
	}
}
