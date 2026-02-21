<?php
// Permitir cualquier origen (tu celular)
header("Access-Control-Allow-Origin: *");
// Permitir métodos comunes
header("Access-Control-Allow-Methods: GET, POST, OPTIONS");
// Permitir encabezados personalizados si los usas
header("Access-Control-Allow-Headers: Content-Type");

const COMMAND_FILE = 'command.json';
const TEMP_FILE = 'command.tmp';
const WATCHDOG_FILE = 'watchdog.txt';
const PROGRAMS_JSON = 'programs.json';

function getProgramsList() {
    if (!file_exists(PROGRAMS_JSON)) {
        return array();
    }
    $jsonData = file_get_contents(PROGRAMS_JSON);
    $data = json_decode($jsonData, true);

    // Check if key exists using ternary operator instead of ??
    return isset($data['programs']) ? $data['programs'] : array();
}

function createJsonCommand($commandType, $parameters = array()) {
  $data = array(
    "command_type" => $commandType,
    "parameters" => $parameters
  );

  $jsonData = json_encode($data);
  return $jsonData;
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
        usleep($delay_ms * 1000); // usleep takes microseconds

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
            return false; // Return false if unable to delete the existing new file
        }
    }

    // Check if the existing file exists
    if (file_exists($existingFileName)) {
        // Attempt to rename the file
        if (rename($existingFileName, $newFileName)) {
            return true;
        } else {
            return false;
        }
    } else {
        return false;
    }
}

function getTimestamp() {
    $microtime = microtime(true);
    $milliseconds = sprintf("%03d", ($microtime - floor($microtime)) * 1000);
    return date("Y-m-d H:i:s", $microtime) . ".$milliseconds";
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

// Initialize variables
$footer = "Ready";
$time_stamp = getTimestamp();

// Default location coordinates when not included in POST['X']
$frmX = "-1700";
$frmY = "";

$frm_instance = 0;
$frm_process = "";
$frm_pipe_command = isset($_POST['PipeCommand']) ? $_POST['PipeCommand'] : "";

$name = $gender = $color = "";
$subscribe = false;

// Check if the form is submitted
if ($_SERVER["REQUEST_METHOD"] == "POST") {
    $frmX = htmlspecialchars($_POST['X']);
    $frmY = htmlspecialchars($_POST['Y']);
    $frm_instance = htmlspecialchars($_POST['Instance']);
    $threads = is_numeric($frm_instance) ? (int)$frm_instance : 50;
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

        // Powershell FileSystem Event watcher listens to rename events
        $jsonData = createJsonCommand("RUN", array("program" => $path, "parameter1" => "not-used"));
        writeJsonToFile($jsonData, TEMP_FILE);
        renameFile(TEMP_FILE, COMMAND_FILE);
        $time_stamp = getTimestamp();
    } // end-if Launch


    // CHECK FOR TERMINATE COMMAND
    $post_command = isset($_POST['Exit']) ? $_POST['Exit'] : "";
    if ($post_command == "Terminate") {
        $processName = $_POST['Process'];

        $footer = "Terminate completed - [$processName]";

        $jsonData = createJsonCommand("KILL", array("processName" => $processName));
        writeJsonToFile($jsonData, TEMP_FILE);
        renameFile(TEMP_FILE, COMMAND_FILE);
        $time_stamp = getTimestamp();
    } // end-if Terminate


    // CHECK FOR FOREGROUND COMMAND
    $post_command = isset($_POST['MakeForeground']) ? $_POST['MakeForeground'] : "";
    if ($post_command == "Make Foreground") {
        $processName = $_POST['Process'];

        $footer = "Foreground completed - [$processName]";

        $jsonData = createJsonCommand("FOREGROUND", array("processName" => $processName, "instance" => $frm_instance));
        writeJsonToFile($jsonData, TEMP_FILE);
        renameFile(TEMP_FILE, COMMAND_FILE);
        $time_stamp = getTimestamp();
    } // end-if Foreground


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
            $frmX = fgets($file); // Read the first line
            $frmY = fgets($file); // Read the second line
            fclose($file);
        } else {
            $footer = "<span style='color: #ff6d00;'>Error opening the file.</span>";
            $frmX = "?";
            $frmY = "?";
        }
    } // end-if GetValues


    // CHECK FOR MOVE, MINIMIZE, ETC COMMAND
    $post_command = isset($_POST['Move']) ? $_POST['Move'] : "";
    if ($post_command == "Apply Changes") {
        $processName = $_POST['Process'];

        $state = isset($_POST['State']) ? $_POST['State'] : "0";
        switch ($state) {
            case "0":  // Change X,Y coordinates
                $footer = "Move completed - [$processName]";
                $jsonData = createJsonCommand("CHANGE_LOCATION", array("processName" => $processName, "instance" => $frm_instance, "x" => $frmX, "y" => $frmY));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);
                $time_stamp = getTimestamp();
                break;

            case "-1":  // Minimize
                $footer = "Minimize completed - [$processName]";
                $jsonData = createJsonCommand("MINIMIZE", array("processName" => $processName, "instance" => $frm_instance));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);
                $time_stamp = getTimestamp();
                break;

            case "1":  // Restore
                $footer = "Restore completed - [$processName]";
                $jsonData = createJsonCommand("RESTORE", array("processName" => $processName, "instance" => $frm_instance));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);
                $time_stamp = getTimestamp();
                break;

            case "2":  // Maximize
                $footer = "Maximize completed - [$processName]";
                $jsonData = createJsonCommand("MAXIMIZE", array("processName" => $processName, "instance" => $frm_instance));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);
                $time_stamp = getTimestamp();
                break;
        }
    } // end-if Apply Changes

} //end-if REQUEST_METHOD == POST
?>
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no">
<title>RCU-7 // COMMANDER</title>
<link href="https://fonts.googleapis.com/css2?family=VT323&family=B612+Mono:wght@400;700&display=swap" rel="stylesheet">
<style>
/* ════════════════════════════════════════════
   ROOT VARIABLES — Amber Phosphor Palette
   ════════════════════════════════════════════ */
:root {
    --amber:        #ffb300;
    --amber-bright: #ffe082;
    --amber-dim:    #7a5500;
    --amber-text:   #ffc933;
    --amber-glow:   0 0 6px rgba(255,179,0,0.55), 0 0 20px rgba(255,179,0,0.2);
    --amber-glow-sm:0 0 4px rgba(255,179,0,0.4);

    --bg:           #0a0a0a;
    --panel:        #111111;
    --panel-raised: #161616;
    --bezel:        #1e1e1e;
    --bezel-edge:   #2a2a2a;
    --line:         rgba(255,179,0,0.14);

    --green-led:    #00e676;
    --green-off:    #0d2018;
    --red-led:      #ff3d3d;
    --red-off:      #2a0808;
    --cyan-led:     #4dd0e1;
    --warn:         #ff6d00;

    --font-disp:    'VT323', 'Courier New', monospace;
    --font-body:    'B612 Mono', 'Courier New', monospace;
}

