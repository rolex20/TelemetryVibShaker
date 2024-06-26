# TelemetryVibShaker

How this can help you be a better sim pilot?  A real F-16 pilot knows when the angle of attack is in optimal range because they receive many physical cues such as G's, buffet sounds, the feel of speed and possibly many others.  As a flight-sim pilot, I don't have a way to feel those cues, so I created this set of tools initially to play a sound whenever the F-16 was in the optimal angle of attack, let's say between 11 and 14 which is more or less the on-speed for landing (altough definitively not the same concepts).  

Then I started adding support for other fighter units and other events such as the flaps for example: When I use the F-14 sometimes I use the flaps trick to beat my opponent but then the risk is to forget to raise the flaps before passing 220 knots.  So, this program will make some motors vibrate and the motor is sticked to my joystick, so I feel some vibration which helps me remember to raise the flaps and increase the immersive experience.  The same with the speed brakes.

**Most important/needed components:**

## Export.Lua: 
This is the export.lua script with code that sends telemetry info to the C# program TelemetryVibShaker.

## TelemetryVibShaker:
C# Windows program.  From here you can customize the sound effects and vibration effects to send to the Arduino Vibration Motors or to the Speakers.

## TelemetryVib_WiFi:
Arduino R4 program (WiFi).  Uses the WiFi network to listen for UDP packets containing the vibration intensity for two small cellphone vibration motors.

## TelemetryVib_Ethernet:
Arduino R4 Ethernet program (Ethernet).  Uses the Ethernet network to listen for UDP packets containing the vibration intensity for two small cellphone vibration motors.  Using the LocalNetworkDelay programs I discovered that the WiFi component of the Arduino R4 board, adds 20ms latency internally.  I could also verify that my WiFi network was not the problem by measuring network delay between two programs running in my PC instead of one in the PC and the other in the ArduinoR4WiFi.  This is eliminated when using Ethernet so I rewrote it to eliminate that 20ms.  This was only for fun since I couldn't feel that delay when flying in DCS World.  

## my_ttgo_TelemetryVib_WiFi:
LILYGO TTGO T-Watch 2020 V3 program (WiFi) Uses the WiFi network to listen for UDP packets containing the vibration intensity for the internal vibration motor available in this watch.  It is designed for the AoA, when in optimal AoA for the given player aircraft the internal motor vibrates and the screen goes Green.  (Similar to the Yellow, Green, Red Angle of Attack indicator in the F-16C that virually no one uses because is too low in the cockpit).

Libs and driver used in Win11: https://github.com/rolex20/TelemetryVibShaker/releases/tag/v0.9-ttgolibs 

ToDo1: Complete readme, add diagrams

