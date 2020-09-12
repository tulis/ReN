using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.CI.GitHubActions.Configuration;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.CoverallsNet;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.Tools.SonarScanner;
using Nuke.Common.Utilities.Collections;
using Octokit;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[GitHubActions("continuous"
    , GitHubActionsImage.UbuntuLatest
    , On = new[] { GitHubActionsTrigger.Push }
    , InvokedTargets = new[] { nameof(UploadCoverage)}
    , ImportSecrets = new[] { nameof(CODECOV), nameof(COVERALLS_TOKEN) })]
[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(build => build.Test);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion] readonly GitVersion GitVersion;

    AbsolutePath CoverageOutputFolder = RootDirectory / "coverage-output/";
    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ToolsDirectory => RootDirectory / "tools";
    AbsolutePath ToolCoveralls => ToolsDirectory / "csmacnz.Coveralls";
    AbsolutePath OutputDirectory => RootDirectory / "output";

    [Parameter] readonly string CODECOV;
    [Parameter] readonly string COVERALLS_TOKEN;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(OutputDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            //ProcessTasks.StartProcess(toolPath: "chmod"
            //            , arguments: "777 codecov-uploader"
            //            , workingDirectory: RootDirectory)
            //        .Output.ForEach(output => Console.WriteLine(output.Text));

            DotNetRestore(setting => setting
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            // How to use Sonar — https://github.com/nuke-build/nuke/pull/206
            //SonarScannerTasks
            //    .SonarScannerBegin(setting => setting
            //        .SetProjectKey("Rth")
            //    );

            DotNetBuild(setting => setting
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .EnableNoRestore()
                .EnableRunCodeAnalysis()
                .SetRunCodeAnalysis(true)
                );
            Console.WriteLine("Git SHA: " + this.GitVersion.Sha);
            //SonarScannerTasks.SonarScannerEnd();
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            FileSystemTasks.DeleteDirectory(CoverageOutputFolder);

            DotNetTest(setting => setting
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()

                //https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage?tabs=windows
                //https://www.tonyranieri.com/blog/2019/07/31/Measuring-.NET-Core-Test-Coverage-with-Coverlet/
                //! IMPORTANT: Test project needs to reference coverlet.msbuild nuget package
                .EnableCollectCoverage()
                .SetCoverletOutputFormat(CoverletOutputFormat.opencover)
                .SetCoverletOutput($"{CoverageOutputFolder}/")
            );


        });

    Target ToolsRestore => _ => _
        .Executes(() =>
        {
            DotNetToolInstall(setting => setting
                .SetPackageName("coveralls.net")
                .SetGlobal(false)
                .SetToolInstallationPath(ToolsDirectory)
                );
        });

    Target UploadCoverage => _ => _
        .DependsOn(Test, ToolsRestore)
        .Requires(() => COVERALLS_TOKEN)
        .Executes(() =>
        {
            CoverageOutputFolder.GlobFiles("*.xml").ForEach(openCoverAbsolutePath =>
            {
                CoverallsNetTasks.CoverallsNet(setting => setting
                    .SetRepoToken(COVERALLS_TOKEN)
                    .SetOpenCover(true)
                    .SetInput(openCoverAbsolutePath)
                    .SetToolPath(ToolCoveralls)
                    .SetCommitBranch(this.GitRepository.Branch)
                    //!++ Should use this.GitRepository.Commit
                    //!++ Once nuke is upgraded to version 0.25
                    .SetCommitId(this.GitVersion.Sha)
                    );

            });
        });

    Target CodeCovPermission => _ => _
        .DependsOn(Test)
        .Executes(() =>
        {
            ProcessTasks.StartProcess(toolPath: "chmod"
                    , arguments: "777 codecov-uploader"
                    , workingDirectory: RootDirectory)
                .Output.ForEach(output => Console.WriteLine(output.Text));
        });

    [LocalExecutable("./codecov-uploader")] readonly Tool CodecovUploader;

    Target CodecovUpload => _ => _
        .DependsOn(CodeCovPermission, Test)
        .Requires(() => CODECOV)
        .Executes(() =>
        {
            CodecovUploader(arguments: @$"-t {CODECOV} -s {CoverageOutputFolder}/"
                    , workingDirectory: RootDirectory
                    , timeout: 3600)
                .ForEach(output => Console.WriteLine(output.Text));
        });
}
