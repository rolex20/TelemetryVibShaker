<?php
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


function createJsonPIPE($filePath, $pipename, $message) {
	
    // Define the array structure using the older array syntax
    $data = array(
        "command_type" => "PIPE",
        "parameters" => array(
            "pipename" => $pipename,
			"message" => $message
        )
    );

    // Encode the array to JSON format
    $json_data = json_encode($data);

    // Write the JSON data to the specified file
	return file_put_contents($filePath, $json_data);
}

function createJsonWindowState($filePath, $program, $instance, $new_state) {
    // Define the array structure using the older array syntax
    $data = array(
        "command_type" => $new_state,
        "parameters" => array(
            "processName" => $program,
			"instance" => $instance
        )
    );

    // Encode the array to JSON format
    $json_data = json_encode($data);

    // Write the JSON data to the specified file
	return file_put_contents($filePath, $json_data);
}


function createJsonMOVE($filePath, $program, $instance, $x, $y) {
    // Define the array structure using the older array syntax
    $data = array(
        "command_type" => "CHANGE_LOCATION",
        "parameters" => array(
            "processName" => $program,
			"instance" => $instance,
			"x" => $x,
			"y" => $y
        )
    );

    // Encode the array to JSON format
    $json_data = json_encode($data);

    // Write the JSON data to the specified file
	return file_put_contents($filePath, $json_data);
}


function createJsonGETVALUES($filePath, $program, $instance, $outfile) {
    // Define the array structure using the older array syntax
    $data = array(
        "command_type" => "GET-LOCATION",
        "parameters" => array(
            "processName" => $program,
			"instance" => $instance,
			"outfile" => $outfile
        )
    );

    // Encode the array to JSON format
    $json_data = json_encode($data);

    // Write the JSON data to the specified file
	return file_put_contents($filePath, $json_data);
}


function createJsonFOREGROUND($filePath, $program, $instance) {
    // Define the array structure using the older array syntax
    $data = array(
        "command_type" => "FOREGROUND",
        "parameters" => array(
            "processName" => $program,
			"instance" => $instance
        )
    );

    // Encode the array to JSON format
    $json_data = json_encode($data);

    // Write the JSON data to the specified file
	return file_put_contents($filePath, $json_data);
}


function createJsonKILL($filePath, $program) {
    // Define the array structure using the older array syntax
    $data = array(
        "command_type" => "KILL",
        "parameters" => array(
            "processName" => $program
        )
    );

    // Encode the array to JSON format
    $json_data = json_encode($data);

    // Write the JSON data to the specified file
	return file_put_contents($filePath, $json_data);
}


