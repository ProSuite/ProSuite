using System;
using System.ComponentModel;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.PropertyEditors;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.QA.PropertyEditors
{
	public abstract class ParameterPropertyBase : IQualityConditionContextAware,
	                                              IAttributeInfoProvider
	{
		[CanBeNull] private ParameterConfig _parameterConfig;
		private IQualityConditionContextAware _context;
		private string _attributeName;

		/// <summary>
		/// Initializes a new instance of the <see cref="ParameterPropertyBase"/> class.
		/// </summary>
		/// <param name="parameterConfig">The parameter config.</param>
		protected ParameterPropertyBase([NotNull] ParameterConfig parameterConfig)
		{
			Assert.ArgumentNotNull(parameterConfig, nameof(parameterConfig));

			_parameterConfig = parameterConfig;
		}

		public event EventHandler DataChanged;

		[Browsable(false)]
		public ITestParameterDatasetProvider DatasetProvider { get; set; }

		[Browsable(false)]
		public QualityCondition QualityCondition { get; set; }

		public object GetContext()
		{
			return _context;
		}

		public void SetContext(object context)
		{
			DatasetProvider = null;
			_context = context as IQualityConditionContextAware;

			if (context != null && _context == null)
			{
				throw new InvalidOperationException(
					string.Format("Expected {0}, got {1}",
					              typeof(IQualityConditionContextAware),
					              context.GetType()));
			}

			if (_context != null)
			{
				DatasetProvider = _context.DatasetProvider;
				QualityCondition = _context.QualityCondition;
			}
		}

		public void SetAttributeName(string attributeName)
		{
			_attributeName = attributeName;
			InitTestAttributeValue();
		}

		public void OnDataChanged(EventArgs args)
		{
			if (DataChanged != null)
			{
				DataChanged(this, args);
			}
		}

		public ParameterConfig GetParameterConfig()
		{
			return _parameterConfig;
		}

		protected abstract void InitTestAttributeValue();

		protected string GetAttributeName()
		{
			return _attributeName;
		}

		protected void SetParameterConfig([CanBeNull] ParameterConfig parameterConfig)
		{
			_parameterConfig = parameterConfig;
		}

		public override string ToString()
		{
			return _parameterConfig == null
				       ? "{null}"
				       : _parameterConfig.ToString();
		}

		//#region Implementation of IChangeTracking

		//public void AcceptChanges()
		//{
		//    _isChanged = false;
		//}

		//public bool IsChanged
		//{
		//    get { return _isChanged; }
		//    protected set { _isChanged = value; }
		//}

		//public void RejectChanges()
		//{

		//}

		//#endregion
	}
}
