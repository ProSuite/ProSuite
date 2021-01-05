using System;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	[CLSCompliant(false)]
	public abstract class IssueWriter : IDisposable
	{
		[NotNull] private readonly IIssueAttributeWriter _issueAttributeWriter;

		private int _pendingInserts;
		private int _pendingPointCount;

		private const int _flushInterval = 1000;
		private const int _maximumBufferedPointCount = 100000;

		[CanBeNull] private ICursor _insertCursor;
		[CanBeNull] private IRowBuffer _rowBuffer;

		protected IssueWriter([NotNull] IObjectClass objectClass,
		                      [NotNull] IIssueAttributeWriter issueAttributeWriter)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));
			Assert.ArgumentNotNull(issueAttributeWriter, nameof(issueAttributeWriter));

			ObjectClass = objectClass;
			_issueAttributeWriter = issueAttributeWriter;
			Name = DatasetUtils.GetUnqualifiedName(objectClass);
		}

		[NotNull]
		public string Name { get; }

		[NotNull]
		public IObjectClass ObjectClass { get; }

		public void Write([NotNull] Issue issue, [CanBeNull] IGeometry errorGeometry)
		{
			if (_insertCursor == null)
			{
				_rowBuffer = CreateRowBuffer();

				const bool useBuffering = true;
				_insertCursor = ((ITable) ObjectClass).Insert(useBuffering);
			}

			Write(issue, Assert.NotNull(_rowBuffer), errorGeometry);

			WriteCount++;
			Assert.NotNull(_insertCursor).InsertRow(_rowBuffer);
			_pendingInserts++;
			_pendingPointCount += GetPointCount(errorGeometry);

			if (_pendingInserts > _flushInterval ||
			    _pendingPointCount > _maximumBufferedPointCount)
			{
				Flush();
			}
		}

		public int WriteCount { get; private set; }

		#region Implementation of IDisposable

		public void Dispose()
		{
			Close();
		}

		#endregion

		protected bool IsOpen => _insertCursor != null;

		protected void Close()
		{
			if (_insertCursor == null)
			{
				return;
			}

			Flush();

			if (_insertCursor != null)
			{
				Marshal.ReleaseComObject(_insertCursor);
				_insertCursor = null;
			}

			OnClosed();
		}

		protected virtual void OnClosed() { }

		protected virtual void WriteCore([NotNull] Issue issue,
		                                 [NotNull] IRowBuffer rowBuffer,
		                                 [CanBeNull] IGeometry issueGeometry) { }

		[NotNull]
		protected abstract IRowBuffer CreateRowBuffer();

		private static int GetPointCount([CanBeNull] IGeometry geometry)
		{
			if (geometry == null || geometry.IsEmpty)
			{
				return 0;
			}

			var points = geometry as IPointCollection;
			return points?.PointCount ?? 1;
		}

		private void Flush()
		{
			_insertCursor?.Flush();

			_pendingInserts = 0;
			_pendingPointCount = 0;
		}

		private void Write([NotNull] Issue issue,
		                   [NotNull] IRowBuffer rowBuffer,
		                   [CanBeNull] IGeometry errorGeometry)
		{
			_issueAttributeWriter.WriteAttributes(issue, rowBuffer);

			WriteCore(issue, rowBuffer, errorGeometry);
		}
	}
}
