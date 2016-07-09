﻿using MonoDevelop.Core;
using MonoDevelop.Projects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MonoDevelop.D.Building;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.D.Projects.Dub
{
	/// <summary>
	/// A dub package.
	/// </summary>
	public class DubProject : AbstractDProject
	{
		#region Properties
		bool loading;
		List<string> authors = new List<string>();
		/// <summary>
		/// Project-wide cross-config build settings.
		/// </summary>
		public readonly DubBuildSettings CommonBuildSettings = new DubBuildSettings();

		public readonly DubReferencesCollection DubReferences;
		public override DProjectReferenceCollection References { get { return DubReferences; } }

		public string packageName;
		string displayName;
		public override string Name
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(displayName))
					return displayName;

				if (string.IsNullOrWhiteSpace(packageName))
					return string.Empty;

				var p = packageName.Split(':');
				return p[p.Length - 1];
			}
			set { displayName = value; }
		} // override because the name is normally derived from the file name -- package.json is not the project's file name!
		FilePath filePath;
		public override FilePath FileName
		{
			get { return filePath; }
			set
			{
				filePath = value;
				if (File.Exists(value))
					lastDubJsonModTime = File.GetLastWriteTimeUtc(value);
			}
		}

		DateTime lastDubJsonModTime;
		public override bool NeedsReload
		{
			get
			{
				return lastDubJsonModTime != File.GetLastWriteTimeUtc(FileName);
			}
			set
			{
				//base.NeedsReload = value;
			}
		}


		public string Homepage;
		public string Copyright;
		public List<string> Authors { get { return authors; } }

		public IEnumerable<DubBuildSettings> GetBuildSettings(ConfigurationSelector sel)
		{
			yield return CommonBuildSettings;

			DubProjectConfiguration pcfg;
			if (sel == null || (pcfg = GetConfiguration(sel) as DubProjectConfiguration) == null)
				foreach (DubProjectConfiguration cfg in Configurations)
					yield return cfg.BuildSettings;
			else
				yield return pcfg.BuildSettings;
		}

		public override bool ItemFilesChanged
		{
			get
			{
				return loading;
			}
		}

		static readonly char[] PathSep = { Path.DirectorySeparatorChar };
		static bool CanContainFile(string f)
		{
			var i = f.LastIndexOfAny(PathSep);
			return i < -1 ? !f.StartsWith(".") : f[i + 1] != '.';
		}

		protected override List<FilePath> OnGetItemFiles(bool includeReferencedFiles)
		{
			var files = new List<FilePath>();

			foreach (var dir in GetSourcePaths((ConfigurationSelector)null))
				foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
					if (CanContainFile(f))
						files.Add(new FilePath(f));

			return files;
		}

		public override IEnumerable<string> GetSourcePaths(ConfigurationSelector sel)
		{
			var dirs = new List<string>();
			List<DubBuildSetting> l;
			string d;

			foreach (var sett in GetBuildSettings(sel))
			{
				if (sett.TryGetValue(DubBuildSettings.SourcePathsProperty, out l))
				{
					foreach (var setting in l)
					{
						foreach (var directory in setting.Values)
						{
							d = ProjectBuilder.EnsureCorrectPathSeparators(directory);
							if (!Path.IsPathRooted(d))
							{
								if (this is DubSubPackage)
									(this as DubSubPackage).useOriginalBasePath = true;
								d = Path.GetFullPath(Path.Combine(BaseDirectory.ToString(), d));
								if (this is DubSubPackage)
									(this as DubSubPackage).useOriginalBasePath = false;
							}

							// Ignore os/arch/version constraints for now

							if (dirs.Contains(d) || !Directory.Exists(d))
								continue;

							dirs.Add(d);
						}
					}
				}
			}

			if (dirs.Count == 0)
			{
				d = BaseDirectory.Combine("source").ToString();
				if (Directory.Exists(d))
					dirs.Add(d);

				d = BaseDirectory.Combine("src").ToString();
				if (Directory.Exists(d))
					dirs.Add(d);
			}

			return dirs;
		}

		public readonly SortedSet<string> buildTypes = new SortedSet<string>(new[] { "plain", "debug", "release", "unittest", "docs", "ddox", "profile", "cov", "unittest-cov" });

		public class DubExecTarget : ExecutionTarget
		{
			readonly string id;
			public DubExecTarget(string id)
			{
				this.id = id;
			}

			public override string Id
			{
				get { return id; }
			}

			public override string Name
			{
				get { return id; }
			}
		}

		/// <summary>
		/// http://code.dlang.org/package-format#build-types
		/// </summary>
		protected override IEnumerable<ExecutionTarget> OnGetExecutionTargets(ConfigurationSelector configuration)
		{
			foreach (var buildType in buildTypes)
				yield return new DubExecTarget(buildType);
		}

		public override string ToString()
		{
			return string.Format("[DubProject: Name={0}]", Name);
		}
		#endregion

		#region Constructor & Init
		public DubProject()
		{
			DubReferences = new DubReferencesCollection(this);
		}
		#endregion

		#region Serialize & Deserialize
		internal void BeginLoad()
		{
			loading = true;
			OnBeginLoad();
			Items.Clear();
		}

		void _loadFilesFrom(string dir)
		{
			var baseDir = BaseDirectory;

			foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
			{
				if (CanContainFile(f))
				{
					if (f.StartsWith(baseDir))
						Items.Add(new ProjectFile(f));
					else
						Items.Add(new ProjectFile(f) { Link = f.Substring(dir.Length + 1) });
				}
			}
		}

		IEnumerable<string> AdditionalSourceFiles
		{
			get{
				List<DubBuildSetting> l;
				if (CommonBuildSettings.TryGetValue (DubBuildSettings.SourceFilesProperty, out l))
					foreach (var sett in l)
						foreach (var f in sett.Values)
							yield return f;

				foreach (DubProjectConfiguration cfg in Configurations)
					if (cfg.BuildSettings.TryGetValue (DubBuildSettings.SourceFilesProperty, out l))
						foreach (var sett in l)
							foreach (var f in sett.Values)
								yield return f;
			}
		}

		internal void EndLoad()
		{
			// Load project's files
			var baseDir = BaseDirectory;
			var baseDirs = new List<string>();

			foreach (var dir in GetSourcePaths((ConfigurationSelector)null))
			{
				bool skip = false;
				foreach (var bdir in baseDirs)
					if (dir.StartsWith(bdir))
					{
						skip = true;
						break;
					}
				if (skip)
					continue;

				baseDirs.Add(dir);
				_loadFilesFrom(dir);
			}

			#region Add files specified via sourceFiles
			foreach (var f in AdditionalSourceFiles)
			{
				if (string.IsNullOrWhiteSpace(f))
					continue;

				ProjectFile fileToAdd;

				if (Path.IsPathRooted(f))
				{
					bool skip = false;
					foreach (var dir in baseDirs)
						if (f.StartsWith(dir))
						{
							skip = true;
							break;
						}
					fileToAdd = skip ? null : new ProjectFile(f);
				}
				else
					fileToAdd = new ProjectFile(baseDir.Combine(f).ToString());

				if(fileToAdd != null){
					Items.Add(fileToAdd);
				}
			}
			#endregion

			OnEndLoad();
			loading = false;
		}

		protected override void OnEndLoad()
		{
			DubReferences.FireUpdate();
			base.OnEndLoad();
		}
		#endregion

		#region Building
		public override IEnumerable<SolutionItem> GetReferencedItems(ConfigurationSelector configuration)
		{
			return new SolutionItem[0];
		}

		public bool BuildSettingMatchesConfiguration(DubBuildSetting sett, ConfigurationSelector config)
		{
			return true;
		}

		public override FilePath GetOutputFileName(ConfigurationSelector configuration)
		{
			var cfg = GetConfiguration(configuration) as DubProjectConfiguration;

			string targetPath = null, targetName = null, targetType = null;
			CommonBuildSettings.TryGetTargetFileProperties(this, configuration, ref targetType, ref targetName, ref targetPath);
			if(cfg != null)
				cfg.BuildSettings.TryGetTargetFileProperties(this, configuration, ref targetType, ref targetName, ref targetPath);

			if (string.IsNullOrWhiteSpace(targetPath))
				targetPath = BaseDirectory.ToString();
			else if (!Path.IsPathRooted(targetPath))
				targetPath = BaseDirectory.Combine(targetPath).ToString();

			if (string.IsNullOrWhiteSpace(targetName))
			{
				var packName = packageName.Split(':');
				targetName = packName[packName.Length - 1];
			}

			if (string.IsNullOrWhiteSpace(targetType) ||
				(targetType = targetType.ToLowerInvariant()) == "executable" ||
				targetType == "autodetect")
			{
				if (OS.IsWindows)
					targetName += ".exe";
			}
			else
			{
				//TODO
			}


			return Path.Combine(targetPath, targetName);
		}

		protected override void PopulateOutputFileList(List<FilePath> list, ConfigurationSelector configuration)
		{
			base.PopulateOutputFileList(list, configuration);
		}

		protected override BuildResult DoBuild(ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return DubBuilder.BuildProject(this, monitor, configuration);
		}

		public override NativeExecutionCommand CreateExecutionCommand(ConfigurationSelector conf)
		{
			var sr = new StringBuilder();
			DubBuilder.Instance.BuildProgramArgAppendix(sr, this, GetConfiguration(conf) as DubProjectConfiguration);

			var cmd = base.CreateExecutionCommand(conf);

			cmd.Arguments = sr.ToString();
			cmd.WorkingDirectory = BaseDirectory;
			return cmd;
		}

		protected override bool OnGetCanExecute(ExecutionContext context, ConfigurationSelector configuration)
		{
			if (!base.OnGetCanExecute(context, configuration))
				return false;

			string targetPath = null, targetName = null, targetType = null;
			CommonBuildSettings.TryGetTargetFileProperties(this, configuration, ref targetType, ref targetName, ref targetPath);
			var cfg = GetConfiguration(configuration) as DubProjectConfiguration;
			if(cfg != null) cfg.BuildSettings
				.TryGetTargetFileProperties(this, configuration, ref targetType, ref targetName, ref targetPath);

			if (targetType == "autodetect" || string.IsNullOrWhiteSpace(targetType))
			{
				if (string.IsNullOrEmpty(targetName))
					return true;

				var ext = Path.GetExtension(targetName);
				if (ext != null)
					switch (ext.ToLowerInvariant())
					{
						case ".dylib":
						case ".so":
						case ".a":
							return false;
						case ".exe":
						case null:
						default:
							return true;
					}
			}

			return targetType.ToLowerInvariant() == "executable";
		}

		protected override void DoExecute(ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			DubBuilder.ExecuteProject(this, monitor, context, configuration);
		}

		public override SolutionItemConfiguration CreateConfiguration(string name)
		{
			return new DubProjectConfiguration { Name = name };
		}

		protected override void DoClean(ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			base.DoClean(monitor, configuration);
		}
		#endregion

		public override void Save(ProgressMonitor monitor)
		{
			monitor.ReportSuccess("Skip saving dub project.");
		}
	}
}
