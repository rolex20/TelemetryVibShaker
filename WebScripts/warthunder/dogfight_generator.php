<?php
function store_in_globals($key) {		
	if (!empty($_POST[$key])) {
			$value = $_POST[$key];
			$GLOBALS[$key] = $value;
	}
}

function storePostDataAsJson($filename) {
    // Check if $_POST is empty
    if (empty($_POST)) {
		$GLOBALS['status'] = 'Ready';
        return;
    }
	
	$GLOBALS['status'] = 'Mission ready at ' . date('Y-m-d H:i:s');

    // Convert $_POST to JSON
    $jsonData = json_encode($_POST);
    //$formattedJsonData = str_replace(array('{', '}', ','), array("{\n", "\n}", ",\n"), $jsonData);
	$formattedJsonData = $jsonData;

    // Write formatted JSON data to the specified file
    file_put_contents($filename, $formattedJsonData);
}

storePostDataAsJson('mission_data.json');
?>

<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Mission Generator for War Thunder</title>
    <script>
		function updateMissionName() {
				document.getElementById('missionName').value = document.getElementById('playerAircraftSelect').value + " vs " + document.getElementById('enemyAircraftSelect').value;
				document.getElementById('missionDescription').value = "{" + document.getElementById('playerArmamentSelect').value + "} vs {" + document.getElementById('enemyArmamentSelect').value + "}";
		}
	
	
        // Function to set a cookie
        function setCookie(name, value, days) {
            const date = new Date();
            date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
            const expires = "expires=" + date.toUTCString();
            document.cookie = name + "=" + value + ";" + expires + ";path=/";
        }

        // Function to get a cookie
        function getCookie(name) {
            const cname = name + "=";
            const decodedCookie = decodeURIComponent(document.cookie);
            const ca = decodedCookie.split(';');
            for (let i = 0; i < ca.length; i++) {
                let c = ca[i];
                while (c.charAt(0) == ' ') {
                    c = c.substring(1);
                }
                if (c.indexOf(cname) == 0) {
                    return c.substring(cname.length, c.length);
                }
            }
            return "";
        }

        // Function to save selected values in cookies
        function saveSelections() {
            const playerAircraftSelect = document.getElementById('playerAircraftSelect').value;
            const playerArmamentSelect = document.getElementById('playerArmamentSelect').value;
            const terrainSelect = document.getElementById('terrainSelect').value;
            setCookie('selectedAircraft', playerAircraftSelect, 360);
            setCookie('selectedArmament', playerArmamentSelect, 360);
            setCookie('selectedTerrain', terrainSelect, 360);
			
			setCookie('selectedEnvironment', document.getElementById('environmentSelect').value, 360);
			setCookie('selectedWeather', document.getElementById('weatherSelect').value, 360);
			setCookie('selectedAltitude', document.getElementById('altitudeSelect').value, 360);
			
			setCookie('selectedEnemyAircraft', document.getElementById('enemyAircraftSelect').value, 360);
			setCookie('selectedEnemyArmament', document.getElementById('enemyArmamentSelect').value, 360);

			setCookie('selectedPlayerFuel', document.getElementById('playerFuel').value, 360);
			setCookie('selectedEnemyFuel', document.getElementById('enemyFuel').value, 360);
			setCookie('selectedEnemySkill', document.getElementById('enemySkill').value, 360);
			setCookie('selectedPlayerSpeed', document.getElementById('playerSpeed').value, 360);			
        }
		
		function updateValue(id, cookie) {
			const value = getCookie(cookie);
            if (value) {
                document.getElementById(id).value = value;
            }						
		}

        // Function to restore selected values from cookies
        function restoreSelections(data) {		
            const selectedAircraft = getCookie('selectedAircraft');
            const selectedArmament = getCookie('selectedArmament');
			const selectedEnemyAircraft = getCookie('selectedEnemyAircraft');
			const selectedEnemyArmament = getCookie('selectedEnemyArmament');

            if (selectedAircraft) {
                document.getElementById('playerAircraftSelect').value = selectedAircraft;
                updateArmament(data); // Update armament options
				
               // Delay setting the armament to ensure options are loaded
                setTimeout(() => {
                    if (selectedArmament) {
                        document.getElementById('playerArmamentSelect').value = selectedArmament;
                    }
                }, 10);
				
            }
			
            
			if (selectedEnemyAircraft) {
                document.getElementById('enemyAircraftSelect').value = selectedEnemyAircraft;
                updateArmament2(data); // Update armament options
				
               // Delay setting the armament to ensure options are loaded
                setTimeout(() => {
                    if (selectedEnemyArmament) {
                        document.getElementById('enemyArmamentSelect').value = selectedEnemyArmament;
                    }
                }, 10);
				
            }
			
			updateValue('playerSpeed', 'selectedPlayerSpeed');
			updateValue('terrainSelect', 'selectedTerrain');
			updateValue('environmentSelect', 'selectedEnvironment');
			updateValue('weatherSelect', 'selectedWeather');
			updateValue('altitudeSelect', 'selectedAltitude');
			updateValue('playerFuel', 'selectedPlayerFuel');
			updateValue('enemyFuel', 'selectedEnemyFuel');
			updateValue('enemySkill', 'selectedEnemySkill');
        }

        // Function to update armament combo box based on selected player aircraft
        function updateArmament(data) {
            const playerAircraftSelect = document.getElementById('playerAircraftSelect');
            const playerArmamentSelect = document.getElementById('playerArmamentSelect');
            const selectedAircraft = playerAircraftSelect.value;
            const aircraft = data.aircraft.find(a => a.name === selectedAircraft);

            // Clear previous armament options
            while (playerArmamentSelect.firstChild) {
                playerArmamentSelect.removeChild(playerArmamentSelect.firstChild);
            }

            // Populate armament combo box with the new options
            if (aircraft) {
                aircraft.armament.forEach(armament => {
                    const option = document.createElement('option');
                    option.value = armament;
                    option.textContent = armament;
                    playerArmamentSelect.appendChild(option);
                });
            }
        }
		
        // Function to update armament combo box based on selected player aircraft
        function updateArmament2(data) {
            const enemyAircraftSelect = document.getElementById('enemyAircraftSelect');
            const enemyArmamentSelect = document.getElementById('enemyArmamentSelect');
            const selectedAircraft = enemyAircraftSelect.value;
            const aircraft = data.aircraft.find(a => a.name === selectedAircraft);

            // Clear previous armament options
            while (enemyArmamentSelect.firstChild) {
                enemyArmamentSelect.removeChild(enemyArmamentSelect.firstChild);
            }

            // Populate armament combo box with the new options
            if (aircraft) {
                aircraft.armament.forEach(armament => {
                    const option = document.createElement('option');
                    option.value = armament;
                    option.textContent = armament;
                    enemyArmamentSelect.appendChild(option);
                });
            }
        }
		

        document.addEventListener("DOMContentLoaded", function() {
            // Fetch JSON data from the server
            fetch('warthunder.json?rnd=3056')
                .then(response => response.json())
                .then(data => {
                    const playerAircraftSelect = document.getElementById('playerAircraftSelect');
					const enemyAircraftSelect = document.getElementById('enemyAircraftSelect');
                    const terrainSelect = document.getElementById('terrainSelect');

                    // Sort aircraft alphabetically by name
                    data.aircraft.sort((a, b) => a.name.localeCompare(b.name));
					
					// Populate player aircraft combo box
                    data.aircraft.forEach(aircraft => {
                        const option = document.createElement('option');
                        option.value = aircraft.name;
                        option.textContent = aircraft.name;
                        playerAircraftSelect.appendChild(option);
                    });
					
                    // Populate enemy aircraft combo box
                    data.aircraft.forEach(aircraft => {
                        const option = document.createElement('option');
                        option.value = aircraft.name;
                        option.textContent = aircraft.name;
                        enemyAircraftSelect.appendChild(option);
                    });
					

                    // Populate terrain combo box
                    data.terrain.forEach(terrain => {
                        const option = document.createElement('option');
                        option.value = terrain.name;
                        option.textContent = terrain.name;
                        terrainSelect.appendChild(option);
                    });
					
					// Populate environment options
					const environmentSelect = document.getElementById("environmentSelect");					
					for (const env of data.environment) {
						const option = document.createElement("option");
						option.value = env;
						option.textContent = env;
						environmentSelect.appendChild(option);
					}
					

					// Populate weather options
					const weatherSelect = document.getElementById("weatherSelect");					
					for (const env of data.weather) {
						const option = document.createElement("option");
						option.value = env;
						option.textContent = env;
						weatherSelect.appendChild(option);
					}
					
					
					// Populate altitude options
					const altitudeSelect = document.getElementById("altitudeSelect");					
					for (const env of data.altitude) {
						const option = document.createElement("option");
						option.value = env;
						option.textContent = env;
						altitudeSelect.appendChild(option);
					}

                    // Restore selections from cookies
                    restoreSelections(data);

                    // Add event listener to aircraft combo boxes
                    playerAircraftSelect.addEventListener('change', function() {
                        updateArmament(data);
						updateMissionName()
                    });
					enemyAircraftSelect.addEventListener('change', function() {
						updateArmament2(data);
						updateMissionName()
					});
					playerArmamentSelect.addEventListener('change', function() {
						updateMissionName()
                    });
					enemyArmamentSelect.addEventListener('change', function() {
						updateMissionName()
                    });

                    // Initialize armament combo box
                    updateArmament(data);
					updateArmament2(data);
					
					// Delay setting the armament to ensure options are loaded
					setTimeout(() => {
							updateMissionName();
					}, 10);						
                })
                .catch(error => console.error('Error fetching the JSON data:', error));

            // Save selections before form submission
            const form = document.querySelector('form');
            if (form) {
                form.addEventListener('submit', saveSelections);
            }
			
        });
    </script>
