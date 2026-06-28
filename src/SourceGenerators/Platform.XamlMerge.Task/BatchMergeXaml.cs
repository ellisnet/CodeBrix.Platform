// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Modified for Uno support by David John Oliver, Jerome Laban
#nullable disable

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CodeBrix.Platform.UI.Tasks.BatchMerge //Was previously: Uno.UI.Tasks.BatchMerge
{
	public class BatchMergeXaml_v0 : CustomTask
	{
		[Required]
		public ITaskItem[] Pages { get; set; }

		[Required]
		public string MergedXamlFile { get; set; }

		[Required]
		public string ProjectFullPath { get; set; }

		[Required]
		public string TlogReadFilesOutputPath { get; set; }

		[Required]
		public string TlogWriteFilesOutputPath { get; set; }

		[Output]
		public string[] FilesWritten
		{
			get { return filesWritten.ToArray(); }
		}

		private List<string> filesWritten = new List<string>();

		public override bool Execute()
		{
			MergedDictionary mergedDictionary = MergedDictionary.CreateMergedDicionary();
			List<string> pages = new List<string>();

			if (Pages != null)
			{
				foreach (ITaskItem pageItem in Pages)
				{
					string page = pageItem.ItemSpec;
					if (File.Exists(page))
					{
						pages.Add(page);
					}
					else
					{
						LogError($"Can't find page {page}!");
					}
				}
			}

			if (HasLoggedErrors)
			{
				return false;
			}

			LogMessage($"Merging XAML files into {MergedXamlFile}...");

			var projectBasePath = Path.GetDirectoryName(Path.GetFullPath(ProjectFullPath));

			// Compute each page's project-relative path, normalized to forward slashes so it is byte-identical
			// regardless of the build host's path separator ('\' on Windows, '/' on Linux/macOS). This is the
			// string emitted as the "origin:" comment AND the key the output is ordered by.
			//
			// The merged content is emitted in the order the pages are processed, so the pages must be ordered
			// deterministically here: MSBuild hands us the Page items in filesystem-enumeration order, which
			// differs by build host (NTFS returns them sorted, ext4 does not), and that would make the merged
			// file churn in source control between OSes even though the content is identical. Ordering by the
			// normalized relative path with an ordinal (culture-independent) comparison yields the same order
			// on every platform.
			var orderedPages = pages
				.Select(page => new
				{
					Page = page,
					RelativePath = Path.GetFullPath(page)
						.Replace(projectBasePath, "")
						.TrimStart(Path.DirectorySeparatorChar)
						.Replace('\\', '/')
				})
				.OrderBy(entry => entry.RelativePath, StringComparer.Ordinal)
				.ToList();

			foreach (var entry in orderedPages)
			{
				try
				{
					mergedDictionary.MergeContent(
						content: File.ReadAllText(entry.Page),
						filePath: entry.RelativePath);
				}
				catch (Exception)
				{
					LogError($"Exception found when merging page {entry.Page}!");
					throw;
				}
			}

			Directory.CreateDirectory(Path.GetDirectoryName(MergedXamlFile));
			Directory.CreateDirectory(Path.GetDirectoryName(TlogReadFilesOutputPath));
			Directory.CreateDirectory(Path.GetDirectoryName(TlogWriteFilesOutputPath));

			mergedDictionary.FinalizeXaml();
			filesWritten.Add(Utils.RewriteFileIfNecessary(MergedXamlFile, mergedDictionary.ToString()));

			File.WriteAllLines(TlogReadFilesOutputPath, Pages.Select(page => page.ItemSpec));
			File.WriteAllLines(TlogWriteFilesOutputPath, FilesWritten);

			return !HasLoggedErrors;
		}
	}
}