function createJsonRUN($filePath, $program, $parameter1) {
    // Define the array structure using the older array syntax
    $data = array(
        "command_type" => "RUN",
        "parameters" => array(
            "program" => $program,
            "parameter1" => $parameter1
        )
    );

    // Encode the array to JSON format
    $json_data = json_encode($data);

    // Write the JSON data to the specified file
	return file_put_contents($filePath, $json_data);
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
$frmX = "-1700";
$frmY = "";

$frm_instance = 0;
$frm_process = "";

$name = $gender = $color = "";
$subscribe = false;

// Check if the form is submitted
if ($_SERVER["REQUEST_METHOD"] == "POST") {
	$frmX = htmlspecialchars($_POST['X']);
	$frmY = htmlspecialchars($_POST['Y']);
	$frm_instance = htmlspecialchars($_POST['Instance']);
	$frm_process = htmlspecialchars($_POST['Process']);
	
	
	// CHECK FOR PIPE COMMAND
	$post_command = isset($_POST['Command'])? $_POST['Command']: "";
	if ($post_command == "Command") { // Check if the comand requested was to Launch a program
		$pipe_tuple = $_POST['PipeCommand'];	
		$pipe_items = explode('|', $pipe_tuple);

		// Assign each value to a variable
		$pipename = $pipe_items[0];
		$message = $pipe_items[1];
		
		$footer = "Pipe [$pipename] - [$message]";
				
		# My Powershell FileSystem Event watcher is configured to listen to rename events
		# This way I avoid I/O bouncing/flapping, etc, just one event generated
		createJsonPIPE("command.tmp", $pipename, $message);
		renameFile("command.tmp", "command.json");
		$time_stamp = getTimestamp();
	} // end-if 
	
		
	// CHECK FOR LAUNCH COMMAND
	$post_command = isset($_POST['Launch'])? $_POST['Launch']: "";
	if ($post_command == "Launch") { // Check if the comand requested was to Launch a program
		$jsonFilePath = 'programs.json';
		$processName = $_POST['Process'];
		$path = getPathByProcessName($jsonFilePath, $processName);

		if ($path !== null) {
			$footer = "Launch requested - '$processName': $path\n";
		} else {
			$footer = "Launch process '$processName' not found.\n";
		}
		
		# My Powershell FileSystem Event watcher is configured to listen to rename events
		# This way I avoid I/O bouncing/flapping, etc, just one event generated
		createJsonRUN("command.tmp", $path, "not-used");
		renameFile("command.tmp", "command.json");				
		$time_stamp = getTimestamp();
	} // end-if Launch
	

	// CHECK FOR TERMINATE COMMAND
	$post_command = isset($_POST['Exit'])? $_POST['Exit']: "";
	if ($post_command == "Terminate") { // Check if the comand requested was to Launch a program
		$processName = $_POST['Process'];
		
		$footer = "Terminate requested - [$processName]";
				
		# My Powershell FileSystem Event watcher is configured to listen to rename events
		# This way I avoid I/O bouncing/flapping, etc, just one event generated
		createJsonKILL("command.tmp", $processName);
		renameFile("command.tmp", "command.json");
		$time_stamp = getTimestamp();
	} // end-if 
	
	// CHECK FOR FOREGROUND COMMAND
	$post_command = isset($_POST['MakeForeground'])? $_POST['MakeForeground']: "";
	if ($post_command == "Make Foreground") { // Check if the comand requested was to Launch a program
		$processName = $_POST['Process'];
		
		$footer = "Foreground requested - [$processName]";
				
		# My Powershell FileSystem Event watcher is configured to listen to rename events
		# This way I avoid I/O bouncing/flapping, etc, just one event generated
		createJsonFOREGROUND("command.tmp", $processName, $frm_instance);
		renameFile("command.tmp", "command.json");
		$time_stamp = getTimestamp();
	} // end-if 


	// CHECK FOR GET-VALUES COMMAND
	$post_command = isset($_POST['GetValues'])? $_POST['GetValues']: "";
	if ($post_command == "GetValues") { // Check if the comand requested was to Launch a program
		$processName = $_POST['Process'];
		
		$footer = "GetValues requested - [$processName]";
				
		# My Powershell FileSystem Event watcher is configured to listen to rename events
		# This way I avoid I/O bouncing/flapping, etc, just one event generated
		$outfile = "C:\\wamp\\www\\remote_control\\outfile.txt";
		if (file_exists($outfile)) { unlink($outfile); }
		
		createJsonGETVALUES("command.tmp", $processName, $frm_instance, $outfile);
		renameFile("command.tmp", "command.json");
		$time_stamp = getTimestamp();
		
		usleep(200000); // allow time for the file to be created
		$file = fopen(basename($outfile), "r");		
		if ($file) {			
			$frmX = fgets($file); // Read the first line					
			$frmY = fgets($file); // Read the second line			
			fclose($file);			
		} else {
			$footer = "Error opening the file.";
		}		
		
	} // end-if 


	// CHECK FOR MOVE,MINIMIZE,ETC COMMAND
	$post_command = isset($_POST['Move'])? $_POST['Move']: "";
	if ($post_command == "Apply Changes") { // Check if the comand requested was to Launch a program
		$processName = $_POST['Process'];
				
		$state = isset($_POST['State'])? $_POST['State']: "0";
		switch ($state) {
				case "0":  // Change X,Y coordinates
					$footer = "Move requested - [$processName]";
							
					# My Powershell FileSystem Event watcher is configured to listen to rename events
					# This way I avoid I/O bouncing/flapping, etc, just one event generated
					createJsonMOVE("command.tmp", $processName, $frm_instance, $frmX, $frmY);
					renameFile("command.tmp", "command.json");
					$time_stamp = getTimestamp();
					break;

				case "-1":	// Minimize
					$footer = "Minimize requested - [$processName]";
							
					# My Powershell FileSystem Event watcher is configured to listen to rename events
					# This way I avoid I/O bouncing/flapping, etc, just one event generated
					createJsonWindowState("command.tmp", $processName, $frm_instance, "MINIMIZE");
					renameFile("command.tmp", "command.json");
					$time_stamp = getTimestamp();
					break;
			
				case "1":	// Restore
					$footer = "Restore requested - [$processName]";
							
					# My Powershell FileSystem Event watcher is configured to listen to rename events
					# This way I avoid I/O bouncing/flapping, etc, just one event generated
					createJsonWindowState("command.tmp", $processName, $frm_instance, "RESTORE");
					renameFile("command.tmp", "command.json");
					$time_stamp = getTimestamp();
					break;
					
				case "2":	// Maximize
					$footer = "Restore requested - [$processName]";
							
					# My Powershell FileSystem Event watcher is configured to listen to rename events
					# This way I avoid I/O bouncing/flapping, etc, just one event generated
					createJsonWindowState("command.tmp", $processName, $frm_instance, "MAXIMIZE");
					renameFile("command.tmp", "command.json");
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


        function testSelection() {
            var selectId = 'mySelectBox';
            var text = document.getElementById('optionText').value;
            var result = selectOptionByIdAndText(selectId, text);
            //alert(result ? 'Option selected successfully!' : 'Option not found.');
        }


	window.onload = function() {
<? if ($frm_process != "") { ?>
		selectOptionByIdAndText('Process', '<? echo $frm_process ?>');
<? } ?>
	};
</script>
</head>
<body>

<div class="container">
	<form name="RemoteControlForm" method="POST" align="center">
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
		
		
		
		<p><br><label id="lblCommand" for="PipeCommand">Select Command:</label>
		<select name="PipeCommand">
			<option selected="selected" value="0">[SELECT COMMAND]</option>
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
			<option value="PerformanceMonitorCommandsPipe|ALIGN_LEFT">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ALIGN_LEFT</option>
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
			<option value="TelemetryVibShakerPipeCommands|ALIGN_LEFT">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ALIGN_LEFT</option>
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
			<option value="WarThunderExporterPipeCommands|ALIGN_LEFT">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ALIGN_LEFT</option>
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
			<option value="SimConnectExporterPipeCommands|ALIGN_LEFT">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ALIGN_LEFT</option>
			<option value="SimConnectExporterPipeCommands|FOREGROUND">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;FOREGROUND</option>
		</select>

		
		<input type="submit" class="submit-btn" name="Command" value="Command">


		<!-- Footer message -->
		<div id="result" class="footer-message">[<?php echo $time_stamp; ?>] <?php echo $footer; ?></div>
		<p><div id="result" class="footer-message"><?php echo 'PHP version: ' . phpversion();?></div>
	</form>
</div>

</body>
</html>
