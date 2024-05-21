#include "config_types.h"
#include "config_hw.h"


// Common Brightness levels: REGULAR, LOW, OFF
#define REG_BRIGHTNESS 128
#define LOW_BRIGHTNESS 30
#define TURN_OFF_LCD 0

#define COLORSET_SIZE 5

class ScreenSaver
{
	const uint32_t colorset[COLORSET_SIZE] = {TFT_BLACK, TFT_YELLOW, TFT_DARKGREEN, TFT_GREEN, TFT_RED};

	public:
		ScreenSaver();
		void SetTTGO(TTGOClass *t);
		void AwakeScreen(ulong now, uint8_t newBrightness);
		void SaveScreen(ulong now, uint8_t newBrightness);
		void SetBrightness(ulong now, uint8_t level);
		void SetWaitTime(int seconds1, int seconds2);
		void SetBackground(ulong now, uint32_t color);
		void SetStripedBackground(ulong now, byte index); // an imaginary stripe of 4 possible colors that let you know if you're ok or not (within optimal AoA for example)

		uint8_t GetCurrentBrightness();
		
    private:
		uint32_t currentBackground;
		ulong highBrightnessTimeStamp;
		uint8_t lastBrightness;
		bool checkScreen1, checkScreen2;
		TTGOClass *ttgo;
		int wait1; // in seconds
		int wait2; // in seconds.  brightness set to zero after this
};

// index=0 => TFT_BLACK, index=1 => TFT_YELLOW, index=2 => TFT_DARKGREEN, index=3 => TFT_GREEN, index=4 => TFT_RED
void ScreenSaver::SetStripedBackground(ulong now, byte index) 
{	
	if (index>=COLORSET_SIZE) return;
	//redefine color as one of the four possible values
	uint32_t color = colorset[index];
	SetBackground(now, color);
}

void ScreenSaver::SetBackground(ulong now, uint32_t color)
{
	if (color != currentBackground) 
	{
		currentBackground = color;
		ttgo->tft->fillScreen(color);
		SetBrightness(now, REG_BRIGHTNESS);
	}
}

ScreenSaver::ScreenSaver() 
{
	highBrightnessTimeStamp = 0;
	lastBrightness = 0;
	ttgo = NULL;
	checkScreen1 = true;
	checkScreen2 = false;
	wait1 = 30;
	wait2 = 120;
	currentBackground = 0xFFFF - 1; //uncommon value 
}


void ScreenSaver::SetTTGO(TTGOClass *t)
{
	ttgo = t;
}

// the parameter now is in seconds, not milliseconds
void ScreenSaver::AwakeScreen(ulong now, uint8_t newBrightness)
{
	if (now == highBrightnessTimeStamp) return; // avoid repeated presses

	if (newBrightness != lastBrightness) ttgo->setBrightness(newBrightness);	
	highBrightnessTimeStamp = now;
	lastBrightness = newBrightness;

	checkScreen1 = true;
}

// the parameter now is in seconds, not milliseconds
void ScreenSaver::SaveScreen(ulong now, uint8_t newBrightness)
{
	if (GetCurrentBrightness() == 0) return; // screen is already off

	if (checkScreen1 && (now - highBrightnessTimeStamp > wait1)) {
    	if (newBrightness != lastBrightness) ttgo->setBrightness(newBrightness);
		lastBrightness = newBrightness;			
		checkScreen1 = false;
      	checkScreen2 = true;				
	} else if (checkScreen2 && (now - highBrightnessTimeStamp > wait2)) 
	{
		SetBackground(now, TFT_BLACK); // screen might be, yellow,green or red so make sure is black before turning it off
    	lastBrightness = TURN_OFF_LCD;
		ttgo->setBrightness(TURN_OFF_LCD);	
		checkScreen2 = false;				
	}
}

void ScreenSaver::SetBrightness(ulong now, uint8_t level)
{
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