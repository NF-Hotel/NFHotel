// Starts the services of the backup target that have stopped (MIL-074), with no window.
//
// The scheduled task "\NFHotel\Backup target up" runs it at sign-in and every hour:
//   wscript.exe //B //Nologo "<folder of the backup target>\start-target.js"
// It runs `docker compose up -d` with the settings file in its own folder, which changes
// nothing while both services run, waits for it and ends with its exit code. Windows Script
// Host is a program without a console, so no window opens, and the last result of the task
// is the result of docker compose (a console host started headless would always give 0).
var COMMAND_NOT_STARTED = 1;
var WINDOW_HIDDEN = 0;
var WAIT_FOR_EXIT = true;

var shell = new ActiveXObject("WScript.Shell");
var files = new ActiveXObject("Scripting.FileSystemObject");
var folder = files.GetParentFolderName(WScript.ScriptFullName);
var docker = shell.ExpandEnvironmentStrings(
  "%ProgramFiles%\\Docker\\Docker\\resources\\bin\\docker.exe");
var command = "\"" + docker + "\" compose --project-directory \"" + folder +
  "\" --env-file \"" + folder + "\\backup-target.env\" up -d";

var exitCode;
try {
  exitCode = shell.Run(command, WINDOW_HIDDEN, WAIT_FOR_EXIT);
} catch (error) {
  // docker.exe is missing or cannot start: the task reports it as a failed run.
  exitCode = COMMAND_NOT_STARTED;
}
WScript.Quit(exitCode);
