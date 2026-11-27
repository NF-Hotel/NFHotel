// Tests of ops/deploy/backup-target/start-target.js (MIL-091), the script that the scheduled
// task "\NFHotel\Backup target up" runs with Windows Script Host on the backup host. Node.js
// runs the script in a context of its own, with stand-ins for what Windows Script Host gives
// it: WScript, ActiveXObject (WScript.Shell and Scripting.FileSystemObject), GetObject (the
// WMI classes MSFT_NetIPAddress and MSFT_NetTCPConnection) and Enumerator. Each case sets what
// docker compose ends with, which addresses and listeners the host has and what the settings
// file holds, and checks the result of the script and the order of its commands and checks.
// The last check reads register-start-task.ps1 for the delay of the run at sign-in.
//
// Usage: node start-target.test.js [<start-target.js> [<register-start-task.ps1>]]
//   The defaults are the files in ops/deploy/backup-target. Older copies, such as those of
//   main before MIL-091, show which checks fail without the change.
// Exit:  0 every check passed; 1 a check failed
// Needs: Node.js 18 or later; test-start-target.sh runs it in the Node.js image of Ops CI.
"use strict";

const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

const TARGET_FOLDER = path.join(__dirname, "..", "deploy", "backup-target");
const scriptPath = process.argv[2] || path.join(TARGET_FOLDER, "start-target.js");
const registrationPath = process.argv[3] || path.join(TARGET_FOLDER, "register-start-task.ps1");
const examplePath = path.join(TARGET_FOLDER, "backup-target.env.example");

// The backup host as the stand-ins show it.
const HOST_FOLDER = "C:\\Users\\host\\nfhotel-backup-target";
const SETTINGS_FILE = `${HOST_FOLDER}\\backup-target.env`;
const DOCKER = "C:\\Program Files\\Docker\\Docker\\resources\\bin\\docker.exe";
const COMPOSE = `"${DOCKER}" compose --project-directory "${HOST_FOLDER}" --env-file "${SETTINGS_FILE}" `;
const TAILNET_ADDRESS = "100.64.0.1"; // BACKUP_TARGET_BIND of backup-target.env.example
const HOME_ADDRESS = "192.168.1.20";
const FOR_READING = 1;
const TCP_LISTEN = 2;
const TCP_ESTABLISHED = 5;
// The results of start-target.js, which the scheduled task shows as its last result.
const RESULT = { portAnswers: 0, composeFailed: 1, noAddress: 2, noPort: 3, cannotCheck: 4 };
// The longest that one run may wait in all, well within the 10 minutes of the task.
const MAX_WAIT_MS = 60000;

const exampleLines = fs.readFileSync(examplePath, "utf8").split(/\r?\n/);

// The example settings file as a copy on Windows has it (CRLF), with some lines changed: a
// value replaces the value of its key, and null removes the line.
function settingsWith(changes = {}) {
  const lines = [];
  for (const line of exampleLines) {
    const key = line.startsWith("#") ? "" : line.split("=")[0];
    if (!Object.hasOwn(changes, key)) {
      lines.push(line);
    } else if (changes[key] !== null) {
      lines.push(`${key}=${changes[key]}`);
    }
  }
  return lines.join("\r\n");
}

// A backup host on which everything works; each case changes what it needs.
function host(changes = {}) {
  return {
    settings: settingsWith(),
    upExit: 0,
    restartExit: 0,
    canStartDocker: true,
    hasWmi: true,
    addresses: [HOME_ADDRESS, TAILNET_ADDRESS],
    listenerPort: 2222,
    listening: "always", // "always", "after the restart" or "never"
    ...changes,
  };
}

// The end of a run of the script, as WScript.Quit gives it.
class Quit extends Error {
  constructor(code) {
    super(`WScript.Quit(${code})`);
    this.code = code;
  }
}

