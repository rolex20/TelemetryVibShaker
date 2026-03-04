<?php
/**
 * PHP 5.3.4 Named Pipe test for Windows
 * Connects to: \\.\pipe\ipc_pipe_vr_server_commands
 * Sends: "ECHO\r\n"
 * Reads: one line response (expected "ECHO")
 */

header('Content-Type: text/plain; charset=utf-8');

$pipeName = 'ipc_pipe_vr_server_commands';
$pipePath = '\\\\.\\pipe\\' . $pipeName;

echo "Connecting to pipe: {$pipePath}\r\n";

$fp = @fopen($pipePath, 'r+b'); // read/write binary
if ($fp === false) {
    $err = error_get_last();
    echo "FAILED to open pipe.\r\n";
    if ($err && isset($err['message'])) {
        echo "Error: " . $err['message'] . "\r\n";
    }
    echo "\r\nNotes:\r\n";
    echo "- Make sure Start-CommandWatchers.ps1 has ipcServer enabled and is running.\r\n";
    echo "- Pipe server name must match: ipc_pipe_vr_server_commands\r\n";
    exit;
}

// Make I/O behave predictably
stream_set_blocking($fp, true);
stream_set_timeout($fp, 2); // seconds

$cmd = "ECHO\r\n";
echo "Sending: {$cmd}";
fwrite($fp, $cmd);
fflush($fp);

// Read one line response
$response = fgets($fp);
$meta = stream_get_meta_data($fp);

fclose($fp);

if ($response === false) {
    echo "No response (timeout=" . ($meta['timed_out'] ? 'true' : 'false') . ")\r\n";
} else {
    echo "Response: " . trim($response) . "\r\n";
}

echo "Done.\r\n";
?>
