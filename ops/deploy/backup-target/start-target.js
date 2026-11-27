// Starts the services of the backup target that have stopped (MIL-074), and restarts the
// service backup-target once when it runs without its published port (MIL-091), with no window.
//
// The scheduled task "\NFHotel\Backup target up" runs it 5 minutes after sign-in and every hour:
//   wscript.exe //B //Nologo "<folder of the backup target>\start-target.js"
// It runs `docker compose up -d` with the settings file in its own folder, which changes nothing
// while both services run. A service that Docker Desktop started before the host had its
// tailnet address runs without its port and still looks Up, so up -d leaves it as it is. So the
// script then checks that something listens on the address and port of backup-target.env
// (BACKUP_TARGET_BIND, and BACKUP_TARGET_PORT, 2222 when it is not set). When nothing listens
// there while the host has the address, it restarts the service backup-target once, which
// publishes the port again, and checks again. While the host lacks the address, it changes
// nothing, and the next run tries again.
//
// Windows Script Host is a program without a console, so no window opens, and the last result
// of the task is the result of this script (a console host started headless would always give 0):
//   0  the port answers
//   1  docker compose failed, or docker.exe could not start
//   2  the host does not have the address of BACKUP_TARGET_BIND (Tailscale is not up yet)
//   3  the port does not answer after the one restart
//   4  the check could not run: backup-target.env gives no valid address or port, or Windows
//      Management Instrumentation (WMI) did not answer
// The addresses and listeners come from WMI (root\StandardCimv2, the classes behind
// Get-NetIPAddress and Get-NetTCPConnection), so no console program runs and nothing depends on
// the language of Windows. Windows Script Host runs JScript 5.8 (ECMAScript 3): var, and no JSON
// or array methods of later editions. Tests: ops/tests/test-start-target.sh.
var PORT_ANSWERS = 0;
var COMPOSE_FAILED = 1;
var NO_ADDRESS = 2;
var NO_PORT = 3;
var CANNOT_CHECK = 4;

var WINDOW_HIDDEN = 0;
var WAIT_FOR_EXIT = true;
var FOR_READING = 1;
var TCP_STATE_LISTEN = 2;
var DEFAULT_PORT = "2222";
var HIGHEST_PORT = 65535;
var HIGHEST_BYTE = 255;
// A service that has just started needs a few seconds to publish its port: up to 10 checks,
// 2 seconds apart, after the start and again after the restart.
var PORT_CHECKS = 10;
var PORT_CHECK_PAUSE_MS = 2000;
var IPV4_ADDRESS = /^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})$/;
var IPV6_ADDRESS = /^[0-9a-f]{0,4}(:[0-9a-f]{0,4}){2,7}$/i;
var DIGITS = /^\d{1,5}$/;
var SPACE = /\s/;

var shell = new ActiveXObject("WScript.Shell");
var files = new ActiveXObject("Scripting.FileSystemObject");
var folder = files.GetParentFolderName(WScript.ScriptFullName);
var settingsFile = folder + "\\backup-target.env";
var docker = shell.ExpandEnvironmentStrings(
  "%ProgramFiles%\\Docker\\Docker\\resources\\bin\\docker.exe");
var compose = "\"" + docker + "\" compose --project-directory \"" + folder +
  "\" --env-file \"" + settingsFile + "\" ";
var wmi = null;

// The text without the white space at its ends. A loop, because /\s+$/ backtracks over each run
// of white space inside the text, so its time would grow with the square of the length of a line.
function trim(text) {
  var start = 0;
  var end = text.length;
  while (start < end && SPACE.test(text.charAt(start))) {
    start++;
  }
  while (end > start && SPACE.test(text.charAt(end - 1))) {
    end--;
  }
  return text.substring(start, end);
}

// Runs docker compose with the given arguments, hidden, waits for it and tells whether it
// succeeded.
function composeSucceeds(args) {
  try {
    return shell.Run(compose + args, WINDOW_HIDDEN, WAIT_FOR_EXIT) === 0;
  } catch (error) {
    // docker.exe is missing or cannot start.
    return false;
  }
}

// A value as docker compose reads it from a settings file: the text inside its quotes, so a
// comment after the closing quote is left out, or, when it has none, the text before a comment
// after " #". An address and a port have no escapes, so none is read.
function plainValue(raw) {
  var value = trim(raw);
  var quote = value.charAt(0);
  var closingQuote = quote === "\"" || quote === "'" ? value.indexOf(quote, 1) : -1;
  if (closingQuote > 0) {
    return value.substring(1, closingQuote);
  }
  var comment = value.indexOf(" #");
  return comment < 0 ? value : trim(value.substring(0, comment));
}