// The Enumerator of JScript over a collection of WMI.
function Enumerator(collection) {
  this.rows = collection.rows;
  this.index = 0;
}
Enumerator.prototype.atEnd = function atEnd() {
  return this.index >= this.rows.length;
};
Enumerator.prototype.moveNext = function moveNext() {
  this.index += 1;
};
Enumerator.prototype.item = function item() {
  return this.rows[this.index];
};

function collection(rows) {
  return { rows, Count: rows.length };
}

// Runs start-target.js on a stand-in host and records, in order, the steps it took: "up" and
// "restart" for the docker compose commands, "address" and "port" for the checks.
function run(source, state) {
  const steps = [];
  const commands = [];
  const queries = [];
  const sleeps = [];
  const unexpected = [];
  let hasRestarted = false;

  const shell = {
    ExpandEnvironmentStrings: (text) => text.split("%ProgramFiles%").join("C:\\Program Files"),
    Run: (command, window, wait) => {
      commands.push({ command, window, wait });
      let exitCode = 0;
      if (command === `${COMPOSE}up -d`) {
        steps.push("up");
        exitCode = state.upExit;
      } else if (command === `${COMPOSE}restart backup-target`) {
        steps.push("restart");
        hasRestarted = true;
        exitCode = state.restartExit;
      } else {
        unexpected.push(command);
      }
      if (!state.canStartDocker) {
        throw new Error("The system cannot find the file specified.");
      }
      return exitCode;
    },
  };
  const files = {
    GetParentFolderName: (filePath) => filePath.substring(0, filePath.lastIndexOf("\\")),
    OpenTextFile: (filePath, mode) => {
      if (filePath !== SETTINGS_FILE || mode !== FOR_READING || state.settings === null) {
        throw new Error(`File not found: ${filePath}`);
      }
      const text = state.settings;
      return {
        AtEndOfStream: text.length === 0,
        ReadAll: () => {
          if (text.length === 0) {
            throw new Error("Input past end of file");
          }
          return text;
        },
        Close: () => {},
      };
    },
  };
  const listeners = (query) => {
    // Neither of the first two counts: a listener on another address, and a connection on the
    // address of the target that is not a listener.
    const rows = [
      { LocalAddress: "127.0.0.1", LocalPort: state.listenerPort, State: TCP_LISTEN },
      { LocalAddress: TAILNET_ADDRESS, LocalPort: state.listenerPort, State: TCP_ESTABLISHED },
    ];
    if (state.listening === "always" || (state.listening === "after the restart" && hasRestarted)) {
      rows.push({ LocalAddress: TAILNET_ADDRESS, LocalPort: state.listenerPort, State: TCP_LISTEN });
    }
    const port = /\bLocalPort\s*=\s*(\d+)/i.exec(query);
    return port ? rows.filter((row) => row.LocalPort === Number(port[1])) : rows;
  };
  const wmi = {
    ExecQuery: (query) => {
      queries.push(query);
      if (/\bFROM\s+MSFT_NetIPAddress\b/i.test(query)) {
        steps.push("address");
        return collection(state.addresses.map((address) => ({ IPAddress: address })));
      }
      if (/\bFROM\s+MSFT_NetTCPConnection\b/i.test(query)) {
        steps.push("port");
        return collection(listeners(query));
      }
      unexpected.push(query);
      return collection([]);
    },
  };
  const context = vm.createContext({
    WScript: {
      ScriptFullName: `${HOST_FOLDER}\\start-target.js`,
      Quit: (code) => {
        throw new Quit(code);
      },
      Sleep: (milliseconds) => {
        sleeps.push(milliseconds);
      },
    },
    ActiveXObject: function ActiveXObject(progId) {
      if (progId === "WScript.Shell") {
        return shell;
      }
      if (progId === "Scripting.FileSystemObject") {
        return files;
      }
      throw new Error(`no stand-in for ${progId}`);
    },
    GetObject: (moniker) => {
      if (!state.hasWmi || !/^winmgmts:.*root\\StandardCimv2$/i.test(moniker)) {
        throw new Error(`WMI does not answer: ${moniker}`);
      }
      return wmi;
    },
    Enumerator,
  });

  let result;
  try {
    vm.runInContext(source, context, { filename: path.basename(scriptPath), timeout: 5000 });
    result = "no WScript.Quit";
  } catch (error) {
    result = error instanceof Quit ? error.code : `error: ${error.message}`;
  }
  return { result, steps: steps.join(" "), commands, queries, sleeps, unexpected };
}

