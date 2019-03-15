using System;
using NUnit.Framework;

namespace GameServerExample2B.Test
{
    public class TestGameServer
    {
        private FakeTransport transport;
        private FakeClock clock;
        private GameServer server;
        private GameObject gameObj;
        private GameClient client;


        [SetUp]
        public void SetupTests()
        {
            transport = new FakeTransport();
            clock = new FakeClock();
            server = new GameServer(transport, clock);
        }

        [Test]
        public void TestZeroNow()
        {
            Assert.That(server.Now, Is.EqualTo(0));
        }

        [Test]
        public void TestClientsOnStart()
        {
            Assert.That(server.NumClients, Is.EqualTo(0));
        }

        [Test]
        public void TestGameObjectsOnStart()
        {
            Assert.That(server.NumGameObjects, Is.EqualTo(0));
        }

        [Test]
        public void TestJoinNumOfClients()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(1));
        }

        [Test]
        public void TestJoinNumOfGameObjects()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            Assert.That(server.NumGameObjects, Is.EqualTo(1));
        }

        [Test]
        public void TestWelcomeAfterJoin()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            FakeData welcome = transport.ClientDequeue();
            Assert.That(welcome.data[0], Is.EqualTo(1));
        }

        [Test]
        public void TestSpawnAvatarAfterJoin()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientDequeue();
            Assert.That(() => transport.ClientDequeue(), Throws.InstanceOf<FakeQueueEmpty>());
        }

        [Test]
        public void TestJoinSameClient()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(1));
        }

        [Test]
        public void TestJoinSameAddressClient()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "tester", 1);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinSameAddressAvatars()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "tester", 1);
            server.SingleStep();
            Assert.That(server.NumGameObjects, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinTwoClientsSamePort()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "foobar", 0);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinTwoClientsWelcome()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();

            Assert.That(transport.ClientQueueCount, Is.EqualTo(5));

            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("foobar"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("foobar"));
        }

        [Test]
        public void TestEvilUpdate()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            uint avatarId = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);
            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();

            Packet move = new Packet(3, avatarId, 1.0f, 1.0f, 2.0f);
            transport.ClientEnqueue(move, "foobar", 1);
            server.SingleStep();

            GameObject GObj = server.GetGameObj(avatarId);

            Assert.That(GObj.X, Is.EqualTo(0));
            Assert.That(GObj.Y, Is.EqualTo(0));
            Assert.That(GObj.Z, Is.EqualTo(0));
        }

        [Test]
        public void TestPositionPacket()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "pippo", 0);
            server.SingleStep();
            uint avatarId = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);

            Packet Move = new Packet(3, avatarId, 1.0f, 1.0f, 1.0f);

            transport.ClientEnqueue(Move, "pippo", 0);
            server.SingleStep();

            GameObject GObj = server.GetGameObj(avatarId);

            Assert.That(GObj.X, Is.EqualTo(1));
            Assert.That(GObj.Y, Is.EqualTo(1));
            Assert.That(GObj.Z, Is.EqualTo(1));
        }

        [Test]
        public void TestAddAClientInServer()
        {
            Packet packet = new Packet(0);

            transport.ClientEnqueue(packet, "pippo", 0);

            server.SingleStep();

            Assert.That(server.clientsTableProp.Count, Is.EqualTo(1));
        }

        [Test]
        public void TestMalus()
        {
            Packet packet = new Packet(0);

            transport.ClientEnqueue(packet, "pippo", 0);
            server.SingleStep();

            transport.ClientEnqueue(packet, "pippo", 0);
            server.SingleStep();

            transport.ClientDequeue();

            Assert.That(server.clientsTableProp[transport.ClientDequeue().endPoint].Malus, Is.EqualTo(1));
        }

        [Test]
        public void TestAck()
        {
            Packet packet = new Packet(0);

            transport.ClientEnqueue(packet, "pippo", 0);
            server.SingleStep();

            Console.WriteLine(server.clientsTableProp[transport.ClientDequeue().endPoint].AckProp.Count);

            Assert.That(server.clientsTableProp[transport.ClientDequeue().endPoint].AckProp.Count, Is.Not.EqualTo(2));
        }


    }
}
