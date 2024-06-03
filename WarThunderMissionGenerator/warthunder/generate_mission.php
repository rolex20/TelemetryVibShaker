<?php
header('Content-Type: text/plain; charset=utf-8');
header('Content-Disposition: inline; filename="my-file.txt"');
function readJsonFile($filename) {
    // Read the JSON file into a string
    $jsonString = file_get_contents($filename);

    // Decode the JSON string into a PHP associative array
    $jsonData = json_decode($jsonString, true);

    // Return the decoded data (JSON object)
    return $jsonData;
}

function getJsonValue($jsonObject, $key) {
		if (empty($jsonObject[$key])) {
			return "";
		} else {			
			return $jsonObject[$key];
		}
}

// Example usage:
$filename = 'mission_data.json'; // Replace with your actual filename
$jsonObject = readJsonFile($filename);

//echo getJsonValue($jsonObject, 'missionDescription');
?>
selected_tag:t=""
bin_dump_file:t=""

mission_settings{
  player{
    army:i=1
    wing:t="Player"
  }

  player_teamB{
    army:i=2
    wing:t="Enemy"
  }

  mission{
    type:t="singleMission"
    level:t="levels/air_israel.bin"
    environment:t="<?=getJsonValue($jsonObject,'environmentSelect')?>"
    weather:t="<?=getJsonValue($jsonObject,'weatherSelect')?>"
    locName:t="<?=getJsonValue($jsonObject,'missionName')?>"
    locDesc:t="<?=getJsonValue($jsonObject,'missionDescription')?>"
    isLimitedFuel:b=yes
    isLimitedAmmo:b=yes
    scoreLimit:i=50000
    isBotsAllowed:b=yes
    maxRespawns:i=2
    restoreType:t="attempts"
  }

  atmosphere{
    pressure:r=760
    temperature:r=15
  }

  briefing{
    place_loc:t=""
    date_loc:t=""
    objective_loc:t=""
    music:t="action_01"
  }
}

imports{}
triggers{
  isCategory:b=yes
  is_enabled:b=yes

  "Mission Success when Enemy Dead"{
    is_enabled:b=yes
    comments:t=""

    props{
      actionsType:t="PERFORM_ONE_BY_ONE"
      conditionsType:t="ALL"
      enableAfterComplete:b=no
    }

    events{
      periodicEvent{
        time:r=1
      }
    }

    conditions{
      unitWhenStatus{
        object_type:t="isKilled"
        check_objects:t="any"
        object_marking:i=0
        object_var_name:t=""
        object_var_comp_op:t="equal"
        object_var_value:i=0
        target_type:t="isAlive"
        check_period:r=1
        object:t="Enemy"
      }
    }

    actions{
      triggerDisable{
        target:t="Enemy Attacks Player"
      }

      moSetObjectiveStatus{
        target:t="Defeat the bogey"
        status:i=2
        object_marking:i=0
        object_var_comp_op:t="equal"
        object_var_name:t=""
      }

      wait{
        time:r=2
      }

      missionCompleted{
        timer:b=yes
        showCompleteMessage:b=yes
        playCompleteMusic:b=yes
      }
    }

    else_actions{}
  }

  "Enemy Attacks Player"{
    is_enabled:b=yes
    comments:t=""

    props{
      actionsType:t="PERFORM_ONE_BY_ONE"
      conditionsType:t="ALL"
      enableAfterComplete:b=yes
    }

    events{
      periodicEvent{
        time:r=1
      }
    }

    conditions{}
    actions{
      unitAttackTarget{
        playerAttracted:b=no
        object:t="Enemy"
        attack_type:t="attack_target"
        target:t="Player"
      }
    }

    else_actions{}
  }

  "Mission Failed when Player Dead"{
    is_enabled:b=yes
    comments:t=""

    props{
      actionsType:t="PERFORM_ONE_BY_ONE"
      conditionsType:t="ALL"
      enableAfterComplete:b=no
    }

    events{
      periodicEvent{
        time:r=1
      }
    }

    conditions{
      unitWhenStatus{
        object_type:t="isKilled"
        check_objects:t="any"
        object_marking:i=0
        object_var_name:t=""
        object_var_comp_op:t="equal"
        object_var_value:i=0
        target_type:t="isAlive"
        check_period:r=1
        object:t="Player"
      }
    }

    actions{
      triggerDisable{
        target:t="Enemy Attacks Player"
      }

      moSetObjectiveStatus{
        target:t="Defeat the bogey"
        status:i=3
        object_marking:i=0
        object_var_comp_op:t="equal"
        object_var_name:t=""
      }

      wait{
        time:r=10
      }

      missionFailed{
        timer:b=no
      }
    }

    else_actions{}
  }
}

