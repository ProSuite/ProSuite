using System.ComponentModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class ScalarProperty<T> : ParameterProperty<ScalarParameterConfig<T>>,
	                                 IRevertibleChangeTracking
	{
		private T _orig;
		private bool _isChanged;

		/// <summary>
		/// Initializes a new instance of the <see cref="ScalarProperty&lt;T&gt;"/> class.
		/// </summary>
		public ScalarProperty() : base(new ScalarParameterConfig<T>()) { }

		protected override void InitTestAttributeValue()
		{
			string parameterName = GetAttributeName();

			var testParam = new TestParameter(parameterName, typeof(T));

			TestParameterValue testValue = new ScalarTestParameterValue(
				testParam, string.Format("{0}", default(T)));

			GetParameterConfig().SetTestParameterValue(testValue);
		}

		[DisplayName("Value")]
		public virtual T Scalar
		{
			get
			{
				ScalarParameterConfig<T> config = GetParameterConfig();
				return config.Scalar;
			}
			set
			{
				if (_isChanged == false)
				{
					_orig = Scalar;
					_isChanged = true;
				}

				ScalarParameterConfig<T> config = GetParameterConfig();
				config.Scalar = value;
			}
		}

		[Browsable(false)]
		public bool IsChanged
		{
			get { return _isChanged; }
		}

		public void AcceptChanges()
		{
			_isChanged = false;
		}

		public void RejectChanges()
		{
			if (IsChanged)
			{
				Scalar = _orig;
			}

			_isChanged = false;
		}
	}
}
