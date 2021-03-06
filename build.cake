#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0

// Cake Addins
#addin nuget:?package=Cake.FileHelpers&version=2.0.0

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var VERSION = "2.10.0";
var NUGET_SUFIX = ".1";

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

var artifacts = new [] {
    new Artifact {
        SolutionPath = "./braintree-core.sln",
        AssemblyInfoPath = "./Naxam.BrainTree.Core/Properties/AssemblyInfo.cs",
        NuspecPath = "./core.nuspec",
        DownloadUrl = "http://central.maven.org/maven2/com/braintreepayments/api/core/{0}/core-{0}.aar",
        JarPath = "./Naxam.BrainTree.Core/Jars/core.aar"
    }
};

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Downloads")
    .Does(() =>
{
    foreach(var artifact in artifacts) {
        var downloadUrl = string.Format(artifact.DownloadUrl, VERSION);
        var jarPath = string.Format(artifact.JarPath, VERSION);

        DownloadFile(downloadUrl, jarPath);
    }
});

Task("Clean")
    .Does(() =>
{
    CleanDirectory("./packages");

    var nugetPackages = GetFiles("./*.nupkg");

    foreach (var package in nugetPackages)
    {
        DeleteFile(package);
    }
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    foreach(var artifact in artifacts) {
        NuGetRestore(artifact.SolutionPath);
    }
});

Task("Build")
    .IsDependentOn("Downloads")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    foreach(var artifact in artifacts) {
        MSBuild(artifact.SolutionPath, settings => settings.SetConfiguration(configuration));
    }
});

Task("UpdateVersion")
    .Does(() => 
{
    foreach(var artifact in artifacts) {
        ReplaceRegexInFiles(artifact.AssemblyInfoPath, "\\[assembly\\: AssemblyVersion([^\\]]+)\\]", string.Format("[assembly: AssemblyVersion(\"{0}\")]", VERSION));
    }
});

Task("Pack")
    .IsDependentOn("UpdateVersion")
    .IsDependentOn("Build")
    .Does(() =>
{
    foreach(var artifact in artifacts) {
        NuGetPack(artifact.NuspecPath, new NuGetPackSettings {
            Version = VERSION + NUGET_SUFIX,
            ReleaseNotes = new [] {
                $"Braintree SDK - Core v{VERSION}"
            }
        });
    }
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Pack");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);

class Artifact {
    public string AssemblyInfoPath { get; set; }

    public string SolutionPath { get; set; }

    public string DownloadUrl  { get; set; }

    public string JarPath { get; set; }

    public string NuspecPath { get; set; }
}