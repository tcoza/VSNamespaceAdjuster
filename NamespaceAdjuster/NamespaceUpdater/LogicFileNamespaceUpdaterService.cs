﻿using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace NamespaceUpdater
{
	internal abstract class LogicFileNamespaceUpdaterService : IFileNamespaceUpdater
	{
		protected abstract string SupportedFileExtension { get; }
		protected abstract string NamespaceStartLimiter { get; }
		protected abstract string NamespaceEndLimiter { get; }
		protected abstract Match FindNamespaceMatch(string fileContent);

		protected abstract string BuildNamespaceLine(string desiredNamespace);

		protected abstract MatchCollection FindUsingMatches(string fileContent);
		protected string NewLine { get; set; } = Environment.NewLine;
		private void SetNewLine(string fileContent)
		{
			var isCrlf = fileContent.IndexOf("\r\n") > -1;

			if (isCrlf) NewLine = "\r\n";
			else NewLine = "\n";
		}

		public bool SupportsFileExtension(string fileExtension)
		{
			return fileExtension.Equals(SupportedFileExtension);
		}

		public virtual bool UpdateFileNamespace(ref string fileContent, string desiredNamespace)
		{
			if (string.IsNullOrEmpty(desiredNamespace)) return false;

			SetNewLine(fileContent);

			var namespaceMatch = FindNamespaceMatch(fileContent);

			return namespaceMatch.Success ?
				UpdateNamespace(ref fileContent, desiredNamespace, namespaceMatch) :
				CreateNamespace(ref fileContent, desiredNamespace);
		}

		private bool UpdateNamespace(ref string fileContent, string desiredNamespace, Match namespaceMatch)
		{
			var fileRequiresUpdate = false;

			var namespaceGroup = namespaceMatch.Groups.OfType<Group>().Where(g => !(g is Match)).FirstOrDefault();

			if (namespaceGroup == null) return false;

			var currentNamespace = namespaceGroup.Value.Trim();

			if (currentNamespace != desiredNamespace)
			{
				fileRequiresUpdate = true;
				fileContent = fileContent.Substring(0, namespaceGroup.Index) + desiredNamespace + fileContent.Substring(namespaceGroup.Index + namespaceGroup.Value.Trim().Length);
			}

			return fileRequiresUpdate;
		}

		private bool CreateNamespace(ref string fileContent, string desiredNamespace)
		{
			var usingMatches = FindUsingMatches(fileContent);
			var lastUsing = usingMatches.OfType<Match>().LastOrDefault();

			string usingSectionContent = string.Empty;
			if (lastUsing != null)
			{
				var indexAfterUsing = lastUsing.Index + lastUsing.Length;
				usingSectionContent = fileContent.Substring(0, indexAfterUsing).Trim();

				fileContent = fileContent.Substring(indexAfterUsing);
			}

			fileContent =
				(string.IsNullOrEmpty(usingSectionContent) ? string.Empty : usingSectionContent + NewLine + NewLine) +
				BuildNamespaceLine(desiredNamespace) + NewLine +
				NamespaceStartLimiter +
				fileContent.Trim() +
				NewLine + NamespaceEndLimiter;

			return true;
		}

	}
}
