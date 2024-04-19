--local wwtlfs=require('lfs')
--dofile(wwtlfs.writedir()..'Scripts/wwt/wwtExport.lua')
--The two lines above are from Winwing and their SimAppPro software

-- Data export script for DCS, version 1.2.
-- Copyright (C) 2006-2014, Eagle Dynamics.
-- See http://www.lua.org for Lua script system info 
-- We recommend to use the LuaSocket addon (http://www.tecgraf.puc-rio.br/luasocket) 
-- to use standard network protocols in Lua scripts.
-- LuaSocket 2.0 files (*.dll and *.lua) are supplied in the Scripts/LuaSocket folder
-- and in the installation folder of the DCS. 

-- Expand the functionality of following functions for your external application needs.
-- Look into Saved Games\DCS\Logs\dcs.log for this script errors, please.


-- AoA Telemetry Code for TelemetryVibShaker:
-- Functions to export AoA (Angle of Attack)
-- AoA telemetry will be send via UDP to the C# app
-- You'll need getDeciSecondsAsInt(), LuaExportAfterNextFrame(), LuaExportStart() and LuaExportStop()

local function getDeciSecondsAsInt()
  return math.floor(os.clock() * 20) -- before it was 10 for deciseconds, now I want 20 samples instead of 10 per second
end


function LuaExportStart()

	package.path  = package.path..";"..lfs.currentdir().."/LuaSocket/?.lua"
	package.cpath = package.cpath..";"..lfs.currentdir().."/LuaSocket/?.dll"

	socket = require("socket")
	server = "127.0.0.1" -- Normally the C# app runs in the same CPU where DCS is running to make the sound comes from the same speakers as DCS World
	port = 54671 -- If you change this port, change it also in the C# app

	udp = socket.udp()
	udp:settimeout(0) -- minimize any blocking, don't care if one datagram is missing.  settimeout() no longer has an effect on udp:: but is left to clarify the intention
	udp:setpeername(server, port)
	last_time_stamp = 0
	last_UnitId = "none"
end

function tvsSanitizeByte(value) 
	if (value > 255) then
		return 255
	elseif (value < 0) then
		return 0
	else
		return math.floor(value)
	end
end 


function LuaExportAfterNextFrame()

	if udp then
		local new_time_stamp = getDeciSecondsAsInt()	
		if new_time_stamp ~= last_time_stamp then -- Avoid send something every millisecond, , no need for more resolution, i.e. only send every tenth of a second to minimize cpu load
			local unitId = LoGetPlayerPlaneId() or 0
			if unitId then 
				if (lastUnitId == unitId) then
					-- use the following for my new program
					local AoA = tvsSanitizeByte(LoGetAngleOfAttack() or 0)
					local speed = ((LoGetTrueAirSpeed() or 0) / 10) --we want to send decameters/second so that it fits in 1 byte					
					local mechInfo = LoGetMechInfo()
					local sb, flaps
					if (mechInfo) then
						sb = ((mechInfo.speedbrakes.value or 0) * 100)
						flaps = ((mechInfo.flaps.value or 0) * 100)						
					else
						sb = 0
						flaps = 0
					end
					udp:send(string.char(AoA, sb, flaps, speed))
					
					--use the following for my older program
					--local AoA = LoGetAngleOfAttack() or 0
					--udp:send(AoA)
					
					--use the following for debugging
					--udp:send(string.format("AoA: %.4f TimeStamp: %.4f\n",AoA, new_time_stamp))					
					--udp:send(string.format("Speedbrakes status: %s, value: %s", mechInfo.speedbrakes.status, mechInfo.speedbrakes.value))
				else  -- one AoA sample will be missed, no big deal
					local unitObject = LoGetObjectById(unitId)
					if unitObject then
						udp:send(unitObject.Name)
					else
						udp:send("unknown")
					end
					lastUnitId = unitId
				end
			else
				lastUnitId = -1
			end 
			
			last_time_stamp = new_time_stamp
		end
	end

end

function LuaExportStop()
	if udp then 
		udp:close()
	end
end


-- If you have Tacview, you might have some lines like the line below.
-- Don't modify them, they are not part of the AoA Telemetry C# app

local Tacviewlfs=require('lfs');dofile(Tacviewlfs.writedir()..'Scripts/TacviewGameExport.lua')
