﻿using MonoDevelop.Core;
using MonoDevelop.Projects.Extensions;
using System;
using System.Collections.Generic;
using MonoDevelop.Projects;
using System.Linq;
using System.IO;

namespace MonoDevelop.D.Projects.Dub.DefinitionFormats
{
	public class DubFileFormat : FileFormat
	{
		public bool CanReadFile(FilePath file, Type expectedObjectType)
		{
			return DubFileManager.Instance.CanLoad(file.FileName) &&
			(expectedObjectType.Equals(typeof(WorkspaceItem)) ||
			expectedObjectType.Equals(typeof(SolutionItem)));
		}

		public bool CanWriteFile(object obj)
		{
			return true; // Everything has to be manipulated manually (atm)!
		}

		public void ConvertToFormat(object obj)
		{

		}

		public IEnumerable<string> GetCompatibilityWarnings(object obj)
		{
			yield return string.Empty;
		}

		public List<FilePath> GetItemFiles(object obj)
		{
			return new List<FilePath>();
		}

		public FilePath GetValidFormatName(object obj, FilePath fileName)
		{
			return fileName;
		}

		public object ReadFile(FilePath file, Type expectedType, ProgressMonitor monitor)
		{
			if (expectedType.IsAssignableFrom(typeof(WorkspaceItem)))
				return DubFileManager.Instance.LoadAsSolution(file, monitor);
			if (expectedType.IsAssignableFrom(typeof(SolutionItem)))
				return DubFileManager.Instance.LoadProject(file, Ide.IdeApp.Workspace.GetAllSolutions().First(), monitor);
			return null;
		}

		public bool SupportsFramework(Core.Assemblies.TargetFramework framework)
		{
			return false;
		}

		public bool SupportsMixedFormats { get { return true; } }

		public void WriteFile(FilePath file, object obj, ProgressMonitor monitor)
		{
			//monitor.ReportError ("Can't write dub package information! Change it manually in the definition file!", new InvalidOperationException ());
		}
	}
}