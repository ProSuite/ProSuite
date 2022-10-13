using System.ComponentModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.UI.QA.PropertyEditors
{
	public abstract class ScalarParameterConfig : ParameterConfig { }

	public class ScalarParameterConfig<T> : ScalarParameterConfig
	{
		[DisplayName("Value")]
		public virtual T Scalar
		{
			get
			{
				var scalar = (ScalarTestParameterValue) GetTestParameterValue();
				return (T) scalar.GetValue(typeof(T));
			}
			set { SetValue(value); }
		}

		public void SetValue(object value)
		{
			TestParameterValue scalar = GetTestParameterValue();
			if (scalar == null)
			{
				var testParam = new TestParameter(GetAttributeName(), typeof(T));

				scalar = new ScalarTestParameterValue(testParam, value.ToString());

				SetTestParameterValue(scalar);
			}
			else
			{
				scalar.StringValue = value.ToString();
			}

			OnDataChanged(null);
		}
	}
}
