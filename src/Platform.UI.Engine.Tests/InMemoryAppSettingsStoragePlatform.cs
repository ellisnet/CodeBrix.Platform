#nullable enable

using System;
using System.Collections.Generic;
using CodeBrix.Platform.AppSettings.Contracts;
using CodeBrix.Platform.Foundation.Extensibility;

namespace CodeBrix.Platform.UI.Engine.Tests;

/// <summary>
/// The storage a platform supplies to the settings engine (IAppSettingsStoragePlatform), kept in memory: the engine
/// runs exactly as a CodeBrix.Mobile app would run it, with no database engine in the process.
/// </summary>
internal sealed class InMemoryAppSettingsStoragePlatform : IAppSettingsStoragePlatform
{
	private static readonly object _gate = new();
	private static bool _registered;

	private readonly Dictionary<string, Dictionary<string, string>> _databases = new(StringComparer.Ordinal);

	/// <summary>Registers one in-memory storage platform for the process (as a platform bootstrap would).</summary>
	internal static void EnsureRegistered()
	{
		lock (_gate)
		{
			if (!_registered)
			{
				var platform = new InMemoryAppSettingsStoragePlatform();
				ApiExtensibility.Register(typeof(IAppSettingsStoragePlatform), _ => platform);
				_registered = true;
			}
		}
	}

	public string GetDefaultDirectory(string appName) => "/memory/" + appName;

	public IAppSettingsStorage Open(string appName, string directoryPath, IDictionary<string, string> values, Func<DateTime> clock)
	{
		var fresh = !_databases.TryGetValue(directoryPath, out var rows);
		rows ??= _databases[directoryPath] = new Dictionary<string, string>(StringComparer.Ordinal);
		values.Clear();
		foreach (var row in rows)
		{
			values[row.Key] = row.Value;
		}

		return new Storage(directoryPath, rows, fresh);
	}

	/// <summary>One in-memory settings "database".</summary>
	private sealed class Storage : IAppSettingsStorage
	{
		private readonly Dictionary<string, string> _rows;
		private bool _closed;

		public Storage(string directoryPath, Dictionary<string, string> rows, bool fresh)
		{
			DirectoryPath = directoryPath;
			DatabaseFilePath = directoryPath + "/settings.sqlite";
			WasCreatedFresh = fresh;
			_rows = rows;
		}

		public string DirectoryPath { get; }
		public string DatabaseFilePath { get; }
		public bool WasCreatedFresh { get; }
		public bool WasRestoredFromBackup => false;
		public bool WasReplacedByImport => false;
		public int AutoBackups { get; private set; }

		public void Write(string key, string json) => Rows()[key] = json;
		public void Delete(string key) => Rows().Remove(key);
		public void CreateAutoBackupAndPrune(int retainCount) => AutoBackups++;
		public string GetExportDestination(string destinationFilePath) => destinationFilePath;
		public void ExportTo(string fullPath) => Rows();
		public void StageIncomingFile(string sourceFilePath) => throw new NotSupportedException();
		public void Dispose() => _closed = true;

		private Dictionary<string, string> Rows() => _closed ? throw new ObjectDisposedException("AppSettingsStore") : _rows;
	}
}
