#include <Arduino.h>

// LILIGO T-WATCH 2020V3
// UDP Server to receive telemetry from DCS World, etc
// It vibrates a motor and change the screen background depending on the 
// bytes received via UDP
// The first byte specifies the intensity to vibrate the motor
// The second byte specifies the background color for the screen
// Note: Don't know yet how to change vibration intensity on my LILIGO T-WATCH 2020 V3

// **************************************
// TOP INCLUDE FILES
// **************************************
#include <WiFi.h>
#include "time.h"
#include "ScreenSaver.h"
#include "wifi_secrets.h"

// *************************************
// TOP GLOBALS FOR THE USER INTERFACE
// *************************************
TTGOClass *ttgo;
ScreenSaver screenSaver;
#include "UI_stuff.h"

// *************************************
// TELEMETRY GLOBALS
// *************************************
const int motor1_pin = 4;           // Can be used for AoA, stall, etc
const int seconds_threshold = 2;    // After this #seconds, stop the motors if no more packets are received
const int stop_motor = 0;
const unsigned int localPort = 54671;     // Local port to listen on
WiFiUDP Udp;

void setup()
{
    // Set 20MHz operating speed to reduce power consumption
    //setCpuFrequencyMhz(20); // This speed is not enough for initial Setup (WiFi, etc)

    initialSetup();
    initialWiFiSetup();
    initialClockSetup();

    //attach touch screen interrupt pin
    pinMode(TP_INT, INPUT);

    // prepare motor
    pinMode(motor1_pin, OUTPUT);

    // start listening for telemetry
    Udp.begin(localPort);    
    tft->println("UDP Server: Listening on port " + String(localPort));

    // set font for UI (clock)
    tft->setFreeFont(&FreeMonoOblique9pt7b);
}

// call digitalWrite only if needed
bool vibrate_motor(int pin, int new_intensity, int &prev_intensity) {
    new_intensity = (new_intensity>0)? HIGH: LOW;  
    if (new_intensity != prev_intensity) {
      prev_intensity = new_intensity;
      digitalWrite(pin, new_intensity);
      return true;
    }

    return false;
}

void loop() {
    int packetSize, req_intensity;
    int lastMotor1_intensity = 0;
    int lastMotor2_intensity = 0;
    ulong packet_timestamp = 0;
    ulong timestamp;
    bool motor_check = true;
    bool color_check = true;
    const unsigned int BUFF_SIZE = 32;
    byte buffer[BUFF_SIZE];
    byte index;


    for ( ; ; ) 
    {
      packetSize = Udp.parsePacket();
      timestamp = millis() / 1000; // interested in seconds

      if (packetSize) {
          GoHighSpeed();
          Udp.read(buffer, BUFF_SIZE);
          packet_timestamp = timestamp;
          motor_check = true;      

          req_intensity = buffer[0];
          vibrate_motor(motor1_pin, req_intensity, lastMotor1_intensity);

          if (packetSize >= 2) {
                index = buffer[1];
                screenSaver.SetStripedBackground(timestamp, index);
                color_check = true;
          } 

      } else if (motor_check && (timestamp - packet_timestamp >= seconds_threshold)) { // check if motors continue to vibrate without receiving more requests
          // lets reduce a little the number of times to check for this
          motor_check = false;

          // stop motor1 in case it is still vibrating
          vibrate_motor(motor1_pin, stop_motor, lastMotor1_intensity);
          
      } else if (color_check && (timestamp - packet_timestamp >= seconds_threshold)) { // check if got stuck in yellow,green or red
          // lets reduce a little the number of times to check for this
          color_check = false;

          // return background color to black if necessary
          screenSaver.SetBackground(timestamp, TFT_BLACK);

          // turn off the screen
          screenSaver.SetBrightness(timestamp+1, TURN_OFF_LCD);

      } else {
          // Update user interface (clock)

          // Wake the screen if the user touches the touchscreen
          bool forcedUpdate = AwakeScreenOnTouch(timestamp);

          // Update the clock
          UpdateUI(forcedUpdate, timestamp);

          // Go Low Power/Low Speed Mode if there is no UDP Packets for 10 seconds
          if (timestamp-packet_timestamp>10) GoLowSpeed();

      }

    } // end-for

} // function-loop

