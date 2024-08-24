<?php
const COMMAND_FILE = 'command.json';
const TEMP_FILE = 'command.tmp';
const WATCHDOG_FILE = 'watchdog.txt';
const PROGRAMS_JSON = 'programs.json';

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

// Location coordinates
$frmX = "-1500";
$frmY = "";

$frm_instance = 0;
$frm_process = "";
$frm_pipe_command = isset($_POST['PipeCommand'])? $_POST['PipeCommand']: "";

$name = $gender = $color = "";
$subscribe = false;

// Check if the form is submitted
if ($_SERVER["REQUEST_METHOD"] == "POST") {
	$frmX = htmlspecialchars($_POST['X']);
	$frmY = htmlspecialchars($_POST['Y']);
	$frm_instance = htmlspecialchars($_POST['Instance']);
	$frm_process = htmlspecialchars($_POST['Process']);
	
	
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
  
			case "MSFS1":
			  $footer = "Boost 1 for MSFS Priority and Reassign Ideal Processors (not Affinity)";
			  $jsonData = createJsonCommand("GAME", array(
				"processName" => "FlightSimulator",
				"jsonFile" => "C:\\MyPrograms\\My Apps\\TelemetryVibShaker\\WebScripts\\ps_scripts\\action-per-process-boost1.json"
			  ));
			  writeJsonToFile($jsonData, TEMP_FILE);
			  renameFile(TEMP_FILE, COMMAND_FILE);
			  break;
				  

			case "MSFS2":
			  $footer = "Boost 2 for MSFS Affinity, Priority and Reassign Ideal Processors";
			  $jsonData = createJsonCommand("GAME", array(
				"processName" => "FlightSimulator",
				"jsonFile" => "C:\\MyPrograms\\My Apps\\TelemetryVibShaker\\WebScripts\\ps_scripts\\action-per-process-boost2.json"
			  ));
			  writeJsonToFile($jsonData, TEMP_FILE);
			  renameFile(TEMP_FILE, COMMAND_FILE);
			  break;
			  
			case "ACES":
			  $footer = "All Boost for War Thunder (Affinity, Priority and Reassign Ideal Processors)";
			  $jsonData = createJsonCommand("GAME", array(
				"processName" => "aces",
				"jsonFile" => "C:\\MyPrograms\\My Apps\\TelemetryVibShaker\\WebScripts\\ps_scripts\\action-per-process-boost1.json"
			  ));
			  writeJsonToFile($jsonData, TEMP_FILE);
			  renameFile(TEMP_FILE, COMMAND_FILE);
			  break;			  

			case "DCS":
			  $footer = "Boost for DCS World[Priority]: OVR_Server[Priority, Affinity, Ideal Processors] and Joystick_Gremlin[Priority, Affinity, Ideal Processors]";
			  $jsonData = createJsonCommand("GAME", array(
				"processName" => "dcs",
				"jsonFile" => "C:\\MyPrograms\\My Apps\\TelemetryVibShaker\\WebScripts\\ps_scripts\\action-per-process-boost1.json"
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
	if ($post_command == "Launch") { // Check if the comand requested was to Launch a program
		$jsonFilePath = 'programs.json';
		$processName = $_POST['Process'];
		$path = getPathByProcessName($jsonFilePath, $processName);

		if ($path !== null) {
			$footer = "Launch completed - '$processName': $path\n";
		} else {
			$footer = "Launch process '$processName' not found.\n";
		}
		
		// My Powershell FileSystem Event watcher is configured to listen to rename events
		// This way I avoid I/O bouncing/flapping, etc, just one event generated
		$jsonData = createJsonCommand("RUN", array("program" => $path, "parameter1" => "not-used"));
		writeJsonToFile($jsonData, TEMP_FILE);		
		renameFile(TEMP_FILE, COMMAND_FILE);				
		$time_stamp = getTimestamp();
	} // end-if Launch
	

	// CHECK FOR TERMINATE COMMAND
	$post_command = isset($_POST['Exit'])? $_POST['Exit']: "";
	if ($post_command == "Terminate") { // Check if the comand requested was to Launch a program
		$processName = $_POST['Process'];
		
		$footer = "Terminate completed - [$processName]";
				
		// My Powershell FileSystem Event watcher is configured to listen to rename events
		// This way I avoid I/O bouncing/flapping, etc, just one event generated
		$jsonData = createJsonCommand("KILL", array("processName" => $processName));
		writeJsonToFile($jsonData, TEMP_FILE);
		renameFile(TEMP_FILE, COMMAND_FILE);
		$time_stamp = getTimestamp();
	} // end-if 
	
	// CHECK FOR FOREGROUND COMMAND
	$post_command = isset($_POST['MakeForeground'])? $_POST['MakeForeground']: "";
	if ($post_command == "Make Foreground") { // Check if the comand requested was to Launch a program
		$processName = $_POST['Process'];
		
		$footer = "Foreground completed - [$processName]";
				
		// My Powershell FileSystem Event watcher is configured to listen to rename events
		// This way I avoid I/O bouncing/flapping, etc, just one event generated		
		$jsonData = createJsonCommand("FOREGROUND", array("processName" => $processName, "instance" => $frm_instance));
		writeJsonToFile($jsonData, TEMP_FILE);
		renameFile(TEMP_FILE, COMMAND_FILE);
  
		$time_stamp = getTimestamp();
	} // end-if 


	// CHECK FOR GET-VALUES COMMAND
	$post_command = isset($_POST['GetValues'])? $_POST['GetValues']: "";
	if ($post_command == "GetValues") { // Check if the comand requested was to Launch a program
		$processName = $_POST['Process'];
		
		$footer = "GetValues completed - [$processName]";
				
		// My Powershell FileSystem Event watcher is configured to listen to rename events
		// This way I avoid I/O bouncing/flapping, etc, just one event generated
		$outfile = "C:\\MyPrograms\\wamp\\www\\remote_control\\outfile.txt";
		if (file_exists(basename($outfile))) { unlink(basename($outfile)); }
		
		$jsonData = createJsonCommand("GET-LOCATION", array("processName" => $processName, "instance" => $frm_instance, "outfile" => $outfile));
		writeJsonToFile($jsonData, TEMP_FILE);
		renameFile(TEMP_FILE, COMMAND_FILE);		
		$time_stamp = getTimestamp();
		
		$file=tryOpenFile(basename($outfile), 50, 100); // wait up to 5 seconds
		if ($file) {			
			$frmX = fgets($file); // Read the first line					
			$frmY = fgets($file); // Read the second line			
			fclose($file);			
		} else {
			$footer = "<span style='color: red;'>Error opening the file.</span>";
			$frmX = "?";
			$frmY = "?";
		}		
		
	} // end-if 


	// CHECK FOR MOVE,MINIMIZE,ETC COMMAND
	$post_command = isset($_POST['Move'])? $_POST['Move']: "";
	if ($post_command == "Apply Changes") { // Check if the comand requested was to Launch a program
		$processName = $_POST['Process'];
				
		$state = isset($_POST['State'])? $_POST['State']: "0";
		switch ($state) {
				case "0":  // Change X,Y coordinates
					$footer = "Move completed - [$processName]";
							
					// My Powershell FileSystem Event watcher is configured to listen to rename events
					// This way I avoid I/O bouncing/flapping, etc, just one event generated
					
					$jsonData = createJsonCommand("CHANGE_LOCATION", array("processName" => $processName, "instance" => $frm_instance, "x" => $frmX, "y" => $frmY));
					writeJsonToFile($jsonData, TEMP_FILE);
					renameFile(TEMP_FILE, COMMAND_FILE);
	  
					$time_stamp = getTimestamp();
					break;

				case "-1":	// Minimize
					$footer = "Minimize completed - [$processName]";
							
					// My Powershell FileSystem Event watcher is configured to listen to rename events
					// This way I avoid I/O bouncing/flapping, etc, just one event generated
					
					$jsonData = createJsonCommand("MINIMIZE", array("processName" => $processName, "instance" => $frm_instance));
					writeJsonToFile($jsonData, TEMP_FILE);
					renameFile(TEMP_FILE, COMMAND_FILE);					
					$time_stamp = getTimestamp();
					break;
			
				case "1":	// Restore
					$footer = "Restore completed - [$processName]";
							
					// My Powershell FileSystem Event watcher is configured to listen to rename events
					// This way I avoid I/O bouncing/flapping, etc, just one event generated
					$jsonData = createJsonCommand("RESTORE", array("processName" => $processName, "instance" => $frm_instance));
					writeJsonToFile($jsonData, TEMP_FILE);
					renameFile(TEMP_FILE, COMMAND_FILE);					
										
					$time_stamp = getTimestamp();
					break;
					
				case "2":	// Maximize
					$footer = "Maximize completed - [$processName]";
							
					// My Powershell FileSystem Event watcher is configured to listen to rename events
					// This way I avoid I/O bouncing/flapping, etc, just one event generated
					
					$jsonData = createJsonCommand("MAXIMIZE", array("processName" => $processName, "instance" => $frm_instance));
					writeJsonToFile($jsonData, TEMP_FILE);
					renameFile(TEMP_FILE, COMMAND_FILE);
					
					$time_stamp = getTimestamp();
					break;					
		}		
	} // end-if 


} //end-if REQUEST_METHOD == POST


?>
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Window Remote Control Reposition</title>
<style>
  body { font-family: Arial, sans-serif; background-color: #f7f7f7; margin: 0; padding: 10px; }
  .container { max-width: 600px; margin: auto; background: #fff; padding: 10px; box-shadow: 0 3px 6px rgba(0,0,0,0.1); }
  h2 { color: #333;  margin-bottom: 10px; margin-top: 5px;}
  .welcome-message { margin-bottom: 10px; }
  select, input[type="text"] { width: 100%; padding: 10px; margin-bottom: 20px; border: 1px solid #ddd; border-radius: 4px; box-sizing: border-box; }
  .submit-btn { background-color: #5cb85c; color: white; padding: 10px 20px; border: none; border-radius: 4px; cursor: pointer; }
  .submit-btn:hover { background-color: #4cae4c; }
  .footer-message {
    text-align: left;
    margin-top: 20px;
    padding: 10px;
    background-color: #f2f2f2;
    border-radius: 4px;
    color: #333;
  }  
</style>

<script>
<? if ($frm_process != "") { ?>
	function selectOptionByIdAndText(selectId, text) {
		// Get the select element by its ID
		var selectElement = document.getElementById(selectId);
		
		// Check if the select element exists
		if (selectElement) {
			// Loop through all options in the select element
			for (var i = 0; i < selectElement.options.length; i++) {
				// Check if the option's text matches the provided text
				if (selectElement.options[i].value === text) {
					// Set the selected index to the matching option
					selectElement.selectedIndex = i;
					return true; // Option found and selected
				}
			}
		}
		return false; // Option not found
	}

	// update the previously selected values if any.  server generated.
	window.onload = function() {
		var program = '<?= $frm_process ?>';
		var pipe = '<?= $frm_pipe_command ?>';
		
		if (program != '') selectOptionByIdAndText('Process', program);
		selectOptionByIdAndText('PipeCommand', pipe);		
	}

	
<? } ?>	

	function changeFontColor(elementId, newColor) {
		// Get the element by its ID
		var element = document.getElementById(elementId);
		
		// Check if the element exists
		if (element) {
			// Change the font color of the element
			element.style.color = newColor;
		} else {
			console.log("Element with ID " + elementId + " not found.");
		}
	}



</script>
</head>
<body>

<div id="allcontainer" class="container">
	<form id="RemoteControlForm" name="RemoteControlForm" method="POST" align="center">
		<a href="#" onclick="window.location.href = window.location.href.split('?')[0];"><h2>Window Remote Movement</h2></a>
		<p class="welcome-message">Use this app to <a href="#lblMove">move</a> windows or send <a href="#lblCommand">commands</a> when playing in VR</p>

		<!-- Combo box for options -->
		<label for="Process">Select Process:</label>
		<select id='Process' name='Process' required>
			<option  value='PerformanceMonitor'>PerformanceMonitor</option>
			<option  value='WarThunderExporter'>WarThunderExporter</option>
			<option  value='TelemetryVibShaker'>TelemetryVibShaker</option>
			<option  value='FalconExporter'>FalconExporter</option>
			<option  value='RemoteWindowControl'>RemoteWindowControl</option>
			<option  value='SimConnectExporter'>SimConnectExporter</option>
			<option  value='dcs'>DCS World</option>
			<option  value='FlightSimulator'>Microsoft Flight Simulator</option>
			<option  value='aces'>War Thunder</option>
			<option  value='falcon'>Falcon BMS</option>
			<option  value='Time'>Clock</option>			
			<option  value='HWiNFO64'>HWiNFO64</option>
			<option  value='msedge'>Edge Web Browser</option>			
			<option  value='CalculatorApp'>Calc</option>
			<option  value='Notepad'>Notepad</option>			
		</select>


		<br><label for="Instance">Select Instance:</label>
		<input type='text' name='Instance' required value='<?php echo $frm_instance; ?>' placeholder='type process instance #'>

		<br><label for="State">Select State:</label>
		<select name="State">
			<option selected="selected" value="0">NoChange</option>
			<option value="-1">Minimize</option>
			<option value="1">Restore</option>
			<option value="2">Maximize</option>
		</select>


		<!-- Text boxes for user input -->
		<br><label id="lblMove" for="X">New X:</label><input type='text' value="<?php echo $frmX; ?>" name="X" placeholder='type new X coordinates'>
		<br><label for="Y">New Y:</label><input type='text' value="<?php echo $frmY; ?>" name="Y" placeholder='type new Y coordinates'>
		<!-- Add more text boxes here -->
		<!-- Submit buttons -->
		<input type="submit" class="submit-btn" name="Move" value="Apply Changes">
		<input type="submit" class="submit-btn" name="MakeForeground" value="Make Foreground"><br><br>
		<input type="submit" class="submit-btn" name="GetValues" value="GetValues">
		<input type="submit" class="submit-btn" name="Exit" value="Terminate">
		<input type="submit" class="submit-btn" name="Launch" value="Launch">
		
		<p><label id="lblSpecial" for="SpecialCommand">Select Special Command:</label>
		<select id="SpecialCommand" name="SpecialCommand">
			<option selected value="0">[SELECT SPECIAL COMMAND]</option>		
			<option value="WATCHDOG">WATCHDOG: Watcher for JSON Gaming Commands</option>
			<option value="HIGHPERFORMANCE">POWER SCHEME: HIGH POWER</option>
			<option value="BALANCED">POWER SCHEME: BALANCED</option>
			<option value="BALANCED80">POWER SCHEME: BALANCED MAX 80</option>
			<option value="READPOWERSCHEME">READ CURRENT POWER PLAN</option>
			<option value="MSFS1">BOOST LEVEL 1 FOR MSFS</option>
			<option value="MSFS2">BOOST LEVEL 2 FOR MSFS</option>
			<option value="ACES">BOOST FOR WAR THUNDER</option>			
			<option value="DCS">BOOST FOR DCS WORLD</option>			
		</select>
		
		<br><label id="lblCommand" for="PipeCommand">Select Pipe Command:</label>
		<select id="PipeCommand" name="PipeCommand">
			<option selected value="0">[SELECT PIPE COMMAND]</option>
			<option value="0">[PERFORMANCE MONITOR]</option>
			
			<option value="PerformanceMonitorCommandsPipe|CYCLE_CPU_ALARM">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;CYCLE_CPU_ALARM</option>
			<option value="PerformanceMonitorCommandsPipe|PROCESSOR_UTILITY">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;PROCESSOR_UTILITY</option>
			<option value="PerformanceMonitorCommandsPipe|PROCESSOR_TIME">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;PROCESSOR_TIME</option>
			<option value="PerformanceMonitorCommandsPipe|INTERRUPT_TIME">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;INTERRUPT_TIME</option>
			<option value="PerformanceMonitorCommandsPipe|DPC_TIME">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;DPC_TIME</option>
			<option value="PerformanceMonitorCommandsPipe|GPU_UTILIZATION">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;GPU_UTILIZATION</option>
			<option value="PerformanceMonitorCommandsPipe|GPU_TEMPERATURE">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;GPU_TEMPERATURE</option>
			<option value="PerformanceMonitorCommandsPipe|GPU_MEMORY_UTILIZATION">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;GPU_MEMORY_UTILIZATION</option>
			<option value="PerformanceMonitorCommandsPipe|MONITOR">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;MONITOR</option>
			<option value="PerformanceMonitorCommandsPipe|ALERTS">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ALERTS</option>
			<option value="PerformanceMonitorCommandsPipe|SETTINGS">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;SETTINGS</option>
			<option value="PerformanceMonitorCommandsPipe|ERRORS">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ERRORS</option>
			<option value="PerformanceMonitorCommandsPipe|RESTART_COUNTERS">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;RESTART_COUNTERS</option>
			<option value="PerformanceMonitorCommandsPipe|MINIMIZE">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;MINIMIZE</option>
			<option value="PerformanceMonitorCommandsPipe|RESTORE">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;RESTORE</option>
			<option value="PerformanceMonitorCommandsPipe|ALIGN_LEFT-1500">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ALIGN_LEFT</option>
			<option value="PerformanceMonitorCommandsPipe|TOPMOST">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;TOPMOST</option>
			<option value="PerformanceMonitorCommandsPipe|CLOSE">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;CLOSE</option>
			<option value="PerformanceMonitorCommandsPipe|FOREGROUND">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;FOREGROUND</option>

			<option value="0">[TELEMETRY VIB SHAKER]</option>
			<option value="TelemetryVibShakerPipeCommands|MONITOR">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;MONITOR</option>
			<option value="TelemetryVibShakerPipeCommands|NOT_FOUNDS">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;NOT_FOUNDS</option>
			<option value="TelemetryVibShakerPipeCommands|SETTINGS">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;SETTINGS</option>
			<option value="TelemetryVibShakerPipeCommands|STOP">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;STOP</option>
			<option value="TelemetryVibShakerPipeCommands|START">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;START</option>
			<option value="TelemetryVibShakerPipeCommands|CYCLE_STATISTICS">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;CYCLE_STATISTICS</option>
			<option value="TelemetryVibShakerPipeCommands|MINIMIZE">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;MINIMIZE</option>
			<option value="TelemetryVibShakerPipeCommands|RESTORE">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;RESTORE</option>
			<option value="TelemetryVibShakerPipeCommands|ALIGN_LEFT-1500">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ALIGN_LEFT</option>
			<option value="TelemetryVibShakerPipeCommands|TOPMOST">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;TOPMOST</option>
			<option value="TelemetryVibShakerPipeCommands|CLOSE">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;CLOSE</option>
			<option value="TelemetryVibShakerPipeCommands|FOREGROUND">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;FOREGROUND</option>

			<option value="0">[WAR THUNDER EXPORTER]</option>
			<option value="WarThunderExporterPipeCommands|MONITOR">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;MONITOR</option>
			<option value="WarThunderExporterPipeCommands|CYCLE_STATISTICS">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;CYCLE_STATISTICS</option>
			<option value="WarThunderExporterPipeCommands|SETTINGS">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;SETTINGS</option>
			<option value="WarThunderExporterPipeCommands|INTERVAL_100">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;INTERVAL_100</option>
			<option value="WarThunderExporterPipeCommands|INTERVAL_50">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;INTERVAL_50</option>
			<option value="WarThunderExporterPipeCommands|STOP">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;STOP</option>
			<option value="WarThunderExporterPipeCommands|START">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;START</option>
			<option value="WarThunderExporterPipeCommands|NOT_FOUNDS">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;NOT_FOUNDS</option>
			<option value="WarThunderExporterPipeCommands|MINIMIZE">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;MINIMIZE</option>
			<option value="WarThunderExporterPipeCommands|RESTORE">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;RESTORE</option>
			<option value="WarThunderExporterPipeCommands|ALIGN_LEFT-1500">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ALIGN_LEFT</option>
			<option value="WarThunderExporterPipeCommands|TOPMOST">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;TOPMOST</option>
			<option value="WarThunderExporterPipeCommands|CLOSE">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;CLOSE</option>
			<option value="WarThunderExporterPipeCommands|FOREGROUND">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;FOREGROUND</option>

			<option value="0">[SIM CONNECT EXPORTER]</option>
			<option value="SimConnectExporterPipeCommands|MONITOR">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;MONITOR</option>
			<option value="SimConnectExporterPipeCommands|CYCLE_STATISTICS">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;CYCLE_STATISTICS</option>
			<option value="SimConnectExporterPipeCommands|SHOW_GFORCES">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;SHOW_GFORCES</option>
			<option value="SimConnectExporterPipeCommands|SETTINGS">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;SETTINGS</option>
			<option value="SimConnectExporterPipeCommands|STOP">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;STOP</option>
			<option value="SimConnectExporterPipeCommands|START">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;START</option>
			<option value="SimConnectExporterPipeCommands|MINIMIZE">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;MINIMIZE</option>
			<option value="SimConnectExporterPipeCommands|RESTORE">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;RESTORE</option>
			<option value="SimConnectExporterPipeCommands|ALIGN_LEFT-1500">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ALIGN_LEFT</option>
			<option value="SimConnectExporterPipeCommands|TOPMOST">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;TOPMOST</option>
			<option value="SimConnectExporterPipeCommands|CLOSE">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;CLOSE</option>
			<option value="SimConnectExporterPipeCommands|FOREGROUND">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;FOREGROUND</option>
			
			<option value="0">[POWERSHELL IPC PIPE THREAD]</option>
			<option value="ipc_pipe_vr_server_commands|SHOW_PROCESS">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;SHOW PROCESSES TIMES</option>
			
		</select>

		
		<input type="submit" class="submit-btn" name="Command" value="Command">


		<!-- Footer message -->
		<div id="result" class="footer-message">[<?php echo $time_stamp; ?>] <?php echo $footer; ?></div>
		<p><div id="phpversion" class="footer-message"><?php echo 'PHP version: ' . phpversion();?></div>
	</form>
</div>

<script>
// The following two functions will let the user know 
// that processing has started in case Powershell takes too long
// If you see issues posting, remove them for testing

		
        function handleFormSubmit(event, divId, newText, newColor) {
            const divElement = document.getElementById(divId);
			divElement.style.color = newColor;
            divElement.innerText = newText;
        }

        const forms = document.querySelectorAll('form');
        forms.forEach(form => {
            form.addEventListener('submit', (event) => {
                handleFormSubmit(event, 'result', 'Processing', 'blue');
            });
        });		
</script>

</body>
</html>