let passed = 0;
let failed = 0;
function check(name, isPassed, details) {
  if (isPassed) {
    passed += 1;
    console.log(`PASS  ${name}`);
  } else {
    failed += 1;
    console.log(`FAIL  ${name}\n      ${details}`);
  }
}

const source = fs.readFileSync(scriptPath, "utf8");
const outcomes = [];

function describe(outcome) {
  const unexpected = outcome.unexpected.length ? `; unexpected: ${outcome.unexpected.join(" | ")}` : "";
  return `result ${outcome.result}; steps: ${outcome.steps || "none"}${unexpected}`;
}

// Runs one case and checks its result and the order of its steps.
function expectRun(name, state, result, steps) {
  const outcome = run(source, state);
  outcomes.push(outcome);
  check(name, outcome.result === result && steps.test(outcome.steps) && outcome.unexpected.length === 0,
    describe(outcome));
  return outcome;
}

// The five cases of criterion 1 of MIL-091.
const answering = expectRun("a port that answers: the address and the port are checked, nothing is restarted, result 0",
  host(), RESULT.portAnswers, /^up address port$/);
expectRun("no listener while the host has the address: one restart, then the port answers, result 0",
  host({ listening: "after the restart" }), RESULT.portAnswers, /^up address( port)+ restart port$/);
const neverAnswering = expectRun(
  "no listener after the restart either: no second restart, result 3 (a listener on another address and a connection that is not a listener do not count)",
  host({ listening: "never" }), RESULT.noPort, /^up address( port)+ restart( port)+$/);
expectRun("the host lacks the tailnet address: nothing is restarted, result 2",
  host({ listening: "never", addresses: [HOME_ADDRESS] }), RESULT.noAddress, /^up address$/);
expectRun("docker compose up -d fails: nothing is checked or restarted, result 1",
  host({ upExit: 18 }), RESULT.composeFailed, /^up$/);

// docker compose that cannot run.
expectRun("docker.exe cannot start: result 1", host({ canStartDocker: false }), RESULT.composeFailed, /^up$/);
expectRun("the restart fails: result 1", host({ listening: "never", restartExit: 1 }), RESULT.composeFailed,
  /^up address( port)+ restart$/);

// The settings file.
const otherPort = expectRun("BACKUP_TARGET_PORT=2223: the port of the settings file is the one checked",
  host({ settings: settingsWith({ BACKUP_TARGET_PORT: "2223" }), listenerPort: 2223 }), RESULT.portAnswers,
  /^up address port$/);
check("the check asks for the listeners of port 2223", otherPort.queries.some((query) => /LocalPort\s*=\s*2223\b/.test(query)),
  otherPort.queries.join(" | "));
expectRun("a listener on another port than BACKUP_TARGET_PORT does not count, result 3",
  host({ settings: settingsWith({ BACKUP_TARGET_PORT: "2223" }) }), RESULT.noPort, /^up address( port)+ restart( port)+$/);
const defaultPort = expectRun("without BACKUP_TARGET_PORT the port is 2222",
  host({ settings: settingsWith({ BACKUP_TARGET_PORT: null }) }), RESULT.portAnswers, /^up address port$/);
check("the check asks for the listeners of port 2222", defaultPort.queries.some((query) => /LocalPort\s*=\s*2222\b/.test(query)),
  defaultPort.queries.join(" | "));
expectRun("an empty BACKUP_TARGET_PORT is 2222 too", host({ settings: settingsWith({ BACKUP_TARGET_PORT: "" }) }),
  RESULT.portAnswers, /^up address port$/);
