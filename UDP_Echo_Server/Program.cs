/* Temporary program to test sending data via UDP
 * This program is not needed for TelemetryVibShaker and I am not using/maintaining this anymore
 */

using System;
using System.Net.Sockets;
using System.Net;
using System.Text;


namespace UDP_Echo_Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Create a UDP socket
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // Bind the socket to the port 54671
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, 54671);
            server.Bind(localEP);

            // Display a message to indicate the server is started
            Console.WriteLine("UDP echo server is started on port 59671");

            // Create a buffer to store the received data
            byte[] buffer = new byte[1024];

            // Loop forever to receive and echo data
            while (true)
            {
                // Receive data from any remote endpoint
                EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                int received = server.ReceiveFrom(buffer, ref remoteEP);

                for (int i = 0; i < received; i++)
                {
                    byte d = buffer[i];
                    Console.Write($"[{d}]");
                }
                Console.WriteLine();

                // Echo the data back to the remote endpoint
                server.SendTo(buffer, 0, received, SocketFlags.None, remoteEP);
                Console.WriteLine("Echoed {0} bytes to {1}", received, remoteEP);

            }
        }
    }
}
