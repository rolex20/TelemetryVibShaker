// The latest version of this program is now built using Visual Studio Code and PlatformIO
// You can find it here: C:\Users\ralch\Documents\PlatformIO\Projects\ttgo_TelVib


// This program is now obsolete
// I moved to Visual Studio Code because of its incremental compilation
// which is much faster than arduino IDE that all the time tries to compile everything



#include <WiFi.h>
#include "time.h"
#include "ScreenSaver.h"
#include "wifi_secrets.h"

TTGOClass *ttgo;
ScreenSaver screenSaver;

#include "UI_stuff.h"

// Telemetry globals
const int vibrationPin = 4; // Vibration motor connected to GPIO4
unsigned int localPort = 54671;      // local port to listen on
WiFiUDP Udp;


void setup()
{
    // Set 20MHz operating speed to reduce power consumption
    //setCpuFrequencyMhz(20);    

    initialSetup();
    initialWiFiSetup();
    initialClockSetup();


    // set font for Main Clock
    tft->setFreeFont(&FreeMonoOblique9pt7b);

    // prepare motor
    pinMode(vibrationPin, OUTPUT);

    //attach touch screen interrupt pin
    pinMode(TP_INT, INPUT);
    
}


void loop()
{
    ulong now;
    
    now  = millis() / 1000; // only interested in seconds

    // Wake the screen if the user touches the touchscreen
    bool forcedUpdate = AwakeScreenOnTouch(now);

    // Update the clock
    UpdateUI(forcedUpdate, now);
}
