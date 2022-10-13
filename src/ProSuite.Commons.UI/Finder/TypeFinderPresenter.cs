using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;
using ProSuite.Commons.UI.ScreenBinding.Lists;

namespace ProSuite.Commons.UI.Finder
{
	internal class TypeFinderPresenter<T> : ITypeFinderObserver
	{
		private readonly bool _includeObsoleteTypes;
		private readonly Predicate<Type> _match;
		private readonly ITypeFinderView _view;
		private readonly Type _initialSelection;

		// ReSharper disable once StaticFieldInGenericType
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TypeFinderPresenter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <param name="initialSelection">The initial selection.</param>
		/// <param name="match">Optional predicate for matching types.</param>
		/// <param name="includeObsoleteTypes">if set to <c>true</c>, obsolete types are included. 
		/// Otherwise they are excluded from the list of types.</param>
		public TypeFinderPresenter([NotNull] ITypeFinderView view,
		                           [CanBeNull] Type initialSelection,
		                           [CanBeNull] Predicate<Type> match,
		                           bool includeObsoleteTypes)
		{
			Assert.ArgumentNotNull(view, nameof(view));

			_view = view;
			_initialSelection = initialSelection;
			_match = match;
			_includeObsoleteTypes = includeObsoleteTypes;

			_view.Observer = this;
			_view.Text = string.Format("Type Finder: {0}", typeof(T).FullName);

			UpdateAppearance();
		}

		#endregion

		#region ITypeFinderObserver Members

		void ITypeFinderObserver.AssemblyPathChanged()
		{
			_view.OKEnabled = false;

			string assemblyPath = _view.AssemblyPath;

			if (! File.Exists(assemblyPath))
			{
				_view.ClearTypeRows();
				_view.SetAssemblyError("Assembly path does not exist: {0}", assemblyPath);
				return;
			}

			Assembly assembly;
			try
			{
				assembly = Assembly.LoadFile(assemblyPath);
			}
			catch (Exception e)
			{
				_view.ClearTypeRows();
				_view.SetAssemblyError("Unable to load assembly: {0}", e.Message);
				return;
			}

			var typeRows =
				new SortableBindingList<TypeTableRow>();

			var excludedObsoleteCount = 0;
			foreach (Type type in assembly.GetTypes())
			{
				if (! typeof(T).IsAssignableFrom(type) ||
				    type.IsAbstract ||
				    _match != null && ! _match(type))
				{
					continue;
				}

				bool isObsolete = ReflectionUtils.IsObsolete(type);

				if (_includeObsoleteTypes || ! isObsolete)
				{
					typeRows.Add(new TypeTableRow(type));
				}
				else
				{
					excludedObsoleteCount++;
				}
			}

			_view.StatusText = GetLoadStatusMessage(typeRows, excludedObsoleteCount);

			_view.ClearAssemblyError();
			_view.SetTypeRows(typeRows);

			UpdateAppearance();
		}

		void ITypeFinderObserver.TypeSelectionChanged()
		{
			UpdateAppearance();
		}

		void ITypeFinderObserver.OKClicked()
		{
			ReturnSelectedTypes();
		}

		void ITypeFinderObserver.RowDoubleClicked()
		{
			ReturnSelectedTypes();
		}

		void ITypeFinderObserver.ViewLoaded()
		{
			var initialSelectionLoaded = false;
			if (_initialSelection != null)
			{
				Assembly assembly = _initialSelection.Assembly;

				if (File.Exists(assembly.Location))
				{
					_view.AssemblyPath = assembly.Location;

					_view.TrySelectRow(new TypeTableRow(_initialSelection));
					initialSelectionLoaded = true;
				}
			}

			if (! initialSelectionLoaded)
			{
				string lastUsedAssemblyPath = _view.LastUsedAssemblyPath;

				if (! string.IsNullOrEmpty(lastUsedAssemblyPath))
				{
					try
					{
						Assembly assembly = Assembly.LoadFile(lastUsedAssemblyPath);

						_view.AssemblyPath = assembly.Location;
					}
					catch (Exception e)
					{
						_msg.DebugFormat("Error loading last used assembly {0}: {1}",
						                 lastUsedAssemblyPath,
						                 e.Message);
					}
				}
			}
		}

		#endregion

		[NotNull]
		private static string GetLoadStatusMessage(
			[NotNull] ICollection<TypeTableRow> typeRows,
			int excludedObsoleteCount)
		{
			var sb = new StringBuilder();

			sb.AppendFormat("{0} matching type{1} found", typeRows.Count,
			                typeRows.Count == 1
				                ? string.Empty
				                : "s");

			if (excludedObsoleteCount > 0)
			{
				sb.AppendFormat(". ({0} obsolete type{1} excluded)", excludedObsoleteCount,
				                excludedObsoleteCount == 1
					                ? string.Empty
					                : "s");
			}

			return sb.ToString();
		}

		private void ReturnSelectedTypes()
		{
			List<Type> result = _view.GetSelectedTypeRows().Select(row => row.Type).ToList();

			_view.SelectedTypes = result;
			_view.DialogResult = DialogResult.OK;
			_view.Close();
		}

		private void UpdateAppearance()
		{
			_view.OKEnabled = _view.SelectedTypeCount > 0;
			_view.SelectAllTypesEnabled = _view.TypeCount > 0;
			_view.SelectNoTypesEnabled = _view.TypeCount > 0;
		}
	}
}
