﻿

using System.Diagnostics;
using System.Numerics;
using System.Threading;
using TelemetryVibShaker.Properties;

namespace TelemetryVibShaker
{
    // This is a specialized class to manage AoA sound effects
    internal class AoA_SoundManager
    {
        public bool EnableEffect1 = true;
        public bool EnableEffect2 = true;

        public SoundEffectStatus Status = SoundEffectStatus.Invalid;

        public int AoA1;          // Optimal angle of attack.  Lower limit. (Sound Effect 1)
        public int AoA2;          // Optimal angle of attack.  Upper limit. (Sound Effect 1)
                                  // If newAoA > AoA2 then Sound Effect 2 should be heard.

        private MediaPlayer mp1;    // for the optimal AoA sound effectType
        private float lastVolume1; // cached volume for mp1


        private MediaPlayer mp2;    // for the non optimal sound effectType (above optimal AoA)
        private float lastVolume2; // cached volume for mp2

        // Support members to play the alarm after 30 minutes of flight in each new plane
        public bool PlayAlarm = false;
        private CancellationTokenSource? cancellationPlayAlarm;


        private float volumeAmplifier1;
        public float VolumeAmplifier1
        {
            get { return volumeAmplifier1; }
            set
            {
                if (value < 0.0f) volumeAmplifier1 = 0.0f;
                else if (value > 1.0f) volumeAmplifier1 = 1.0f;
                else volumeAmplifier1 = value;

                if (mp1 != null) mp1.Volume = lastVolume1 * volumeAmplifier1;
            }
        }

        private float volumeAmplifier2;
        public float VolumeAmplifier2
        {
            get { return volumeAmplifier2; }
            set
            {
                if (value < 0.0f) volumeAmplifier2 = 0.0f;
                else if (value > 1.0f) volumeAmplifier2 = 1.0f;
                else volumeAmplifier2 = value;

                if (mp2 != null) mp2.Volume = lastVolume2 * volumeAmplifier2;
            }
        }




        public AoA_SoundManager(String sound1, String sound2, float VolAmplifier1, float VolAmplifier2, int AudioDeviceIndex, bool PlayAlarm)
        {
            Status = SoundEffectStatus.NotPlaying;

            mp1 = new MediaPlayer(AudioDeviceIndex);
            mp1.Open(sound1);
            lastVolume1 = 0.0f;
            VolumeAmplifier1 = VolAmplifier1;
            mp1.PlayLooping();

            mp2 = new MediaPlayer(AudioDeviceIndex);
            mp2.Open(sound2);
            lastVolume2 = 0.0f;
            VolumeAmplifier2 = VolAmplifier2;
            mp2.PlayLooping();


            // Basically ignore the limits, because the unit type is not known at this point
            AoA1 = 360;
            AoA2 = 360;



            Status = SoundEffectStatus.Ready;
        }

        public void StopEffects()
        {
            if (mp1 != null)
            {
                mp1.Stop();
                mp1.Volume = 0.0f;
            }

            if (mp2 != null)
            {
                mp2.Stop();
                mp2.Volume = 0.0f;
            }

            lastVolume1 = 0.0f;
            lastVolume2 = 0.0f;

            Status = SoundEffectStatus.NotPlaying;
        }

        public bool EffectsAreActive()
        {
            return ((lastVolume1 > 0.0f) || (lastVolume2 > 0.0f));
        }

        // Set Volume to zero for both sound effects
        public void MuteEffects()
        {
            if (lastVolume1 > 0.0f)
            {
                lastVolume1 = 0.0f;
                mp1.Volume = 0.0f;
            }

            if (lastVolume2 > 0.0f)
            {
                lastVolume2 = 0.0f;
                mp2.Volume = 0.0f;
            }
            Status = SoundEffectStatus.Ready;
        }

