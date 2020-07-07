using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;
using ProSuite.Commons.UI.ScreenBinding.Configuration;
using ProSuite.Commons.UI.ScreenBinding.Elements;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.Commons.UI.ScreenBinding.ScreenStates;

namespace ProSuite.Commons.UI.ScreenBinding
{
	public class ScreenBinder<M> : IScreenBinder where M : class
	{
		private readonly List<IScreenElement> _allElements = new List<IScreenElement>();
		private readonly List<IBoundPart> _boundElements = new List<IBoundPart>();
		private readonly IValidationMonitor _monitor;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ScreenBinder&lt;M&gt;"/> class.
		/// </summary>
		public ScreenBinder() : this(new NulloValidationMonitor()) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ScreenBinder&lt;M&gt;"/> class.
		/// </summary>
		/// <param name="monitor">The monitor.</param>
		public ScreenBinder([NotNull] IValidationMonitor monitor)
		{
			Assert.ArgumentNotNull(monitor, nameof(monitor));

			_monitor = monitor;
			_monitor.Binder = this;
		}

		#endregion

		#region IScreenBinder Members

		public void AddElement(IScreenElement element)
		{
			Assert.ArgumentNotNull(element, nameof(element));

			_allElements.Add(element);
			var boundElement = element as IBoundPart;
			if (boundElement == null)
			{
				return;
			}

			boundElement.Binder = this;
			_boundElements.Add(boundElement);

			if (Model != null)
			{
				boundElement.Bind(Model);
			}
		}

		public void InsideLatch(Action action)
		{
			bool wasLatched = IsLatched;
			IsLatched = true;
			try
			{
				action();
			}
			finally
			{
				IsLatched = wasLatched;
			}
		}

		public Action OnChange { set; get; } = delegate { };

		public void MakeReadOnly(bool readOnly)
		{
			IScreenState state = readOnly
				                     ? (IScreenState) new DisableAllScreenState()
				                     : new EnableAllScreenState();
			EnableControls(state);
		}

		public bool IsLatched { get; private set; }

		public void EnableControls(IScreenState state)
		{
			IsLatched = true;
			_allElements.ForEach(element => element.EnableControl(state));
			IsLatched = false;
		}

		public void MessageElements(Action<IScreenElement> action)
		{
			_allElements.ForEach(action);
		}

		public void ResetToOriginalValues()
		{
			_boundElements.ForEach(element => element.Reset());
		}

		public void UpdateScreen()
		{
			_boundElements.ForEach(element => element.Update());
			_allElements.ForEach(element => element.UpdateDisplayState(Model));
		}

		public void Validate(IBoundScreenElement element)
		{
			Assert.ArgumentNotNull(element, nameof(element));

			if (Model == null)
			{
				return;
			}

			_monitor.ValidateField(element, Model);
		}

		public void ShowErrorMessages(IBoundScreenElement element,
		                              params string[] messages)
		{
			Assert.ArgumentNotNull(element, nameof(element));
			Assert.ArgumentNotNull(messages, nameof(messages));

			_monitor.ShowErrorMessages(element, messages);
		}

		public void Focus(object control)
		{
			Assert.ArgumentNotNull(control, nameof(control));

			IScreenElement element = FindElementForControl(control);

			if (element == null)
			{
				return;
			}

			bool oldLatched = IsLatched;
			IsLatched = true;
			try
			{
				element.Focus();
			}
			finally
			{
				IsLatched = oldLatched;
			}
		}

		public void Focus(string label)
		{
			IScreenElement element = FindElement(label);

			if (element == null)
			{
				return;
			}

			var control = element.Control as Control;

			if (control != null)
			{
				Focus(control);
			}
		}

		public void BindToModel(object target)
		{
			BindToModel((M) target);
		}

		public bool ApplyChangesToModel()
		{
			var returnValue = true;

			_boundElements.ForEach(
				delegate(IBoundPart element)
				{
					returnValue = returnValue && element.ApplyChanges();
				}
			);

			return returnValue;
		}

		public IScreenElement FindElementForControl(object control)
		{
			return _allElements.Find(element => (element.Control == control));
		}

		public IScreenElement FindElement(string labelOrAlias)
		{
			return _allElements.Find(element => element.Matches(labelOrAlias));
		}

		public IBoundScreenElement FindElementByField(string fieldName)
		{
			return (IBoundScreenElement) _boundElements.Find(
				boundPart => (Equals(boundPart.FieldName, fieldName)));
		}

		public void SetDefaultValues()
		{
			_boundElements.ForEach(element => element.SetDefaults());
		}

		IEnumerator<IScreenElement> IEnumerable<IScreenElement>.GetEnumerator()
		{
			return _allElements.GetEnumerator();
		}

		public IEnumerator GetEnumerator()
		{
			return ((IEnumerable<IScreenElement>) this).GetEnumerator();
		}

		public bool IsDirty()
		{
			if (Model == null)
			{
				return false;
			}

			foreach (IBoundPart element in _boundElements)
			{
				if (! element.IsDirty())
				{
					continue;
				}

				if (_msg.IsVerboseDebugEnabled)
				{
					_msg.DebugFormat("Element for field {0} is dirty", element.FieldName);
				}

				return true;
			}

			return false;
		}

		#endregion

		public void Hide([NotNull] string propertyName)
		{
			Assert.ArgumentNotNullOrEmpty(propertyName, nameof(propertyName));

			string fieldName = ReflectionUtils.GetProperty<M>(propertyName).Name;

			IBoundScreenElement element = FindElementByField(fieldName);
			Assert.NotNull(element, "element not found for {0}", propertyName);

			element.Hide();
		}

