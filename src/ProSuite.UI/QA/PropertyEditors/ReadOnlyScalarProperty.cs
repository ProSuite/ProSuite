using System.ComponentModel;
using System.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class ReadOnlyScalarProperty<T> : ParameterProperty<ScalarParameterConfig<T>>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ScalarProperty&lt;T&gt;"/> class.
		/// </summary>
		public ReadOnlyScalarProperty() : base(new ScalarParameterConfig<T>()) { }

		protected override void InitTestAttributeValue()
		{
			string parameterName = GetAttributeName();

			var testParam = new TestParameter(parameterName, typeof(T));

			TestParameterValue testValue = new ScalarTestParameterValue(
				testParam, string.Format("{0}", default(T)));

			GetParameterConfig().SetTestParameterValue(testValue);
		}

		[DisplayName("Value")]
		[ReadOnly(true)]
		[UsedImplicitly]
		public virtual T Scalar
		{
			get { return GetParameterConfig().Scalar; }
			set { throw new ReadOnlyException(); }
		}
	}
}
