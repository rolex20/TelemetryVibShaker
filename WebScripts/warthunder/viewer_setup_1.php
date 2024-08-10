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

// Read the json with all the parameters generated for this mission
$filename = 'mission_data.json';
$jsonObject = readJsonFile($filename);
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
    wing:t="Harrier"
  }

  mission{
    type:t="singleMission"
    level:t="levels/port_moresby.bin"
    environment:t="<?=getJsonValue($jsonObject,'environmentSelect')?>"
    weather:t="<?=getJsonValue($jsonObject,'weatherSelect')?>"
    locName:t="Viewer Mission"
    locDesc:t="You can check other aircraft ordnance/armament.  Based on the Falklands mission."
    isLimitedFuel:b=yes
    isLimitedAmmo:b=yes
    isBotsAllowed:b=yes
    restoreType:t="attempts"
  }

  briefing{
    place_loc:t="briefing-place-loc"
    date_loc:t="briefing-date loc"
    objective_loc:t="briefing-objective loc"
    music:t="action_01"
  }
}

imports{}
triggers{
  isCategory:b=yes
  is_enabled:b=yes

  "Attack Argentina"{
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
        object:t="squad_01"
        attack_type:t="kill_target"
        target:t="Argentina"
      }

      unitAttackTarget{
        playerAttracted:b=no
        object:t="Harrier"
        attack_type:t="kill_target"
        target:t="Argentina"
      }
    }

    else_actions{}
  }

  "Attack England"{
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

    conditions{}
    actions{
      unitAttackTarget{
        playerAttracted:b=no
        object:t="A-4s"
        attack_type:t="attack_target"
        target:t="squad_01"
        weaponType:t="bombs"
      }
    }

    else_actions{}
  }

  "Bombers are hit - mission failed"{
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
        check_objects:t="all"
        object_marking:i=0
        object_var_name:t=""
        object_var_comp_op:t="equal"
        object_var_value:i=0
        target_type:t="isAlive"
        check_period:r=1
        object:t="A-4s"
      }
    }

    actions{
      moSetObjectiveStatus{
        target:t="Cover Bombers until they return to land"
        status:i=3
        object_marking:i=0
        object_var_comp_op:t="equal"
        object_var_name:t=""
        team:t="Both"
      }

      wait{
        time:r=4
      }
    }

    else_actions{}
  }

  "Harriers are dead"{
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
        check_objects:t="all"
        object_marking:i=0
        object_var_name:t=""
        object_var_comp_op:t="equal"
        object_var_value:i=0
        target_type:t="isAlive"
        check_period:r=1
        object:t="Harrier"
      }
    }

    actions{
      moSetObjectiveStatus{
        target:t="Intercept or Disable Harriers"
        status:i=2
        object_marking:i=0
        object_var_comp_op:t="equal"
        object_var_name:t=""
      }
    }

    else_actions{}
  }

  "Harriers dead and bombers alive"{
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
        object_type:t="isAlive"
        check_objects:t="all"
        object_marking:i=0
        object_var_name:t=""
        object_var_comp_op:t="equal"
        object_var_value:i=0
        target_type:t="isAlive"
        check_period:r=1
        object:t="A-4s"
      }

      unitWhenStatus{
        object_type:t="isKilled"
        check_objects:t="all"
        object_marking:i=0
        object_var_name:t=""
        object_var_comp_op:t="equal"
        object_var_value:i=0
        target_type:t="isAlive"
        check_period:r=1
        object:t="Harrier"
      }
    }

    actions{
      moSetObjectiveStatus{
        target:t="Cover Bombers until they return to land"
        status:i=2
        object_marking:i=0
        object_var_comp_op:t="equal"
        object_var_name:t=""
      }

      wait{
        time:r=4
      }

      missionCompleted{
        showCompleteMessage:b=yes
        playCompleteMusic:b=yes
      }
    }

    else_actions{}
  }

  "England Naval units are hit"{
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
      unitWhenHit{
        target:t="squad_01"
        offender:t="A-4s"
      }
    }

    actions{
      moSetObjectiveStatus{
        target:t="Cause relevant damage to England naval units"
        status:i=2
        object_marking:i=0
        object_var_comp_op:t="equal"
        object_var_name:t=""
      }

      wait{
        time:r=10
      }

      unitMoveTo{
        object_marking:i=0
        object_var_name:t=""
        object_var_comp_op:t="equal"
        object_var_value:i=0
        target:t="escape zone"
        target_var_name:t=""
        target_var_comp_op:t="equal"
        target_var_value:i=0
        target_marking:i=0
        waypointReachedDist:r=10
        recalculatePathDist:r=-1
        follow_target:b=no
        fastClimb:b=no
        destTimeMode:b=no
        teleportHeightType:t="absolute"
        useUnitHeightForTele:b=yes
        shouldKeepFormation:b=no
        teleportHeightValue:r=1000
        horizontalDirectionForTeleport:b=yes
        object:t="A-4s"
        speed:r=900
      }
    }

    else_actions{}
  }

  "Harriers are winchester"{
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
        object_type:t="noAmmo"
        check_objects:t="all"
        object_marking:i=0
        object_var_name:t=""
        object_var_comp_op:t="equal"
        object_var_value:i=0
        target_type:t="isAlive"
        check_period:r=1
        object:t="Harrier"
      }
    }

    actions{
      moSetObjectiveStatus{
        target:t="Intercept or Disable Harriers"
        status:i=2
        object_marking:i=0
        object_var_comp_op:t="equal"
        object_var_name:t=""
      }
    }

    else_actions{}
  }

  "Harriers can't fight"{
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
        object_type:t="cantFight"
        check_objects:t="all"
        object_marking:i=0
        object_var_name:t=""
        object_var_comp_op:t="equal"
        object_var_value:i=0
        target_type:t="isAlive"
        check_period:r=1
        object:t="Harrier"
      }
    }

    actions{
      moSetObjectiveStatus{
        target:t="Intercept or Disable Harriers"
        status:i=2
        object_marking:i=0
        object_var_comp_op:t="equal"
        object_var_name:t=""
      }
    }

    else_actions{}
  }

  "Bombers have escaped"{
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
      unitWhenInArea{
        math:t="2D"
        object_type:t="isAlive"
        object_marking:i=0
        object_var_name:t=""
        object_var_comp_op:t="equal"
        check_objects:t="all"
        object:t="A-4s"
        target:t="escape zone"
      }
    }

    actions{
      triggerDisable{
        target:t="Attack Argentina"
      }

      unitMoveTo{
        object_marking:i=0
        object_var_name:t=""
        object_var_comp_op:t="equal"
        object_var_value:i=0
        target:t="harrier cap zone"
        target_var_name:t=""
        target_var_comp_op:t="equal"
        target_var_value:i=0
        target_marking:i=0
        waypointReachedDist:r=10
        recalculatePathDist:r=-1
        follow_target:b=no
        fastClimb:b=no
        destTimeMode:b=no
        teleportHeightType:t="absolute"
        useUnitHeightForTele:b=yes
        shouldKeepFormation:b=no
        teleportHeightValue:r=1000
        horizontalDirectionForTeleport:b=yes
        object:t="Harrier"
        speed:r=900
      }

      moSetObjectiveStatus{
        target:t="Cover Bombers until they return to land"
        status:i=2
        object_marking:i=0
        object_var_comp_op:t="equal"
        object_var_name:t=""
      }

      moSetObjectiveStatus{
        target:t="Intercept or Disable Harriers"
        status:i=2
        object_marking:i=0
        object_var_comp_op:t="equal"
        object_var_name:t=""
      }

      wait{
        time:r=4
      }

      missionCompleted{
        showCompleteMessage:b=yes
        playCompleteMusic:b=yes
      }
    }

    else_actions{}
  }
}

