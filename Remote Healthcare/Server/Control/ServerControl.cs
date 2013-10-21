using System.Xml.Linq;
using Kettler_X7_Lib.Objects;
using Server.Model;
using Server.View;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Server.Control
{
    ///<summary>
    ///The base class for the Control.
    ///</summary>
    class ServerControl
    {
        private ServerModel serverModel;
        private TcpListener tcpListener;
        static X509Certificate serverCertificate = null;

        public ServerControl()
        {
            serverModel = new ServerModel();
            serverModel.doctors = readDoctorFile("doctors.xml");
            tcpListener = new TcpListener(System.Net.IPAddress.Any, 31337);
            listenForClients();
        }

        ///<summary>
        ///Start listening for connecting TcpClients.
        ///</summary>
        public void listenForClients()
        {
            serverCertificate = X509Certificate.CreateFromCertFile("Healthcare.cer");
            tcpListener.Start();
            TcpClient tempClient;

            for (; ; )
            {
                try
                {
                    ServerView.writeToConsole("Listening...");
                    tempClient = tcpListener.AcceptTcpClient();
                    acceptAClient(tempClient);
                    ServerView.writeToConsole("Connected.");
                }
                catch (Exception)
                {
                }
               
                Thread.Sleep(10);
            }
        }

        ///<summary>
        ///Instantiate a Clientobject for a connected TcpClient.
        ///</summary>
        public void acceptAClient(TcpClient client)
        {
            new Client(client, this);
        }

        ///<summary>
        ///Forward client to servermodel's dictionary.
        ///</summary>
        public void addClientToList(Client client)
        {
            serverModel.writeBikeData(client, null);
        }

        ///<summary>
        ///Creates a Packet containing the online client list.
        ///</summary>
        public Packet getOnlineClientListPacket()
        {
            Packet pack = new Packet();
            pack.Flag = Packet.PacketFlag.PACKETFLAG_RESPONSE_USERLIST;

            List<String> list = new List<string>();
            foreach (Client client in serverModel.onlineClients)
            {
                if (!client.isDoctor)
                {
                    list.Add(client.userName);
                }
            }
            pack.Data = list;
            return pack;
        }

        ///<summary>
        ///Change desired online/offline status for a client.
        ///</summary>
        public void changeClientStatus(Client client, String status)
        {
            serverModel.changeClientStatus(client, status);
            try
            {
                foreach (Client tempClient in serverModel.onlineClients)
                {
                    if (tempClient.isDoctor)
                        tempClient.sendHandler(getOnlineClientListPacket());
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        ///<summary>
        ///Forward chatmessage received from client to servermodel.
        ///</summary>
        public void forwardMessage(Packet pack, Client client)
        {
            ChatMessage message = (ChatMessage)pack.Data;

            if (client.isDoctor)
            {
                foreach (Client tempClient in serverModel.onlineClients)
                {
                    if (tempClient.userName == message.Receiver || message.Receiver == "ALL")
                    {
                        tempClient.sendHandler(pack);
                    }
                }
            }
            else
            {
                foreach (Client tempClient in serverModel.onlineClients)
                {
                    if (tempClient.isDoctor)
                    {
                        tempClient.sendHandler(pack);
                    }
                }
            }
        }

        ///<summary>
        ///Forward bikecommand received from client to servermodel.
        ///</summary>
        public void forwardBikeCommand(Packet pack)
        {
            BikeControl bikeControl = (BikeControl)pack.Data;
            foreach (Client client in serverModel.onlineClients)
            {
                if (client.userName == bikeControl.Receiver)
                {
                    client.sendHandler(pack);
                }
            }
        }

        /// <summary>
        ///Forwards value packets to client.
        /// </summary>
        public void ForwardedValuePacket(Client client, Packet pack)
        {
            foreach (Client tempClient in serverModel.onlineClients)
            {
                Value value = (Value) pack.Data;
                value.Client = client.userName;
                pack.Data = value;
                if (tempClient.isDoctor)
                {
                    tempClient.sendHandler(pack);
                }
            }
        }

        /// <summary>
        ///Returns a sendable packet based on requestvalue
        /// </summary>
        public Packet requestSpecifiedClientData(RequestValue requestValue)
        {
            Packet pack = new Packet();
            pack.Flag = Packet.PacketFlag.PACKETFLAG_RESPONSE_VALUES;
            pack.Data = serverModel.readSpecifiedBikeData(requestValue.ClientName, requestValue.Start, requestValue.End);
            return pack;
        }

        ///<summary>
        ///Check the Handshake Request for login information and respond.
        /// </summary>
        public void handshakeResponse(Client client, Handshake shake)
        {
            ResponseHandshake response = new ResponseHandshake();
            response.Result = ResponseHandshake.ResultType.RESULTTYPE_ACCESSDENIED;

            switch (shake.ClientFlag)
            {
                case Kettler_X7_Lib.Objects.Client.ClientFlag.CLIENTFLAG_DOCTORAPP:
                    DoctorCredentials doc = serverModel.doctors.Find(x => x.username == shake.Username);

                    if (doc == null)
                        response.Result = ResponseHandshake.ResultType.RESULTTYPE_INVALIDCREDENTIALS;
                    else
                    {
                        if (doc.password == shake.Password)
                        {
                            if (serverModel.onlineClients.Contains(client))
                            {
                                response.Result = ResponseHandshake.ResultType.RESULTTYPE_ACCESSDENIED;
                            }
                            else
                            {
                                response.Result = ResponseHandshake.ResultType.RESULTTYPE_OK;
                            }
                        }
                        else
                        {
                            doc.logins.Add(new Log(DateTime.Now, false));
                            if (doc.logins.Count > 5)
                            {
                                bool timeOutAccount = true;
                                TimeSpan logSpan = new TimeSpan(1, 0, 0);
                                for (int i = doc.logins.Count; i <= doc.logins.Count - 5; i--)
                                {
                                    if (!doc.logins[i].accepted)
                                    {
                                        if (DateTime.Now.Subtract(doc.logins[i].login) >= logSpan)
                                            timeOutAccount = false;
                                    }
                                    else
                                        timeOutAccount = false;
                                }

                                if (timeOutAccount)
                                    response.Result =
                                        ResponseHandshake.ResultType.RESULTTYPE_ACCESSDENIED;
                                else
                                    response.Result = ResponseHandshake.ResultType.RESULTTYPE_INVALIDCREDENTIALS;
                            }
                            else
                                response.Result = ResponseHandshake.ResultType.RESULTTYPE_INVALIDCREDENTIALS;
                        }
                    }
                    break;
                case Kettler_X7_Lib.Objects.Client.ClientFlag.CLIENTFLAG_CUSTOMERAPP:
                    if (serverModel.onlineClients.Contains(client))
                    {
                        //is logged in already, send message back!
                        response.Result = ResponseHandshake.ResultType.RESULTTYPE_INVALIDCREDENTIALS;
                    }
                    else
                    {
                        // everything's alright!
                        addClientToList(client);
                        response.Result = ResponseHandshake.ResultType.RESULTTYPE_OK;
                    }
                    break;
                default:
                    break;
            }

            Packet responsePack = new Packet();
            responsePack.Data = response;
            responsePack.Flag = Packet.PacketFlag.PACKETFLAG_RESPONSE_HANDSHAKE;
            client.sendHandler(responsePack);

            if (response.Result == ResponseHandshake.ResultType.RESULTTYPE_OK)
                changeClientStatus(client, "online");
            else            
            {
                client = null;
            }
        }

        ///<summary>
        ///Forward client data to servermodel.
        ///</summary>
        public void writeToModel(Client client, Value values)
        {
            serverModel.writeBikeData(client, values);
        }

        ///<summary>
        ///Tell servermodel to finalize data after a client has disconnected.
        ///</summary>
        public void finalizeClient()
        {
            serverModel.finalizeData();
        }

        ///<summary>
        ///Loads the specified file and returns a list from that file.
        ///</summary>
        public List<DoctorCredentials> readDoctorFile(String filepath)
        {
            List<DoctorCredentials> list = new List<DoctorCredentials>();
            XDocument doc = XDocument.Load(filepath);
            var doctors = doc.Descendants("doctor");
            foreach (XElement xElement in doctors)
            {
                string name = xElement.Element("name").Value;
                string pw = xElement.Element("password").Value;
                list.Add(new DoctorCredentials(name, pw));
            }
            return list;
        }

        ///<summary>
        ///Returns the X509 Certificate.
        ///</summary>
        public X509Certificate getCertificate()
        {
            return serverCertificate;
        }
    }
}
