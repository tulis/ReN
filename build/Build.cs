using System;
using System.Collections.Generic;
using System.Linq;
using EnumsNET;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
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
using Nuke.Common.Tools.Git;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.Tools.SonarScanner;
using Nuke.Common.Utilities.Collections;
using Octokit;

[GitHubActions("continuous"
    , GitHubActionsImage.UbuntuLatest
    , On = new[] { GitHubActionsTrigger.Push }
    , InvokedTargets = new[] { nameof(UploadCoverageToCoveralls)}
    , ImportSecrets = new[] { nameof(COVERALLS_TOKEN) })]
//[GitHubActions(
//    "deployment"
//    , GitHubActionsImage.UbuntuLatest
//    //, OnPushBranches = new[] { MasterBranch, ReleaseBranchPrefix + "/*" }
//    , InvokedTargets = new[] { nameof(Publish) }
//    , ImportGitHubTokenAs = nameof(GITHUB_TOKEN)
//    , ImportSecrets =
//        new[]
//        {
//            nameof(NUGET_API_KEY)
//        })]
[AzurePipelines(
    suffix: null
    , AzurePipelinesImage.UbuntuLatest
    , AzurePipelinesImage.WindowsLatest
    , AzurePipelinesImage.MacOsLatest
    , InvokedTargets = new[] { nameof(UploadCoverageToAzurePipelines) }
    , NonEntryTargets = new[] { nameof(Restore), nameof(Compile), nameof(Test) })]
