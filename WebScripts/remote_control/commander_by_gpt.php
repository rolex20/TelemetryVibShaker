<?php
// commander.php (HUD redesign) — same backend behavior, improved mobile UI.
//
// Original behaviors preserved:
// - Writes JSON commands to command.tmp then renames to command.json
// - Launch / Terminate / Foreground / GetValues / Move|Min|Restore|Max
// - Special commands (Watchdog, Power schemes, Show Threads, Boost 1-3)
// - Pipe commands (PIPE: pipename|message)
// - Reads programs from programs.json
// - GetValues waits for outfile.txt and hydrates X/Y
//
// NOTE: This file keeps POST field names and submit names/values identical to the original.

header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: GET, POST, OPTIONS");
header("Access-Control-Allow-Headers: Content-Type");

// Handle CORS preflight cleanly (doesn't affect GET/POST behavior)
if ($_SERVER["REQUEST_METHOD"] === "OPTIONS") {
    http_response_code(204);
    exit;
}

const COMMAND_FILE  = 'command.json';
const TEMP_FILE     = 'command.tmp';
const WATCHDOG_FILE = 'watchdog.txt';
const PROGRAMS_JSON = 'programs.json';

function getProgramsList() {
    if (!file_exists(PROGRAMS_JSON)) {
        return array();
    }
    $jsonData = file_get_contents(PROGRAMS_JSON);
    $data = json_decode($jsonData, true);

    // Compatible with older PHP: avoid ?? operator
    return isset($data['programs']) ? $data['programs'] : array();
}

function createJsonCommand($commandType, $parameters = array()) {
    $data = array(
        "command_type" => $commandType,
        "parameters" => $parameters
    );
    return json_encode($data);
}

function writeJsonToFile($jsonData, $filePath) {
    return file_put_contents($filePath, $jsonData);
}

function createSpan($color, $text) {
    return "<span style='color: $color;'>$text</span>";
}

function tryOpenFile($filename, $retries, $delay_ms) {
    $attempt = 0;
    $file = false;

    while ($attempt < $retries) {
        // delay first, to allow WaitFor-Json-Commands.ps1 to complete
        usleep($delay_ms * 1000);
        $file = @fopen($filename, "r");
        if ($file) {
            return $file;
        }
        $attempt++;
    }
    return false;
}

function renameFile($existingFileName, $newFileName) {
    // Check if the new file name already exists and delete it
    if (file_exists($newFileName)) {
        if (!unlink($newFileName)) {
            return false;
        }
    }
    // Check if the existing file exists
    if (file_exists($existingFileName)) {
        return rename($existingFileName, $newFileName);
    }
    return false;
}

function getTimestamp() {
    $microtime = microtime(true);
    $milliseconds = sprintf("%03d", ($microtime - floor($microtime)) * 1000);
    return date("Y-m-d H:i:s", (int)$microtime) . ".$milliseconds";
}

function getPathByProcessName($jsonFilePath, $processName) {
    $jsonData = file_get_contents($jsonFilePath);
    $data = json_decode($jsonData, true);
    if ($data === null) {
        die('Error decoding JSON');
    }
    foreach ($data['programs'] as $program) {
        if ($program['processName'] === $processName) {
            return $program['path'];
        }
    }
    return null;
}

function h($s) {
    return htmlspecialchars((string)$s, ENT_QUOTES | ENT_SUBSTITUTE, 'UTF-8');
}

// -------------------------
// Initialize variables
// -------------------------
$footer = "Ready";
$time_stamp = getTimestamp();

// Default location coordinates when not included in POST['X']
$frmX = "-1700";
$frmY = "";

$frm_instance = 0;
$frm_process = "";
$frm_pipe_command = isset($_POST['PipeCommand']) ? $_POST['PipeCommand'] : "";