		public void Hide([NotNull] Expression<Func<M, object>> expression)
		{
			string fieldName = ReflectionHelper.GetProperty(expression).Name;

			IBoundScreenElement element = FindElementByField(fieldName);
			Assert.NotNull(element, "element not found for {0}", fieldName);

			element.Hide();
		}

		public void Show([NotNull] string propertyName)
		{
			Assert.ArgumentNotNullOrEmpty(propertyName, nameof(propertyName));

			string fieldName = ReflectionUtils.GetProperty<M>(propertyName).Name;

			IBoundScreenElement element = FindElementByField(fieldName);
			Assert.NotNull(element, "element not found for {0}", propertyName);

			element.Show();
		}

		public void Show([NotNull] Expression<Func<M, object>> expression)
		{
			string fieldName = ReflectionHelper.GetProperty(expression).Name;

			IBoundScreenElement element = FindElementByField(fieldName);
			Assert.NotNull(element, "element not found for {0}", fieldName);

			element.Show();
		}

		public void CopySettings([NotNull] IScreenDriver driver)
		{
			_allElements.ForEach(e => e.CopyFrom(driver));
		}

		[NotNull]
		public ScreenBinder<CHILD> AddChildBinder<CHILD>([NotNull] string propertyName)
			where CHILD : class
		{
			Assert.ArgumentNotNullOrEmpty(propertyName, nameof(propertyName));

			PropertyInfo property = ReflectionUtils.GetProperty<M>(propertyName);

			return CreateChildScreenBinder<CHILD>(property);
		}

		[NotNull]
		public ScreenBinder<CHILD> AddChildBinder<CHILD>(
			[NotNull] Expression<Func<M, object>> expression) where CHILD : class
		{
			PropertyInfo property = ReflectionHelper.GetProperty(expression);

			return CreateChildScreenBinder<CHILD>(property);
		}

		public void FillList<T>([NotNull] object control,
		                        [NotNull] IEnumerable<T> list,
		                        [NotNull] string propertyName)
			where T : IComparable
		{
			Assert.ArgumentNotNullOrEmpty(propertyName, nameof(propertyName));

			var picklist = new Picklist<T>(list);
			picklist.DisplayMember = ReflectionUtils.GetProperty<T>(propertyName).Name;

			var element = (IListElement) FindElementForControl(control);
			Assert.NotNull(element, "element not found");

			element.FillWithList(picklist);
		}

		public void FillList<T>([NotNull] object control,
		                        [NotNull] IEnumerable<T> list,
		                        [NotNull] Expression<Func<T, object>> expression)
			where T : IComparable
		{
			var picklist = new Picklist<T>(list);
			picklist.DisplayMember = ReflectionHelper.GetProperty(expression).Name;

			var element = (IListElement) FindElementForControl(control);
			Assert.NotNull(element, "element not found for {0}", control);

			element.FillWithList(picklist);
		}

		/// <summary>
		/// Establishes the bindings to a given model instance.
		/// </summary>
		/// <param name="model">The model.</param>
		/// <remarks>This should also be called after the changes to the model were somehow <i>applied</i>
		/// (e.g. persisted). Only during this call is the new baseline for the IsDirty check established.
		/// </remarks>
		public void BindToModel([NotNull] M model)
		{
			Assert.ArgumentNotNull(model, nameof(model));

			Model = model;
			IsLatched = true;

			try
			{
				_boundElements.ForEach(element => element.Bind(model));

				// execute display actions
				_allElements.ForEach(element => element.UpdateDisplayState(model));
			}
			finally
			{
				IsLatched = false;
			}
		}

		public M Model { get; private set; }

		[NotNull]
		public PropertyBindingExpression Bind([NotNull] string propertyName)
		{
			Assert.ArgumentNotNullOrEmpty(propertyName, nameof(propertyName));

			IPropertyAccessor accessor = GetAccessor(propertyName);

			return new PropertyBindingExpression(this, accessor);
		}

		// to simplify cases where there is no build-in expression
		[NotNull]
		public IPropertyAccessor GetAccessor([NotNull] string propertyName)
		{
			Assert.ArgumentNotNullOrEmpty(propertyName, nameof(propertyName));

			return ReflectionHelper.GetPropertyAccessor<M>(propertyName);
		}

		[NotNull]
		public IPropertyAccessor GetAccessor(
			[NotNull] Expression<Func<M, object>> expression)
		{
			Assert.ArgumentNotNull(expression, nameof(expression));

			return ReflectionHelper.GetPropertyAccessor(expression);
		}

		[NotNull]
		public PropertyBindingExpression Bind(
			[NotNull] Expression<Func<M, object>> expression)
		{
			IPropertyAccessor accessor = ReflectionHelper.GetPropertyAccessor(expression);
			return new PropertyBindingExpression(this, accessor);
		}

		public void RemoveElementForControl([NotNull] Control control)
		{
			IScreenElement element = FindElementForControl(control);
			if (element == null)
			{
				return;
			}

			_allElements.Remove(element);

			var boundScreenElement = element as IBoundScreenElement;

			if (boundScreenElement != null)
			{
				boundScreenElement.StopBinding();
				_boundElements.Remove(boundScreenElement);
			}
		}

		[NotNull]
		public ButtonElement BindButton([NotNull] Button button)
		{
			var element = new ButtonElement(button);
			AddElement(element);

			return element;
		}

		[NotNull]
		private ScreenBinder<CHILD> CreateChildScreenBinder<CHILD>(
			[NotNull] PropertyInfo property)
			where CHILD : class
		{
			var child = new ChildScreenBinder<CHILD>(property, _monitor);
			_boundElements.Add(child);

			var result = (ScreenBinder<CHILD>) child.InnerBinder;

			result.OnChange = () => OnChange();

			return result;
		}
	}
}
