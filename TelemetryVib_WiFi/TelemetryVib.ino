/*
  Receives UDP packets with 0-255 intensity values to move vibration motors
  Based on the "WiFi UDP Send and Receive String" example
 */


#include <WiFiS3.h>

//////////////////////////////
// Network Variables
//////////////////////////////
int status = WL_IDLE_STATUS;
#include "arduino_secrets.h" 
/////// enter your sensitive data in the Secret tab/arduino_secrets.h
char ssid[] = SECRET_SSID;        // your network SSID (name)
char pass[] = SECRET_PASS;        // your network password (use for WPA, or use as key for WEP)
WiFiUDP Udp;                     

//int keyIndex = 0;            // your network key index number (needed only for WEP)

//////////////////////////////
// Telemetry Variables
//////////////////////////////
unsigned int localPort = 54671;     // local port to listen on
byte packetBuffer[256];             //buffer to hold incoming packet

// when I have uses for more than 2 vib motors, I'll should change this to an array
const int motor1_pin = 3;           // Speed brakes
const int motor2_pin = 6;          // In AoA
const int seconds_threshold = 2;    // After this #seconds, stop the motors if no more packets are received
const int stop_motor = 0;

void setup() {



  //Initialize serial and wait for port to open:
  Serial.begin(9600);
  while (!Serial) {
    ; // wait for serial port to connect. Needed for native USB port only
  }
  delay(900);
  Serial.println("\nStarting...");

  // Preparing the pins for output
  Serial.println("Preparing pins for output\n");  
  pinMode(motor1_pin, OUTPUT);
  pinMode(motor2_pin, OUTPUT);
  pinMode(LED_BUILTIN, OUTPUT); // This LED will be used to signal UDP-Server-Ready


  // check for the WiFi module:
  Serial.println("Initializing communication with WiFi module and checking firmware version\n");
  if (WiFi.status() == WL_NO_MODULE) {
    Serial.println("Communication with WiFi module failed!");
    // don't continue
    while (true);
  }

  String fv = WiFi.firmwareVersion();
  if (fv < WIFI_FIRMWARE_LATEST_VERSION) {
    Serial.println("Please upgrade the firmware");
  }


  // Attempt to connect to WiFi network:
  Serial.print("Attempting to connect to SSID:"); 
  Serial.println(ssid); 
  while (status != WL_CONNECTED) {

    // Define the static IP address, gateway and subnet mask
    // I need a fixed IP address in my locak network
    IPAddress local_ip(192, 168, 1, 249);
    IPAddress dns_server(216, 230, 147, 90);
    IPAddress gateway(192, 168, 1, 1);
    IPAddress subnet(255, 255, 255, 0);

    WiFi.config(local_ip,  dns_server,  gateway, subnet);

    // Connect to WPA/WPA2 network. Change this line if using open or WEP network:
    status = WiFi.begin(ssid, pass);

    // wait 10 seconds for connection:
    if (status != WL_CONNECTED) {
       Serial.print(".");
      delay(10000);
    }
  }
  Serial.println("\nConnected to WiFi");
  printWifiStatus();

  Serial.println("\nUDP WiFi Server: Listening...");
  // if you get a connection, report back via serial:
  Udp.begin(localPort);
}

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
    unsigned long timestamp;
    bool motor_check = true;

    // Signal start of operations
    digitalWrite(LED_BUILTIN, HIGH);


    for ( ; ; ) {
      packetSize = Udp.parsePacket();
      timestamp = millis() / 1000; // interested in seconds

      if (packetSize) {
          Udp.read(packetBuffer, 255);
          packet_timestamp = timestamp;
          motor_check = true;      

          req_intensity = packetBuffer[0];
          vibrate_motor(motor1_pin, req_intensity, lastMotor1_intensity);

          if (packetSize >= 2) {
            req_intensity = packetBuffer[1];
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


void printWifiStatus() {
  // print the SSID of the network you're attached to:
  Serial.print("SSID: ");
  Serial.println(WiFi.SSID());

  // print your board's IP address:
  IPAddress ip = WiFi.localIP();
  Serial.print("IP Address: ");
  Serial.println(ip);

  // print the received signal strength:
  long rssi = WiFi.RSSI();
  Serial.print("signal strength (RSSI):");
  Serial.print(rssi);
  Serial.println(" dBm");
}