// -------------------------
// Handle POST (same behavior as original)
// -------------------------
if ($_SERVER["REQUEST_METHOD"] == "POST") {

    // These were required in the original form; keep same assumptions.
    $frmX = htmlspecialchars($_POST['X']);
    $frmY = htmlspecialchars($_POST['Y']);
    $frm_instance = htmlspecialchars($_POST['Instance']);

    $threads = is_numeric($frm_instance) ? (int)$frm_instance : 50; // busiest threads
    if ($threads <= 0) $threads = 50;

    $frm_process = htmlspecialchars($_POST['Process']);

    // CHECK FOR PIPE/SPECIAL COMMAND
    $post_command = isset($_POST['Command']) ? $_POST['Command'] : "";
    if ($post_command == "Command") {
        $special_command = isset($_POST['SpecialCommand']) ? $_POST['SpecialCommand'] : "";
        $pipe_tuple = isset($_POST['PipeCommand']) ? $_POST['PipeCommand'] : "";

        switch ($special_command) {
            case "WATCHDOG":
                $footer = "Watchdog with sound.";
                $jsonData = createJsonCommand("WATCHDOG", array("outfile" => WATCHDOG_FILE, "sound" => 1));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);
                break;

            case "HIGHPERFORMANCE":
                $footer = "2 Set Power Scheme to High Power";
                $jsonData = createJsonCommand("POWERSCHEME", array("schemeName" => "High Performance"));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);
                break;

            case "BALANCED":
                $footer = "Set Power Scheme to Balanced";
                $jsonData = createJsonCommand("POWERSCHEME", array("schemeName" => "Balanced"));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);
                break;

            case "BALANCED80":
                $footer = "Set Power Scheme to Balanced Max CPU 80%";
                $jsonData = createJsonCommand("POWERSCHEME", array("schemeName" => "Balanced with Max 80"));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);
                break;

            case "READPOWERSCHEME":
                $footer = "2Read current power plan";
                $jsonData = createJsonCommand("READPOWERSCHEME", array("schemeName" => "NA"));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);
                break;

            case "SHOW_THREADS":
                $footer = "Show Threads for VR Flight Games";
                $jsonData = createJsonCommand("SHOW_THREADS", array("threadsLimit" => $threads));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);
                break;

            case "BOOST_1":
                $footer = "Boost Level 1: AboveNormal_Priority + Reassign-Ideal-Processors";
                $jsonData = createJsonCommand("GAME_BOOST", array(
                    "processName" => "FlightSimulator",
                    "jsonFile" => "C:\\MyPrograms\\My Apps\\TelemetryVibShaker\\WebScripts\\ps_scripts\\action-per-process-boost1.json",
                    "threadsLimit" => $threads
                ));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);
                break;

            case "BOOST_2":
                $footer = "Boost Level 2: AboveNormal_Priority + Reassign-Ideal-Processors + CPU_SETS";
                $jsonData = createJsonCommand("GAME_BOOST", array(
                    "processName" => "FlightSimulator",
                    "jsonFile" => "C:\\MyPrograms\\My Apps\\TelemetryVibShaker\\WebScripts\\ps_scripts\\action-per-process-boost2.json",
                    "threadsLimit" => $threads
                ));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);
                break;

            case "BOOST_3":
                $footer = "Boost Level 3: AboveNormal_Priority + Reassign-Ideal-Processors + Hard-Affinities";
                $jsonData = createJsonCommand("GAME_BOOST", array(
                    "processName" => "FlightSimulator",
                    "jsonFile" => "C:\\MyPrograms\\My Apps\\TelemetryVibShaker\\WebScripts\\ps_scripts\\action-per-process-boost3.json",
                    "threadsLimit" => $threads
                ));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);
                break;

            default:
                if (strpos($pipe_tuple, '|') !== false) {
                    $pipe_items = explode('|', $pipe_tuple);
                    $pipename = $pipe_items[0];
                    $message = $pipe_items[1];
                    $footer = "Pipe [$pipename] - [$message]";
                    $jsonData = createJsonCommand("PIPE", array("pipename" => $pipename, "message" => $message));
                    writeJsonToFile($jsonData, TEMP_FILE);
                    renameFile(TEMP_FILE, COMMAND_FILE);
                }
                break;
        }

        $time_stamp = getTimestamp();
    } // end-if Command

    // CHECK FOR LAUNCH COMMAND
    $post_command = isset($_POST['Launch']) ? $_POST['Launch'] : "";
    if ($post_command == "Launch") {
        $processName = $_POST['Process'];
        $path = getPathByProcessName(PROGRAMS_JSON, $processName);

        if ($path !== null) {
            $footer = "Launch completed - '$processName': $path\n";
        } else {
            $footer = "Launch process '$processName' not found.\n";
        }

        $jsonData = createJsonCommand("RUN", array("program" => $path, "parameter1" => "not-used"));
        writeJsonToFile($jsonData, TEMP_FILE);
        renameFile(TEMP_FILE, COMMAND_FILE);
        $time_stamp = getTimestamp();
    }

    // CHECK FOR TERMINATE COMMAND
    $post_command = isset($_POST['Exit']) ? $_POST['Exit'] : "";
    if ($post_command == "Terminate") {
        $processName = $_POST['Process'];
        $footer = "Terminate completed - [$processName]";

        $jsonData = createJsonCommand("KILL", array("processName" => $processName));
        writeJsonToFile($jsonData, TEMP_FILE);
        renameFile(TEMP_FILE, COMMAND_FILE);
        $time_stamp = getTimestamp();
    }

    // CHECK FOR FOREGROUND COMMAND
    $post_command = isset($_POST['MakeForeground']) ? $_POST['MakeForeground'] : "";
    if ($post_command == "Make Foreground") {
        $processName = $_POST['Process'];
        $footer = "Foreground completed - [$processName]";

        $jsonData = createJsonCommand("FOREGROUND", array("processName" => $processName, "instance" => $frm_instance));
        writeJsonToFile($jsonData, TEMP_FILE);
        renameFile(TEMP_FILE, COMMAND_FILE);
        $time_stamp = getTimestamp();
    }

    // CHECK FOR GET-VALUES COMMAND
    $post_command = isset($_POST['GetValues']) ? $_POST['GetValues'] : "";
    if ($post_command == "GetValues") {
        $processName = $_POST['Process'];
        $footer = "GetValues completed - [$processName]";

        $outfile = "C:\\MyPrograms\\wamp\\www\\remote_control\\outfile.txt";
        if (file_exists(basename($outfile))) { unlink(basename($outfile)); }

        $jsonData = createJsonCommand("GET-LOCATION", array("processName" => $processName, "instance" => $frm_instance, "outfile" => $outfile));
        writeJsonToFile($jsonData, TEMP_FILE);
        renameFile(TEMP_FILE, COMMAND_FILE);
        $time_stamp = getTimestamp();

        $file = tryOpenFile(basename($outfile), 50, 100); // wait up to 5 seconds
        if ($file) {
            $frmX = fgets($file);
            $frmY = fgets($file);
            fclose($file);
        } else {
            $footer = "<span style='color: red;'>Error opening the file.</span>";
            $frmX = "?";
            $frmY = "?";
        }
    }

    // CHECK FOR MOVE,MINIMIZE,ETC COMMAND
    $post_command = isset($_POST['Move']) ? $_POST['Move'] : "";
    if ($post_command == "Apply Changes") {
        $processName = $_POST['Process'];
        $state = isset($_POST['State']) ? $_POST['State'] : "0";

        switch ($state) {
            case "0":
                $footer = "Move completed - [$processName]";
                $jsonData = createJsonCommand("CHANGE_LOCATION", array("processName" => $processName, "instance" => $frm_instance, "x" => $frmX, "y" => $frmY));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);
                $time_stamp = getTimestamp();
                break;

            case "-1":
                $footer = "Minimize completed - [$processName]";
                $jsonData = createJsonCommand("MINIMIZE", array("processName" => $processName, "instance" => $frm_instance));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);
                $time_stamp = getTimestamp();
                break;

            case "1":
                $footer = "Restore completed - [$processName]";
                $jsonData = createJsonCommand("RESTORE", array("processName" => $processName, "instance" => $frm_instance));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);
                $time_stamp = getTimestamp();
                break;

            case "2":
                $footer = "Maximize completed - [$processName]";
                $jsonData = createJsonCommand("MAXIMIZE", array("processName" => $processName, "instance" => $frm_instance));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);
                $time_stamp = getTimestamp();
                break;
        }
    }
} // end POST

