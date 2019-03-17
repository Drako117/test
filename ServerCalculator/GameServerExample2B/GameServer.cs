using System;
using System.Net;
using System.Diagnostics;
using System.Collections.Generic;

namespace GameServerExample2B
{
    public class GameServer
    {

        private delegate void GameCommand(byte[] data, EndPoint sender);

        private Dictionary<byte, GameCommand> commandsTable;

        private Dictionary<EndPoint, GameClient> clientsTable;

        public Dictionary<EndPoint, GameClient> clientsTableProp { get { return clientsTable; } }

        private IGameTransport transport;

        public bool isError;

        public uint NumClients
        {
            get
            {
                return (uint)clientsTable.Count;
            }
        }

        public GameServer(IGameTransport gameTransport)
        {
   
            transport = gameTransport;
            isError = false;
            clientsTable = new Dictionary<EndPoint, GameClient>();
            commandsTable = new Dictionary<byte, GameCommand>();
            commandsTable[0] = Addiction;
            commandsTable[1] = Subtraction;
            commandsTable[2] = Moltiplication;
            commandsTable[3] = Division;
        }

        private void Addiction(byte[] data, EndPoint sender)
        {
            GameClient client = new GameClient(this, sender);
            clientsTable[sender] = client;

            float A = BitConverter.ToSingle(data, 1);
            float B = BitConverter.ToSingle(data, 5);

            float amount = A + B;

            Packet totPacket = new Packet(0, amount);
            clientsTable[sender].Enqueue(totPacket);
        }

        private void Subtraction(byte[] data, EndPoint sender)
        {
            GameClient client = new GameClient(this, sender);
            clientsTable[sender] = client;

            float A = BitConverter.ToSingle(data, 1);
            float B = BitConverter.ToSingle(data, 5);

            float amount = A - B;

            Packet totPacket = new Packet(1, amount);
            clientsTable[sender].Enqueue(totPacket);
        }

        private void Moltiplication(byte[] data, EndPoint sender)
        {
            GameClient client = new GameClient(this, sender);
            clientsTable[sender] = client;

            float A = BitConverter.ToSingle(data, 1);
            float B = BitConverter.ToSingle(data, 5);

            float amount = A * B;

            Packet totPacket = new Packet(2, amount);
            clientsTable[sender].Enqueue(totPacket);
        }

        private void Division(byte[] data, EndPoint sender)
        {

            GameClient client = new GameClient(this, sender);
            clientsTable[sender] = client;

            float amount = 0;

            float A = BitConverter.ToSingle(data, 1);
            float B = BitConverter.ToSingle(data, 5);

            if(B <= 0 || A < 0)
            {
                isError = true;
            }
            else
            {
                isError = false;
                amount = A / B;
            }

            Packet totPacket = new Packet(3, amount);
            clientsTable[sender].Enqueue(totPacket);
           
        }

        public void Run()
        {

            Console.WriteLine("server started");
            while (true)
            {
                SingleStep();
            }
        }

        private float currentNow;
        public float Now
        {
            get
            {
                return currentNow;
            }
        }

        public void SingleStep()
        {
            EndPoint sender = transport.CreateEndPoint();
            byte[] data = transport.Recv(256, ref sender);
            if (data != null)
            {
                byte gameCommand = data[0];
                if (commandsTable.ContainsKey(gameCommand))
                {
                    commandsTable[gameCommand](data, sender);
                }
            }

            foreach (GameClient client in clientsTable.Values)
            {
                client.Process();
            }
        }


        public bool Send(Packet packet, EndPoint endPoint)
        {
            return transport.Send(packet.GetData(), endPoint);
        }
    }
}
