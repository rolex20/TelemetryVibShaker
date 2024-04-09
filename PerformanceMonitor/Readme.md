Monitors CPU Utilization to compare Performance, Efficiency and HyperThreading (HT) cores and GPU Utilization visually.  Specifically for the 20 CPUs in my Intel 12700K and my Asus TUF RTX 4090.   

Observations used to decide between Power Mode=Balance, Power Mode=High Performance or PowerMode=High Performance + "Processor performance core parking min cores" & "Processor performance core parking min cores for Processor Power Efficiency Class 1".

Those configurations were necessary to fix the stuttering with DCS World 2.9 according to:

[Disabling Core Parking in windows fixed my stuttering in menu and game - Multi-Threading Bug Reports (Temp) - ED Forums (dcs.world)](https://forum.dcs.world/topic/335866-disabling-core-parking-in-windows-fixed-my-stuttering-in-menu-and-game/?_fromLogin=1) and

[MT freezes even in main menu - Page 19 - Multi-Threading Bug Reports (Temp) - ED Forums (dcs.world)](https://forum.dcs.world/topic/328792-mt-freezes-even-in-main-menu/page/19/#comment-5318289)

The program is specific for my 12700K and Nvidia GPUs but with a little more time, it can be made generic for any other processor in case someone finds this useful.

The advantage of this program is that it allows to compare graphically and side by side Performance with HT with Efficiency to know what the game is doing and more or less how the operating system is assigning the CPUs.   It is also very light.

I found that High Performance with Core Parking is only useful for DCS World while 2.9 still has MT issues.  This configuration is not really useful for MSFS2020.

This is how it looks, notice how you can easily compare Core vs HT assignment and Efficiency core assignment as well:


![image](https://github.com/rolex20/TelemetryVibShaker/assets/62082564/c4fab194-d1ff-4aa3-b43a-ae6b8de9f2fb)



It includes a light web server on port 8080 to move the program to a different location in the screen.  I use this from my smartphone when I am in VR and it is difficult to move the program when the resolution change in my second monitor.