/* ════════════════════════════════════════════
   RESET & BASE
   ════════════════════════════════════════════ */
*, *::before, *::after {
    box-sizing: border-box;
    margin: 0; padding: 0;
    -webkit-tap-highlight-color: transparent;
}

html { font-size: 16px; }

body {
    font-family: var(--font-body);
    background: var(--bg);
    color: var(--amber-text);
    min-height: 100vh;
    padding-bottom: 80px;
    overflow-x: hidden;
}

/* ── SCANLINE OVERLAY (from mockup 1) ── */
body::before {
    content: '';
    position: fixed;
    inset: 0;
    background: repeating-linear-gradient(
        0deg,
        transparent,
        transparent 2px,
        rgba(0,0,0,0.09) 2px,
        rgba(0,0,0,0.09) 4px
    );
    pointer-events: none;
    z-index: 9999;
}

/* Subtle phosphor noise grain */
body::after {
    content: '';
    position: fixed;
    inset: 0;
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='200' height='200'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.85' numOctaves='4' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='200' height='200' filter='url(%23n)' opacity='0.025'/%3E%3C/svg%3E");
    pointer-events: none;
    z-index: 9998;
}

/* ════════════════════════════════════════════
   COCKPIT HEADER
   ════════════════════════════════════════════ */
.cockpit-header {
    background: linear-gradient(180deg, #1a1a1a 0%, #0e0e0e 100%);
    border-bottom: 2px solid var(--bezel-edge);
    padding: 10px 14px 8px;
    position: sticky;
    top: 0;
    z-index: 200;
    box-shadow: 0 3px 16px rgba(0,0,0,0.9);
    cursor: pointer;
}

.cockpit-header::after {
    content: '';
    position: absolute;
    bottom: -3px; left: 0; right: 0;
    height: 1px;
    background: linear-gradient(90deg, transparent 0%, var(--amber-dim) 30%, var(--amber) 50%, var(--amber-dim) 70%, transparent 100%);
    opacity: 0.5;
}

.hdr-inner {
    display: flex;
    align-items: center;
    justify-content: space-between;
}

.unit-id {
    font-family: var(--font-disp);
    font-size: 24px;
    color: var(--amber);
    text-shadow: var(--amber-glow);
    letter-spacing: 4px;
    line-height: 1;
}

.unit-sub {
    font-size: 8px;
    letter-spacing: 3px;
    color: var(--amber-dim);
    margin-top: 2px;
    text-transform: uppercase;
}

.hdr-right {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    gap: 5px;
}

.led-row {
    display: flex;
    align-items: center;
    gap: 5px;
}

.led {
    width: 8px; height: 8px;
    border-radius: 50%;
    flex-shrink: 0;
}
.led-green  { background: var(--green-led); box-shadow: 0 0 6px var(--green-led); animation: led-blink 2s ease-in-out infinite; }
.led-red    { background: var(--red-off); }
.led-amber  { background: var(--amber-dim); }

@keyframes led-blink { 0%,100%{opacity:1} 50%{opacity:0.35} }

.led-label    { font-size: 8px; letter-spacing: 1.5px; color: var(--amber-dim); }
.led-label.on { color: var(--green-led); }

.hdr-clock {
    font-family: var(--font-disp);
    font-size: 17px;
    color: var(--amber);
    letter-spacing: 2px;
}

/* ════════════════════════════════════════════
   SCROLL AREA
   ════════════════════════════════════════════ */
.scroll { padding: 10px 10px 0; }

/* ════════════════════════════════════════════
   INSTRUMENT PANEL (bezel card)
   ════════════════════════════════════════════ */
.instrument {
    margin-bottom: 10px;
    border-radius: 3px;
    background: var(--panel);
    border: 2px solid var(--bezel-edge);
    box-shadow:
        inset 0 1px 0 rgba(255,255,255,0.03),
        inset 0 -1px 0 rgba(0,0,0,0.6),
        0 4px 14px rgba(0,0,0,0.75);
    position: relative;
    overflow: hidden;
}

/* Corner rivet decorations */
.instrument > .rivet-tl,
.instrument > .rivet-tr,
.instrument > .rivet-bl,
.instrument > .rivet-br {
    position: absolute;
    font-family: var(--font-disp);
    font-size: 9px;
    color: #2a2a2a;
    line-height: 1;
    z-index: 1;
    pointer-events: none;
}
.rivet-tl { top: 3px;  left: 4px; }
.rivet-tr { top: 3px;  right: 4px; }
.rivet-bl { bottom: 3px; left: 4px; }
.rivet-br { bottom: 3px; right: 4px; }

/* Part number badge */
.part-num {
    position: absolute;
    bottom: 4px; right: 14px;
    font-size: 7px;
    letter-spacing: 1px;
    color: #1e1800;
    pointer-events: none;
    z-index: 1;
}

/* ── Nameplate / toggle header ── */
.inst-nameplate {
    display: flex;
    align-items: center;
    justify-content: space-between;
    background: #0e0e0e;
    border-bottom: 1px solid #282828;
    padding: 8px 14px;
    cursor: pointer;
    user-select: none;
}
.inst-nameplate:active { background: #131300; }

.nameplate-left {
    display: flex;
    align-items: center;
    gap: 9px;
}

.nameplate-led {
    width: 7px; height: 7px;
    border-radius: 50%;
    flex-shrink: 0;
}

.inst-title {
    font-family: var(--font-disp);
    font-size: 20px;
    color: var(--amber);
    letter-spacing: 3px;
    text-shadow: 0 0 8px rgba(255,179,0,0.4);
    line-height: 1;
}

.inst-code {
    font-size: 7px;
    letter-spacing: 2px;
    color: var(--amber-dim);
    margin-top: 1px;
}

.inst-caret {
    font-family: var(--font-disp);
    font-size: 20px;
    color: var(--amber-dim);
    transition: transform 0.28s ease;
    flex-shrink: 0;
}

.instrument.collapsed .inst-caret { transform: rotate(-90deg); }
.instrument.collapsed .inst-body  { display: none; }

.inst-body { padding: 12px 12px 16px; }

/* ════════════════════════════════════════════
   FORM FIELD COMPONENTS
   ════════════════════════════════════════════ */
.field-row { margin-bottom: 11px; }
.field-row:last-child { margin-bottom: 0; }

.field-tag {
    display: block;
    font-size: 8px;
    letter-spacing: 2.5px;
    color: var(--amber-dim);
    text-transform: uppercase;
    margin-bottom: 5px;
}

/* Phosphor display - SELECT */
.phos-select,
.phos-input {
    width: 100%;
    background: #050505;
    border: 1px solid var(--amber-dim);
    border-radius: 2px;
    color: var(--amber);
    font-family: var(--font-disp);
    font-size: 19px;
    padding: 8px 12px;
    outline: none;
    appearance: none;
    -webkit-appearance: none;
    letter-spacing: 1px;
    text-shadow: var(--amber-glow-sm);
    transition: border-color 0.18s, box-shadow 0.18s;
    line-height: 1.2;
}

.phos-select {
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='8'%3E%3Cpath d='M1 1l5 5 5-5' stroke='%23ffb300' stroke-width='1.5' fill='none'/%3E%3C/svg%3E");
    background-repeat: no-repeat;
    background-position: right 10px center;
    cursor: pointer;
    padding-right: 30px;
}

.phos-select:focus,
.phos-input:focus {
    border-color: var(--amber);
    box-shadow: var(--amber-glow);
}

.phos-select option { background: #0a0800; }

.row-2 {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 9px;
}

.amber-rule {
    border: none;
    border-top: 1px solid var(--line);
    margin: 11px 0;
}

/* ════════════════════════════════════════════
   WINDOW STATE — 4-button toggle row
   ════════════════════════════════════════════ */
.state-row {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 5px;
}

.sw-btn {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 3px;
    padding: 10px 4px 7px;
    border-radius: 3px;
    border: 1px solid #282828;
    background: #0d0d0d;
    color: var(--amber-dim);
    font-family: var(--font-disp);
    font-size: 13px;
    letter-spacing: 1px;
    cursor: pointer;
    transition: all 0.12s;
    /* Physical raised-button effect */
    box-shadow: 0 2px 0 #060606, inset 0 1px 0 rgba(255,255,255,0.04);
    line-height: 1;
}

.sw-btn .sw-ico { font-size: 17px; }

.sw-btn:active {
    box-shadow: inset 0 2px 4px rgba(0,0,0,0.6);
    transform: translateY(1px);
}

.sw-btn.active {
    background: #140e00;
    border-color: var(--amber);
    color: var(--amber);
    box-shadow: 0 0 5px rgba(255,179,0,0.3), 0 2px 0 #060606, inset 0 1px 0 rgba(255,255,255,0.04);
}

.sw-indicator {
    width: 6px; height: 6px;
    border-radius: 50%;
    background: var(--green-off);
    flex-shrink: 0;
}
.sw-btn.active .sw-indicator {
    background: var(--green-led);
    box-shadow: 0 0 5px var(--green-led);
}

/* ════════════════════════════════════════════
   ACTION BUTTONS — 3-column grid
   ════════════════════════════════════════════ */
.action-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 7px;
}

/* Base action button — physical key feel */
.act-btn {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 4px;
    padding: 13px 4px 10px;
    border-radius: 3px;
    border: 1px solid;
    font-family: var(--font-disp);
    font-size: 14px;
    letter-spacing: 1px;
    cursor: pointer;
    transition: all 0.10s;
    position: relative;
    overflow: hidden;
    line-height: 1;
}

.act-btn .act-ico { font-size: 21px; }

.act-btn:active {
    transform: translateY(2px);
    box-shadow: none !important;
}

/* CAUTION label for dangerous buttons */
.act-btn.caution::before {
    content: 'CAUTION';
    position: absolute;
    top: 4px; left: 0; right: 0;
    text-align: center;
    font-size: 6px;
    letter-spacing: 1.5px;
    opacity: 0.45;
}

/* Color variants */
.btn-amber {
    background: #100c00;
    border-color: var(--amber-dim);
    color: var(--amber);
    box-shadow: 0 3px 0 #060400;
}
.btn-amber:hover { background: #1a1300; }

.btn-green {
    background: #001508;
    border-color: #1a5c2a;
    color: var(--green-led);
    box-shadow: 0 3px 0 #000a03;
}
.btn-green:hover { background: #002010; }

.btn-red {
    background: #160000;
    border-color: #5c1a1a;
    color: var(--red-led);
    box-shadow: 0 3px 0 #0a0000;
}
.btn-red:hover { background: #220000; }

.btn-warn {
    background: #0f0600;
    border-color: #5c2e00;
    color: var(--warn);
    box-shadow: 0 3px 0 #070300;
}
.btn-warn:hover { background: #180900; }

/* ════════════════════════════════════════════
   SPECIAL COMMANDS — button grid
   ════════════════════════════════════════════ */
.spec-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 6px;
}

.sys-tag {
    font-family: var(--font-disp);
    font-size: 11px;
    letter-spacing: 2px;
    color: var(--amber-dim);
    grid-column: 1 / -1;
    border-bottom: 1px solid var(--line);
    padding-bottom: 3px;
    margin-top: 6px;
}
.sys-tag:first-child { margin-top: 0; }

.spec-btn {
    background: #080700;
    border: 1px solid #242000;
    border-radius: 3px;
    color: var(--amber-text);
    font-family: var(--font-disp);
    font-size: 14px;
    letter-spacing: 1px;
    padding: 9px 9px 8px;
    cursor: pointer;
    text-align: left;
    transition: all 0.10s;
    box-shadow: 0 2px 0 #040300, inset 0 1px 0 rgba(255,255,255,0.015);
    line-height: 1.25;
}
.spec-btn:active {
    transform: translateY(1px);
    box-shadow: none;
    background: #110e00;
}
.spec-btn .s-name { display: block; font-size: 14px; }
.spec-btn .s-desc { display: block; font-size: 9px; font-family: var(--font-body); color: var(--amber-dim); margin-top: 2px; letter-spacing: 0; line-height: 1.3; }

/* Color variants for spec buttons */
.spec-btn.danger    { border-color: #3a0000; color: var(--warn); background: #0a0200; }
.spec-btn.ok        { border-color: #003010; color: #3dcc6a;  background: #010d04; }
.spec-btn.info      { border-color: #003040; color: var(--cyan-led); background: #01080c; }
.spec-btn.watchdog  { border-color: #5c3000; color: var(--warn); background: #0c0600; }
.spec-btn.boost-nuke{ border-color: #5c2000; color: #ff6d00; background: #0d0400; grid-column: 1/-1; }
.spec-btn.full-row  { grid-column: 1/-1; }

/* ════════════════════════════════════════════
   PIPE COMMANDS — tabs + button grid
   ════════════════════════════════════════════ */
.pipe-tabs-row {
    display: flex;
    gap: 0;
    overflow-x: auto;
    scrollbar-width: none;
    -webkit-overflow-scrolling: touch;
    margin-bottom: 11px;
    border: 1px solid #282828;
    border-radius: 3px;
    overflow: hidden;
    flex-shrink: 0;
}
.pipe-tabs-row::-webkit-scrollbar { display: none; }

.ptab {
    flex-shrink: 0;
    background: #0a0a0a;
    border: none;
    border-right: 1px solid #282828;
    color: var(--amber-dim);
    font-family: var(--font-disp);
    font-size: 14px;
    letter-spacing: 1.5px;
    padding: 8px 11px;
    cursor: pointer;
    transition: all 0.12s;
    white-space: nowrap;
    line-height: 1;
}
.ptab:last-child { border-right: none; }
.ptab.active {
    background: #140e00;
    color: var(--amber);
    text-shadow: 0 0 6px rgba(255,179,0,0.5);
    border-bottom: 2px solid var(--amber);
}
.ptab:active { background: #1a1100; }

.pipe-content { display: none; }
.pipe-content.active { display: block; }

.pipe-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 5px;
}

.pcmd-btn {
    background: #070600;
    border: 1px solid #1e1a00;
    border-radius: 2px;
    color: var(--amber-text);
    font-family: var(--font-disp);
    font-size: 13px;
    letter-spacing: 0.5px;
    padding: 10px 5px 8px;
    cursor: pointer;
    text-align: center;
    transition: all 0.10s;
    box-shadow: 0 2px 0 #030200;
    line-height: 1.1;
}
.pcmd-btn:active {
    background: #1c1500;
    color: var(--amber-bright);
    border-color: var(--amber-dim);
    box-shadow: none;
    transform: translateY(1px);
    text-shadow: var(--amber-glow-sm);
}

/* ════════════════════════════════════════════
   STATUS TAPE — fixed bottom
   ════════════════════════════════════════════ */
.status-tape {
    position: fixed;
    bottom: 0; left: 0; right: 0;
    background: #070707;
    border-top: 2px solid #282828;
    padding: 7px 14px 10px;
    z-index: 300;
    box-shadow: 0 -3px 12px rgba(0,0,0,0.85);
}

.status-tape::before {
    content: '';
    position: absolute;
    top: 0; left: 0; right: 0;
    height: 1px;
    background: linear-gradient(90deg, transparent, var(--amber-dim), transparent);
}

.tape-label {
    font-size: 7px;
    letter-spacing: 3px;
    color: var(--amber-dim);
    margin-bottom: 3px;
    text-transform: uppercase;
}

.tape-display {
    font-family: var(--font-disp);
    font-size: 17px;
    color: var(--amber);
    text-shadow: var(--amber-glow-sm);
    letter-spacing: 1px;
    display: flex;
    align-items: center;
    gap: 7px;
    overflow: hidden;
}

/* Blinking cursor */
.tape-cursor {
    display: inline-block;
    width: 9px; height: 15px;
    background: var(--amber);
    animation: cur-blink 1.1s step-end infinite;
    flex-shrink: 0;
    vertical-align: middle;
}
@keyframes cur-blink { 0%,100%{opacity:1} 50%{opacity:0} }

/* id="result" lives inside tape-msg — JS and PHP both update it */
#result {
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    transition: color 0.3s;
}

/* PHP version footer line */
.php-ver {
    font-size: 8px;
    letter-spacing: 1px;
    color: #1e1600;
    margin-top: 3px;
}
</style>

<!-- ═══════════ JAVASCRIPT — all original logic preserved ═══════════ -->
<script>
<?php if ($frm_process != "") { ?>
    // Restore previously selected values after form submit
    function selectOptionByIdAndText(selectId, text) {
        var selectElement = document.getElementById(selectId);
        if (selectElement) {
            for (var i = 0; i < selectElement.options.length; i++) {
                if (selectElement.options[i].value === text) {
                    selectElement.selectedIndex = i;
                    return true;
                }
            }
        }
        return false;
    }

    window.onload = function() {
        var program = '<?= htmlspecialchars($frm_process, ENT_QUOTES) ?>';
        var pipe    = '<?= htmlspecialchars($frm_pipe_command, ENT_QUOTES) ?>';
        if (program !== '') selectOptionByIdAndText('Process', program);
        selectOptionByIdAndText('PipeCommand', pipe);
    };
<?php } ?>

    // ── CLOCK ──────────────────────────────────
    function updateClock() {
        var now = new Date();
        var h = String(now.getHours()).padStart(2,'0');
        var m = String(now.getMinutes()).padStart(2,'0');
        var s = String(now.getSeconds()).padStart(2,'0');
        document.getElementById('hdr-clock').textContent = h + ':' + m + ':' + s;
    }
    setInterval(updateClock, 1000);

    // ── INSTRUMENT PANEL COLLAPSE ──────────────
    function toggleInst(id) {
        document.getElementById(id).classList.toggle('collapsed');
    }

    // ── WINDOW STATE BUTTONS ───────────────────
    // Updates the hidden State input; visual toggle on the 4 sw-btn elements
    function setWindowState(btn, val) {
        document.getElementById('hdnState').value = val;
        var group = btn.closest('.state-row').querySelectorAll('.sw-btn');
        group.forEach(function(b){ b.classList.remove('active'); });
        btn.classList.add('active');
    }

    // ── SPECIAL COMMAND buttons ────────────────
    // Sets SpecialCommand hidden select, clears PipeCommand, fires Command submit
    function fireSpecial(val) {
        document.getElementById('SpecialCommand').value = val;
        document.getElementById('PipeCommand').value    = '0';
        document.getElementById('btnCommand').click();
    }

    // ── PIPE COMMAND buttons ───────────────────
    // Sets PipeCommand hidden select value, clears SpecialCommand, fires Command submit
    function firePipe(pipename, message) {
        document.getElementById('PipeCommand').value    = pipename + '|' + message;
        document.getElementById('SpecialCommand').value = '0';
        document.getElementById('btnCommand').click();
    }

    // ── PIPE TAB switcher ──────────────────────
    function setPipeTab(id, btn) {
        document.querySelectorAll('.ptab').forEach(function(t){ t.classList.remove('active'); });
        document.querySelectorAll('.pipe-content').forEach(function(c){ c.classList.remove('active'); });
        btn.classList.add('active');
        document.getElementById('pipe-' + id).classList.add('active');
    }

    // ── SUBMIT FEEDBACK (original behavior) ───
    // Any form submit → show "Processing" in the status tape (id="result")
    function handleFormSubmit(event, divId, newText, newColor) {
        var el = document.getElementById(divId);
        el.style.color = newColor;
        el.innerText = newText;
    }

    document.addEventListener('DOMContentLoaded', function() {
        updateClock();
        var forms = document.querySelectorAll('form');
        forms.forEach(function(form) {
            form.addEventListener('submit', function(event) {
                handleFormSubmit(event, 'result', 'PROCESSING...', '#4dd0e1');
            });
        });
    });
</script>
</head>
<body>

<!-- ═══════════════════════════════════════════
     COCKPIT HEADER — click to reset page
     ═══════════════════════════════════════════ -->
<div class="cockpit-header" onclick="window.location.href = window.location.href.split('?')[0];">
    <div class="hdr-inner">
        <div>
            <div class="unit-id">RCU-7 // COMMANDER</div>
            <div class="unit-sub">REMOTE CONTROL UNIT &bull; FLIGHT OPS SYSTEM</div>
        </div>
        <div class="hdr-right">
            <div class="led-row">
                <span class="led led-green"></span>
                <span class="led-label on">ONLINE</span>
                &nbsp;
                <span class="led led-red"></span>
                <span class="led-label">FAULT</span>
            </div>
            <div class="hdr-clock" id="hdr-clock">00:00:00</div>
        </div>
    </div>
</div>


<!-- ═══════════════════════════════════════════
     MAIN FORM — one form wrapping everything
     (original architecture: multiple submit
      buttons, PHP detects which was pressed)
     ═══════════════════════════════════════════ -->
<form id="RemoteControlForm" name="RemoteControlForm" method="POST">

<!-- ════ HIDDEN FORM MACHINERY ════
     These hidden inputs replace the original
     State select and provide the backing store
     for SpecialCommand / PipeCommand when
     buttons are used instead of dropdowns. -->

<!-- State: original <select name="State"> replaced by hidden input.
     setWindowState() JS updates this on sw-btn click. -->
<input type="hidden" name="State" id="hdnState" value="0">

<!-- SpecialCommand select — hidden, set by fireSpecial() -->
<select name="SpecialCommand" id="SpecialCommand" style="display:none">
    <option value="0" selected>[NONE]</option>
    <option value="WATCHDOG">WATCHDOG</option>
    <option value="HIGHPERFORMANCE">HIGHPERFORMANCE</option>
    <option value="BALANCED">BALANCED</option>
    <option value="BALANCED80">BALANCED80</option>
    <option value="READPOWERSCHEME">READPOWERSCHEME</option>
    <option value="SHOW_THREADS">SHOW_THREADS</option>
    <option value="BOOST_1">BOOST_1</option>
    <option value="BOOST_2">BOOST_2</option>
    <option value="BOOST_3">BOOST_3</option>
</select>

<!-- PipeCommand select — hidden, set by firePipe() OR restored by JS -->
<select name="PipeCommand" id="PipeCommand" style="display:none">
    <option value="0" selected>[NONE]</option>
    <!-- PERFORMANCE MONITOR -->
    <option value="PerformanceMonitorCommandsPipe|CYCLE_CPU_ALARM">PerformanceMonitorCommandsPipe|CYCLE_CPU_ALARM</option>
    <option value="PerformanceMonitorCommandsPipe|PROCESSOR_UTILITY">PerformanceMonitorCommandsPipe|PROCESSOR_UTILITY</option>
    <option value="PerformanceMonitorCommandsPipe|PROCESSOR_TIME">PerformanceMonitorCommandsPipe|PROCESSOR_TIME</option>
    <option value="PerformanceMonitorCommandsPipe|INTERRUPT_TIME">PerformanceMonitorCommandsPipe|INTERRUPT_TIME</option>
    <option value="PerformanceMonitorCommandsPipe|DPC_TIME">PerformanceMonitorCommandsPipe|DPC_TIME</option>
    <option value="PerformanceMonitorCommandsPipe|GPU_UTILIZATION">PerformanceMonitorCommandsPipe|GPU_UTILIZATION</option>
    <option value="PerformanceMonitorCommandsPipe|GPU_TEMPERATURE">PerformanceMonitorCommandsPipe|GPU_TEMPERATURE</option>
    <option value="PerformanceMonitorCommandsPipe|GPU_MEMORY_UTILIZATION">PerformanceMonitorCommandsPipe|GPU_MEMORY_UTILIZATION</option>
    <option value="PerformanceMonitorCommandsPipe|MONITOR">PerformanceMonitorCommandsPipe|MONITOR</option>
    <option value="PerformanceMonitorCommandsPipe|ALERTS">PerformanceMonitorCommandsPipe|ALERTS</option>
    <option value="PerformanceMonitorCommandsPipe|SETTINGS">PerformanceMonitorCommandsPipe|SETTINGS</option>
    <option value="PerformanceMonitorCommandsPipe|ERRORS">PerformanceMonitorCommandsPipe|ERRORS</option>
    <option value="PerformanceMonitorCommandsPipe|RESTART_COUNTERS">PerformanceMonitorCommandsPipe|RESTART_COUNTERS</option>
    <option value="PerformanceMonitorCommandsPipe|MINIMIZE">PerformanceMonitorCommandsPipe|MINIMIZE</option>
    <option value="PerformanceMonitorCommandsPipe|RESTORE">PerformanceMonitorCommandsPipe|RESTORE</option>
    <option value="PerformanceMonitorCommandsPipe|ALIGN_LEFT-1500">PerformanceMonitorCommandsPipe|ALIGN_LEFT-1500</option>
    <option value="PerformanceMonitorCommandsPipe|TOPMOST">PerformanceMonitorCommandsPipe|TOPMOST</option>
    <option value="PerformanceMonitorCommandsPipe|CLOSE">PerformanceMonitorCommandsPipe|CLOSE</option>
    <option value="PerformanceMonitorCommandsPipe|FOREGROUND">PerformanceMonitorCommandsPipe|FOREGROUND</option>
    <!-- TELEMETRY VIB SHAKER -->
    <option value="TelemetryVibShakerPipeCommands|MONITOR">TelemetryVibShakerPipeCommands|MONITOR</option>
    <option value="TelemetryVibShakerPipeCommands|NOT_FOUNDS">TelemetryVibShakerPipeCommands|NOT_FOUNDS</option>
    <option value="TelemetryVibShakerPipeCommands|SETTINGS">TelemetryVibShakerPipeCommands|SETTINGS</option>
    <option value="TelemetryVibShakerPipeCommands|STOP">TelemetryVibShakerPipeCommands|STOP</option>
    <option value="TelemetryVibShakerPipeCommands|START">TelemetryVibShakerPipeCommands|START</option>
    <option value="TelemetryVibShakerPipeCommands|CYCLE_STATISTICS">TelemetryVibShakerPipeCommands|CYCLE_STATISTICS</option>
    <option value="TelemetryVibShakerPipeCommands|MINIMIZE">TelemetryVibShakerPipeCommands|MINIMIZE</option>
    <option value="TelemetryVibShakerPipeCommands|RESTORE">TelemetryVibShakerPipeCommands|RESTORE</option>
    <option value="TelemetryVibShakerPipeCommands|ALIGN_LEFT-1500">TelemetryVibShakerPipeCommands|ALIGN_LEFT-1500</option>
    <option value="TelemetryVibShakerPipeCommands|TOPMOST">TelemetryVibShakerPipeCommands|TOPMOST</option>
    <option value="TelemetryVibShakerPipeCommands|CLOSE">TelemetryVibShakerPipeCommands|CLOSE</option>
    <option value="TelemetryVibShakerPipeCommands|FOREGROUND">TelemetryVibShakerPipeCommands|FOREGROUND</option>
    <!-- WAR THUNDER EXPORTER -->
    <option value="WarThunderExporterPipeCommands|MONITOR">WarThunderExporterPipeCommands|MONITOR</option>
    <option value="WarThunderExporterPipeCommands|CYCLE_STATISTICS">WarThunderExporterPipeCommands|CYCLE_STATISTICS</option>
    <option value="WarThunderExporterPipeCommands|SETTINGS">WarThunderExporterPipeCommands|SETTINGS</option>
    <option value="WarThunderExporterPipeCommands|INTERVAL_100">WarThunderExporterPipeCommands|INTERVAL_100</option>
    <option value="WarThunderExporterPipeCommands|INTERVAL_50">WarThunderExporterPipeCommands|INTERVAL_50</option>
    <option value="WarThunderExporterPipeCommands|STOP">WarThunderExporterPipeCommands|STOP</option>
    <option value="WarThunderExporterPipeCommands|START">WarThunderExporterPipeCommands|START</option>
    <option value="WarThunderExporterPipeCommands|NOT_FOUNDS">WarThunderExporterPipeCommands|NOT_FOUNDS</option>
    <option value="WarThunderExporterPipeCommands|MINIMIZE">WarThunderExporterPipeCommands|MINIMIZE</option>
    <option value="WarThunderExporterPipeCommands|RESTORE">WarThunderExporterPipeCommands|RESTORE</option>
    <option value="WarThunderExporterPipeCommands|ALIGN_LEFT-1500">WarThunderExporterPipeCommands|ALIGN_LEFT-1500</option>
    <option value="WarThunderExporterPipeCommands|TOPMOST">WarThunderExporterPipeCommands|TOPMOST</option>
    <option value="WarThunderExporterPipeCommands|CLOSE">WarThunderExporterPipeCommands|CLOSE</option>
    <option value="WarThunderExporterPipeCommands|FOREGROUND">WarThunderExporterPipeCommands|FOREGROUND</option>
    <!-- SIM CONNECT EXPORTER -->
    <option value="SimConnectExporterPipeCommands|MONITOR">SimConnectExporterPipeCommands|MONITOR</option>
    <option value="SimConnectExporterPipeCommands|CYCLE_STATISTICS">SimConnectExporterPipeCommands|CYCLE_STATISTICS</option>
    <option value="SimConnectExporterPipeCommands|SHOW_GFORCES">SimConnectExporterPipeCommands|SHOW_GFORCES</option>
    <option value="SimConnectExporterPipeCommands|SETTINGS">SimConnectExporterPipeCommands|SETTINGS</option>
    <option value="SimConnectExporterPipeCommands|STOP">SimConnectExporterPipeCommands|STOP</option>
    <option value="SimConnectExporterPipeCommands|START">SimConnectExporterPipeCommands|START</option>
    <option value="SimConnectExporterPipeCommands|MINIMIZE">SimConnectExporterPipeCommands|MINIMIZE</option>
    <option value="SimConnectExporterPipeCommands|RESTORE">SimConnectExporterPipeCommands|RESTORE</option>
    <option value="SimConnectExporterPipeCommands|ALIGN_LEFT-1500">SimConnectExporterPipeCommands|ALIGN_LEFT-1500</option>
    <option value="SimConnectExporterPipeCommands|TOPMOST">SimConnectExporterPipeCommands|TOPMOST</option>
    <option value="SimConnectExporterPipeCommands|CLOSE">SimConnectExporterPipeCommands|CLOSE</option>
    <option value="SimConnectExporterPipeCommands|FOREGROUND">SimConnectExporterPipeCommands|FOREGROUND</option>
    <!-- PS IPC -->
    <option value="ipc_pipe_vr_server_commands|SHOW_PROCESS">ipc_pipe_vr_server_commands|SHOW_PROCESS</option>
</select>

<!-- Hidden Command submit — triggered by fireSpecial() / firePipe() -->
<input type="submit" name="Command" value="Command" id="btnCommand" style="display:none">


<div class="scroll">

    <!-- ═══════════════════════════════════════
         INSTRUMENT 1 — WINDOW CONTROL
         ═══════════════════════════════════════ -->
    <div class="instrument" id="inst-window">
        <span class="rivet-tl">⊕</span><span class="rivet-tr">⊕</span>
        <span class="rivet-bl">⊕</span><span class="rivet-br">⊕</span>

        <div class="inst-nameplate" onclick="toggleInst('inst-window')">
            <div class="nameplate-left">
                <span class="nameplate-led" style="background:var(--green-led);box-shadow:0 0 5px var(--green-led);"></span>
                <div>
                    <div class="inst-title">WINDOW CONTROL</div>
                    <div class="inst-code">SYS / WIN / POS-CTL</div>
                </div>
            </div>
            <span class="inst-caret">&#9662;</span>
        </div>

        <div class="inst-body">

            <!-- Process selector -->
            <div class="field-row">
                <span class="field-tag">TGT PROCESS</span>
                <select class="phos-select" id="Process" name="Process" required>
                    <?php
                    $programs = getProgramsList();
                    foreach ($programs as $prog) {
                        $pName = htmlspecialchars($prog['processName']);
                        $fName = htmlspecialchars(isset($prog['friendlyName']) ? $prog['friendlyName'] : $pName);
                        echo "<option value='$pName'>$fName</option>";
                    }
                    ?>
                </select>
            </div>

            <!-- Instance and Window State side by side -->
            <div class="row-2">
                <div class="field-row" style="margin-bottom:0">
                    <span class="field-tag">INSTANCE / THREADS</span>
                    <input class="phos-input" type="text" name="Instance" id="Instance"
                           value="<?php echo htmlspecialchars($frm_instance); ?>"
                           placeholder="0" required>
                </div>
                <div class="field-row" style="margin-bottom:0">
                    <span class="field-tag">WINDOW STATE</span>
                    <div class="state-row" style="grid-template-columns:repeat(2,1fr);gap:4px;margin-top:0;">
                        <button type="button" class="sw-btn active" onclick="setWindowState(this,'0')">
                            <span class="sw-ico">&#9674;</span>NO CHG
                            <span class="sw-indicator"></span>
                        </button>
                        <button type="button" class="sw-btn" onclick="setWindowState(this,'-1')">
                            <span class="sw-ico">&#8863;</span>MIN
                            <span class="sw-indicator"></span>
                        </button>
                        <button type="button" class="sw-btn" onclick="setWindowState(this,'1')">
                            <span class="sw-ico">&#9645;</span>REST
                            <span class="sw-indicator"></span>
                        </button>
                        <button type="button" class="sw-btn" onclick="setWindowState(this,'2')">
                            <span class="sw-ico">&#11035;</span>MAX
                            <span class="sw-indicator"></span>
                        </button>
                    </div>
                </div>
            </div>

            <hr class="amber-rule">

            <!-- X / Y coordinates -->
            <div class="row-2">
                <div class="field-row" style="margin-bottom:0">
                    <span class="field-tag">X COORD</span>
                    <input class="phos-input" type="text" name="X" id="X"
                           value="<?php echo htmlspecialchars($frmX); ?>"
                           placeholder="-1700">
                </div>
                <div class="field-row" style="margin-bottom:0">
                    <span class="field-tag">Y COORD</span>
                    <input class="phos-input" type="text" name="Y" id="Y"
                           value="<?php echo htmlspecialchars($frmY); ?>"
                           placeholder="">
                </div>
            </div>

            <hr class="amber-rule">

            <!-- Action buttons — 3×2 grid -->
            <div class="action-grid">
                <!-- Row 1 -->
                <button type="submit" class="act-btn btn-amber" name="Move" value="Apply Changes">
                    <span class="act-ico">&#128205;</span>APPLY
                </button>
                <button type="submit" class="act-btn btn-amber" name="MakeForeground" value="Make Foreground">
                    <span class="act-ico">&#128065;</span>FGND
                </button>
                <button type="submit" class="act-btn btn-amber" name="GetValues" value="GetValues">
                    <span class="act-ico">&#128203;</span>GET VAL
                </button>
                <!-- Row 2 -->
                <button type="submit" class="act-btn btn-green" name="Launch" value="Launch">
                    <span class="act-ico">&#128640;</span>LAUNCH
                </button>
                <button type="submit" class="act-btn btn-red caution" name="Exit" value="Terminate">
                    <span class="act-ico">&#9760;</span>KILL
                </button>
                <button type="button" class="act-btn btn-warn"
                        onclick="window.location.href=window.location.href.split('?')[0];">
                    <span class="act-ico">&#8634;</span>RESET
                </button>
            </div>

        </div><!-- /inst-body -->
        <span class="part-num">P/N: WC-001</span>
    </div><!-- /inst-window -->


    <!-- ═══════════════════════════════════════
         INSTRUMENT 2 — SPECIAL COMMANDS
         ═══════════════════════════════════════ -->
    <div class="instrument" id="inst-special">
        <span class="rivet-tl">⊕</span><span class="rivet-tr">⊕</span>
        <span class="rivet-bl">⊕</span><span class="rivet-br">⊕</span>

        <div class="inst-nameplate" onclick="toggleInst('inst-special')">
            <div class="nameplate-left">
                <span class="nameplate-led" style="background:var(--amber-dim);"></span>
                <div>
                    <div class="inst-title">SPECIAL COMMANDS</div>
                    <div class="inst-code">SYS / PWR / THD-CTL</div>
                </div>
            </div>
            <span class="inst-caret">&#9662;</span>
        </div>

        <div class="inst-body">
            <div class="spec-grid">

                <div class="sys-tag">// WATCHDOG</div>
                <button type="button" class="spec-btn watchdog full-row" onclick="fireSpecial('WATCHDOG')">
                    <span class="s-name">&#9888; WATCHDOG</span>
                    <span class="s-desc">Start JSON command watcher + audio alert</span>
                </button>

                <div class="sys-tag">// POWER SCHEME</div>
                <button type="button" class="spec-btn danger" onclick="fireSpecial('HIGHPERFORMANCE')">
                    <span class="s-name">&#9889; HIGH PERF</span>
                    <span class="s-desc">Max CPU power</span>
                </button>
                <button type="button" class="spec-btn ok" onclick="fireSpecial('BALANCED')">
                    <span class="s-name">&#9878; BALANCED</span>
                    <span class="s-desc">Standard power</span>
                </button>
                <button type="button" class="spec-btn ok" onclick="fireSpecial('BALANCED80')">
                    <span class="s-name">&#9878; BAL 80%</span>
                    <span class="s-desc">Max CPU 80%</span>
                </button>
                <button type="button" class="spec-btn info" onclick="fireSpecial('READPOWERSCHEME')">
                    <span class="s-name">&#128225; READ PLAN</span>
                    <span class="s-desc">Query current scheme</span>
                </button>

                <div class="sys-tag">// THREAD ANALYSIS</div>
                <button type="button" class="spec-btn full-row" style="border-color:#4a1a6d;color:#c084f5;background:#060108;" onclick="fireSpecial('SHOW_THREADS')">
                    <span class="s-name">&#9990; SHOW THREADS</span>
                    <span class="s-desc">Top N threads by CPU time — VR flight games (N = Instance field)</span>
                </button>

                <div class="sys-tag">// CPU BOOST — P-CORE SCHEDULER</div>
                <button type="button" class="spec-btn ok" onclick="fireSpecial('BOOST_1')">
                    <span class="s-name">&#9650; BOOST 1</span>
                    <span class="s-desc">AboveNormal + P-Core Ideal</span>
                </button>
                <button type="button" class="spec-btn ok" onclick="fireSpecial('BOOST_2')">
                    <span class="s-name">&#9650;&#9650; BOOST 2</span>
                    <span class="s-desc">+ CPU_SETS</span>
                </button>
                <button type="button" class="spec-btn boost-nuke" onclick="fireSpecial('BOOST_3')">
                    <span class="s-name">&#9650;&#9650;&#9650; BOOST 3 — HARD AFFINITY</span>
                    <span class="s-desc">Nuclear option: AboveNormal + P-Core Ideal + Hard Thread Affinities</span>
                </button>

            </div>
        </div><!-- /inst-body -->
        <span class="part-num">P/N: SC-002</span>
    </div><!-- /inst-special -->


    <!-- ═══════════════════════════════════════
         INSTRUMENT 3 — PIPE COMMANDS
         ═══════════════════════════════════════ -->
    <div class="instrument" id="inst-pipe">
        <span class="rivet-tl">⊕</span><span class="rivet-tr">⊕</span>
        <span class="rivet-bl">⊕</span><span class="rivet-br">⊕</span>

        <div class="inst-nameplate" onclick="toggleInst('inst-pipe')">
            <div class="nameplate-left">
                <span class="nameplate-led" style="background:var(--cyan-led);box-shadow:0 0 5px var(--cyan-led);"></span>
                <div>
                    <div class="inst-title">PIPE COMMANDS</div>
                    <div class="inst-code">SYS / IPC / PIPE-CTL</div>
                </div>
            </div>
            <span class="inst-caret">&#9662;</span>
        </div>

        <div class="inst-body">

            <!-- Tab strip -->
            <div class="pipe-tabs-row">
                <button type="button" class="ptab active" onclick="setPipeTab('perf',this)">PERF-MON</button>
                <button type="button" class="ptab" onclick="setPipeTab('tvs',this)">TVS</button>
                <button type="button" class="ptab" onclick="setPipeTab('wt',this)">WAR-THU</button>
                <button type="button" class="ptab" onclick="setPipeTab('sim',this)">SIMCON</button>
                <button type="button" class="ptab" onclick="setPipeTab('ps',this)">PS-IPC</button>
            </div>

            <!-- PerformanceMonitor -->
            <div class="pipe-content active" id="pipe-perf">
                <div class="pipe-grid">
                    <button type="button" class="pcmd-btn" onclick="firePipe('PerformanceMonitorCommandsPipe','CYCLE_CPU_ALARM')">CPU ALARM</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('PerformanceMonitorCommandsPipe','PROCESSOR_UTILITY')">PROC UTIL</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('PerformanceMonitorCommandsPipe','PROCESSOR_TIME')">PROC TIME</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('PerformanceMonitorCommandsPipe','INTERRUPT_TIME')">INTR TIME</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('PerformanceMonitorCommandsPipe','DPC_TIME')">DPC TIME</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('PerformanceMonitorCommandsPipe','GPU_UTILIZATION')">GPU UTIL</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('PerformanceMonitorCommandsPipe','GPU_TEMPERATURE')">GPU TEMP</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('PerformanceMonitorCommandsPipe','GPU_MEMORY_UTILIZATION')">GPU MEM</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('PerformanceMonitorCommandsPipe','MONITOR')">MONITOR</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('PerformanceMonitorCommandsPipe','ALERTS')">ALERTS</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('PerformanceMonitorCommandsPipe','SETTINGS')">SETTINGS</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('PerformanceMonitorCommandsPipe','ERRORS')">ERRORS</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('PerformanceMonitorCommandsPipe','RESTART_COUNTERS')">RESTART</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('PerformanceMonitorCommandsPipe','MINIMIZE')">MINIMIZE</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('PerformanceMonitorCommandsPipe','RESTORE')">RESTORE</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('PerformanceMonitorCommandsPipe','ALIGN_LEFT-1500')">ALIGN-L</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('PerformanceMonitorCommandsPipe','TOPMOST')">TOPMOST</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('PerformanceMonitorCommandsPipe','CLOSE')">CLOSE</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('PerformanceMonitorCommandsPipe','FOREGROUND')">FGND</button>
                </div>
            </div>

            <!-- TelemetryVibShaker -->
            <div class="pipe-content" id="pipe-tvs">
                <div class="pipe-grid">
                    <button type="button" class="pcmd-btn" onclick="firePipe('TelemetryVibShakerPipeCommands','MONITOR')">MONITOR</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('TelemetryVibShakerPipeCommands','NOT_FOUNDS')">NOT FND</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('TelemetryVibShakerPipeCommands','SETTINGS')">SETTINGS</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('TelemetryVibShakerPipeCommands','STOP')">STOP</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('TelemetryVibShakerPipeCommands','START')">START</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('TelemetryVibShakerPipeCommands','CYCLE_STATISTICS')">CYC STATS</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('TelemetryVibShakerPipeCommands','MINIMIZE')">MINIMIZE</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('TelemetryVibShakerPipeCommands','RESTORE')">RESTORE</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('TelemetryVibShakerPipeCommands','ALIGN_LEFT-1500')">ALIGN-L</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('TelemetryVibShakerPipeCommands','TOPMOST')">TOPMOST</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('TelemetryVibShakerPipeCommands','CLOSE')">CLOSE</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('TelemetryVibShakerPipeCommands','FOREGROUND')">FGND</button>
                </div>
            </div>

            <!-- WarThunder -->
            <div class="pipe-content" id="pipe-wt">
                <div class="pipe-grid">
                    <button type="button" class="pcmd-btn" onclick="firePipe('WarThunderExporterPipeCommands','MONITOR')">MONITOR</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('WarThunderExporterPipeCommands','CYCLE_STATISTICS')">CYC STATS</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('WarThunderExporterPipeCommands','SETTINGS')">SETTINGS</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('WarThunderExporterPipeCommands','INTERVAL_100')">INT 100</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('WarThunderExporterPipeCommands','INTERVAL_50')">INT 50</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('WarThunderExporterPipeCommands','STOP')">STOP</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('WarThunderExporterPipeCommands','START')">START</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('WarThunderExporterPipeCommands','NOT_FOUNDS')">NOT FND</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('WarThunderExporterPipeCommands','MINIMIZE')">MINIMIZE</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('WarThunderExporterPipeCommands','RESTORE')">RESTORE</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('WarThunderExporterPipeCommands','ALIGN_LEFT-1500')">ALIGN-L</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('WarThunderExporterPipeCommands','TOPMOST')">TOPMOST</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('WarThunderExporterPipeCommands','CLOSE')">CLOSE</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('WarThunderExporterPipeCommands','FOREGROUND')">FGND</button>
                </div>
            </div>

            <!-- SimConnect -->
            <div class="pipe-content" id="pipe-sim">
                <div class="pipe-grid">
                    <button type="button" class="pcmd-btn" onclick="firePipe('SimConnectExporterPipeCommands','MONITOR')">MONITOR</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('SimConnectExporterPipeCommands','CYCLE_STATISTICS')">CYC STATS</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('SimConnectExporterPipeCommands','SHOW_GFORCES')">G-FORCES</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('SimConnectExporterPipeCommands','SETTINGS')">SETTINGS</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('SimConnectExporterPipeCommands','STOP')">STOP</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('SimConnectExporterPipeCommands','START')">START</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('SimConnectExporterPipeCommands','MINIMIZE')">MINIMIZE</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('SimConnectExporterPipeCommands','RESTORE')">RESTORE</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('SimConnectExporterPipeCommands','ALIGN_LEFT-1500')">ALIGN-L</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('SimConnectExporterPipeCommands','TOPMOST')">TOPMOST</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('SimConnectExporterPipeCommands','CLOSE')">CLOSE</button>
                    <button type="button" class="pcmd-btn" onclick="firePipe('SimConnectExporterPipeCommands','FOREGROUND')">FGND</button>
                </div>
            </div>

            <!-- PowerShell IPC Pipe -->
            <div class="pipe-content" id="pipe-ps">
                <div class="pipe-grid">
                    <button type="button" class="pcmd-btn" style="grid-column:1/-1;"
                            onclick="firePipe('ipc_pipe_vr_server_commands','SHOW_PROCESS')">SHOW PROCESS TIMES</button>
                </div>
            </div>

        </div><!-- /inst-body -->
        <span class="part-num">P/N: PC-003</span>
    </div><!-- /inst-pipe -->

</div><!-- /scroll -->


<!-- ═══════════════════════════════════════════
     STATUS TAPE — fixed bottom bar
     id="result" receives JS "Processing" text
     AND PHP $footer output on page reload
     ═══════════════════════════════════════════ -->
<div class="status-tape">
    <div class="tape-label">SYS OUTPUT // LAST CMD</div>
    <div class="tape-display">
        <span class="tape-cursor"></span>
        <span id="result">[<?php echo $time_stamp; ?>] <?php echo $footer; ?></span>
    </div>
    <div class="php-ver"><?php echo 'PHP ' . phpversion(); ?></div>
</div>

</form><!-- /RemoteControlForm -->

</body>
</html>
