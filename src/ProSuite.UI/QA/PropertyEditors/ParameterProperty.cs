using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.UI.QA.PropertyEditors
{
	public abstract class ParameterProperty<T> : ParameterPropertyBase
		where T : ParameterConfig
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ParameterProperty&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="parameterConfig">The parameter config.</param>
		protected ParameterProperty([NotNull] T parameterConfig) : base(parameterConfig) { }

		public new T GetParameterConfig()
		{
			return (T) base.GetParameterConfig();
		}

		protected void SetParameterConfig(T parameterConfig)
		{
			base.SetParameterConfig(parameterConfig);
		}
	}
}