[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(build => build.Test);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [CI] readonly AzurePipelines AzurePipelines;
    [CI] readonly GitHubActions GitHubActions;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [Nuke.Common.Required] [GitVersion(Framework = "net5.0", NoFetch = true)] readonly GitVersion GitVersion;

    AbsolutePath CoverageOutputFolder = RootDirectory / "coverage-output/";

    AbsolutePath PackageDirectory => this.OutputDirectory / "packages";
    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ToolsDirectory => RootDirectory / "tools";
    AbsolutePath ToolCoveralls => this.ToolsDirectory / "csmacnz.Coveralls";
    AbsolutePath OutputDirectory => RootDirectory / "output";

    [Parameter] readonly string COVERALLS_TOKEN;
    [Parameter] readonly string GITHUB_TOKEN;
    [Parameter] readonly string NUGET_API_KEY;
    [Parameter] readonly string GOPATH;
    [Parameter] readonly string BUMP_STABILITY;
    [Parameter] readonly string BUMP_SEMANTIC;

    string GitLatestTag { get; set; }

    Nuke.Common.ProjectModel.Project RthProject => Solution.GetProject("Rth");
    string ExpandedGoPath => EnvironmentInfo.ExpandVariables($"${nameof(this.GOPATH)}");
    string GoGitSemvToolPath => $"{this.ExpandedGoPath}/bin/git-semv";
    string GitHubPackageSource => $"https://nuget.pkg.github.com/{GitHubActions.GitHubRepositoryOwner}/index.json";
    string GitHubRemoteUrl => $"https://{this.GitHubActions.GitHubActor}:{this.GITHUB_TOKEN}@github.com/{this.GitHubActions.GitHubRepository}.git";
    bool IsOriginalRepository => GitRepository.Identifier == "tulis/Rth";
    string NuGetPackageSource => "https://api.nuget.org/v3/index.json";
    IReadOnlyCollection<AbsolutePath> PackageFiles => this.PackageDirectory.GlobFiles("*.nupkg");

    // https://github.com/linyows/git-semv
    Target InstallGoGitSemver => _ => _
        .Executes(() =>
        {
            Logger.Info($"{nameof(this.GOPATH)}={this.ExpandedGoPath}");

            var goProcess = ProcessTasks.StartProcess(
                toolPath: "go"
                , arguments: $"version"
                , logInvocation: true
                , logOutput: true);

            goProcess.AssertZeroExitCode();

            goProcess = ProcessTasks.StartProcess(
                toolPath: "go"
                , arguments: $"get -v -u github.com/linyows/git-semv/cmd/git-semv"
                , logInvocation: true
                , logOutput: true);

            goProcess.AssertZeroExitCode();

            goProcess = ProcessTasks.StartProcess(
                toolPath: "ls"
                , arguments: $"-halF {this.ExpandedGoPath}/bin"
                , logInvocation: true
                , logOutput: true);

            goProcess.AssertZeroExitCode();
        });

    Target BumpVersion => _ => _
        .DependsOn(this.InstallGoGitSemver)
        .Requires(() => EnumExtension.IsEnumValid<Versioning.Semantic>(this.BUMP_SEMANTIC))
        .Requires(() => EnumExtension.IsEnumValid<Versioning.Stability>(this.BUMP_STABILITY))
        .Requires(() => GitTasks.GitHasCleanWorkingCopy())
        .Executes(() =>
        {
            var stability = EnumExtension.ParseDisplayName<Versioning.Stability>(this.BUMP_STABILITY);
            var semantic = EnumExtension.ParseDisplayName<Versioning.Semantic>(this.BUMP_SEMANTIC);

            var goGitSemvArguments = (stability) switch
            {
                (Versioning.Stability.Release) => $"{semantic.AsString(EnumFormat.DisplayName)}"
                , _ => $"{semantic.AsString(EnumFormat.DisplayName)} --pre {stability.AsString(EnumFormat.DisplayName)}"
            };

            var gitSemvProcess = ProcessTasks.StartProcess(
                toolPath: this.GoGitSemvToolPath
                , arguments: goGitSemvArguments
                , logInvocation: true
                , logOutput: true);

            gitSemvProcess.AssertZeroExitCode();

            var nextVersion = gitSemvProcess.Output.FirstOrDefault().Text;

            GitTasks.Git(arguments: $"tag -a {nextVersion}"
                , logInvocation: true
                , logOutput: true);

            GitTasks.Git(arguments: $"push '{this.GitHubRemoteUrl}' {nextVersion}"
                , logInvocation: true
                , logOutput: true);
        });

    Target Clean => _ => _
        .Before(this.Restore)
        .Executes(() =>
        {
            this.SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(FileSystemTasks.DeleteDirectory);
            this.TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(FileSystemTasks.DeleteDirectory);
            FileSystemTasks.EnsureCleanDirectory(this.OutputDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(setting => setting
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(this.Restore)
        .Executes(() =>
        {
            // How to use Sonar — https://github.com/nuke-build/nuke/pull/206
            //SonarScannerTasks
            //    .SonarScannerBegin(setting => setting
            //        .SetProjectKey("Rth")
            //    );

            DotNetTasks.DotNetBuild(setting => setting
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
            Console.WriteLine("Git InformationalVersion: " + this.GitVersion.InformationalVersion);

            var publishConfigurations =
                from project in new[] { this.RthProject }
                from framework in project.GetTargetFrameworks()
                select new { project, framework };

            DotNetTasks.DotNetPublish(_ => _
                    .SetNoRestore(this.InvokedTargets.Contains(this.Restore))
                    .SetConfiguration(Configuration)
                    .SetRepositoryUrl(GitRepository.HttpsUrl)
                    .SetAssemblyVersion(GitVersion.AssemblySemVer)
                    .SetFileVersion(GitVersion.AssemblySemFileVer)
                    .SetInformationalVersion(GitVersion.InformationalVersion)
                    .CombineWith(publishConfigurations, (_, v) => _
                        .SetProject(v.project)
                        .SetFramework(v.framework))
                , degreeOfParallelism: 10);

            //SonarScannerTasks.SonarScannerEnd();
        });

    Target CC => _ => _.Triggers(this.Clean, this.Compile);

    Target Pack => _ => _
        .DependsOn(this.Compile)
        .Produces(this.PackageDirectory / "*.nupkg")
        .Executes(() =>
        {
            DotNetTasks.DotNetPack(_ => _
                .SetProject(Solution)
                .SetNoBuild(this.InvokedTargets.Contains(this.Compile))
                .SetConfiguration(Configuration)
                .SetOutputDirectory(this.PackageDirectory)
                .SetVersion(GitVersion.NuGetVersionV2)
                //.SetPackageReleaseNotes(GetNuGetReleaseNotes(ChangelogFile, GitRepository))
            );
        });

    Target Publish => _ => _
        .ProceedAfterFailure()
        .DependsOn(this.BumpVersion, this.Clean, this.Test, this.Pack)
        .Consumes(this.Pack)
        .Requires(() => !String.IsNullOrWhiteSpace(this.NUGET_API_KEY) || !this.IsOriginalRepository)
        .Requires(() => GitTasks.GitHasCleanWorkingCopy())
        .Requires(() => this.Configuration.Equals(Configuration.Release))
        .Requires(() => this.IsOriginalRepository)
        //!++ We are not using GitFlow
        //.Requires(() => this.IsOriginalRepository && this.GitRepository.IsOnMasterBranch()
        //    || this.IsOriginalRepository && this.GitRepository.IsOnReleaseBranch()
        //    || !this.IsOriginalRepository && this.GitRepository.IsOnDevelopBranch()
        //)
        .Executes(() =>
        {
            ControlFlow.Assert(this.PackageFiles.Count == 1, "packages.Count == 1");

            if (this.IsOriginalRepository)
            {
                DotNetTasks.DotNetNuGetAddSource(_ => _
                    .SetSource(this.GitHubPackageSource)
                    .SetUsername(this.GitHubActions.GitHubActor)
                    .SetPassword(this.GITHUB_TOKEN)
                    .SetStorePasswordInClearText(storePasswordInClearText: true));

                DotNetTasks.DotNetNuGetPush(_ => _
                    .SetSource(this.GitHubPackageSource)
                    .SetApiKey(this.GITHUB_TOKEN)
                    .CombineWith(this.PackageFiles, (_, v) => _
                        .SetTargetPath(v))
                , degreeOfParallelism: 5
                , completeOnFailure: true);
            }

            DotNetTasks.DotNetNuGetPush(_ => _
                    .SetSource(this.NuGetPackageSource)
                    .SetApiKey(this.NUGET_API_KEY)
                    .CombineWith(this.PackageFiles, (_, v) => _
                        .SetTargetPath(v))
                , degreeOfParallelism: 5
                , completeOnFailure: true);
        });


    //+ Partition # should match number of test projects
    [Partition(1)] readonly Partition TestPartition;

    Target Test => _ => _
        .DependsOn(this.Compile)
        .Partition(() => TestPartition)
        .Executes(() =>
        {
            FileSystemTasks.DeleteDirectory(CoverageOutputFolder);

            DotNetTasks.DotNetTest(setting => setting
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
            DotNetTasks.DotNetToolInstall(setting => setting
                .SetPackageName("coveralls.net")
                .SetGlobal(false)
                .SetToolInstallationPath(this.ToolsDirectory)
                );
        });

    Target UploadCoverageToCoveralls => _ => _
        .DependsOn(this.Test, this.ToolsRestore)
        .Requires(() => COVERALLS_TOKEN)
        .Executes(() =>
        {
            CoverageOutputFolder.GlobFiles("*.opencover.xml").ForEach(openCoverAbsolutePath =>
            {
                CoverallsNetTasks.CoverallsNet(setting => setting
                    .SetRepoToken(COVERALLS_TOKEN)
                    .SetOpenCover(true)
                    .SetInput(openCoverAbsolutePath)
                    .SetProcessToolPath(this.ToolCoveralls)
                    .SetCommitBranch(this.GitRepository.Branch)
                    //!++ Should use this.GitRepository.Commit
                    //!++ Once nuke is upgraded to version 0.25
                    .SetCommitId(this.GitVersion.Sha)
                    );
            });
        });

    Target UploadCoverageToAzurePipelines => _ => _
        .DependsOn(this.Test)
        .Executes(() =>
        {
            ReportGeneratorTasks.ReportGenerator(setting => setting
                .SetReports(CoverageOutputFolder / "*.opencover.xml")
                .SetReportTypes(ReportTypes.Cobertura, ReportTypes.HtmlInline)
                .SetTargetDirectory(CoverageOutputFolder)
                .SetFramework("net5.0")
                );

            AzurePipelines?.PublishCodeCoverage(
                AzurePipelinesCodeCoverageToolType.Cobertura,
                CoverageOutputFolder / $"{nameof(ReportTypes.Cobertura)}.xml",
                CoverageOutputFolder);
        });
}
