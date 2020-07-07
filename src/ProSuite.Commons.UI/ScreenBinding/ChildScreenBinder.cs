using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.ScreenBinding
{
	public class ChildScreenBinder<T> : IBoundPart where T : class
	{
		[NotNull] private readonly PropertyInfo _property;

		/// <summary>
		/// Initializes a new instance of the <see cref="ChildScreenBinder&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <param name="monitor">The monitor.</param>
		public ChildScreenBinder([NotNull] PropertyInfo property,
		                         [NotNull] IValidationMonitor monitor)
		{
			Assert.ArgumentNotNull(property, nameof(property));
			Assert.ArgumentNotNull(monitor, nameof(monitor));

			_property = property;
			InnerBinder = new ScreenBinder<T>(monitor);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ChildScreenBinder&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="innerBinder">The inner binder.</param>
		/// <param name="property">The property.</param>
		public ChildScreenBinder([NotNull] IScreenBinder innerBinder,
		                         [NotNull] PropertyInfo property)
		{
			Assert.ArgumentNotNull(innerBinder, nameof(innerBinder));
			Assert.ArgumentNotNull(property, nameof(property));

			InnerBinder = innerBinder;
			_property = property;
		}

		public IScreenBinder InnerBinder { get; }

		#region IBoundPart Members

		public void Bind(object model)
		{
			object target = _property.GetValue(model, null);
			InnerBinder.BindToModel(target);
		}

		public bool ApplyChanges()
		{
			return InnerBinder.ApplyChangesToModel();
		}

		public void Reset()
		{
			InnerBinder.ResetToOriginalValues();
		}

		public void Update()
		{
			InnerBinder.UpdateScreen();
		}

		public IScreenBinder Binder
		{
			set
			{
				// no-op 
			}
		}

		public string FieldName => _property.Name;

		public void StopBinding() { }

		public void SetDefaults()
		{
			InnerBinder.SetDefaultValues();
		}

		public bool IsDirty()
		{
			return InnerBinder.IsDirty();
		}

		#endregion
	}
}