// -------------------------
// Prepare program list for UI
// -------------------------
$programs = getProgramsList();

// Pipe commands (same values as original; UI grouped)
$pipeGroups = array(
    "PERFORMANCE MONITOR" => array(
        "PerformanceMonitorCommandsPipe|CYCLE_CPU_ALARM" => "CYCLE_CPU_ALARM",
        "PerformanceMonitorCommandsPipe|PROCESSOR_UTILITY" => "PROCESSOR_UTILITY",
        "PerformanceMonitorCommandsPipe|PROCESSOR_TIME" => "PROCESSOR_TIME",
        "PerformanceMonitorCommandsPipe|INTERRUPT_TIME" => "INTERRUPT_TIME",
        "PerformanceMonitorCommandsPipe|DPC_TIME" => "DPC_TIME",
        "PerformanceMonitorCommandsPipe|GPU_UTILIZATION" => "GPU_UTILIZATION",
        "PerformanceMonitorCommandsPipe|GPU_TEMPERATURE" => "GPU_TEMPERATURE",
        "PerformanceMonitorCommandsPipe|GPU_MEMORY_UTILIZATION" => "GPU_MEMORY_UTILIZATION",
        "PerformanceMonitorCommandsPipe|MONITOR" => "MONITOR",
        "PerformanceMonitorCommandsPipe|ALERTS" => "ALERTS",
        "PerformanceMonitorCommandsPipe|SETTINGS" => "SETTINGS",
        "PerformanceMonitorCommandsPipe|ERRORS" => "ERRORS",
        "PerformanceMonitorCommandsPipe|RESTART_COUNTERS" => "RESTART_COUNTERS",
        "PerformanceMonitorCommandsPipe|MINIMIZE" => "MINIMIZE",
        "PerformanceMonitorCommandsPipe|RESTORE" => "RESTORE",
        "PerformanceMonitorCommandsPipe|ALIGN_LEFT-1500" => "ALIGN_LEFT",
        "PerformanceMonitorCommandsPipe|TOPMOST" => "TOPMOST",
        "PerformanceMonitorCommandsPipe|CLOSE" => "CLOSE",
        "PerformanceMonitorCommandsPipe|FOREGROUND" => "FOREGROUND",
    ),
    "TELEMETRY VIB SHAKER" => array(
        "TelemetryVibShakerPipeCommands|MONITOR" => "MONITOR",
        "TelemetryVibShakerPipeCommands|NOT_FOUNDS" => "NOT_FOUNDS",
        "TelemetryVibShakerPipeCommands|SETTINGS" => "SETTINGS",
        "TelemetryVibShakerPipeCommands|STOP" => "STOP",
        "TelemetryVibShakerPipeCommands|START" => "START",
        "TelemetryVibShakerPipeCommands|CYCLE_STATISTICS" => "CYCLE_STATISTICS",
        "TelemetryVibShakerPipeCommands|MINIMIZE" => "MINIMIZE",
        "TelemetryVibShakerPipeCommands|RESTORE" => "RESTORE",
        "TelemetryVibShakerPipeCommands|ALIGN_LEFT-1500" => "ALIGN_LEFT",
        "TelemetryVibShakerPipeCommands|TOPMOST" => "TOPMOST",
        "TelemetryVibShakerPipeCommands|CLOSE" => "CLOSE",
        "TelemetryVibShakerPipeCommands|FOREGROUND" => "FOREGROUND",
    ),
    "WAR THUNDER EXPORTER" => array(
        "WarThunderExporterPipeCommands|MONITOR" => "MONITOR",
        "WarThunderExporterPipeCommands|CYCLE_STATISTICS" => "CYCLE_STATISTICS",
        "WarThunderExporterPipeCommands|SETTINGS" => "SETTINGS",
        "WarThunderExporterPipeCommands|INTERVAL_100" => "INTERVAL_100",
        "WarThunderExporterPipeCommands|INTERVAL_50" => "INTERVAL_50",
        "WarThunderExporterPipeCommands|STOP" => "STOP",
        "WarThunderExporterPipeCommands|START" => "START",
        "WarThunderExporterPipeCommands|NOT_FOUNDS" => "NOT_FOUNDS",
        "WarThunderExporterPipeCommands|MINIMIZE" => "MINIMIZE",
        "WarThunderExporterPipeCommands|RESTORE" => "RESTORE",
        "WarThunderExporterPipeCommands|ALIGN_LEFT-1500" => "ALIGN_LEFT",
        "WarThunderExporterPipeCommands|TOPMOST" => "TOPMOST",
        "WarThunderExporterPipeCommands|CLOSE" => "CLOSE",
        "WarThunderExporterPipeCommands|FOREGROUND" => "FOREGROUND",
    ),
    "SIM CONNECT EXPORTER" => array(
        "SimConnectExporterPipeCommands|MONITOR" => "MONITOR",
        "SimConnectExporterPipeCommands|CYCLE_STATISTICS" => "CYCLE_STATISTICS",
        "SimConnectExporterPipeCommands|SHOW_GFORCES" => "SHOW_GFORCES",
        "SimConnectExporterPipeCommands|SETTINGS" => "SETTINGS",
        "SimConnectExporterPipeCommands|STOP" => "STOP",
        "SimConnectExporterPipeCommands|START" => "START",
        "SimConnectExporterPipeCommands|MINIMIZE" => "MINIMIZE",
        "SimConnectExporterPipeCommands|RESTORE" => "RESTORE",
        "SimConnectExporterPipeCommands|ALIGN_LEFT-1500" => "ALIGN_LEFT",
        "SimConnectExporterPipeCommands|TOPMOST" => "TOPMOST",
        "SimConnectExporterPipeCommands|CLOSE" => "CLOSE",
        "SimConnectExporterPipeCommands|FOREGROUND" => "FOREGROUND",
    ),
    "POWERSHELL IPC PIPE THREAD" => array(
        "ipc_pipe_vr_server_commands|SHOW_PROCESS" => "SHOW PROCESSES TIMES",
    ),
);

