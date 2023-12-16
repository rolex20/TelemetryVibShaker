using System.Net.Sockets;
namespace TelemetryVibShaker
{
    internal class MotorController
    {
        private byte [] datagram;
        private EffectDefinition[] effect;
        private UdpClient udpSender;
        private string serverIP;
        private int serverPort;
        public bool Enabled;
        public MotorController(string IP, int Port, EffectDefinition[] Effect)
        {
            effect = Effect;
            udpSender = null;
            serverIP = IP;
            serverPort = Port;
            datagram = new byte[effect.Length];
            Enabled = true;
        }

        public void Connect()
        {
            UdpClient udpSender = new UdpClient();
            udpSender.Connect(serverIP, serverPort);
        }

        /// <summary>
        /// Process telemetry and calculates the required outputs to be sent to the UPD microcontrollers powering the vibration motors.
        /// Make sure you have called Connect() before any call to ProcessEffect
        /// </summary>
        /// <param name="telData">Latest telemetry data</param>
        public void ProcessEffect(TelemetryData telData)
        {
            if (Enabled)
            {
                for (int i = 0; i < effect.Length; i++)
                    datagram[i] = (byte)effect[i].CalculateOutput(telData);

                // send the message
                // the destination is defined by the call to .Connect()
                udpSender.BeginSend(datagram, datagram.Length, new AsyncCallback(SendCallback), udpSender);
            }
        }

        public static void SendCallback(IAsyncResult ar)
        {
            UdpClient u = (UdpClient)ar.AsyncState;
            u.EndSend(ar);
        }

        public void ChangeAoARange(int AoA1, int AoA2)
        {
            for(int i=0; i< effect.Length;i++)
                effect[i].ChangeAoARange(AoA1, AoA2);
        }

        


    }
}
