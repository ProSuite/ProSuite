using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.QA;
#if ArcGIS || Server
using ProSuite.DomainModel.AO.QA;
using ProSuite.UI.QA.PropertyEditors;
#endif

namespace ProSuite.UI.QA.Controls
{
	public partial class QualityConditionParametersControl : UserControl
	{
		private QualityCondition _qualityCondition;
		private bool _readOnly;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityConditionParametersControl"/> class.
		/// </summary>
		public QualityConditionParametersControl()
		{
			InitializeComponent();

			_propertyGrid.ToolbarVisible = false;
			_readOnly = true;
		}

		[Browsable(false)]
		public bool AutoSyncQualityCondition { get; set; }

		[CanBeNull]
		[Browsable(false)]
		public QualityCondition QualityCondition
		{
			set
			{
				if (AutoSyncQualityCondition)
				{
					SyncQualityCondition();
				}

				_qualityCondition = value;
				SetPropertyGrid(value, _readOnly);
			}
			get { return _qualityCondition; }
		}

		[CanBeNull]
		public ITestParameterDatasetProvider TestParameterDatasetProvider { get; set; }

		private void SetPropertyGrid([CanBeNull] QualityCondition condition,
		                             bool readOnly)
		{
			if (condition == null)
			{
				return;
			}

#if ArcGIS || Server

			TestFactory factory = TestFactoryUtils.CreateTestFactory(condition);
			if (factory == null)
			{
				return;
			}

			if (readOnly)
			{
				condition = (QualityCondition) condition.CreateCopy();
			}

			TestParameterValueUtils.SyncParameterValues(condition);

			TestDescriptor testDescriptor = condition.TestDescriptor;

			string testAssemblyName = testDescriptor != null &&
			                          (testDescriptor.TestClass != null ||
			                           testDescriptor.TestFactoryDescriptor != null)
				                          ? testDescriptor.AssemblyName
				                          : null;

			ITestConfigurator testConfigurator =
				DefaultTestConfiguratorFactory.CreateTestConfigurator(
					"ConfigTest", factory.Parameters, testAssemblyName, readOnly);

			// TODO: set readonly state of propteryGrid 

			testConfigurator.DatasetProvider = TestParameterDatasetProvider;
			testConfigurator.QualityCondition = condition;

			SetConfigurator(testConfigurator);

#else
			throw new NotImplementedException(
				"Legacy property editors with AO dependency not supported in current environment");
#endif
		}

		public bool ReadOnly
		{
			get { return _readOnly; }
			set { _readOnly = value; }
		}

		public void SyncQualityCondition()
		{
			var configurator = _propertyGrid.SelectedObject as ITestConfigurator;
			configurator?.SyncParameterValues();
		}

		private void SetConfigurator([CanBeNull] ITestConfigurator configurator)
		{
			// configurator may be null

			var old = _propertyGrid.SelectedObject as ITestConfigurator;
			if (old != null)
			{
				old.DataChanged -= configurator_DataChanged;
			}

			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
			try
			{
				_propertyGrid.SelectedObject = configurator;
			}
			catch (NullReferenceException)
			{
				// Bug in property Grid !?, do it again
				_propertyGrid.SelectedObject = configurator;
			}
			finally
			{
				AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
			}

			if (configurator != null)
			{
				configurator.DataChanged += configurator_DataChanged;
			}

			_propertyGrid.ExpandAllGridItems();
		}

		private static Assembly CurrentDomain_AssemblyResolve(object sender,
		                                                      ResolveEventArgs args)
		{
			return AssemblyResolveUtils.TryLoadAssembly(
				args.Name, Assembly.GetExecutingAssembly().CodeBase, _msg.Debug);
		}

		private void configurator_DataChanged(object sender, EventArgs e)
		{
			//ITestConfigurator configurator = (ITestConfigurator)sender;
			//_observer.SetTestParameterValues(configurator.GetTestParameterValues());

			//			_observer.NotifyChanged(true);
		}

		private void _propertyGrid_PropertyValueChanged(object s,
		                                                PropertyValueChangedEventArgs e)
		{
			var configurator = (ITestConfigurator) _propertyGrid.SelectedObject;

			//_observer.SetTestParameterValues(configurator.GetTestParameterValues());

			//_observer.NotifyChanged(true);
		}
	}
}