        // This function is called on every frame, so avoid function calls, etc.
        // In this one case, I don't mind repeating 20 lines instead of creating a generic subrotine        
        public bool UpdateEffect(int newAoA)
        {
            bool volumeHasChanged = false;

            // Update sound effectType 1
            if (EnableEffect1)
            {
                if ((newAoA >= AoA1) && (newAoA <= AoA2))
                {
                    //float newVolume = lastVolume1 + 0.20f; // progresively start the sound effectType
                    //if (newVolume > 1.0f) newVolume = 1.0f;
                    float newVolume = 1.0f;

                    if (lastVolume1 != newVolume) // Avoid calling MediaPlayer.Volume if not needed 
                    {
                        lastVolume1 = newVolume;
                        newVolume *= volumeAmplifier1;
                        mp1.Volume = newVolume;
                        volumeHasChanged = true;
                    }
                }
                else // Mute Sound Effect 1
                {
                    if (lastVolume1 > 0.0f)  // Avoid calling MediaPlayer.Volume if not needed 
                    {
                        lastVolume1 = 0.0f;
                        mp1.Volume = 0.0f; // mp1.Dispatcher.BeginInvoke(new Action(() => { mp1.Volume = 0.0; }));
                        volumeHasChanged = true;
                    }

                }
            }

            // Update sound effectType 2
            if (EnableEffect2)
            {
                if (newAoA > AoA2)
                {
                    //float newVolume = lastVolume2 + 0.20f; // progresively start the sound effectType
                    //if (newVolume > 1.0f) newVolume = 1.0f;
                    float newVolume = 1.0f;

                    if (lastVolume2 != newVolume) // Avoid calling MediaPlayer.Volume if not needed 
                    {
                        lastVolume2 = newVolume;
                        newVolume *= volumeAmplifier2;
                        mp2.Volume = newVolume; //mp2.Dispatcher.BeginInvoke(new Action(() => { mp2.Volume = newVolume; }));
                        volumeHasChanged = true;
                    }
                }
                else // Mute Sound Effect 2
                {
                    if (lastVolume2 > 0.0f)  // Avoid calling MediaPlayer.Volume if not needed 
                    {
                        lastVolume2 = 0.0f;
                        mp2.Volume = 0.0f;  //mp2.Dispatcher.BeginInvoke(new Action(() => { mp2.Volume = 0.0; }));
                        volumeHasChanged = true;
                    }

                }
            }

            //Status = (lastVolume1 > 0.0f || lastVolume2 > 0.0f) ? SoundEffectStatus.Playing : SoundEffectStatus.Ready;
            if (newAoA > AoA2)
                Status = SoundEffectStatus.Playing2;
            else if (newAoA < AoA1)
                Status = SoundEffectStatus.Ready;
            else
                Status = SoundEffectStatus.Playing1;


            return volumeHasChanged;
        }


        public void ScheduleAlarm(bool aircraftFound)
        {
            if (cancellationPlayAlarm is null)
            {
                cancellationPlayAlarm = new CancellationTokenSource();
            }
            else
            {
                cancellationPlayAlarm.Cancel();
                cancellationPlayAlarm.Dispose();
                cancellationPlayAlarm = new CancellationTokenSource();
            }
            
            string soundEffectFileName = (aircraftFound ? Properties.Settings.Default.AircraftFoundSoundEffect : Properties.Settings.Default.AircraftNotFoundSoundEffect);
            MediaPlayer soundEffectPlayer = new MediaPlayer(mp1.DeviceIndex);
            soundEffectPlayer.Open(soundEffectFileName);
            soundEffectPlayer.Volume = volumeAmplifier1;
            soundEffectPlayer.Play();

            Task.Run(async () =>
            {
                MediaPlayer reminderSoundEffect = new MediaPlayer(mp1.DeviceIndex);
                reminderSoundEffect.Open(Properties.Settings.Default.HalfAnHourAlarmSoundEffect);
                try
                {
                    while (!cancellationPlayAlarm.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(30), cancellationPlayAlarm.Token);

                        if (!cancellationPlayAlarm.IsCancellationRequested && PlayAlarm)
                        {                            
                            reminderSoundEffect.Volume = volumeAmplifier1;
                            reminderSoundEffect.Play();
                        }
                        //Thread.Sleep(2000); // Give a little time before scheduling again
                    }
                }
                catch (TaskCanceledException)
                {
                    // we can ignore this
                }
                finally
                {
                    // ignore for now
                }
            });
        }


    }
}
