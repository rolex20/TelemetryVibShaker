#include "config_types.h"
#include "config_hw.h"

class ScreenSaver
{
		public:
			ScreenSaver();
			void SetTTGO(TTGOClass *t);
			void AwakeScreen(ulong now, uint8_t newBrightness);
			void SaveScreen(ulong now, uint8_t newBrightness);
      void SetBrightness(uint8_t level);
      void SetWaitTime(int seconds1, int seconds2);
      uint8_t GetCurrentBrightness();
		
    private:
		ulong highBrightnessTimeStamp;
		uint8_t lastBrightness;
		bool checkScreen1, checkScreen2;
		TTGOClass *ttgo;
    int wait1; // in seconds
    int wait2; // in seconds.  brightness set to zero after this
};

ScreenSaver::ScreenSaver() 
{
	highBrightnessTimeStamp = 0;
	lastBrightness = 0;
	ttgo = NULL;
	checkScreen1 = true;
  checkScreen2 = false;
  wait1 = 30;
  wait2 = 120;
}


void ScreenSaver::SetTTGO(TTGOClass *t)
{
	ttgo = t;
}

// the parameter now is in seconds, not milliseconds
void ScreenSaver::AwakeScreen(ulong now, uint8_t newBrightness)
{
      if (newBrightness != lastBrightness) ttgo->setBrightness(newBrightness);	
			highBrightnessTimeStamp = now;
			lastBrightness = newBrightness;
			
			checkScreen1 = true;
}

// the parameter now is in seconds, not milliseconds
void ScreenSaver::SaveScreen(ulong now, uint8_t newBrightness)
{
	if (checkScreen1 && (now - highBrightnessTimeStamp > wait1)) {
      if (newBrightness != lastBrightness) ttgo->setBrightness(newBrightness);
      lastBrightness = newBrightness;			
			checkScreen1 = false;
      checkScreen2 = true;				
	}	else if (checkScreen2 && (now - highBrightnessTimeStamp > wait2)) {
      lastBrightness = 0;
			ttgo->setBrightness(0);	
			checkScreen2 = false;				
	}
}

void ScreenSaver::SetBrightness(uint8_t level)
{
  unsigned long now = millis() / 1000;
  AwakeScreen(now, level);
}

void ScreenSaver::SetWaitTime(int seconds1, int seconds2) 
{
  wait1 = seconds1;
  wait2 = seconds2;
}

uint8_t ScreenSaver::GetCurrentBrightness() 
{
  return lastBrightness;
}