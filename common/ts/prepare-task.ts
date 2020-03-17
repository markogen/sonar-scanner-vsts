import * as path from "path";
import * as semver from "semver";
import * as tl from "azure-pipelines-task-lib/task";
import { Guid } from "guid-typescript";
import Endpoint, { EndpointType } from "./sonarqube/Endpoint";
import Scanner, { ScannerMode } from "./sonarqube/Scanner";
import { toCleanJSON } from "./helpers/utils";
import { getServerVersion } from "./helpers/request";
import * as azdoApiUtils from "./helpers/azdo-api-utils";
import { REPORT_TASK_NAME, SONAR_TEMP_DIRECTORY_NAME } from "./sonarqube/TaskReport";

const REPO_NAME_VAR = "Build.Repository.Name";

export default async function prepareTask(endpoint: Endpoint, rootPath: string) {
  if (
    endpoint.type === EndpointType.SonarQube &&
    (endpoint.url.startsWith("https://sonarcloud.io") ||
      endpoint.url.startsWith("https://sonarqube.com"))
  ) {
    tl.warning(
      "There is a dedicated extension for SonarCloud: https://marketplace.visualstudio.com/items?itemName=SonarSource.sonarcloud"
    );
  }

  const scannerMode: ScannerMode = ScannerMode[tl.getInput("scannerMode")];
  const scanner = Scanner.getPrepareScanner(rootPath, scannerMode);

  const props: { [key: string]: string } = {};

  if (await branchFeatureSupported(endpoint)) {
    await populateBranchAndPrProps(props);
    /* branchFeatureSupported method magically checks everything we need for the support of the below property, 
    so we keep it like that for now, waiting for a hardening that will refactor this (at least by renaming the method name) */
    tl.debug(
      "SonarCloud or SonarQube version >= 7.2.0 detected, setting report-task.txt file to its newest location."
    );
    props["sonar.scanner.metadataFilePath"] = reportPath();
    tl.debug(`[SQ] Branch and PR parameters: ${JSON.stringify(props)}`);
  }

  tl
    .getDelimitedInput("extraProperties", "\n")
    .filter(keyValue => !keyValue.startsWith("#"))
    .map(keyValue => keyValue.split(/=(.+)/))
    .forEach(([k, v]) => (props[k] = v));

  tl.setVariable("SONARQUBE_SCANNER_MODE", scannerMode);
  tl.setVariable("SONARQUBE_ENDPOINT", endpoint.toJson(), true);
  tl.setVariable(
    "SONARQUBE_SCANNER_PARAMS",
    toCleanJSON({
      ...endpoint.toSonarProps(),
      ...scanner.toSonarProps(),
      ...props
    })
  );

  await scanner.runPrepare();
}

async function branchFeatureSupported(endpoint) {
  if (endpoint.type === EndpointType.SonarCloud) {
    return true;
  }
  const serverVersion = await getServerVersion(endpoint);
  return serverVersion >= semver.parse("7.2.0");
}

export async function populateBranchAndPrProps(props: { [key: string]: string }) {
  props['foo'] = "bar";
}

export function reportPath(): string {
  return path.join(
    tl.getVariable("Agent.TempDirectory"),
    SONAR_TEMP_DIRECTORY_NAME,
    tl.getVariable("Build.BuildNumber"),
    Guid.create().toString(),
    REPORT_TASK_NAME
  );
}

/**
 * Waiting for https://github.com/Microsoft/vsts-tasks/issues/7592
 * query the repo to get the full name of the default branch.
 * @param collectionUrl
 */
export async function getDefaultBranch(collectionUrl: string) {
  const DEFAULT = "refs/heads/master";
  try {
    const vsts = azdoApiUtils.getWebApi(collectionUrl);
    const gitApi = await vsts.getGitApi();
    const repo = await gitApi.getRepository(
      tl.getVariable(REPO_NAME_VAR),
      tl.getVariable("System.TeamProject")
    );
    tl.debug(`Default branch of this repository is '${repo.defaultBranch}'`);
    return repo.defaultBranch;
  } catch (e) {
    tl.warning("Unable to get default branch, defaulting to 'master': " + e);
    return DEFAULT;
  }
}