$selectedProcess = $frm_process;
$selectedPipe = $frm_pipe_command;

// Derive a lightweight status label for the HUD
$statusLabel = "READY";
if ($_SERVER["REQUEST_METHOD"] === "POST") {
    $statusLabel = "DONE";
    if (stripos($footer, "Error") !== false) $statusLabel = "ERROR";
}
?>
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover" />
  <title>Commander — HUD</title>
  <meta name="theme-color" content="#05080f" />
  <style>
    :root{
      --bg:#05080f;
      --panel: rgba(9, 14, 26, .72);
      --line: rgba(0, 255, 225, .22);
      --txt:#e6fff9;
      --muted: rgba(230,255,249,.7);
      --danger:#ff3b57;
      --ok:#40ffbd;
      --accent:#00ffe1;
      --accent2:#7cf6ff;
      --shadow: 0 18px 40px rgba(0,0,0,.55);
      --r: 18px;
      --tap: 52px;
      --mono: ui-monospace, SFMono-Regular, Menlo, Consolas, "Liberation Mono", monospace;
      --ui: system-ui, -apple-system, Segoe UI, Roboto, Arial, sans-serif;
    }
    *{box-sizing:border-box}
    html,body{height:100%}
    body{
      margin:0;
      font-family: var(--ui);
      background: radial-gradient(1200px 800px at 15% 0%, rgba(0,255,225,.10), transparent 55%),
                  radial-gradient(900px 700px at 90% 10%, rgba(124,246,255,.10), transparent 60%),
                  linear-gradient(180deg, #040611, #070a14 45%, #040611);
      color:var(--txt);
      overflow-x:hidden;
    }
    /* HUD grid + scanlines */
    body:before{
      content:"";
      position:fixed; inset:0;
      background:
        linear-gradient(rgba(0,255,225,.06) 1px, transparent 1px),
        linear-gradient(90deg, rgba(0,255,225,.05) 1px, transparent 1px);
      background-size: 38px 38px;
      opacity:.22;
      pointer-events:none;
      mix-blend-mode: screen;
    }
    body:after{
      content:"";
      position:fixed; inset:0;
      background: repeating-linear-gradient(
        to bottom,
        rgba(255,255,255,.03) 0px,
        rgba(255,255,255,.03) 1px,
        transparent 2px,
        transparent 5px
      );
      opacity:.18;
      pointer-events:none;
      mix-blend-mode: overlay;
    }

    .safe-area{
      padding: max(14px, env(safe-area-inset-top)) 14px calc(88px + env(safe-area-inset-bottom)) 14px;
      max-width: 760px;
      margin: 0 auto;
    }
    header{
      display:flex; align-items:center; justify-content:space-between;
      gap:12px;
      margin-bottom: 10px;
    }
    .title{
      display:flex; flex-direction:column; gap:2px;
      min-width: 180px;
    }
    .title a{
      text-decoration:none;
      color:inherit;
      display:inline-flex;
      align-items:baseline;
      gap:10px;
    }
    .title h1{
      margin:0;
      font-family: var(--mono);
      font-size: 18px;
      letter-spacing: .08em;
      text-transform: uppercase;
      color: var(--accent2);
      line-height: 1.1;
    }
    .title .sub{
      font-family: var(--mono);
      font-size: 12px;
      letter-spacing: .06em;
      color: var(--muted);
    }

    .status{
      min-width: 170px;
      padding: 10px 12px;
      border-radius: 14px;
      border: 1px solid var(--line);
      background: linear-gradient(180deg, rgba(0,255,225,.08), rgba(0,255,225,.02));
      box-shadow: 0 0 0 1px rgba(0,255,225,.05) inset, 0 12px 22px rgba(0,0,0,.35);
      font-family: var(--mono);
      font-size: 12px;
      color: var(--accent);
      text-align:right;
    }
    .status b{ color: var(--txt); font-weight:800; letter-spacing:.06em; }
    .status .mini{ color: rgba(230,255,249,.70); }

    .card{
      background: linear-gradient(180deg, rgba(8,13,24,.76), rgba(8,13,24,.52));
      border: 1px solid var(--line);
      box-shadow: var(--shadow), 0 0 0 1px rgba(0,255,225,.05) inset;
      border-radius: var(--r);
      padding: 14px;
      backdrop-filter: blur(10px);
      -webkit-backdrop-filter: blur(10px);
      position:relative;
      overflow:hidden;
    }
    .card:before{
      content:"";
      position:absolute; inset:-1px;
      background: radial-gradient(650px 120px at 20% 0%, rgba(0,255,225,.18), transparent 55%),
                  radial-gradient(650px 120px at 80% 0%, rgba(124,246,255,.14), transparent 60%);
      pointer-events:none;
      opacity:.8;
    }
    .card > *{ position:relative; }
    .stack{ display:grid; gap:12px; }

    .row{ display:grid; grid-template-columns: 1fr; gap:12px; }
    @media (min-width: 560px){
      .row.two{ grid-template-columns: 1fr 1fr; }
      .row.three{ grid-template-columns: 1fr 1fr 1fr; }
    }

    label{
      display:block;
      font-size: 12px;
      font-family: var(--mono);
      letter-spacing: .06em;
      text-transform: uppercase;
      color: var(--muted);
      margin: 0 0 6px 2px;
    }

    input[type="text"], select{
      width:100%;
      height: var(--tap);
      border-radius: 14px;
      border: 1px solid rgba(0,255,225,.22);
      background: rgba(0,0,0,.28);
      color: var(--txt);
      padding: 0 12px;
      outline:none;
      box-shadow: 0 0 0 1px rgba(0,255,225,.05) inset;
      font-size: 16px;
    }
    input::placeholder{ color: rgba(230,255,249,.42); }

    .hint{
      font-family: var(--mono);
      font-size: 12px;
      color: rgba(230,255,249,.55);
      margin-top: 6px;
      line-height: 1.3;
    }

    .seg{ display:flex; gap:10px; flex-wrap:wrap; }
    .chip{
      border: 1px solid rgba(0,255,225,.25);
      background: rgba(0,255,225,.06);
      color: var(--accent2);
      font-family: var(--mono);
      height: 40px;
      padding: 0 12px;
      border-radius: 999px;
      display:inline-flex;
      align-items:center; justify-content:center;
      cursor:pointer;
      user-select:none;
      transition: transform .06s ease, background .2s ease;
      white-space:nowrap;
    }
    .chip:active{ transform: translateY(1px); }

    .btnRow{ display:grid; grid-template-columns: 1fr 1fr; gap:10px; }
    @media (min-width: 560px){
      .btnRow{ grid-template-columns: repeat(4, 1fr); }
      .btnRow.secondary{ grid-template-columns: repeat(3, 1fr); }
    }

    button{
      height: var(--tap);
      border-radius: 16px;
      border: 1px solid rgba(0,255,225,.28);
      background: linear-gradient(180deg, rgba(0,255,225,.18), rgba(0,255,225,.06));
      color: var(--txt);
      font-family: var(--mono);
      letter-spacing: .06em;
      text-transform: uppercase;
      font-size: 13px;
      cursor:pointer;
      box-shadow: 0 0 26px rgba(0,255,225,.12);
      transition: transform .06s ease, filter .2s ease, opacity .2s ease;
    }
    button:active{ transform: translateY(1px); }
    button.secondary{
      background: rgba(255,255,255,.03);
      border-color: rgba(0,255,225,.18);
      color: rgba(230,255,249,.86);
      box-shadow:none;
    }
    button.danger{
      border-color: rgba(255,59,87,.55);
      background: linear-gradient(180deg, rgba(255,59,87,.18), rgba(255,59,87,.06));
      box-shadow: 0 0 28px rgba(255,59,87,.12);
    }
    button:disabled{ opacity:.55; cursor:not-allowed; }

    .divider{
      height:1px;
      background: linear-gradient(90deg, transparent, rgba(0,255,225,.32), transparent);
      margin: 12px 0;
    }

    /* Bottom nav */
    .bottom{
      position:fixed;
      left:0; right:0; bottom:0;
      padding: 10px 10px calc(10px + env(safe-area-inset-bottom)) 10px;
      background: linear-gradient(180deg, rgba(5,8,15,0), rgba(5,8,15,.75) 40%, rgba(5,8,15,.92));
      backdrop-filter: blur(10px);
      -webkit-backdrop-filter: blur(10px);
      border-top: 1px solid rgba(0,255,225,.18);
    }
    .tabs{
      max-width: 760px; margin: 0 auto;
      display:grid; grid-template-columns: repeat(3, 1fr);
      gap: 10px;
    }
    .tabBtn{
      height: 54px;
      border-radius: 16px;
      border: 1px solid rgba(0,255,225,.18);
      background: rgba(0,0,0,.18);
      color: rgba(230,255,249,.75);
      font-family: var(--mono);
      text-transform: uppercase;
      letter-spacing: .08em;
      font-size: 12px;
      display:flex;
      align-items:center; justify-content:center;
      gap:8px;
      cursor:pointer;
    }
    .tabBtn.active{
      background: linear-gradient(180deg, rgba(0,255,225,.18), rgba(0,255,225,.05));
      border-color: rgba(0,255,225,.35);
      color: var(--txt);
      box-shadow: 0 0 24px rgba(0,255,225,.12);
    }
    .ico{ width:18px; height:18px; display:inline-block; }
    .section{ display:none; }
    .section.active{ display:grid; gap:12px; }

    /* Toast */
    .toast{
      position: fixed;
      left: 12px; right: 12px;
      top: calc(10px + env(safe-area-inset-top));
      max-width: 760px;
      margin: 0 auto;
      padding: 12px 14px;
      border-radius: 16px;
      border: 1px solid rgba(0,255,225,.28);
      background: rgba(10, 14, 26, .86);
      box-shadow: 0 14px 35px rgba(0,0,0,.55);
      font-family: var(--mono);
      font-size: 12px;
      color: var(--txt);
      opacity:0;
      transform: translateY(-10px);
      pointer-events:none;
      transition: opacity .18s ease, transform .18s ease;
    }
    .toast.show{ opacity:1; transform: translateY(0); }
    .toast .small{ color: rgba(230,255,249,.65); display:block; margin-top:4px; }

    /* Guarded terminate */
    .hold{
      display:flex; align-items:center; justify-content:space-between;
      gap:10px;
      padding: 12px;
      border-radius: 16px;
      border: 1px dashed rgba(255,59,87,.55);
      background: rgba(255,59,87,.06);
    }
    .hold b{ font-family: var(--mono); }
    .holdProgress{
      height: 10px;
      flex: 1;
      border-radius: 999px;
      background: rgba(255,255,255,.06);
      overflow:hidden;
      border: 1px solid rgba(255,59,87,.25);
    }
    .holdProgress > i{
      display:block;
      height:100%;
      width:0%;
      background: linear-gradient(90deg, rgba(255,59,87,.2), rgba(255,59,87,.85));
    }

    .kbd{
      font-family: var(--mono);
      font-size: 12px;
      color: rgba(230,255,249,.72);
      line-height:1.35;
    }
    .kbd code{
      background: rgba(0,255,225,.08);
      border: 1px solid rgba(0,255,225,.18);
      padding: 1px 6px;
      border-radius: 8px;
      color: var(--accent2);
    }

    .sys{
      font-family: var(--mono);
      font-size: 12px;
      color: rgba(230,255,249,.62);
      line-height: 1.35;
      word-break: break-word;
    }
    .sys b{ color: rgba(230,255,249,.88); }
  </style>
  <noscript>
    <style>
      .section{ display:grid !important; }
      .bottom{ display:none !important; }
      /* Make sure the commands/programs sections are reachable without JS */
    </style>
  </noscript>
</head>
<body>
  <div class="toast" id="toast">
    <div id="toastTitle">Commander</div>
    <span class="small" id="toastBody">Ready.</span>
  </div>

  <div class="safe-area">
    <header>
      <div class="title">
        <a href="#" id="resetLink" title="Reset / reload">
          <h1>COMMANDER</h1>
        </a>
        <div class="sub">VR HUD — phone-first, big targets</div>
      </div>

      <div class="status" id="resultHUD">
        <b id="statusMain"><?php echo h($statusLabel); ?></b><br/>
        <span class="mini" id="statusSub"><?php echo "[" . h($time_stamp) . "] "; ?><span id="statusFooter"><?php echo $footer; ?></span></span>
      </div>
    </header>

    <form id="RemoteControlForm" name="RemoteControlForm" method="POST" action="">
      <!-- WINDOW -->
      <section class="section active" id="sec-window" aria-label="Window">
        <div class="card stack">
          <div class="row">
            <div>
              <label for="processSearch">Target Process (search)</label>
              <input id="processSearch" type="text" placeholder="type to filter… (e.g., FlightSimulator)" autocomplete="off" />
              <div class="hint">Filters the list below (client-side only).</div>
            </div>
          </div>

          <div class="row">
            <div>
              <label for="Process">Select Process</label>
              <select id="Process" name="Process" required>
                <?php foreach ($programs as $prog): 
                    $pName = htmlspecialchars($prog['processName']);
                    $fName = htmlspecialchars(isset($prog['friendlyName']) ? $prog['friendlyName'] : $pName);
                    $sel = ($selectedProcess !== "" && $selectedProcess === $pName) ? " selected" : "";
                ?>
                  <option value="<?php echo $pName; ?>"<?php echo $sel; ?>><?php echo $fName; ?></option>
                <?php endforeach; ?>
              </select>
            </div>
          </div>

          <div class="row two">
            <div>
              <label for="Instance">Instance / Threads Limit</label>
              <input id="Instance" name="Instance" type="text" inputmode="numeric" value="<?php echo h($frm_instance); ?>" placeholder="instance # (or threads limit)" required />
              <div class="hint">Used for window instance and also threadsLimit for SHOW_THREADS / BOOST (defaults to 50 if invalid).</div>
            </div>
            <div>
              <label for="State">State</label>
              <select id="State" name="State">
                <option selected value="0">NoChange (Move)</option>
                <option value="-1">Minimize</option>
                <option value="1">Restore</option>
                <option value="2">Maximize</option>
              </select>
            </div>
          </div>

          <div class="divider"></div>

          <div class="row two">
            <div>
              <label for="X">New X</label>
              <input id="X" name="X" type="text" value="<?php echo h($frmX); ?>" placeholder="e.g., -1700" />
            </div>
            <div>
              <label for="Y">New Y</label>
              <input id="Y" name="Y" type="text" value="<?php echo h($frmY); ?>" placeholder="e.g., 0" />
            </div>
          </div>

          <div class="seg" aria-label="Presets">
            <div class="chip" data-fill-x="-1700">X -1700</div>
            <div class="chip" data-fill-x="-1500">X -1500</div>
            <div class="chip" data-fill-x="0">X 0</div>
            <div class="chip" data-fill-y="0">Y 0</div>
            <div class="chip" data-fill-y="100">Y 100</div>
          </div>

          <div class="divider"></div>

          <div class="btnRow" aria-label="Quick state actions">
            <button type="submit" name="Move" value="Apply Changes" data-set-state="0">MOVE</button>
            <button type="submit" name="Move" value="Apply Changes" data-set-state="-1">MIN</button>
            <button type="submit" name="Move" value="Apply Changes" data-set-state="1">RESTORE</button>
            <button type="submit" name="Move" value="Apply Changes" data-set-state="2">MAX</button>
          </div>

          <div class="btnRow secondary" style="margin-top:10px">
            <button class="secondary" type="submit" name="MakeForeground" value="Make Foreground">FOREGROUND</button>
            <button class="secondary" type="submit" name="GetValues" value="GetValues">GET X/Y</button>
            <button class="secondary" type="submit" name="Launch" value="Launch">LAUNCH</button>
          </div>
        </div>

        <div class="card">
          <div class="hold">
            <div class="kbd">
              <b>Terminate</b> is guarded: press &amp; hold<br/>
              <span style="color:rgba(255,59,87,.9)">Hold for 0.8s</span> to send <code>KILL</code>
            </div>
            <div class="holdProgress" aria-label="Hold progress"><i id="holdBar"></i></div>
            <button class="danger" type="button" id="terminateBtn">HOLD</button>
            <!-- Real submit button (triggered by JS) -->
            <button class="danger" type="submit" name="Exit" value="Terminate" id="terminateSubmit" style="display:none">Terminate</button>
            <noscript>
              <button class="danger" type="submit" name="Exit" value="Terminate">TERMINATE</button>
            </noscript>
          </div>
        </div>

        <div class="card">
          <div class="sys">
            <b>Status:</b> <span id="result">[<?php echo h($time_stamp); ?>] <?php echo $footer; ?></span><br/>
            <b>PHP:</b> <?php echo h(phpversion()); ?>
          </div>
        </div>
      </section>

      <!-- PROGRAMS -->
      <section class="section" id="sec-programs" aria-label="Programs">
        <div class="card stack">
          <div class="kbd">
            Program actions operate on the currently selected <code>Process</code>.
          </div>

          <div class="btnRow secondary">
            <button class="secondary" type="submit" name="Launch" value="Launch">LAUNCH</button>
            <button class="secondary" type="submit" name="MakeForeground" value="Make Foreground">FOREGROUND</button>
            <button class="secondary" type="submit" name="GetValues" value="GetValues">GET X/Y</button>
          </div>

          <div class="divider"></div>

          <div class="kbd">Danger zone (guarded):</div>
          <div class="hold">
            <div class="kbd">
              <b>KILL</b> selected process<br/>press &amp; hold to confirm
            </div>
            <div class="holdProgress"><i id="holdBar2"></i></div>
            <button class="danger" type="button" id="terminateBtn2">HOLD</button>
            <button class="danger" type="submit" name="Exit" value="Terminate" id="terminateSubmit2" style="display:none">Terminate</button>
            <noscript>
              <button class="danger" type="submit" name="Exit" value="Terminate">TERMINATE</button>
            </noscript>
          </div>

          <div class="divider"></div>
          <div class="sys">
            <b>Last:</b> [<?php echo h($time_stamp); ?>] <?php echo $footer; ?>
          </div>
        </div>
      </section>

      <!-- COMMANDS -->
      <section class="section" id="sec-commands" aria-label="Commands">
        <div class="card stack">
          <div class="row">
            <div>
              <label for="SpecialCommand">Special Command</label>
              <select id="SpecialCommand" name="SpecialCommand">
                <option value="0" selected>[SELECT SPECIAL COMMAND]</option>
                <option value="WATCHDOG">WATCHDOG: Watcher for JSON Gaming Commands</option>
                <option value="HIGHPERFORMANCE">POWER SCHEME: HIGH POWER</option>
                <option value="BALANCED">POWER SCHEME: BALANCED</option>
                <option value="BALANCED80">POWER SCHEME: BALANCED MAX 80</option>
                <option value="READPOWERSCHEME">READ CURRENT POWER PLAN</option>
                <option value="SHOW_THREADS">Show Thread times for VR Flight Games</option>
                <option value="BOOST_1">BOOST 1: AboveNormal, IdealThread: P-Cores</option>
                <option value="BOOST_2">BOOST 2: AboveNormal, IdealThread: P-Cores + CPU_SETS</option>
                <option value="BOOST_3">BOOST 3: AboveNormal, IdealThread: P-Cores + Hard Affinity</option>
              </select>
            </div>
          </div>

          <button type="submit" name="Command" value="Command" id="runSpecialBtn">RUN SPECIAL</button>

          <div class="divider"></div>

          <div class="row">
            <div>
              <label for="PipeCommand">Pipe Command</label>
              <select id="PipeCommand" name="PipeCommand">
                <option value="0"<?php echo ($selectedPipe === "" || $selectedPipe === "0") ? " selected" : ""; ?>>[SELECT PIPE COMMAND]</option>
                <?php foreach ($pipeGroups as $groupName => $items): ?>
                  <optgroup label="<?php echo h($groupName); ?>">
                    <?php foreach ($items as $value => $label): 
                      $sel = ($selectedPipe !== "" && $selectedPipe === $value) ? " selected" : "";
                    ?>
                      <option value="<?php echo h($value); ?>"<?php echo $sel; ?>><?php echo h($label); ?></option>
                    <?php endforeach; ?>
                  </optgroup>
                <?php endforeach; ?>
              </select>
            </div>
          </div>

          <button type="submit" name="Command" value="Command" id="sendPipeBtn" class="secondary">SEND PIPE</button>

          <div class="divider"></div>
          <div class="kbd">
            Both buttons submit <code>Command=Command</code>. UI clears the other dropdown so intent is unambiguous.
          </div>
        </div>
      </section>
    </form>
  </div>

  <div class="bottom" role="navigation" aria-label="Bottom navigation">
    <div class="tabs">
      <button class="tabBtn active" type="button" data-tab="window">
        <svg class="ico" viewBox="0 0 24 24" fill="none">
          <path d="M4 6.5h16v11H4v-11Z" stroke="currentColor" stroke-width="1.6"/>
          <path d="M4 9h16" stroke="currentColor" stroke-width="1.6"/>
        </svg>
        WINDOW
      </button>
      <button class="tabBtn" type="button" data-tab="programs">
        <svg class="ico" viewBox="0 0 24 24" fill="none">
          <path d="M7 7h10v10H7V7Z" stroke="currentColor" stroke-width="1.6"/>
          <path d="M4 12h3M17 12h3" stroke="currentColor" stroke-width="1.6"/>
        </svg>
        PROGRAMS
      </button>
      <button class="tabBtn" type="button" data-tab="commands">
        <svg class="ico" viewBox="0 0 24 24" fill="none">
          <path d="M6 8h12M6 12h12M6 16h12" stroke="currentColor" stroke-width="1.6"/>
        </svg>
        COMMANDS
      </button>
    </div>
  </div>

  <script>
    (function(){
      const toast = document.getElementById('toast');
      const toastTitle = document.getElementById('toastTitle');
      const toastBody = document.getElementById('toastBody');

      const statusMain = document.getElementById('statusMain');
      const statusSub = document.getElementById('statusSub');

      const form = document.getElementById('RemoteControlForm');

      const sec = {
        window: document.getElementById('sec-window'),
        programs: document.getElementById('sec-programs'),
        commands: document.getElementById('sec-commands')
      };

      // Reset link (strip querystring)
      document.getElementById('resetLink').addEventListener('click', (e)=>{
        e.preventDefault();
        window.location.href = window.location.href.split('?')[0];
      });

      // Toast helper
      let toastTimer = 0;
      function showToast(title, body){
        toastTitle.textContent = title;
        toastBody.textContent = body;
        toast.classList.add('show');
        clearTimeout(toastTimer);
        toastTimer = setTimeout(()=>toast.classList.remove('show'), 1400);
      }

      // tab switching
      document.querySelectorAll('.tabBtn').forEach(btn=>{
        btn.addEventListener('click', ()=>{
          document.querySelectorAll('.tabBtn').forEach(b=>b.classList.remove('active'));
          btn.classList.add('active');
          const t = btn.dataset.tab;
          Object.keys(sec).forEach(k=>sec[k].classList.toggle('active', k === t));
          showToast("TAB", "Switched to " + t.toUpperCase());
        });
      });

      // preset chips
      document.querySelectorAll('.chip').forEach(ch=>{
        ch.addEventListener('click', ()=>{
          const x = ch.getAttribute('data-fill-x');
          const y = ch.getAttribute('data-fill-y');
          if(x !== null){ document.getElementById('X').value = x; showToast("X PRESET", "X → " + x); }
          if(y !== null){ document.getElementById('Y').value = y; showToast("Y PRESET", "Y → " + y); }
        });
      });

      // quick state buttons (write State before submit)
      form.addEventListener('click', (e)=>{
        const btn = e.target.closest('button[type="submit"][data-set-state]');
        if(!btn) return;
        document.getElementById('State').value = btn.dataset.setState;
      });

      // command split intent (avoid accidental "special=0 vs pipe=0" ambiguity)
      const runSpecialBtn = document.getElementById('runSpecialBtn');
      const sendPipeBtn = document.getElementById('sendPipeBtn');
      if (runSpecialBtn){
        runSpecialBtn.addEventListener('click', ()=>{
          const pipe = document.getElementById('PipeCommand');
          if(pipe) pipe.value = "0";
        });
      }
      if (sendPipeBtn){
        sendPipeBtn.addEventListener('click', ()=>{
          const special = document.getElementById('SpecialCommand');
          if(special) special.value = "0";
        });
      }

      // Process filter
      const processSelect = document.getElementById('Process');
      const originalOptions = Array.from(processSelect.options).map(o=>({value:o.value, text:o.text}));
      document.getElementById('processSearch').addEventListener('input', (e)=>{
        const q = e.target.value.trim().toLowerCase();
        const selected = processSelect.value;
        processSelect.innerHTML = "";
        originalOptions
          .filter(o => !q || o.text.toLowerCase().includes(q) || o.value.toLowerCase().includes(q))
          .forEach(o=>{
            const opt = document.createElement('option');
            opt.value = o.value;
            opt.textContent = o.text;
            if(o.value === selected) opt.selected = true;
            processSelect.appendChild(opt);
          });
      });

      // guarded terminate (press & hold)
      function wireHold(holdBtnId, barId, submitId){
        const holdBtn = document.getElementById(holdBtnId);
        const bar = document.getElementById(barId);
        const submit = document.getElementById(submitId);
        if(!holdBtn || !bar || !submit) return;

        let t0 = 0;
        let raf = 0;
        const HOLD_MS = 800;

        function tick(now){
          const dt = now - t0;
          const p = Math.min(1, dt / HOLD_MS);
          bar.style.width = (p*100).toFixed(0) + "%";
          if(p < 1){
            raf = requestAnimationFrame(tick);
          }else{
            bar.style.width = "100%";
            submit.click();
          }
        }
        function start(){
          t0 = performance.now();
          bar.style.width = "0%";
          raf = requestAnimationFrame(tick);
        }
        function stop(){
          cancelAnimationFrame(raf);
          raf = 0;
          bar.style.width = "0%";
        }

        holdBtn.addEventListener('pointerdown', (e)=>{ e.preventDefault(); start(); showToast("ARM", "Holding…"); });
        holdBtn.addEventListener('pointerup', (e)=>{ e.preventDefault(); stop(); });
        holdBtn.addEventListener('pointerleave', stop);
        holdBtn.addEventListener('pointercancel', stop);
      }
      wireHold('terminateBtn', 'holdBar', 'terminateSubmit');
      wireHold('terminateBtn2', 'holdBar2', 'terminateSubmit2');

      // Submit UX: show processing + disable controls to prevent double-submit
      function setProcessing(){
        statusMain.textContent = "PROCESSING";
        statusSub.style.color = "rgba(124,246,255,.9)";
        showToast("PROCESSING", "Sending command…");
        // Disable all buttons to avoid multiple submits
        document.querySelectorAll('button').forEach(b=>{
          // allow hold buttons to be disabled too (they're a request)
          b.disabled = true;
        });
      }

      form.addEventListener('submit', (e)=>{
        // In the original you updated #result; keep that id and update too.
        const result = document.getElementById('result');
        if(result){
          result.style.color = 'rgba(124,246,255,.95)';
          result.textContent = 'Processing';
        }
        setProcessing();
      }, true);

      // Initial toast showing last status (on page load, from server)
      try{
        const footerText = document.getElementById('statusFooter')?.innerText || "";
        showToast(statusMain.innerText || "READY", footerText.replace(/\s+/g,' ').trim().slice(0, 120) || "Ready.");
      }catch(_){}
    })();
  </script>
</body>
</html>
