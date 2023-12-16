using System.Net.Sockets;
namespace TelemetryVibShaker
{
    internal class VibrationMotor
    {
        private byte [] datagram;
        private EffectDefinition[] effect;
        private UdpClient udpSender;
        private string serverIP;
        private int serverPort;
        public VibrationMotor(string IP, int Port, EffectDefinition[] Effect)
        {
            effect = Effect;
            udpSender = null;
            serverIP = IP;
            serverPort = Port;
            datagram = new byte[effect.Length];
        }

        public void Connect()
        {
            UdpClient udpSender = new UdpClient();
            udpSender.Connect(serverIP, serverPort);
        }

        public void ProcessEffect(TelemetryData telData)
        {
            for (int i = 0; i < effect.Length; i++)
                datagram[i] = (byte)effect[i].CalculateOutput(telData);

            // send the message
            // the destination is defined by the call to .Connect()
            udpSender.BeginSend(datagram, datagram.Length, new AsyncCallback(SendCallback), udpSender);
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
