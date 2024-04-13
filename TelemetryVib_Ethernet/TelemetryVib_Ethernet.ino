/*
  Receives UDP packets with 0-255 intensity values to move vibration motors
  Based on the "WiFi UDP Send and Receive String" example
 */




#include <Ethernet.h>
#include <EthernetUdp.h>

//////////////////////////////
// Network Variables
//////////////////////////////
unsigned int localPort = 54671;     // local port to listen on
EthernetUDP Udp;                    // An EthernetUDP instance to let us send and receive packets over UDP

//////////////////////////////
// Telemetry Variables
//////////////////////////////
// when I have uses for more than 2 vib motors, I'll should change this to an array
const int motor1_pin = 3;           // Speed brakes
const int motor2_pin = 6;          // Flaps or In AoA, etc
const int seconds_threshold = 2;    // After this #seconds, stop the motors if no more packets are received
const int stop_motor = 0;

void setup() {
  // Enter a MAC address for your controller below.
  // This is the mac address printed on the sticker of my Ethernet Shield 2
  byte mac[] = {
    0xA8, 0x61, 0x0A, 0xAF, 0x19, 0x04
  };


  //Initialize serial and wait for port to open:
  Serial.begin(9600);
  int retries = 0;
  while (!Serial && retries++< 100) { ; } // wait for serial port to connect. Needed for native USB port only

  delay(900);
  Serial.println("\nStarting...");

  // Preparing the pins for output
  Serial.println("Preparing pins for output\n");  
  pinMode(motor1_pin, OUTPUT);
  pinMode(motor2_pin, OUTPUT);
  pinMode(LED_BUILTIN, OUTPUT); // This LED will be used to signal UDP-Server-Ready



  // start the Ethernet
    IPAddress local_ip(192, 168, 1, 249);
    IPAddress dns(216, 230, 147, 90);
    IPAddress gateway(192, 168, 1, 1);
    IPAddress subnet(255, 255, 255, 0);
    Ethernet.begin(mac, local_ip, dns, gateway, subnet);

  // Check for Ethernet hardware present
  if (Ethernet.hardwareStatus() == EthernetNoHardware) {
    Serial.println("Ethernet shield was not found.  Sorry, can't run without hardware. Connect ethernet shield and restart the board.");
    for (;;) delay(60000);
  }
  if (Ethernet.linkStatus() == LinkOFF) {
    Serial.println("Ethernet cable is not connected.  Connect ethernet cable and restart the board.");
    for (;;) delay(60000);
  }

    Serial.print("Ethernet IP address:");
    Serial.println(Ethernet.localIP());


  // start UDP
  Serial.println("Listening for packets via UDP (Ethernet)...");
  Udp.begin(localPort);
}

// call analogWrite only if needed
void vibrate_motor(int pin, int new_intensity, int &prev_intensity) {
  if (new_intensity != prev_intensity) {
    prev_intensity = new_intensity;
    analogWrite(pin, new_intensity);        
  }
}


void loop() {
    int packetSize, req_intensity;
    int lastMotor1_intensity = 0;
    int lastMotor2_intensity = 0;
    unsigned long packet_timestamp = 0;
    unsigned long timestamp = 0;
    bool motor_check = true;
    const unsigned int BUFF_SIZE = 64;
    byte buffer[BUFF_SIZE];

    // Signal start of operations
    digitalWrite(LED_BUILTIN, HIGH);
    
    for ( ; ; ) 
    {
      packetSize = Udp.parsePacket();
      timestamp = millis() / 1000; // interested in seconds

      if (packetSize) {
          Udp.read(buffer, BUFF_SIZE);
          packet_timestamp = timestamp;
          motor_check = true;      

          req_intensity = buffer[0];
          vibrate_motor(motor1_pin, req_intensity, lastMotor1_intensity);

          if (packetSize >= 2) {
            req_intensity = buffer[1];
            vibrate_motor(motor2_pin, req_intensity, lastMotor2_intensity);
          } 

      } else if (motor_check && (timestamp - packet_timestamp >= seconds_threshold)) { // check if motors continue to vibrate without receiving more requests
          // lets reduce a little the number of times to check for this
          motor_check = false;

          // stop motor1 if on
          vibrate_motor(motor1_pin, stop_motor, lastMotor1_intensity);          

          // stop motor2 if on
          vibrate_motor(motor2_pin, stop_motor, lastMotor2_intensity);
      }

    } // end-for

} // function-loop
