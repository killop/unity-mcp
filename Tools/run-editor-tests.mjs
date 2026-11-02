import fs from "node:fs";
import path from "node:path";
import { spawnSync } from "node:child_process";

if (!process.argv[2] || process.argv[2].startsWith("--")) {
  throw new Error("Usage: node Tools/run-editor-tests.mjs <UnityProject> [--unity=<Unity.exe>] [--package=<name>] [--assembly=<name>]");
}
const project = path.resolve(process.argv[2]);
const unity = process.argv.find(arg => arg.startsWith("--unity="))?.slice("--unity=".length)
  || process.env.UNITY_EDITOR_PATH
  || "C:/Program Files/Unity/Hub/Editor/2022.3.60f1/Editor/Unity.exe";
const configureOnly = process.argv.includes("--configure-only");
const assembly = process.argv.find(arg => arg.startsWith("--assembly="))?.slice("--assembly=".length)
  || "UnityMcp.Editor.Tests";
const packageName = process.argv.find(arg => arg.startsWith("--package="))?.slice("--package=".length)
  || "unity-mcp";
const manifestPath = path.join(project, "Packages", "manifest.json");
const manifest = JSON.parse(fs.readFileSync(manifestPath, "utf8"));
manifest.dependencies["com.unity.test-framework"] = "1.1.33";
manifest.testables = [...new Set([...(manifest.testables || []), packageName])];
fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2) + "\n");
console.log(`Configured Test Runner for ${project}`);
if (configureOnly) process.exit(0);

const resultDir = path.join(project, "TestResults");
fs.mkdirSync(resultDir, { recursive: true });
const resultPath = path.join(resultDir, assembly + "-editmode.xml");
const logPath = path.join(resultDir, assembly + "-editmode.log");
fs.rmSync(resultPath, { force: true });
const args = ["-batchmode", "-nographics", "-projectPath", project,
  "-runTests", "-testPlatform", "EditMode", "-assemblyNames", assembly,
  "-testResults", resultPath, "-logFile", logPath];
console.log(`Running Unity EditMode tests; log: ${logPath}`);
const run = spawnSync(unity, args, { stdio: "inherit", windowsHide: true });
if (run.error) throw run.error;
console.log(`Unity exit code: ${run.status}`);
if (fs.existsSync(resultPath)) {
  const xml = fs.readFileSync(resultPath, "utf8");
  const suite = /<test-run\b[^>]*\btotal="(\d+)"[^>]*\bpassed="(\d+)"[^>]*\bfailed="(\d+)"[^>]*\binconclusive="(\d+)"[^>]*\bskipped="(\d+)"/.exec(xml);
  console.log(suite ? `Result: total=${suite[1]}, passed=${suite[2]}, failed=${suite[3]}, inconclusive=${suite[4]}, skipped=${suite[5]}` : "Result XML present, summary not parsed");
  console.log(`Result XML: ${resultPath}`);
} else {
  console.log("No result XML was generated");
}
process.exit(run.status || (fs.existsSync(resultPath) ? 0 : 1));