<style>
  body { font-family: Arial, sans-serif; background-color: #f7f7f7; margin: 0; padding: 10px; }
  .container { max-width: 600px; margin: auto; background: #fff; padding: 10px; box-shadow: 0 3px 6px rgba(0,0,0,0.1); }
  h2 { color: #333;  margin-bottom: 10px; margin-top: 5px;}
  .welcome-message { margin-bottom: 10px; }
  select, input[type="text"] { width: auto; padding: 10px; margin-bottom: 20px; border: 1px solid #ddd; border-radius: 4px; box-sizing: border-box; }
  .submit-btn { background-color: #5cb85c; color: white; padding: 10px 20px; border: none; border-radius: 4px; cursor: pointer; }
  .submit-btn:hover { background-color: #4cae4c; }
  .footer-message {
    text-align: center;
    margin-top: 20px;
    padding: 10px;
    background-color: #f2f2f2;
    border-radius: 4px;
    color: #333;
  }  
</style>
</head>
<body>

<div class="container">
    <form method="POST" align="center" action="dogfight_generator.php">
        <h2><a href="dogfight_generator.php">Dogfight Mission Generator</a></h2>
        <p class="welcome-message">Select parameters and click submit to generate the mission in Israel</p>


        <br><label for="playerAircraftSelect" style="color: blue;">Select Player Aircraft:</label>
        <select id="playerAircraftSelect" name="playerAircraftSelect"></select>

        <br><label for="playerArmamentSelect" style="color: blue;">Select Player Armament:</label>
        <select id="playerArmamentSelect" name="playerArmamentSelect"></select>
		
		<br><label for="playerFuel" style="color: blue;">Select Player Fuel %:</label>
		<input type="number" id="playerFuel" name="playerFuel" min="0" max="100" value="80" step="10">
		
		&nbsp;&nbsp;<label for="playerSpeed" style="color: blue;">Initial Speed knots:</label>
		<input type="number" id="playerSpeed" name="playerSpeed" min="0" max="1000" value="440" step="20"><br>		

        <br><label for="enemyAircraftSelect" style="color: red;">Select Enemy Aircraft:</label>
        <select id="enemyAircraftSelect" name="enemyAircraftSelect"></select>

        <br><label for="enemyArmamentSelect" style="color: red;">Select Enemy Armament:</label>
        <select id="enemyArmamentSelect" name="enemyArmamentSelect"></select>
		
		<br><label for="enemyFuel" style="color: red;">Select Enemy Fuel %:</label>
		<input type="number" id="enemyFuel" name="enemyFuel" min="0" max="100" value="80" step="10">		

		&nbsp;&nbsp;<label for="enemyFuel" style="color: red;">Select AI Skill Level:</label>
		<input type="number" id="enemySkill" name="enemySkill" min="0" max="5" value="5" step="1"><br>			

        <br><label for="terrainSelect">Select Terrain:</label>
        <select id="terrainSelect" name="terrainSelect"></select>
		
        <br><label for="environmentSelect">Select Environment:</label>
        <select id="environmentSelect" name="environmentSelect"></select>
		
        <br><label for="weatherSelect">Select Weather:</label>
        <select id="weatherSelect" name="weatherSelect"></select>
		
        <br><label for="altitudeSelect">Select Altitude:</label>
        <select id="altitudeSelect" name="altitudeSelect"></select>
		
		<br><label for="missionName">Mission Name:</label>
		<input type="text" id="missionName" name="missionName">
				
		<br><label for="missionDescription">Mission Description:</label>
		<input type="text" id="missionDescription" name="missionDescription">
		
		
        <p><button type="submit" class="submit-btn" name="btnSubmit" value="GenerateMission">Generate Mission</button>


        <!-- Footer message -->
        <div id="result" class="footer-message"><A HREF="dogfight_setup_1.php"><?=$GLOBALS['status']?></A></div>
    </form>
</div>

</body>
</html>