mission_objectives{
  isCategory:b=yes
  is_enabled:b=yes

  "Cover Bombers until they return to land"{
    is_enabled:b=yes
    comments:t="Cover Bombers until they return to land"
    type:t="abstractMissionObjective"

    props{
      isPrimary:b=no
      timeLimit:i=1800
    }

    onSuccess{}
    onFailed{}
  }

  "Intercept or Disable Harriers"{
    is_enabled:b=yes
    comments:t="Intercept and destroy harriers"
    type:t="abstractMissionObjective"

    props{
      isPrimary:b=no
      timeLimit:i=1800
    }

    onSuccess{}
    onFailed{}
  }

  "Cause relevant damage to England naval units"{
    is_enabled:b=yes
    comments:t="Cause relevant damage to England naval units"
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
  ships{
    name:t="ships_01"
    tm:m=[[1, 0, 0] [0, 1, 0] [0, 0, 1] [-11734.6, 0, -7912.43]]
    unit_class:t="hms_cargo_ship"
    objLayer:i=1
    closed_waypoints:b=no
    isShipSpline:b=no
    shipTurnRadius:r=100
    weapons:t=""
    bullets0:t=""
    bullets1:t=""
    bullets2:t=""
    bullets3:t=""
    bulletsCount0:i=0
    bulletsCount1:i=0
    bulletsCount2:i=0
    bulletsCount3:i=0
    crewSkillK:r=0
    applyAllMods:b=no

    props{
      army:i=2
      count:i=1
      formation_type:t="rows"
      formation_div:i=3
      formation_step:p2=2.5, 2
      formation_noise:p2=0.1, 0.1
    }

    way{}
  }

  ships{
    name:t="ships_02"
    tm:m=[[1, 0, 0] [0, 1, 0] [0, 0, 1] [-11769.6, 4.57764e-05, -7654.38]]
    unit_class:t="hms_cv_illustrious"
    objLayer:i=1
    closed_waypoints:b=no
    isShipSpline:b=no
    shipTurnRadius:r=100
    weapons:t=""
    bullets0:t=""
    bullets1:t=""
    bullets2:t=""
    bullets3:t=""
    bulletsCount0:i=0
    bulletsCount1:i=0
    bulletsCount2:i=0
    bulletsCount3:i=0
    crewSkillK:r=0
    applyAllMods:b=no

    props{
      army:i=2
      count:i=1
      formation_type:t="rows"
      formation_div:i=3
      formation_step:p2=2.5, 2
      formation_noise:p2=0.1, 0.1
    }

    way{}
  }

  ships{
    name:t="ships_03"
    tm:m=[[1, 0, 0] [0, 1, 0] [0, 0, 1] [-11600, 0, -7400]]
    unit_class:t="hms_leander"
    objLayer:i=1
    closed_waypoints:b=no
    isShipSpline:b=no
    shipTurnRadius:r=100
    weapons:t=""
    bullets0:t=""
    bullets1:t=""
    bullets2:t=""
    bullets3:t=""
    bulletsCount0:i=0
    bulletsCount1:i=0
    bulletsCount2:i=0
    bulletsCount3:i=0
    crewSkillK:r=0
    applyAllMods:b=no

    props{
      army:i=2
      count:i=1
      formation_type:t="rows"
      formation_div:i=3
      formation_step:p2=2.5, 2
      formation_noise:p2=0.1, 0.1
    }

    way{}
  }

  ships{
    name:t="ships_04"
    tm:m=[[1, 0, 0] [0, 1, 0] [0, 0, 1] [-11600, 0, -7100]]
    unit_class:t="hms_cargo_ship"
    objLayer:i=1
    closed_waypoints:b=no
    isShipSpline:b=no
    shipTurnRadius:r=100
    weapons:t=""
    bullets0:t=""
    bullets1:t=""
    bullets2:t=""
    bullets3:t=""
    bulletsCount0:i=0
    bulletsCount1:i=0
    bulletsCount2:i=0
    bulletsCount3:i=0
    crewSkillK:r=0
    applyAllMods:b=no

    props{
      army:i=2
      count:i=1
      formation_type:t="rows"
      formation_div:i=3
      formation_step:p2=2.5, 2
      formation_noise:p2=0.1, 0.1
    }

    way{}
  }

  squad{
    name:t="squad_01"
    tm:m=[[1, 0, 0] [0, 1, 0] [0, 0, 1] [-11300, 1000, -7500]]

    props{
      squad_members:t="ships_02"
      squad_members:t="ships_03"
      squad_members:t="ships_04"
      squad_members:t="ships_01"
    }
  }

  armada{
    name:t="A-4s"
    tm:m=[[-0.929699, 0, 0.368321] [0, 1, 0] [-0.368321, 0, -0.929699] [-1300, 100, -11700]]
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
    bulletsCount0:i=0
    bulletsCount1:i=0
    bulletsCount2:i=0
    bulletsCount3:i=0
    crewSkillK:r=0
    applyAllMods:b=no

    props{
      army:i=1
      count:i=2
      free_distance:r=70
      floating_distance:r=50
      minimum_distance_to_earth:r=20
      altLimit:r=6000
      attack_type:t="fire_at_will"
      skill:i=5
      speed:r=800

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
    name:t="Player"
    tm:m=[[-0.914036, 0, 0.405632] [0, 1, 0] [-0.405632, 0, -0.914036] [-1223.93, 90, -11728.7]]
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
    bulletsCount0:i=0
    bulletsCount1:i=0
    bulletsCount2:i=0
    bulletsCount3:i=0
    crewSkillK:r=0
    applyAllMods:b=no

    props{
      army:i=1
      count:i=1
      free_distance:r=70
      floating_distance:r=50
      minimum_distance_to_earth:r=20
      altLimit:r=6000
      attack_type:t="fire_at_will"
      skill:i=5
      player:b=yes
      speed:r=750

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
    name:t="Harrier"
    tm:m=[[1, 0, 0] [0, 1, 0] [0, 0, 1] [-31000, 2000, -6800]]
    unit_class:t="harrier_frs1_early"
    objLayer:i=1
    closed_waypoints:b=no
    isShipSpline:b=no
    shipTurnRadius:r=100
    weapons:t="harrier_frs1_rocket_aim9l"
    bullets0:t=""
    bullets1:t=""
    bullets2:t=""
    bullets3:t=""
    bulletsCount0:i=0
    bulletsCount1:i=0
    bulletsCount2:i=0
    bulletsCount3:i=0
    crewSkillK:r=0
    applyAllMods:b=no

    props{
      army:i=2
      count:i=2
      free_distance:r=70
      floating_distance:r=50
      minimum_distance_to_earth:r=20
      altLimit:r=6000
      attack_type:t="fire_at_will"
      skill:i=4
      canLeaveRouteForAtack:b=yes
      allowAntimissile:b=yes
      targetAir:b=yes
      targetGnd:b=yes
      speed:r=700

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

  squad{
    name:t="Argentina"
    tm:m=[[1, 0, 0] [0, 1, 0] [0, 0, 1] [-2900, 1000, -7000]]

    props{
      squad_members:t="A-4s"
      squad_members:t="Player"
    }
  }
}

areas{
  "escape zone"{
    type:t="Sphere"
    tm:m=[[7400, 0, 0] [0, 7400, 0] [0, 0, 7400] [-12800, 3000, 18400]]
    objLayer:i=0

    props{}
  }

  "harrier cap zone"{
    type:t="Sphere"
    tm:m=[[10000, 0, 0] [0, 10000, 0] [0, 0, 10000] [-14900, 4600, -18500]]
    objLayer:i=0

    props{}
  }
}

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
