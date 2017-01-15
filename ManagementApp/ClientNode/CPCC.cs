﻿using ClientNode;
using ClientWindow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ClientWindow
{
    class CPCC
    { 
        private ClientWindow clientWindowHandler;
        private int controlPort;
        private TcpClient connection;
        string ip;
        BinaryReader reader;
        BinaryWriter writer;
        private Thread thread;
    
        public CPCC(ClientWindow clientWindowHandler)
        {
            this.clientWindowHandler = clientWindowHandler;
            this.readConfig();
            ip = "127.0.0.1";
        }

        public void connect()
        {
            connection = new TcpClient(ip, controlPort);
            thread = new Thread(connectionThread);
            thread.Start();
        }

        private void connectionThread()
        {
            writer = new BinaryWriter(connection.GetStream());
            reader = new BinaryReader(connection.GetStream());

            while (true)
            {
                string received_data = reader.ReadString();
                JMessage received_object = JMessage.Deserialize(received_data);
                if (received_object.Type == typeof(ControlPacket))
                {
                    ControlPacket packet = received_object.Value.ToObject<ControlPacket>();
                    Console.WriteLine(packet.virtualInterface);

                }
                else
                {
                    Console.WriteLine("Wrong control packet format");
                }
            }
        }

        public void sendRequest(string clientName)
        {
            ControlPacket packet = new ControlPacket(ControlProtocol.CALL_REQUEST, 0, clientName);
            string data = JMessage.Serialize(JMessage.FromValue(packet));
            writer.Write(data);

        }

        private void readConfig()
        {
            XDocument doc = XDocument.Load("config.xml");
            string value = doc.XPathSelectElement("//config[1]/controlPort").Value;
            bool res = int.TryParse(value, out controlPort);
        }
    }
}
