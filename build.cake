#r "System.IO.Compression.FileSystem"
#r "System.Xml"

using System.IO.Compression;
using System.Net;
using System.Xml;

string Target = Argument<string>("Target", "Default");

bool IsCleanEnabled = Argument<bool>("DoClean", true);
bool IsRestoreEnabled = Argument<bool>("DoRestore", true);
bool IsBuildEnabled = Argument<bool>("DoBuild", true);
bool IsTestEnabled = Argument<bool>("DoTest", true);
bool IsPackEnabled = Argument<bool>("DoPack", true);

string Configuration = HasArgument("Configuration") ?
  Argument<string>("Configuration") :
  EnvironmentVariable("Configuration") ?? "Release";

var SolutionDir = Context.Environment.WorkingDirectory.FullPath;
var SolutionFile = System.IO.Path.Combine(SolutionDir, "Revo.sln");

var PackagesDir = System.IO.Path.Combine(SolutionDir, "build", "packages");
var ReportsDir = System.IO.Path.Combine(SolutionDir, "build", "reports");

bool IsCiBuild = BuildSystem.IsRunningOnAzurePipelinesHosted;

int? BuildNumber =
  HasArgument("BuildNumber") ? (int?) Argument<int>("BuildNumber") :
  BuildSystem.IsRunningOnAzurePipelinesHosted ? AzurePipelines.Environment.Build.Id :
  EnvironmentVariable("BuildNumber") != null ? (int?) int.Parse(EnvironmentVariable("BuildNumber")) : null;

var xmlDocument = new XmlDocument();
xmlDocument.Load(System.IO.Path.Combine(SolutionDir, "Common.props"));

string VersionPrefix = ((XmlElement) xmlDocument.SelectSingleNode("Project/PropertyGroup/VersionPrefix")).InnerText;
string VersionSuffix = HasArgument("VersionSuffix") ?
  Argument<string>("VersionSuffix") :
  (xmlDocument.SelectSingleNode("Project/PropertyGroup/VersionSuffix") as XmlElement)?.InnerText;

// append the VersionSuffix for non-release CI builds
string ciTag = BuildSystem.IsRunningOnAzurePipelinesHosted && AzurePipelines.Environment.Repository.SourceBranch.StartsWith("refs/tags/")
  ? AzurePipelines.Environment.Repository.SourceBranch.Substring("refs/tags/".Length)
  : null;
string ciBranch = BuildSystem.IsRunningOnAzurePipelinesHosted ? AzurePipelines.Environment.Repository.SourceBranch : null;

if (BuildNumber.HasValue && ciTag == null && (string.IsNullOrWhiteSpace(ciBranch) || ciBranch != "master"))
{
  VersionSuffix = VersionSuffix != null ?
    $"{VersionSuffix}-build{BuildNumber:00000}" :
    $"build{BuildNumber:00000}";
}

string Version = VersionSuffix?.Length > 0 ? $"{VersionPrefix}-{VersionSuffix}" : VersionPrefix;

if (BuildSystem.IsRunningOnAzurePipelinesHosted)
{
  AzurePipelines.Commands.UpdateBuildNumber(Version);
}

Task("Default")
  .IsDependentOn("Pack");

Task("Clean")
  .WithCriteria(IsCleanEnabled)
  .Does(() =>
  {
    CleanDirectories(new [] { PackagesDir, ReportsDir });

    DotNetCoreClean(SolutionFile,
      new DotNetCoreCleanSettings()
      {
        Verbosity = DotNetCoreVerbosity.Minimal,
          Configuration = Configuration,
          ArgumentCustomization = args => args
      });
  });

Task("Restore")
  .WithCriteria(IsRestoreEnabled)
  .IsDependentOn("Clean")
  .Does(() =>
  {
    DotNetCoreRestore(
      SolutionFile,
      new DotNetCoreRestoreSettings()
      {
        Verbosity = DotNetCoreVerbosity.Normal
      });
  });

Task("Build")
  .WithCriteria(IsBuildEnabled)
  .IsDependentOn("Restore")
  .Does(() =>
  {
    DotNetCoreBuild(SolutionFile,
      new DotNetCoreBuildSettings
      {
        Verbosity = DotNetCoreVerbosity.Minimal,
          Configuration = Configuration,
          VersionSuffix = VersionSuffix,
          ArgumentCustomization = args => args
      });
  });

Task("Test")
  .WithCriteria(IsTestEnabled)
  .IsDependentOn("Build")
  .Does(() =>
  {
    DotNetCoreTest(SolutionFile,
      new DotNetCoreTestSettings
      {
        Configuration = Configuration,
          NoBuild = true,
          NoRestore = true,
          Verbosity = DotNetCoreVerbosity.Minimal,
          ResultsDirectory = ReportsDir,
          Logger = "trx",
          ArgumentCustomization = args =>
          args
          .Append("/p:CollectCoverage={0}", "true")
          .Append("/p:CoverletOutput={0}/", ReportsDir)
          .Append("/p:UseSourceLink={0}", "true")
          .Append("/p:CoverletOutputFormat={0}", "cobertura")
      });
  });

Task("Pack")
  .WithCriteria(IsPackEnabled)
  .IsDependentOn("Test")
  .Does(() =>
  {
    foreach (var projectFile in GetFiles("./**/Revo.*.csproj")) // without the "Revo.*" prefix, it also matches stuff from ./tools
    {
      if (!projectFile.GetFilename().FullPath.StartsWith("Revo.") ||
        projectFile.GetFilename().FullPath.EndsWith(".Tests.csproj") ||
        projectFile.GetFilename().FullPath.StartsWith("Revo.Examples."))
      {
        continue;
      }

      DotNetCorePack(
        projectFile.FullPath,
        new DotNetCorePackSettings
        {
          Configuration = Configuration,
            OutputDirectory = PackagesDir,
            NoBuild = true,
            NoRestore = true,
            IncludeSymbols = true,
            Verbosity = DotNetCoreVerbosity.Minimal,
            VersionSuffix = VersionSuffix
        });
    }
  });

RunTarget(Target);