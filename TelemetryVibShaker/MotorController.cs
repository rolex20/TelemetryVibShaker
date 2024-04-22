using System.Net.Sockets;
namespace TelemetryVibShaker
{
    internal class MotorController
    {
        private byte [] datagram;
        private EffectDefinition[] effect;
        private UdpClient? udpSender;
        private string serverIP;
        private int serverPort;
        public bool Enabled;
        public string Name;
        //private bool connected;
        public MotorController(string Name, string IP, int Port, EffectDefinition[] Effect, bool Enabled)
        {
            effect = Effect;
            this.Name = Name;
            udpSender = null;
            serverIP = IP;
            serverPort = Port;
            datagram = new byte[effect.Length];
            this.Enabled = Enabled;
            //connected = false;
        }

        public void Connect()
        {
            udpSender = new UdpClient();
            udpSender.Connect(serverIP, serverPort);
            //connected = true;
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
                // for now, lets ignore the connected field.  anyways this fails if not connected
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                udpSender.BeginSend(datagram, datagram.Length, new AsyncCallback(SendCallback), udpSender);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
        }

        public static void SendCallback(IAsyncResult ar)
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            UdpClient u = (UdpClient)ar.AsyncState;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            u.EndSend(ar);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

        public void ChangeAoARange(int AoA1, int AoA2)
        {
            for(int i=0; i< effect.Length;i++)
                effect[i].ChangeAoARange(AoA1, AoA2); //ChangeAoARange() will check if this is an AoA type of effect
        }

        


    }
}
