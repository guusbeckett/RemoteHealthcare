using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Server.Control
{
    class Client
    {
        private Thread listenThread;
        private TcpClient tcpClient;
        private SslStream clientStream;
        public String userName { get; set; }
        private String password;
        private ServerControl serverControl;
        public bool isDoctor { get; set; }

        ///<summary>
        ///Standard constructor for the Client class, requiring a TCPClient and a ServerControl.
        ///</summary>
        public Client(TcpClient client, ServerControl sControl)
        {
            isDoctor = false;
            serverControl = sControl;
            tcpClient = client;
            listenThread = new Thread(new ThreadStart(handler));
            listenThread.Start();
        }

        ///<summary>
        ///Forwards incoming clientdata to PacketHandler.
        ///</summary>
        public void handler()
        {
            clientStream = new SslStream(tcpClient.GetStream(), true);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            Kettler_X7_Lib.Objects.Packet pack = null;

            X509Certificate serverCertificate = serverControl.getCertificate();

            try
            {
                clientStream.AuthenticateAsServer(serverCertificate,false, SslProtocols.Tls, false);
            } 
            catch (Exception)
            {
                Console.WriteLine("Authentication failed");
                disconnect();
            }

            for (; ; )
            {
                try
                {
                    pack = formatter.Deserialize(clientStream) as Kettler_X7_Lib.Objects.Packet;
                    PacketHandler.getPacket(serverControl, this, pack);
                }
                catch 
                {
                    disconnect();
                }
                
                Thread.Sleep(10);
            }
        }

        ///<summary>
        ///Disconnects the online users and closes all streams.
        ///</summary>
        private void disconnect()
        {
            serverControl.changeClientStatus(this, "offline");
            serverControl.finalizeClient();
            try
            {
                clientStream.Close();
                tcpClient.Close();
                listenThread.Abort();
            }
            catch
            {
            }
        }

        ///<summary>
        ///Handles outgoing clientdata.
        ///</summary>
        public void sendHandler(Kettler_X7_Lib.Objects.Packet pack)
        {
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            try
            {
                formatter.Serialize(clientStream, pack);
            }
            catch (Exception)
            { }
        }

        ///<summary>
        ///Sets client-credentials.
        ///</summary>
        public void setUsernamePassword(String userName, String password)
        {
            this.userName = userName;
            this.password = password;
        }

        ///<summary>
        ///Override of the HashCode comparing.
        ///</summary>
        public override int GetHashCode()
        {
            return userName.GetHashCode();    
        }

        ///<summary>
        ///Override of the Equals method.
        ///</summary>
        public override bool Equals(object obj)
        {
            if (obj is Client && (obj == this || ((Client) obj).userName == userName))
                return true;
            return false;
        }
    }
}
