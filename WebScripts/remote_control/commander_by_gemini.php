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
		usleep($delay_ms * 1000); // usleep takes microseconds, so we convert milliseconds to microseconds
		
		//if (file_exists($outfile)) { $file = fopen($filename, "r"); }
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
    //return date("Y-m-d H:i:s.v");
    $microtime = microtime(true);
    $milliseconds = sprintf("%03d", ($microtime - floor($microtime)) * 1000);
    return date("Y-m-d H:i:s", $microtime) . ".$milliseconds";	
}

function getPathByProcessName($jsonFilePath, $processName) {
    // Read the JSON file
    $jsonData = file_get_contents($jsonFilePath);
    
    // Decode JSON data into a PHP array
    $data = json_decode($jsonData, true);
    
    // Check if decoding was successful
    if ($data === null) {
        die('Error decoding JSON');
    }
    
    // Search for the processName
    foreach ($data['programs'] as $program) {
        if ($program['processName'] === $processName) {
            return $program['path'];
        }
    }
    
    // Return null if processName is not found
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
$frm_pipe_command = isset($_POST['PipeCommand'])? $_POST['PipeCommand']: "";

$name = $gender = $color = "";
$subscribe = false;

// Check if the form is submitted
if ($_SERVER["REQUEST_METHOD"] == "POST") {
	$frmX = isset($_POST['X']) ? htmlspecialchars($_POST['X']) : "-1700";
	$frmY = isset($_POST['Y']) ? htmlspecialchars($_POST['Y']) : "";
	$frm_instance = isset($_POST['Instance']) ? htmlspecialchars($_POST['Instance']) : 0;
	$threads = is_numeric($frm_instance)?(int)$frm_instance: 50; // how many threads per process should be processed (50 busiest should be enough right)
	if ($threads<=0) $threads = 50;

	$frm_process = isset($_POST['Process']) ? htmlspecialchars($_POST['Process']) : "";
	
	// CHECK FOR PIPE/SPECIAL COMMAND
	$post_command = isset($_POST['Command'])? $_POST['Command']: "";
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
			  $footer = "Set Power Scheme to High Power";
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
			  $footer = "Read current power plan";
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
	} // end-if 
		
	// CHECK FOR LAUNCH COMMAND
	$post_command = isset($_POST['Launch'])? $_POST['Launch']: "";
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
	} // end-if Launch
	
	// CHECK FOR TERMINATE COMMAND
	$post_command = isset($_POST['Exit'])? $_POST['Exit']: "";
	if ($post_command == "Terminate") { 
		$processName = $_POST['Process'];
		$footer = "Terminate completed - [$processName]";
				
		$jsonData = createJsonCommand("KILL", array("processName" => $processName));
		writeJsonToFile($jsonData, TEMP_FILE);
		renameFile(TEMP_FILE, COMMAND_FILE);
		$time_stamp = getTimestamp();
	} // end-if 
	
	// CHECK FOR FOREGROUND COMMAND
	$post_command = isset($_POST['MakeForeground'])? $_POST['MakeForeground']: "";
	if ($post_command == "Make Foreground") { 
		$processName = $_POST['Process'];
		$footer = "Foreground completed - [$processName]";
				
		$jsonData = createJsonCommand("FOREGROUND", array("processName" => $processName, "instance" => $frm_instance));
		writeJsonToFile($jsonData, TEMP_FILE);
		renameFile(TEMP_FILE, COMMAND_FILE);
  
		$time_stamp = getTimestamp();
	} // end-if 

	// CHECK FOR GET-VALUES COMMAND
	$post_command = isset($_POST['GetValues'])? $_POST['GetValues']: "";
	if ($post_command == "GetValues") { 
		$processName = $_POST['Process'];
		$footer = "GetValues completed - [$processName]";
				
		$outfile = "C:\\MyPrograms\\wamp\\www\\remote_control\\outfile.txt";
		if (file_exists(basename($outfile))) { unlink(basename($outfile)); }
		
		$jsonData = createJsonCommand("GET-LOCATION", array("processName" => $processName, "instance" => $frm_instance, "outfile" => $outfile));
		writeJsonToFile($jsonData, TEMP_FILE);
		renameFile(TEMP_FILE, COMMAND_FILE);		
		$time_stamp = getTimestamp();
		
		$file=tryOpenFile(basename($outfile), 50, 100); 
		if ($file) {			
			$frmX = trim(fgets($file)); // Trim to avoid newline breaks in JSON
			$frmY = trim(fgets($file)); 		
			fclose($file);			
		} else {
			$footer = "Error opening the file.";
			$frmX = "?";
			$frmY = "?";
		}		
	} // end-if 

	// CHECK FOR MOVE,MINIMIZE,ETC COMMAND
	$post_command = isset($_POST['Move'])? $_POST['Move']: "";
	if ($post_command == "Apply Changes") { 
		$processName = $_POST['Process'];
		$state = isset($_POST['State'])? $_POST['State']: "0";
		
        switch ($state) {
            case "0":  // Change X,Y coordinates
                $footer = "Move completed - [$processName]";
                $jsonData = createJsonCommand("CHANGE_LOCATION", array("processName" => $processName, "instance" => $frm_instance, "x" => $frmX, "y" => $frmY));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);
                $time_stamp = getTimestamp();
                break;

            case "-1":	// Minimize
                $footer = "Minimize completed - [$processName]";
                $jsonData = createJsonCommand("MINIMIZE", array("processName" => $processName, "instance" => $frm_instance));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);					
                $time_stamp = getTimestamp();
                break;
        
            case "1":	// Restore
                $footer = "Restore completed - [$processName]";
                $jsonData = createJsonCommand("RESTORE", array("processName" => $processName, "instance" => $frm_instance));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);					
                $time_stamp = getTimestamp();
                break;
                
            case "2":	// Maximize
                $footer = "Maximize completed - [$processName]";
                $jsonData = createJsonCommand("MAXIMIZE", array("processName" => $processName, "instance" => $frm_instance));
                writeJsonToFile($jsonData, TEMP_FILE);
                renameFile(TEMP_FILE, COMMAND_FILE);
                $time_stamp = getTimestamp();
                break;					
		}		
	} // end-if 

    // =========================================================================
    // NEW AJAX HANDLER: If request came from our new UI, return JSON and exit
    // =========================================================================
    if (isset($_POST['ajax']) && $_POST['ajax'] == '1') {
        header('Content-Type: application/json');
        echo json_encode([
            'footer' => strip_tags($footer), // Remove span tags for clean UI display
            'timestamp' => $time_stamp,
            'x' => $frmX,
            'y' => $frmY
        ]);
        exit;
    }
} //end-if REQUEST_METHOD == POST
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no">
    <title>Avionics Commander HUD by Gemini</title>
    <link href="https://fonts.googleapis.com/css2?family=VT323&display=swap" rel="stylesheet">
    
    <style>
        :root {
            --bg-color: #060504; --panel-bg: #110e0a;
            --amber: #ffb000; --amber-dim: #553a00; --amber-glow: rgba(255, 176, 0, 0.4);
            --red: #ff3333; --red-dim: #660000; --rivet: #2a241c;
        }

        * { box-sizing: border-box; user-select: none; }
        body { margin: 0; padding: 0; background-color: var(--bg-color); color: var(--amber); font-family: 'VT323', monospace; height: 100vh; overflow: hidden; display: flex; flex-direction: column; }
        .vignette { position: fixed; top: 0; left: 0; width: 100vw; height: 100vh; box-shadow: inset 0 0 100px rgba(0,0,0,0.9); pointer-events: none; z-index: 99; }

        header { padding: 10px 10px 5px; text-align: center; font-size: 1.5rem; text-shadow: 0 0 8px var(--amber-glow); letter-spacing: 2px; }
        .global-target-bar { background: #2a1f00; border-top: 1px solid var(--amber-dim); border-bottom: 1px solid var(--amber-dim); text-align: center; padding: 5px 0; font-size: 1.2rem; color: var(--amber); text-shadow: 0 0 5px var(--amber); letter-spacing: 1px; }

        .nav-tabs { display: flex; background: var(--bg-color); border-bottom: 1px solid var(--amber-dim); }
        .tab { flex: 1; padding: 12px 0; text-align: center; font-family: 'VT323', monospace; font-size: 1.2rem; color: var(--amber-dim); background: #0a0806; border: 1px solid var(--amber-dim); border-bottom: none; cursor: pointer; transition: 0.2s; }
        .tab.active { color: var(--amber); background: var(--panel-bg); border-top: 3px solid var(--amber); text-shadow: 0 0 5px var(--amber-glow); }
        .main-content { flex: 1; overflow-y: auto; padding: 15px; padding-bottom: 100px; }

        .bezel { background-color: var(--panel-bg); border: 4px solid #1a1510; border-radius: 6px; padding: 25px 15px 15px; margin-bottom: 20px; position: relative; box-shadow: inset 0 0 15px rgba(0,0,0,0.8), 0 5px 15px rgba(0,0,0,0.5); background-image: radial-gradient(circle at 12px 12px, var(--rivet) 3px, transparent 4px), radial-gradient(circle at calc(100% - 12px) 12px, var(--rivet) 3px, transparent 4px), radial-gradient(circle at 12px calc(100% - 12px), var(--rivet) 3px, transparent 4px), radial-gradient(circle at calc(100% - 12px) calc(100% - 12px), var(--rivet) 3px, transparent 4px); }
        .part-number { position: absolute; top: 4px; right: 10px; font-size: 0.8rem; color: #665544; }
        .bezel-title { position: absolute; top: -12px; left: 15px; background: var(--bg-color); padding: 0 8px; font-size: 1.2rem; color: var(--amber); border: 1px solid var(--amber-dim); text-shadow: 0 0 4px var(--amber-glow); }

        .grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
        .grid-3 { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 8px; }
        .grid-4 { display: grid; grid-template-columns: 1fr 1fr 1fr 1fr; gap: 5px; }
        
        .btn { background: #18140f; border: 2px solid var(--amber-dim); color: var(--amber); font-family: 'VT323', monospace; font-size: 1.1rem; padding: 10px 5px; text-align: center; text-transform: uppercase; border-radius: 4px; box-shadow: 0 5px 0 var(--amber-dim), inset 0 0 5px rgba(255,176,0,0.1); cursor: pointer; transition: transform 0.05s, box-shadow 0.05s; text-shadow: 0 0 3px var(--amber-glow); overflow: hidden; white-space: nowrap; text-overflow: ellipsis; }
        .btn:active { transform: translateY(5px); box-shadow: 0 0px 0 var(--amber-dim), inset 0 0 15px rgba(255,176,0,0.3); }

        .toggle-btn { background: #0a0806; border: 1px solid var(--amber-dim); color: var(--amber-dim); box-shadow: none; padding: 8px 5px; font-size: 1rem;}
        .toggle-btn.selected { background: #2a1f00; border-color: var(--amber); color: var(--amber); box-shadow: inset 0 0 10px var(--amber-glow); text-shadow: 0 0 5px var(--amber); }

        .btn-caution { border-color: var(--red-dim); color: var(--red); box-shadow: 0 5px 0 var(--red-dim), inset 0 0 5px rgba(255,0,0,0.1); text-shadow: 0 0 3px rgba(255,0,0,0.5); position: relative; margin-top: 15px; }
        .btn-caution::before { content: "CAUTION"; position: absolute; top: -2px; left: 50%; transform: translateX(-50%); background: #000; color: #ffcc00; font-size: 0.6rem; padding: 0 10px; border-bottom: 1px solid var(--red-dim); background-image: repeating-linear-gradient(45deg, #000, #000 5px, #ffcc00 5px, #ffcc00 10px); -webkit-background-clip: text; -webkit-text-fill-color: transparent; }

        .stepper-container { display: flex; align-items: center; justify-content: space-between; background: #000; border: 2px inset #332200; padding: 5px; margin: 15px 0; }
        .stepper-label { margin-left: 10px; font-size: 1.1rem; color: #aa8855; }
        .stepper-controls { display: flex; align-items: center; }
        .stepper-btn { background: #22180c; border: 1px solid var(--amber-dim); color: var(--amber); width: 40px; height: 40px; font-size: 1.5rem; display: flex; align-items: center; justify-content: center; cursor: pointer;}
        .stepper-btn:active { background: var(--amber); color: #000; }
        .stepper-val { width: 60px; text-align: center; font-size: 1.2rem; border: none; background: transparent; color: var(--amber); font-family: 'VT323', monospace; }

        .status-tape { position: fixed; bottom: 0; left: 0; width: 100%; background: #000; border-top: 3px solid var(--amber-dim); padding: 10px 15px; height: 90px; overflow-y: hidden; font-size: 1.1rem; color: var(--amber); box-shadow: 0 -10px 20px rgba(0,0,0,0.9); z-index: 90; }
        .tape-line { margin: 2px 0; opacity: 0.8; }
        .tape-line.new { opacity: 1; text-shadow: 0 0 8px var(--amber); }
        .cursor { display: inline-block; width: 10px; height: 1.1rem; background-color: var(--amber); vertical-align: bottom; animation: blink 1s step-end infinite; }
        @keyframes blink { 50% { opacity: 0; } }

        .tab-content { display: none; }
        .tab-content.active { display: block; }

        /* Scrollable container for apps list */
        .app-list-container { max-height: 180px; overflow-y: auto; padding-right: 5px; margin-bottom: 10px; border-bottom: 1px solid #332200; }
        /* Scrollbar styling */
        ::-webkit-scrollbar { width: 6px; }
        ::-webkit-scrollbar-track { background: #000; }
        ::-webkit-scrollbar-thumb { background: var(--amber-dim); }
    </style>
</head>
<body>
    <div class="vignette"></div>

    <header>CMD_UPLINK // SYS_ONLINE</header>
    <div class="global-target-bar" id="global-target-display">TGT LOCK: [NONE]</div>

    <div class="nav-tabs">
        <div class="tab active" onclick="switchTab('apps')">APPS</div>
        <div class="tab" onclick="switchTab('spat')">SPATIAL</div>
        <div class="tab" onclick="switchTab('sys')">SYSTEM</div>
        <div class="tab" onclick="switchTab('comms')">PIPES</div>
    </div>

    <div class="main-content">
        <!-- TAB 1: APPS & EXECUTION -->
        <div id="tab-apps" class="tab-content active">
            <div class="bezel">
                <div class="part-number">PN: TGT-SLCT-01</div>
                <div class="bezel-title">TARGET SELECT</div>
                <div class="app-list-container">
                    <div class="grid-3" id="app-grid">
                        <?php 
                        $programs = getProgramsList();
                        $firstApp = "";
                        foreach ($programs as $index => $prog): 
                            $pName = htmlspecialchars($prog['processName']);
                            if ($index === 0) $firstApp = $pName;
                            $fName = htmlspecialchars(isset($prog['friendlyName']) ? $prog['friendlyName'] : $pName);
                            // Shorten name for UI fit
                            $shortName = strtoupper(substr($fName, 0, 15)); 
                        ?>
                            <div class="btn toggle-btn <?= $index===0 ? 'selected' : '' ?>" onclick="selectApp(this, '<?= $pName ?>')"><?= $shortName ?></div>
                        <?php endforeach; ?>
                    </div>
                </div>
                
                <div class="stepper-container">
                    <div class="stepper-label">INST/THREADS</div>
                    <div class="stepper-controls">
                        <div class="stepper-btn" onclick="stepVal('inst-val', -1)">-</div>
                        <input type="text" class="stepper-val" id="inst-val" value="<?= $frm_instance==0 ? 50 : $frm_instance ?>">
                        <div class="stepper-btn" onclick="stepVal('inst-val', 1)">+</div>
                    </div>
                </div>
            </div>

            <div class="bezel">
                <div class="part-number">PN: EXEC-CTRL-02</div>
                <div class="bezel-title">EXECUTION</div>
                <div class="grid-2">
                    <div class="btn" onclick="sendAction('Launch', 'Launch')">LAUNCH</div>
                    <div class="btn" onclick="sendAction('MakeForeground', 'Make Foreground')">FOREGROUND</div>
                </div>
                <div class="btn btn-caution" onclick="sendAction('Exit', 'Terminate')">TERMINATE</div>
            </div>
        </div>

        <!-- TAB 2: SPATIAL NAV -->
        <div id="tab-spat" class="tab-content">
            <div class="bezel">
                <div class="part-number">PN: POS-NAV-03</div>
                <div class="bezel-title">SPATIAL NAV</div>
                
                <div class="stepper-container">
                    <div class="stepper-label">X COORD</div>
                    <input type="text" class="stepper-val" id="coord-x" value="<?= $frmX ?>" style="width:100px; text-align:right; margin-right:10px;">
                </div>
                <div class="stepper-container">
                    <div class="stepper-label">Y COORD</div>
                    <input type="text" class="stepper-val" id="coord-y" value="<?= $frmY ?>" style="width:100px; text-align:right; margin-right:10px;">
                </div>

                <div class="btn" style="width:100%; margin-bottom:15px; border-color:#885500;" onclick="sendAction('GetValues', 'GetValues')">PULL CURRENT COORDS</div>

                <div class="grid-2">
                    <div class="btn" onclick="sendMove('0')">APPLY (X,Y)</div>
                    <div class="btn" onclick="sendMove('-1')">MINIMIZE</div>
                    <div class="btn" onclick="sendMove('1')">RESTORE</div>
                    <div class="btn" onclick="sendMove('2')">MAXIMIZE</div>
                </div>
            </div>
        </div>

        <!-- TAB 3: SYSTEM & PWR -->
        <div id="tab-sys" class="tab-content">
            <div class="bezel">
                <div class="part-number">PN: PWR-PLN-04</div>
                <div class="bezel-title">SYSTEM SCHEMES</div>
                <div class="grid-2">
                    <div class="btn toggle-btn" onclick="sendSpecial('HIGHPERFORMANCE')">HIGH PERF</div>
                    <div class="btn toggle-btn" onclick="sendSpecial('BALANCED')">BALANCED</div>
                    <div class="btn toggle-btn" onclick="sendSpecial('BALANCED80')">BAL. MAX 80</div>
                    <div class="btn toggle-btn" onclick="sendSpecial('READPOWERSCHEME')">READ CURR</div>
                </div>
            </div>
            
            <div class="bezel">
                <div class="part-number">PN: BST-OVR-05</div>
                <div class="bezel-title">GAME BOOSTS</div>
                <div class="grid-3">
                    <div class="btn" onclick="sendSpecial('BOOST_1')">BST 1</div>
                    <div class="btn" onclick="sendSpecial('BOOST_2')">BST 2</div>
                    <div class="btn" onclick="sendSpecial('BOOST_3')">BST 3</div>
                </div>
                <div class="btn" style="margin-top:10px; width:100%;" onclick="sendSpecial('SHOW_THREADS')">SHOW THREAD TIMES</div>
            </div>

            <div class="bezel">
                <div class="part-number">PN: WDG-MGR-06</div>
                <div class="bezel-title">WATCHDOG TIES</div>
                <div class="btn" style="width:100%" onclick="sendSpecial('WATCHDOG')">ENABLE JSON WATCHDOG</div>
            </div>
        </div>

        <!-- TAB 4: IPC PIPES -->
        <div id="tab-comms" class="tab-content">
            <div class="bezel">
                <div class="part-number">PN: IPC-DAT-07</div>
                <div class="bezel-title">DATA PIPELINES</div>
                
                <!-- Category Selectors -->
                <div class="grid-3" style="margin-bottom: 15px; border-bottom: 1px solid #332200; padding-bottom: 10px;">
                    <div class="btn toggle-btn selected" onclick="showPipeCat('pipe-perfmon', this)">PERF MON</div>
                    <div class="btn toggle-btn" onclick="showPipeCat('pipe-telemetry', this)">TELEMETRY</div>
                    <div class="btn toggle-btn" onclick="showPipeCat('pipe-warthunder', this)">WAR THUNDER</div>
                    <div class="btn toggle-btn" onclick="showPipeCat('pipe-simconnect', this)">SIM CONNECT</div>
                    <div class="btn toggle-btn" onclick="showPipeCat('pipe-ps', this)">SYS (PS)</div>
                </div>

                <!-- PERF MON PIPES -->
                <div id="pipe-perfmon" class="pipe-cat-grid grid-2">
                    <div class="btn" onclick="sendPipe('PerformanceMonitorCommandsPipe', 'CYCLE_CPU_ALARM')">CYCLE ALARM</div>
                    <div class="btn" onclick="sendPipe('PerformanceMonitorCommandsPipe', 'PROCESSOR_UTILITY')">PROC UTILITY</div>
                    <div class="btn" onclick="sendPipe('PerformanceMonitorCommandsPipe', 'PROCESSOR_TIME')">PROC TIME</div>
                    <div class="btn" onclick="sendPipe('PerformanceMonitorCommandsPipe', 'INTERRUPT_TIME')">INT TIME</div>
                    <div class="btn" onclick="sendPipe('PerformanceMonitorCommandsPipe', 'DPC_TIME')">DPC TIME</div>
                    <div class="btn" onclick="sendPipe('PerformanceMonitorCommandsPipe', 'GPU_UTILIZATION')">GPU UTIL</div>
                    <div class="btn" onclick="sendPipe('PerformanceMonitorCommandsPipe', 'GPU_TEMPERATURE')">GPU TEMP</div>
                    <div class="btn" onclick="sendPipe('PerformanceMonitorCommandsPipe', 'GPU_MEMORY_UTILIZATION')">GPU MEM UTIL</div>
                    <div class="btn" onclick="sendPipe('PerformanceMonitorCommandsPipe', 'MONITOR')">MONITOR</div>
                    <div class="btn" onclick="sendPipe('PerformanceMonitorCommandsPipe', 'ALERTS')">ALERTS</div>
                    <div class="btn" onclick="sendPipe('PerformanceMonitorCommandsPipe', 'SETTINGS')">SETTINGS</div>
                    <div class="btn" onclick="sendPipe('PerformanceMonitorCommandsPipe', 'ERRORS')">ERRORS</div>
                    <div class="btn" onclick="sendPipe('PerformanceMonitorCommandsPipe', 'RESTART_COUNTERS')">RST COUNTERS</div>
                    <div class="btn" onclick="sendPipe('PerformanceMonitorCommandsPipe', 'MINIMIZE')">MINIMIZE</div>
                    <div class="btn" onclick="sendPipe('PerformanceMonitorCommandsPipe', 'RESTORE')">RESTORE</div>
                    <div class="btn" onclick="sendPipe('PerformanceMonitorCommandsPipe', 'ALIGN_LEFT-1500')">ALIGN L-1500</div>
                    <div class="btn" onclick="sendPipe('PerformanceMonitorCommandsPipe', 'TOPMOST')">TOPMOST</div>
                    <div class="btn" onclick="sendPipe('PerformanceMonitorCommandsPipe', 'CLOSE')">CLOSE</div>
                    <div class="btn" onclick="sendPipe('PerformanceMonitorCommandsPipe', 'FOREGROUND')">FOREGROUND</div>
                </div>

                <!-- TELEMETRY PIPES -->
                <div id="pipe-telemetry" class="pipe-cat-grid grid-2" style="display:none;">
                    <div class="btn" onclick="sendPipe('TelemetryVibShakerPipeCommands', 'MONITOR')">MONITOR</div>
                    <div class="btn" onclick="sendPipe('TelemetryVibShakerPipeCommands', 'NOT_FOUNDS')">NOT FOUNDS</div>
                    <div class="btn" onclick="sendPipe('TelemetryVibShakerPipeCommands', 'SETTINGS')">SETTINGS</div>
                    <div class="btn" onclick="sendPipe('TelemetryVibShakerPipeCommands', 'STOP')">STOP</div>
                    <div class="btn" onclick="sendPipe('TelemetryVibShakerPipeCommands', 'START')">START</div>
                    <div class="btn" onclick="sendPipe('TelemetryVibShakerPipeCommands', 'CYCLE_STATISTICS')">CYCLE STATS</div>
                    <div class="btn" onclick="sendPipe('TelemetryVibShakerPipeCommands', 'MINIMIZE')">MINIMIZE</div>
                    <div class="btn" onclick="sendPipe('TelemetryVibShakerPipeCommands', 'RESTORE')">RESTORE</div>
                    <div class="btn" onclick="sendPipe('TelemetryVibShakerPipeCommands', 'ALIGN_LEFT-1500')">ALIGN L-1500</div>
                    <div class="btn" onclick="sendPipe('TelemetryVibShakerPipeCommands', 'TOPMOST')">TOPMOST</div>
                    <div class="btn" onclick="sendPipe('TelemetryVibShakerPipeCommands', 'CLOSE')">CLOSE</div>
                    <div class="btn" onclick="sendPipe('TelemetryVibShakerPipeCommands', 'FOREGROUND')">FOREGROUND</div>
                </div>

                <!-- WAR THUNDER PIPES -->
                <div id="pipe-warthunder" class="pipe-cat-grid grid-2" style="display:none;">
                    <div class="btn" onclick="sendPipe('WarThunderExporterPipeCommands', 'MONITOR')">MONITOR</div>
                    <div class="btn" onclick="sendPipe('WarThunderExporterPipeCommands', 'CYCLE_STATISTICS')">CYCLE STATS</div>
                    <div class="btn" onclick="sendPipe('WarThunderExporterPipeCommands', 'SETTINGS')">SETTINGS</div>
                    <div class="btn" onclick="sendPipe('WarThunderExporterPipeCommands', 'INTERVAL_100')">INTERVAL 100</div>
                    <div class="btn" onclick="sendPipe('WarThunderExporterPipeCommands', 'INTERVAL_50')">INTERVAL 50</div>
                    <div class="btn" onclick="sendPipe('WarThunderExporterPipeCommands', 'STOP')">STOP</div>
                    <div class="btn" onclick="sendPipe('WarThunderExporterPipeCommands', 'START')">START</div>
                    <div class="btn" onclick="sendPipe('WarThunderExporterPipeCommands', 'NOT_FOUNDS')">NOT FOUNDS</div>
                    <div class="btn" onclick="sendPipe('WarThunderExporterPipeCommands', 'MINIMIZE')">MINIMIZE</div>
                    <div class="btn" onclick="sendPipe('WarThunderExporterPipeCommands', 'RESTORE')">RESTORE</div>
                    <div class="btn" onclick="sendPipe('WarThunderExporterPipeCommands', 'ALIGN_LEFT-1500')">ALIGN L-1500</div>
                    <div class="btn" onclick="sendPipe('WarThunderExporterPipeCommands', 'TOPMOST')">TOPMOST</div>
                    <div class="btn" onclick="sendPipe('WarThunderExporterPipeCommands', 'CLOSE')">CLOSE</div>
                    <div class="btn" onclick="sendPipe('WarThunderExporterPipeCommands', 'FOREGROUND')">FOREGROUND</div>
                </div>

                <!-- SIM CONNECT PIPES -->
                <div id="pipe-simconnect" class="pipe-cat-grid grid-2" style="display:none;">
                    <div class="btn" onclick="sendPipe('SimConnectExporterPipeCommands', 'MONITOR')">MONITOR</div>
                    <div class="btn" onclick="sendPipe('SimConnectExporterPipeCommands', 'CYCLE_STATISTICS')">CYCLE STATS</div>
                    <div class="btn" onclick="sendPipe('SimConnectExporterPipeCommands', 'SHOW_GFORCES')">SHOW GFORCES</div>
                    <div class="btn" onclick="sendPipe('SimConnectExporterPipeCommands', 'SETTINGS')">SETTINGS</div>
                    <div class="btn" onclick="sendPipe('SimConnectExporterPipeCommands', 'STOP')">STOP</div>
                    <div class="btn" onclick="sendPipe('SimConnectExporterPipeCommands', 'START')">START</div>
                    <div class="btn" onclick="sendPipe('SimConnectExporterPipeCommands', 'MINIMIZE')">MINIMIZE</div>
                    <div class="btn" onclick="sendPipe('SimConnectExporterPipeCommands', 'RESTORE')">RESTORE</div>
                    <div class="btn" onclick="sendPipe('SimConnectExporterPipeCommands', 'ALIGN_LEFT-1500')">ALIGN L-1500</div>
                    <div class="btn" onclick="sendPipe('SimConnectExporterPipeCommands', 'TOPMOST')">TOPMOST</div>
                    <div class="btn" onclick="sendPipe('SimConnectExporterPipeCommands', 'CLOSE')">CLOSE</div>
                    <div class="btn" onclick="sendPipe('SimConnectExporterPipeCommands', 'FOREGROUND')">FOREGROUND</div>
                </div>

                <!-- POWERSHELL PIPES -->
                <div id="pipe-ps" class="pipe-cat-grid grid-1" style="display:none;">
                    <div class="btn" onclick="sendPipe('ipc_pipe_vr_server_commands', 'SHOW_PROCESS')">SHOW PROCESSES TIMES</div>
                </div>
            </div>
        </div>
    </div>

    <!-- STATUS TAPE (Console) -->
    <div class="status-tape" id="console">
        <div class="tape-line">> UPLINK ESTABLISHED.</div>
        <div class="tape-line">> WAITING FOR COMMAND...<span class="cursor"></span></div>
    </div>

    <script>
        let currentApp = "<?= $firstApp ?>"; 
        document.getElementById('global-target-display').innerText = `TGT LOCK: ${currentApp}`;

        function switchTab(tabId) {
            document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
            document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
            event.target.classList.add('active');
            document.getElementById('tab-' + tabId).classList.add('active');
        }

        function showPipeCat(catId, element) {
            document.querySelectorAll('.pipe-cat-grid').forEach(g => g.style.display = 'none');
            document.getElementById(catId).style.display = 'grid';
            
            // Highlight active toggle
            let parent = element.parentElement;
            parent.querySelectorAll('.btn').forEach(b => b.classList.remove('selected'));
            element.classList.add('selected');
        }

        function selectApp(element, appName) {
            document.querySelectorAll('#app-grid .btn').forEach(b => b.classList.remove('selected'));
            element.classList.add('selected');
            currentApp = appName;
            document.getElementById('global-target-display').innerText = `TGT LOCK: ${currentApp}`;
            logConsole(`TARGET SWITCHED TO: ${currentApp}`);
        }

        function stepVal(inputId, amount) {
            let input = document.getElementById(inputId);
            let val = parseInt(input.value) + amount;
            if (val < 1) val = 1; if (val > 999) val = 999;
            input.value = val;
        }

        function logConsole(msg, isWarning = false) {
            let consoleDiv = document.getElementById('console');
            document.querySelectorAll('.cursor').forEach(c => c.remove());
            
            let newLine = document.createElement('div');
            newLine.className = 'tape-line new';
            let colorStyle = isWarning ? 'color: var(--red); text-shadow: 0 0 5px red;' : '';
            newLine.innerHTML = `> <span style="${colorStyle}">${msg}</span><span class="cursor"></span>`;
            
            consoleDiv.appendChild(newLine);
            consoleDiv.scrollTop = consoleDiv.scrollHeight;
            if (consoleDiv.children.length > 4) consoleDiv.removeChild(consoleDiv.firstChild);
        }

        // =========================================================================
        // AJAX POST LOGIC - Maps exactly to old HTML Form schema
        // =========================================================================
        function triggerAjax(formDataPayload, logMsg) {
            logConsole(`TX: ${logMsg}`, logMsg.includes('TERMINATE'));
            
            // Build absolute baseline parameters
            let formData = new URLSearchParams();
            formData.append('ajax', '1');
            formData.append('Process', currentApp);
            formData.append('Instance', document.getElementById('inst-val').value);
            formData.append('X', document.getElementById('coord-x').value);
            formData.append('Y', document.getElementById('coord-y').value);

            // Append specific action arguments
            for (let [key, value] of Object.entries(formDataPayload)) {
                formData.append(key, value);
            }

            fetch('', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: formData.toString()
            })
            .then(res => res.json())
            .then(data => {
                // Display the raw PHP response footer
                logConsole(`RX: [${data.timestamp}] ${data.footer}`);
                // Update Coordinates automatically (crucial for GetValues)
                if (data.x) document.getElementById('coord-x').value = data.x;
                if (data.y) document.getElementById('coord-y').value = data.y;
            })
            .catch(err => {
                logConsole('SYS ERR: UPLINK FAILED', true);
            });
        }

        // Action wrappers tailored to original PHP `$_POST` hooks
        function sendAction(postKey, postVal) {
            let payload = {};
            payload[postKey] = postVal;
            triggerAjax(payload, `${postKey.toUpperCase()}`);
        }

        function sendMove(state) {
            triggerAjax({ 'Move': 'Apply Changes', 'State': state }, `MOVE [STATE:${state}]`);
        }

        function sendSpecial(command) {
            triggerAjax({ 'Command': 'Command', 'SpecialCommand': command }, `SYS_CMD [${command}]`);
        }

        function sendPipe(pipeline, command) {
            triggerAjax({ 'Command': 'Command', 'PipeCommand': `${pipeline}|${command}` }, `PIPE [${command}]`);
        }
    </script>
</body>
</html>