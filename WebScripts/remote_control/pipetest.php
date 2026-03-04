<?php
/**
 * Named pipe JSON command tester (PHP 5.3.4 compatible).
 *
 * Manual smoke test checklist:
 * 1) Run Start-CommandWatchers.ps1 with ipcServer enabled.
 * 2) Open /remote_control/pipetest.php.
 * 3) Click "Send ECHO" and verify response "ECHO".
 * 4) Click "Run Notepad (JSONB64)" and verify response "OK" plus Notepad launch.
 * 5) Click "Read Power Scheme (JSONB64)" and verify response "OK".
 * 6) Open commander.php and verify file rename command flow still works.
 */

header('Content-Type: text/html; charset=utf-8');

$pipeName = 'ipc_pipe_vr_server_commands';
$pipePath = '\\\\.\\pipe\\' . $pipeName;
$result = '';
$lastCommand = '';

function sendPipeCommand($pipePath, $line, $timeoutSeconds)
{
    $fp = @fopen($pipePath, 'r+b');
    if ($fp === false) {
        $err = error_get_last();
        $msg = 'FAILED to open pipe';
        if ($err && isset($err['message'])) {
            $msg .= ': ' . $err['message'];
        }
        return array(false, $msg);
    }

    stream_set_blocking($fp, true);
    stream_set_timeout($fp, $timeoutSeconds);

    fwrite($fp, $line . "\r\n");
    fflush($fp);

    $response = fgets($fp);
    $meta = stream_get_meta_data($fp);
    fclose($fp);

    if ($response === false) {
        if (!empty($meta['timed_out'])) {
            return array(false, 'No response (timeout)');
        }
        return array(false, 'No response');
    }

    return array(true, trim($response));
}

function sendJsonB64($pipePath, $commandType, $parameters)
{
    $payload = array(
        'command_type' => $commandType,
        'parameters' => $parameters
    );
    $json = json_encode($payload);
    if ($json === false) {
        return array(false, 'Failed to encode JSON payload');
    }

    $b64 = base64_encode($json);
    return sendPipeCommand($pipePath, 'JSONB64 ' . $b64, 3);
}

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $action = isset($_POST['action']) ? $_POST['action'] : '';

    if ($action === 'echo') {
        $lastCommand = 'ECHO';
        $result = sendPipeCommand($pipePath, 'ECHO', 2);
    } elseif ($action === 'run_notepad') {
        $lastCommand = 'JSONB64 RUN notepad.exe';
        $result = sendJsonB64($pipePath, 'RUN', array(
            'program' => 'notepad.exe',
            'parameter1' => 'not-used'
        ));
    } elseif ($action === 'read_powerscheme') {
        $lastCommand = 'JSONB64 READPOWERSCHEME';
        $result = sendJsonB64($pipePath, 'READPOWERSCHEME', array(
            'schemeName' => 'NA'
        ));
    }
}
?>
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<title>Pipe JSON Test</title>
<style>
body { font-family: Arial, sans-serif; margin: 20px; }
button { margin-right: 10px; margin-bottom: 10px; padding: 8px 14px; }
pre { background: #f5f5f5; padding: 10px; border: 1px solid #ddd; }
</style>
</head>
<body>
<h2>Named Pipe JSON Test</h2>
<p>Pipe path: <code><?php echo htmlspecialchars($pipePath); ?></code></p>

<form method="post">
    <button type="submit" name="action" value="echo">Send ECHO</button>
    <button type="submit" name="action" value="run_notepad">Run Notepad (JSONB64)</button>
    <button type="submit" name="action" value="read_powerscheme">Read Power Scheme (JSONB64)</button>
</form>

<?php if (is_array($result)) { ?>
<pre>
Last command: <?php echo htmlspecialchars($lastCommand); ?>
Status: <?php echo $result[0] ? 'SUCCESS' : 'ERROR'; ?>
Response: <?php echo htmlspecialchars($result[1]); ?>
</pre>
<?php } ?>

</body>
</html>
