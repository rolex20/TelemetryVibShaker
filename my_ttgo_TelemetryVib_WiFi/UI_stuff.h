
// Global variables for UI housekeeping

int initialBattPercentage = 0;                // Initial battery level when not charging
ulong initialBattTimeStamp = 0;   // and corresponding timestamp
ulong lastClockTimeStamp = 10; // last time the clock was updated,setting this to 10 minutes forces the initial update, no need to change this
String string;              // global to avoid constructor on each local call


// Clock globals
PCF8563_Class *rtc;
TFT_eSPI *tft;
bool rtcIrq = false;

// Cpu Mhz
uint32_t currentCpuFrequencyMhz = 0;

// Definition of ntp server, gmt offset, etc
#include "config_time.h"

//****************************************************************
// Functions to maintain the UI updated and used for initial setup
//****************************************************************

// Use the line equation from two points to predict battery level
// x = (y +mx1 - y1)/m
// m = (y1-y2)/(x1-x2)
// x is time, y is battery percentage
// This equation will be used to return the time at which the battery will be almost zero
// It calculates x from the linear equation from two points
// Parameters: y - the y-coordinate of the point to find x for
//             x1, y1 - the coordinates of the first point on the line
//             x2, y2 - the coordinates of the second point on the line
// Returns: x - the x-coordinate of the point on the line with the given y
float solveX(float y, float x1, float y1, float x2, float y2) {
  // Calculate the slope of the line
  float slope = (y2 - y1) / (x2 - x1);
  // Calculate the intercept of the line
  float intercept = y1 - slope * x1;
  // Solve for x using the equation y = slope * x + intercept
  float x = (y - intercept) / slope;
  // Return the value of x
  return x;
}

// Calculate minutes left with current battery level
int BattMinutesLeft(ulong now) 
{
  int percentage = ttgo->power->getBattPercentage();  
  int delta = initialBattPercentage - percentage;  // delta<0 could mean that the current battery is greater than before because the watch is charging

  if (delta > 0) { // this is when the clock is not charging 
    // Estimate the second (time) at which the battery will be almost zero (0.1f)
    float x = solveX(0.1f, (float)initialBattTimeStamp, (float)initialBattPercentage, (float)now, (float)percentage);

    int minutesLeft = (x - now) / 60;
    return minutesLeft;
  } else {// this might happen when the clock is charging 
    
    if (ttgo->power->isChargeing()) 
      initialBattPercentage = 0; //reset initial level    
    else if (initialBattPercentage == 0) { //when not charging: update battery level
      initialBattPercentage = ttgo->power->getBattPercentage(); 
      initialBattTimeStamp = now;
    }
    
    return -1;
  }
}


// Wake the screen if the user touches the touchscreen
bool AwakeScreenOnTouch(ulong now)
{

    if (digitalRead(TP_INT) == LOW) {
      if (screenSaver.GetCurrentBrightness() == REG_BRIGHTNESS) // turn on/off the screen on each press
      {
          screenSaver.AwakeScreen(now, TURN_OFF_LCD);
          return false;      
      }

      screenSaver.AwakeScreen(now, REG_BRIGHTNESS);
      return true;
    } else {
      screenSaver.SaveScreen(now, LOW_BRIGHTNESS);
      return false;
    }


}

void ShowBatteryPercentage(ulong now) 
{
      string = String(ttgo->power->getBattPercentage()) + "%    ";
      tft->drawString(string, 10, 220);

      int minutesLeft = BattMinutesLeft(now);
      string = (minutesLeft>=0) ? " minleft:" + String(minutesLeft): "                 ";
      tft->drawString(string, 55, 220);
}

void UpdateClock() 
{
      tft->drawString(rtc->formatDateTime(PCF_TIMEFORMAT_HM), 15, 80, 7);
      tft->drawString(rtc->formatDateTime(PCF_TIMEFORMAT_YYYY_MM_DD), 60, 145);
}