// The settings of backup-target.env: KEY=VALUE lines, and # for comments.
function readSettings() {
  var stream = files.OpenTextFile(settingsFile, FOR_READING);
  var text = stream.AtEndOfStream ? "" : stream.ReadAll();
  stream.Close();
  var values = {};
  var lines = text.split(/\r?\n/);
  for (var i = 0; i < lines.length; i++) {
    var line = trim(lines[i]);
    var equals = line.indexOf("=");
    if (line.charAt(0) !== "#" && equals > 0) {
      values[trim(line.substring(0, equals))] = plainValue(line.substring(equals + 1));
    }
  }
  return values;
}

// Number() reads the digits, which the expressions have already checked, so nothing is left for
// parseInt to cut off; JScript 5.8 has no Number.parseInt.
function isAddress(text) {
  var parts = IPV4_ADDRESS.exec(text);
  if (parts === null) {
    return IPV6_ADDRESS.test(text);
  }
  for (var i = 1; i < parts.length; i++) {
    if (Number(parts[i]) > HIGHEST_BYTE) {
      return false;
    }
  }
  return true;
}

function isPort(text) {
  if (!DIGITS.test(text)) {
    return false;
  }
  var port = Number(text);
  return port >= 1 && port <= HIGHEST_PORT;
}

// The address and port on which the service publishes its ssh, from backup-target.env, or null
// when the file cannot be read or gives no valid address or port.
function readTarget() {
  var values;
  try {
    values = readSettings();
  } catch (error) {
    // The file is missing or cannot be read; docker compose has just read it, so this is rare.
    return null;
  }
  var address = values.BACKUP_TARGET_BIND || "";
  var port = values.BACKUP_TARGET_PORT || DEFAULT_PORT;
  if (!isAddress(address) || !isPort(port)) {
    return null;
  }
  return { address: address.toLowerCase(), port: port };
}

// The rows of a query of WMI in root\StandardCimv2, as an array.
function wmiRows(query) {
  if (wmi === null) {
    wmi = GetObject("winmgmts:\\\\.\\root\\StandardCimv2");
  }
  var rows = [];
  for (var items = new Enumerator(wmi.ExecQuery(query)); !items.atEnd(); items.moveNext()) {
    rows.push(items.item());
  }
  return rows;
}

function hostHasAddress(address) {
  var rows = wmiRows("SELECT IPAddress FROM MSFT_NetIPAddress");
  for (var i = 0; i < rows.length; i++) {
    if (String(rows[i].IPAddress).toLowerCase() === address) {
      return true;
    }
  }
  return false;
}

function hasListener(target) {
  var rows = wmiRows("SELECT LocalAddress, State FROM MSFT_NetTCPConnection WHERE LocalPort = " +
    target.port);
  for (var i = 0; i < rows.length; i++) {
    if (Number(rows[i].State) === TCP_STATE_LISTEN &&
        String(rows[i].LocalAddress).toLowerCase() === target.address) {
      return true;
    }
  }
  return false;
}

function hasListenerWithin(target) {
  for (var check = 1; check <= PORT_CHECKS; check++) {
    if (hasListener(target)) {
      return true;
    }
    if (check < PORT_CHECKS) {
      WScript.Sleep(PORT_CHECK_PAUSE_MS);
    }
  }
  return false;
}

// Checks the port of a running target and restarts the service once when it has no listener.
function mendPort(target) {
  if (!hostHasAddress(target.address)) {
    return NO_ADDRESS;
  }
  if (hasListenerWithin(target)) {
    return PORT_ANSWERS;
  }
  if (!composeSucceeds("restart backup-target")) {
    return COMPOSE_FAILED;
  }
  return hasListenerWithin(target) ? PORT_ANSWERS : NO_PORT;
}

function startTarget() {
  if (!composeSucceeds("up -d")) {
    return COMPOSE_FAILED;
  }
  var target = readTarget();
  if (target === null) {
    return CANNOT_CHECK;
  }
  try {
    return mendPort(target);
  } catch (error) {
    // WMI did not answer, so nothing is known about the address or the port.
    return CANNOT_CHECK;
  }
}

WScript.Quit(startTarget());
