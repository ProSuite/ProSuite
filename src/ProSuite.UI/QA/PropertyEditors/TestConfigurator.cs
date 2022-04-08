using System;
using System.Collections.Generic;
using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.PropertyEditors;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.QA.PropertyEditors
{
	public abstract class TestConfigurator : ITestConfigurator, IDataChanged
	{
		[CanBeNull] private IQualityConditionContextAware _context;
		[CanBeNull] private QualityCondition _qualityCondition;

		#region IDataChanged Members

		public virtual void OnDataChanged(EventArgs e)
		{
			if (DataChanged != null)
			{
				DataChanged(this, null);
			}
		}

		#endregion

		#region ITestConfigurator Members

		public event EventHandler DataChanged;

		[Browsable(false)]
		public ITestParameterDatasetProvider DatasetProvider { get; set; }

		[Browsable(false)]
		public QualityCondition QualityCondition
		{
			get { return _qualityCondition; }
			set
			{
				_qualityCondition = value;
				SetQualityCondition(value);
			}
		}

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

		public abstract IList<TestParameterValue> GetTestParameterValues();

		public abstract string GetTestDescription();

		public virtual Type GetTestClassType()
		{
			return null;
		}

		public virtual int GetTestConstructorId()
		{
			return -1;
		}

		public virtual Type GetFactoryType()
		{
			return null;
		}

		public void SyncParameterValues()
		{
			QualityCondition qualityCondition = _qualityCondition;
			if (qualityCondition == null)
			{
				return;
			}

			var existDict = new Dictionary<TestParameterValue, bool>();
			foreach (TestParameterValue value in qualityCondition.ParameterValues)
			{
				existDict.Add(value, false);
			}

			IList<TestParameterValue> newValues = GetTestParameterValues();

			var missings = new List<TestParameterValue>();
			foreach (TestParameterValue newValue in newValues)
			{
				if (existDict.ContainsKey(newValue))
				{
					existDict[newValue] = true;
				}
				else
				{
					missings.Add(newValue);
				}
			}

			foreach (KeyValuePair<TestParameterValue, bool> keyValuePair in existDict)
			{
				if (keyValuePair.Value == false)
				{
					qualityCondition.RemoveParameterValue(keyValuePair.Key);
				}
			}

			foreach (TestParameterValue missing in missings)
			{
				qualityCondition.AddParameterValue(missing);
			}
		}

		#endregion

		protected virtual void SetQualityCondition(QualityCondition qualityCondition) { }

		protected void testParameter_DataChanged(object sender, EventArgs e)
		{
			OnDataChanged(e);
		}
	}
}
