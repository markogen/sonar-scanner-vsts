namespace AzureDevOpsExtension.IntegrationTests
{
    public static class Constants
    {
        public static string SONARCLOUD_BASE_URL = "https://sonarcloud.io/";

        #region Azure DevOps Server

        public static string AZDO_SERVER_BASE_URL = "http://mickael-caro/";

        public static string AZDO_SERVER_COLLECTION = "DefaultCollection";
        public static string AZDO_SERVER_PROJECT_NAME = "IntegrationTests";



        #endregion

        #region Azure DevOps Services

        public static string AZDO_BASE_URL = "https://dev.azure.com/";

        public static string AZDO_ITS_ORGA = "sonar-azdo-its-orga";
        public static string AZDO_ITS_PROJECT_NAME = "sonar-its-projects";

        public static string AZDO_ITS_DOTNET_PIPELINE_NAME = "dotnet";
        public static string AZDO_ITS_MAVEN_PIPELINE_NAME = "maven";
        public static string AZDO_ITS_GRADLE_PIPELINE_NAME = "gradle";
        public static string AZDO_ITS_SCANNERCLI_PIPELINE_NAME = "scannercli-swift";

        #endregion

        public static string SC_ITS_ORGA = "azdo-extension-its";

        //Don't forget to change projectKey in respectives builds pipelines if changed here first.
        public static string SC_DOTNET_PROJECT_KEY = "its-dotnet";
        public static string SC_MAVEN_PROJECT_KEY = "its-maven";
        public static string SC_GRADLE_PROJECT_KEY = "its-gradle";
        public static string SC_SCANNERCLI_PROJECT_KEY = "its-swift";
    }
}
