This program, listens on the Ethernet network for UDP Packets with two bytes.

- The first byte is the intensity for the first motor.
- The second byte is the intensity for the second motor.
- The motors can be small cellphone vibrator motors sticked to your throttle or joystick.
- I use the first motor to generate vibration effects when the speedbrakes are extended and the second motor to generate vibration effects when the flaps are extended.

- ToDo1:  Add diagram and pictures
- Todo2:  Integrate the two ArduinoR4WiFi programs into 1 that listens in Ethernet if it is connected otherwise, WiFi
- Todo3:  Maybe use the Led Matrix to show some status (right now is covered by the Ethernet Shield)
- Todo4:  Clean the code some more
- Todo5:  Add more motors?  I don't need more effects, do you? The reason I only programmed two motors is because I only have two controls where I can feel them (throttle and joystick).  For bass shakers there are other software already available and more complete.
