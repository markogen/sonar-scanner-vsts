using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzureDevOpsExtension.IntegrationTests
{
    [TestClass]
    public class SonarCloudIntegrationTests
    {
        private VssConnection GetAzDoConnection()
        {
            VssBasicCredential credentials = new VssBasicCredential(string.Empty, Environment.GetEnvironmentVariable("PAT"));
            VssConnection connection = new VssConnection(new Uri(String.Concat(Constants.AZDO_BASE_URL, Constants.AZDO_ITS_ORGA)), credentials);

            return connection;
        }

        [TestMethod]
        public async Task Execute_Scannercli_Build_And_Analysis()
        {
            var scInstance = new SonarCloudCallWrapper();

            Debug.WriteLine("[Scannercli]Deleting SonarCloud project if it already exists...");
            //We first delete the project on SonarCloud
            var isProjectDeleted = await scInstance.DeleteProjectAsync(Constants.SC_SCANNERCLI_PROJECT_KEY);

            Assert.AreEqual(true, isProjectDeleted);

            Debug.WriteLine("[Scannercli]SonarCloud project has been successfully deleted...");
            Debug.WriteLine("[Scannercli]Queuing the corresponding build and waiting for its completion...");

            Build currentBuildResult = await ExecuteBuildAndWaitForCompleted(Constants.AZDO_ITS_SCANNERCLI_PIPELINE_NAME);

            Debug.WriteLine("[Scannercli]Build completed.");

            Assert.AreEqual(BuildResult.Succeeded, currentBuildResult.Result);

            //Checking results on SonarCloud's side. As all pipelines have a Publish Quality Gate Result in them, 
            //we don't need to check if background task is finished, because it should be by then.
            var ncloc = await scInstance.GetNclocForProjectAsync(Constants.SC_SCANNERCLI_PROJECT_KEY);
            var coverage = await scInstance.GetCodeCoveragePercentageForProjectAsync(Constants.SC_SCANNERCLI_PROJECT_KEY);

            Assert.AreEqual(27, ncloc);
            Assert.AreEqual(75.0, coverage);
        }

        [TestMethod]
        public async Task Execute_Gradle_Build_And_Analysis()
        {
            var scInstance = new SonarCloudCallWrapper();

            Debug.WriteLine("[Gradle]Deleting SonarCloud project if it already exists...");
            //We first delete the project on SonarCloud
            var isProjectDeleted = await scInstance.DeleteProjectAsync(Constants.SC_GRADLE_PROJECT_KEY);

            Assert.AreEqual(true, isProjectDeleted);

            Debug.WriteLine("[Gradle]SonarCloud project has been successfully deleted...");
            Debug.WriteLine("[Gradle]Queuing the corresponding build and waiting for its completion...");

            Build currentBuildResult = await ExecuteBuildAndWaitForCompleted(Constants.AZDO_ITS_GRADLE_PIPELINE_NAME);

            Debug.WriteLine("[Gradle]Build completed.");

            Assert.AreEqual(BuildResult.Succeeded, currentBuildResult.Result);

            //Checking results on SonarCloud's side. As all pipelines have a Publish Quality Gate Result in them, 
            //we don't need to check if background task is finished, because it should be by then.
            var ncloc = await scInstance.GetNclocForProjectAsync(Constants.SC_GRADLE_PROJECT_KEY);
            var coverage = await scInstance.GetCodeCoveragePercentageForProjectAsync(Constants.SC_GRADLE_PROJECT_KEY);

            Assert.AreEqual(43, ncloc);
            Assert.AreEqual(22.5, coverage);
        }

        [TestMethod]
        public async Task Execute_Maven_Build_And_Analysis()
        {
            var scInstance = new SonarCloudCallWrapper();

            Debug.WriteLine("[Maven]Deleting SonarCloud project if it already exists...");
            //We first delete the project on SonarCloud
            var isProjectDeleted = await scInstance.DeleteProjectAsync(Constants.SC_MAVEN_PROJECT_KEY);

            Assert.AreEqual(true, isProjectDeleted);

            Debug.WriteLine("[Maven]SonarCloud project has been successfully deleted...");
            Debug.WriteLine("[Maven]Queuing the corresponding build and waiting for its completion...");

            Build currentBuildResult = await ExecuteBuildAndWaitForCompleted(Constants.AZDO_ITS_MAVEN_PIPELINE_NAME);

            Debug.WriteLine("[Maven]Build completed.");

            Assert.AreEqual(BuildResult.Succeeded, currentBuildResult.Result);

            //Checking results on SonarCloud's side. As all pipelines have a Publish Quality Gate Result in them, 
            //we don't need to check if background task is finished, because it should be by then.
            var ncloc = await scInstance.GetNclocForProjectAsync(Constants.SC_MAVEN_PROJECT_KEY);
            var coverage = await scInstance.GetCodeCoveragePercentageForProjectAsync(Constants.SC_MAVEN_PROJECT_KEY);

            Assert.AreEqual(212, ncloc);
            Assert.AreEqual(23.1, coverage);
        }

        [TestMethod]
        public async Task Execute_Dotnet_Build_And_Analysis()
        {
            var scInstance = new SonarCloudCallWrapper();

            Debug.WriteLine("[Dotnet]Deleting SonarCloud project if it already exists...");
            //We first delete the project on SonarCloud
            var isProjectDeleted = await scInstance.DeleteProjectAsync(Constants.SC_DOTNET_PROJECT_KEY);

            Assert.AreEqual(true, isProjectDeleted);

            Debug.WriteLine("[Dotnet]SonarCloud project has been successfully deleted...");
            Debug.WriteLine("[Dotnet]Queuing the corresponding build and waiting for its completion...");

            Build currentBuildResult = await ExecuteBuildAndWaitForCompleted(Constants.AZDO_ITS_DOTNET_PIPELINE_NAME);

            Debug.WriteLine("[Dotnet]Build completed.");

            Assert.AreEqual(BuildResult.Succeeded, currentBuildResult.Result);

            //Checking results on SonarCloud's side. As all pipelines have a Publish Quality Gate Result in them, 
            //we don't need to check if background task is finished, because it should be by then.
            var ncloc = await scInstance.GetNclocForProjectAsync(Constants.SC_DOTNET_PROJECT_KEY);
            var coverage = await scInstance.GetCodeCoveragePercentageForProjectAsync(Constants.SC_DOTNET_PROJECT_KEY);

            Assert.AreEqual(43, ncloc);
            Assert.AreEqual(22.5, coverage);
        }

        private async Task<Build> ExecuteBuildAndWaitForCompleted(string pipelineName)
        {
            var connection = GetAzDoConnection();

            BuildHttpClient buildClient = connection.GetClient<BuildHttpClient>();

            var definitions = await buildClient.GetDefinitionsAsync(project: Constants.AZDO_ITS_PROJECT_NAME);
            var target = definitions.FirstOrDefault(d => d.Name == pipelineName);

            var queuedBuild = await buildClient.QueueBuildAsync(new Build
            {
                Definition = new DefinitionReference
                {
                    Id = target.Id
                },
                Project = target.Project
            });

            var currentBuildResult = await buildClient.GetBuildAsync(queuedBuild.Project.Id, queuedBuild.Id);

            while (currentBuildResult.Status != BuildStatus.Completed)
            {
                Debug.WriteLine("Build is not completed yet, waiting 20 more seconds...");
                Thread.Sleep(TimeSpan.FromSeconds(20));
                currentBuildResult = await buildClient.GetBuildAsync(queuedBuild.Project.Id, queuedBuild.Id);
            }

            return currentBuildResult;
        }
    }
}
