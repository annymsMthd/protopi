#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
#addin nuget:?package=Cake.Git

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var clusterHelloNode1Dir = Directory("./src/cluster-hello-world/node1/");
var clusterHelloNode2Dir = Directory("./src/cluster-hello-world/node2/");
var piHelloDir = Directory("./src/pi-hello-world/");
var artifacts = Directory("./artifacts/");

var deploymentStagingArea = Directory("/tmp/pi-deploy/");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Action<string, string, ConvertableDirectoryPath> executeCommand = (string command, string arguments, ConvertableDirectoryPath workingDirectory) => 
{
    var settings = new ProcessSettings
    {
        WorkingDirectory = workingDirectory,
        Arguments = ProcessArgumentBuilder.FromString(arguments)
    };

    using(var process = StartAndReturnProcess(command, settings))
    {
        process.WaitForExit();
        // This should output 0 as valid arguments supplied
        Information("Exit code: {0}", process.GetExitCode());
    }
};

//////////////////////////////////////////////////////////////////////
// Cluster Hello World
//////////////////////////////////////////////////////////////////////

Task("Clean : [cluster-hello-world]")
    .Does(() =>
{
    CleanDirectory(clusterHelloNode1Dir + Directory("./bin/") + Directory(configuration));
    CleanDirectory(clusterHelloNode2Dir + Directory("./bin/") + Directory(configuration));
});

Task("Restore : [cluster-hello-world]")
    .IsDependentOn("Clean : [cluster-hello-world]")
    .Does(() =>
{
    DotNetCoreRestore(clusterHelloNode1Dir);
    DotNetCoreRestore(clusterHelloNode2Dir);
});

Task("Build : [cluster-hello-world]")
    .IsDependentOn("Restore : [cluster-hello-world]")
    .Does(() =>
{
    DotNetCoreBuild(clusterHelloNode1Dir);
    DotNetCoreBuild(clusterHelloNode2Dir);
});

//////////////////////////////////////////////////////////////////////
// Pi hello world
//////////////////////////////////////////////////////////////////////

Task("Clean : [pi-hello-world]")
    .Does(() =>
{
    CleanDirectory(piHelloDir + Directory("./bin/") + Directory(configuration));
    CleanDirectory(artifacts);
});

Task("Restore : [pi-hello-world]")
    .IsDependentOn("Clean : [pi-hello-world]")
    .Does(() =>
{
    DotNetCoreRestore(piHelloDir);
});

Task("Build : [pi-hello-world]")
    .IsDependentOn("Restore : [pi-hello-world]")
    .Does(() =>
{
    DotNetCoreBuild(piHelloDir);
});

Task("Publish : [pi-hello-world]")
    .IsDependentOn("Build : [pi-hello-world]")
    .Does(() =>
{
    var settings = new DotNetCorePublishSettings
    {
         Configuration = "Release",
         Runtime = "linux-arm",
         OutputDirectory = artifacts
    };

    DotNetCorePublish(piHelloDir, settings);
});

Task("Deploy : [pi-hello-world]")
    .IsDependentOn("Publish : [pi-hello-world]")
    .Does(() =>
{
    var repo = Argument("resin_repo", "");

    EnsureDirectoryExists(deploymentStagingArea);
    CleanDirectory(deploymentStagingArea);

    executeCommand("/usr/bin/git", "clone " + repo, deploymentStagingArea);

    var workingDirectory = deploymentStagingArea + Directory("./protopi/");

    CopyFiles("./artifacts/*", workingDirectory);

    CopyFile("./src/pi-hello-world/Dockerfile", workingDirectory + File("./Dockerfile"));

    executeCommand("/usr/bin/git", "add .", workingDirectory);

    executeCommand("/usr/bin/git", "commit -m \"cake deploy\"", workingDirectory);

    executeCommand("/usr/bin/git", "push", workingDirectory);
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build : [cluster-hello-world]")
    .IsDependentOn("Build : [pi-hello-world]");

Task("Deploy")
    .IsDependentOn("Deploy : [pi-hello-world]");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