mission_objectives{
  isCategory:b=yes
  is_enabled:b=yes

  "Defeat the bogey"{
    is_enabled:b=yes
    comments:t="Defeat the aggressor using BFM with guns or missiles (triggers)"
    type:t="abstractMissionObjective"

    props{
      isPrimary:b=yes
      timeLimit:i=1800
    }

    onSuccess{}
    onFailed{}
  }
}

variables{}
dialogs{}
airfields{}
effects{}
units{
  armada{
    name:t="Player"
    tm:m=[[0.249728, 0, 0.968316] [0, 1, 0] [-0.968316, 0, 0.249728] [-4900, <?=getJsonValue($jsonObject,'altitudeSelect')?>, 17100]]
    unit_class:t="<?=getJsonValue($jsonObject,'playerAircraftSelect')?>"
    objLayer:i=1
    closed_waypoints:b=no
    isShipSpline:b=no
    shipTurnRadius:r=100
    weapons:t="<?=getJsonValue($jsonObject,'playerArmamentSelect')?>"
    bullets0:t=""
    bullets1:t=""
    bullets2:t=""
    bullets3:t=""
    bulletsCount0:i=600
    bulletsCount1:i=600
    bulletsCount2:i=600
    bulletsCount3:i=600
    crewSkillK:r=0
    applyAllMods:b=no

    props{
      army:i=1
      count:i=1
      free_distance:r=70
      floating_distance:r=50
      minimum_distance_to_earth:r=20
      altLimit:r=6000
      attack_type:t="attack_target"
      skill:i=4
      player:b=yes
      use_search_radar:b=yes
      target:t="Enemy"
      allowAntimissile:b=yes
      targetAir:b=yes
      fuel:r=<?=getJsonValue($jsonObject,'playerFuel')?>
	  
      targetableByAi:b=yes
      speed:r=<?=intval(1.852*floatval(getJsonValue($jsonObject, 'playerSpeed')))?>

      plane{
        wing_formation:t="Diamond"
        row_distances:r=3
        col_distances:r=3
        super_formation:t="Diamond"
        super_row_distances:r=1.5
        super_col_distances:r=1.5
        ai_skill:t="NORMAL"
        task:t="FLY_WAYPOINT"
      }
    }

    way{}
  }

  armada{
    name:t="Enemy"
    tm:m=[[-0.180101, 0, -0.983648] [-0.0518687, 0.998609, 0.00949697] [0.98228, 0.052731, -0.179851] [-4035.74, <?=getJsonValue($jsonObject,'altitudeSelect')?>, 20124.7]]
    unit_class:t="<?=getJsonValue($jsonObject,'enemyAircraftSelect')?>"
    objLayer:i=1
    closed_waypoints:b=no
    isShipSpline:b=no
    shipTurnRadius:r=100
    weapons:t="<?=getJsonValue($jsonObject,'enemyArmamentSelect')?>"
    bullets0:t=""
    bullets1:t=""
    bullets2:t=""
    bullets3:t=""
    bulletsCount0:i=500
    bulletsCount1:i=500
    bulletsCount2:i=500
    bulletsCount3:i=500
    crewSkillK:r=0
    applyAllMods:b=no

    props{
      army:i=2
      count:i=1
      free_distance:r=70
      floating_distance:r=50
      minimum_distance_to_earth:r=20
      altLimit:r=50000
      attack_type:t="attack_target"
      skill:i=<?=getJsonValue($jsonObject, 'enemySkill')?> 	  
      use_search_radar:b=yes
      target:t="Player"
      canLeaveRouteForAtack:b=yes
      allowAntimissile:b=yes
      targetAir:b=yes
      targetGnd:b=no
      isDelayed:b=no
      stealthRadius:r=-1
      accuracy:r=0.9
      fuel:r=<?=getJsonValue($jsonObject,'enemyFuel')?>
	  
      targetableByAi:b=yes
	  
      speed:r=<?=intval(1.852*floatval(getJsonValue($jsonObject, 'playerSpeed')))?>

      plane{
        wing_formation:t="Diamond"
        row_distances:r=3
        col_distances:r=3
        super_formation:t="Diamond"
        super_row_distances:r=1.5
        super_col_distances:r=1.5
        ai_skill:t="NORMAL"
        task:t="FLY_WAYPOINT"
      }
    }

    way{}
  }
}

areas{}
objLayers{
  layer{
    enabled:b=yes
  }

  layer{
    enabled:b=yes
  }

  layer{
    enabled:b=yes
  }

  layer{
    enabled:b=yes
  }
}

wayPoints{}