// If the watch is charging, draw a small circle at the top right corner of the screen
void ShowChargingStatus()
{
      ttgo->tft->fillCircle(ttgo->tft->width() - 10, ttgo->tft->height() / 2, 5, (ttgo->power->isChargeing()) ? TFT_GREEN: TFT_BLACK);
}

// Show if WiFi is still connected
void ShowWiFiStatus() 
{
  string = (WiFi.status() == WL_CONNECTED)? WiFi.SSID(): "                ";
  tft->drawString(string, 65, 195, 2);
}

// Print the time every minute
// now is the current second
void UpdateUI(bool forcedUpdate, ulong now) 
{
    ulong nowm = now / 60; // calculate elapsed time in minutes

    if ( ((lastClockTimeStamp!=nowm) && (screenSaver.GetCurrentBrightness()!=0)) || forcedUpdate ) {
      lastClockTimeStamp = nowm;
      UpdateClock();      
      ShowBatteryPercentage(now);
      ShowChargingStatus();
      ShowWiFiStatus();
    }      
}



//**************************************************************************
//Setup routines
//**************************************************************************
void initialSetup() {
    Serial.begin(115200);
    while (!Serial) {
      ; // wait for serial port to connect. Needed for native USB port only
    }

    ttgo = TTGOClass::getWatch();

    ttgo->begin();

    ttgo->openBL();

    screenSaver.SetTTGO(ttgo);
    screenSaver.SetBrightness(0, 255);      // invoke with ficticious timestamps
    screenSaver.SetBackground(1, TFT_BLACK);// invoke with ficticious timestamps, but different

    rtc = ttgo->rtc;

    tft = ttgo->tft;

    tft->setTextColor(TFT_GREEN, TFT_BLACK);

    // show program name
    tft->println("TTGO TELEMETRY UDP SERVER");

    // print current Cpu Speed
    tft->print("Current CPU Speed in Mhz: ");
    currentCpuFrequencyMhz = getCpuFrequencyMhz();
    tft->println(currentCpuFrequencyMhz);    

    // print previous clock
    tft->print("Previous clock: ");
    tft->println(rtc->formatDateTime(PCF_TIMEFORMAT_HMS));
}


void initialWiFiSetup() {
#include "config_ip.h"

    //connect to WiFi
    tft->print("Connecting to ");
    tft->println(SECRET_SSID);

    if (WiFi.config(staticIP, gateway, subnet, dns1, dns2) == false) {
        tft->println("Static IP configuration failed.");
    }

    WiFi.begin(SECRET_SSID, SECRET_PASS);

    while (WiFi.status() != WL_CONNECTED) {
        delay(500);
        tft->print(".");
    }
    tft->print(" CONNECTED!  ");
    tft->println(WiFi.localIP());
}


void initialClockSetup() {

    tft->print("NTP time sync... ");
    //init and get the time
    configTime(gmtOffset_sec, daylightOffset_sec, ntpServer);

    struct tm timeinfo;
    if (!getLocalTime(&timeinfo)) {
        tft->println("Failed, ignoring.");
        //delay(3000);
        //esp_restart();
        //while (1);
    } else {
      // Sync local time to external RTC
      rtc->syncToRtc();
    }

    // print current time
    tft->println(rtc->formatDateTime(PCF_TIMEFORMAT_HMS));

}


// Clock Speed Routines
void GoLowSpeed() {
    if (currentCpuFrequencyMhz != 20) {  
      currentCpuFrequencyMhz = 20;
      setCpuFrequencyMhz(currentCpuFrequencyMhz);
    }
}

void GoHighSpeed() {
    if (currentCpuFrequencyMhz != 240) {
      currentCpuFrequencyMhz = 240;
      setCpuFrequencyMhz(currentCpuFrequencyMhz);
    }
}

uint32_t GetCurrentCpuSpeedMhz() {
  return currentCpuFrequencyMhz;
}