expectRun("a comment after a value and quotes around a value are read as docker compose reads them",
  host({ settings: settingsWith({ BACKUP_TARGET_BIND: `${TAILNET_ADDRESS} # tailscale ip -4`, BACKUP_TARGET_PORT: "\"2222\"" }) }),
  RESULT.portAnswers, /^up address port$/);
expectRun("a quoted value with a comment after it is the text inside the quotes, as docker compose reads it",
  host({ settings: settingsWith({ BACKUP_TARGET_BIND: `'${TAILNET_ADDRESS}' # tailscale ip -4`, BACKUP_TARGET_PORT: "\"2222\" # the default" }) }),
  RESULT.portAnswers, /^up address port$/);
for (const [name, settings] of [
  ["BACKUP_TARGET_BIND that is not an address", settingsWith({ BACKUP_TARGET_BIND: "tailnet-host" })],
  ["no BACKUP_TARGET_BIND", settingsWith({ BACKUP_TARGET_BIND: null })],
  ["BACKUP_TARGET_PORT above 65535", settingsWith({ BACKUP_TARGET_PORT: "65536" })],
  ["BACKUP_TARGET_PORT 0", settingsWith({ BACKUP_TARGET_PORT: "0" })],
  ["an empty settings file", ""],
  ["no settings file", null],
]) {
  expectRun(`${name}: nothing is checked or restarted, result 4`, host({ settings }), RESULT.cannotCheck, /^up$/);
}

// WMI.
expectRun("WMI does not answer: nothing is restarted, result 4", host({ hasWmi: false }), RESULT.cannotCheck, /^up$/);

// How the commands run, and how long a run waits.
const commands = outcomes.flatMap((outcome) => outcome.commands);
check("every docker compose command runs hidden, and the script waits for it",
  commands.length > 0 && commands.every((command) => command.window === 0 && command.wait === true),
  JSON.stringify(commands.filter((command) => command.window !== 0 || command.wait !== true)));
check("a port that answers at once costs no wait", answering.sleeps.length === 0, `waits: ${answering.sleeps.join(", ")}`);
const longestWait = Math.max(...outcomes.map((outcome) => outcome.sleeps.reduce((sum, milliseconds) => sum + milliseconds, 0)));
check(`no run waits more than ${MAX_WAIT_MS / 1000} seconds in all`, longestWait <= MAX_WAIT_MS && neverAnswering.sleeps.length > 0,
  `longest wait: ${longestWait} ms; waits without a listener: ${neverAnswering.sleeps.length}`);

// Windows Script Host runs JScript 5.8: ECMAScript 3, without let, const, arrow functions,
// template strings, JSON or the array and string methods of ECMAScript 5 and later.
const code = source.split("\n")
  .map((line) => line.replace(/^\s*\/\/.*$/, "").replace(/\s\/\/\s.*$/, ""))
  .join("\n");
const newer = [/\blet\s/, /\bconst\s/, /\bclass\s/, /=>/, /`/, /\bJSON\./, /\bObject\.keys\(/, /\bArray\.isArray\(/,
  /\.(trim|forEach|map|filter|reduce|some|every|includes|startsWith|endsWith|find|padStart)\(/];
const found = newer.filter((pattern) => pattern.test(code)).map(String);
check("start-target.js uses only what JScript 5.8 has", found.length === 0, `found: ${found.join(" ")}`);

// The scheduled task: the run at sign-in waits 5 minutes, and the hourly run stays.
const registration = fs.readFileSync(registrationPath, "utf8");
check("register-start-task.ps1 delays the run at sign-in by 5 minutes and keeps the hourly run",
  /\$signInDelay\s*=\s*'PT5M'/.test(registration) && /\$atSignIn\.Delay\s*=\s*\$signInDelay\b/.test(registration) &&
    /-Trigger\s+\$atSignIn,\s*\$hourly\b/.test(registration) && /-RepetitionInterval\s+\$repeatEvery\b/.test(registration),
  "the trigger at sign-in has no Delay of PT5M, or the hourly trigger is missing");

console.log(`${passed} passed, ${failed} failed`);
process.exitCode = failed === 0 ? 0 : 1;
