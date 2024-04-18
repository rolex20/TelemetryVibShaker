/* Only used during testing to see if the delay would be too much with aruinos and the lily go t-watch
 * The delay with Arduino R4 WiFi was 20ms
 * The delay with Arduino R4 using the Ethernet switch was less than 10ms
 * The delay with LilyGo T-Watch was less than 10ms (WiFi only)
 * The delay with two of my PC's was less than 1ms
 * All measures taken in my local network (1Gbps ethernet Switch)
 * All measures taken with this program
 * I am not using/needing this program anymore.
 * */

using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;


namespace LocalNetworkDelay
{
    internal class Program
    {

        private static readonly string DefaultServerAddress = "192.168.1.248";

        static void Main(string[] args)
        {
            byte[] datagramBytes = { 10, 20 };
            float average = (float)0.0;
            int samples = 10;
            int remotePort = 54671;
            int localPort = 54672;

            string arduinoIP = GetServerAddress(args);

            // Create a UDP client and connect to the Arduino IP and port
            UdpClient client = new UdpClient(localPort);

            // Create a remote endpoint for program2
            IPAddress remoteIp = IPAddress.Parse(arduinoIP); // Change this to the Arduino IP
            IPEndPoint remoteEP = new IPEndPoint(remoteIp, remotePort);

            // Create a stopwatch to measure the network delay
            Stopwatch sw = new Stopwatch();


            Console.WriteLine("Destination Address: " + arduinoIP + "\n");

            int passes = 0;
            for (int i = 1; i <= samples + 1; i++)
            {
                sw.Restart();
                client.Send(datagramBytes, datagramBytes.Length, remoteEP);

                byte[] receiveBytes = client.Receive(ref remoteEP);
                sw.Stop();

                long roundTripTime = sw.ElapsedMilliseconds;

                // Display the results
                Console.WriteLine("Round-trip time: {0} ms", roundTripTime);
                Console.WriteLine("Point to point delay: {0} ms", roundTripTime / 2);
                Console.WriteLine("Byte[0]={0}, Byte[1]={1}\n", receiveBytes[0], receiveBytes[1]);

                if (i != 1) { // the first sample is very slow for some reason, ignoring this one
                    average += roundTripTime / (float)2.0;
                    passes++;
                }
            }
            // Close the client
            client.Close();

            average = average / passes;
            Console.WriteLine("Average point to point delay for {0} samples: {1}", passes, average);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();


        }

        private static string GetServerAddress(string[] args)
        {
            if (args.Length == 0)
            {
                return DefaultServerAddress;
            }

            return args[0];
        }
    }
}
