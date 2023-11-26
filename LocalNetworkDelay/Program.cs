using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;


namespace LocalNetworkDelay
{
    internal class Program
    {



        static void Main(string[] args)
        {
            byte[] datagramBytes = { 10, 20 };
            float average = (float)0.0;
            int samples = 10;
            string arduinoIP = "192.168.1.249";
            int remotePort = 54671;
            int localPort = 54672;

            // Create a UDP client and connect to the Arduino IP and port
            UdpClient client = new UdpClient(localPort);

            // Create a remote endpoint for program2
            IPAddress remoteIp = IPAddress.Parse(arduinoIP); // Change this to the Arduino IP
            IPEndPoint remoteEP = new IPEndPoint(remoteIp, remotePort);

            // Create a stopwatch to measure the network delay
            Stopwatch sw = new Stopwatch();


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
    }
}
