using System;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Mirror.Tests
{
    public class MultiplexTest
    {

        Transport transport1;
        Transport transport2;
        MultiplexTransport transport;

        [SetUp]
        public void Setup()
        {
            transport1 = Substitute.For<Transport>();
            transport2 = Substitute.For<Transport>();

            GameObject gameObject = new GameObject();

            transport = gameObject.AddComponent<MultiplexTransport>();
            transport.transports = new[] { transport1, transport2 };

            transport.Awake();
        }

        [TearDown]
        public void TearDown()
        {
            GameObject.DestroyImmediate(transport.gameObject);
        }

        #region Client tests
        // A Test behaves as an ordinary method
        [Test]
        public void TestAvailable()
        {
            transport1.Available().Returns(true);
            transport2.Available().Returns(false);
            Assert.That(transport.Available());
        }

        // A Test behaves as an ordinary method
        [Test]
        public void TestNotAvailable()
        {
            transport1.Available().Returns(false);
            transport2.Available().Returns(false);
            Assert.That(transport.Available(), Is.False);
        }

        // A Test behaves as an ordinary method
        [Test]
        public void TestConnect()
        {
            transport1.Available().Returns(false);
            transport2.Available().Returns(true);
            transport.ClientConnect("some.server.com");

            transport1.DidNotReceive().ClientConnect(Arg.Any<string>());
            transport2.Received().ClientConnect("some.server.com");
        }

        // A Test behaves as an ordinary method
        [Test]
        public void TestConnectFirstUri()
        {
            Uri uri = new Uri("tcp://some.server.com");

            transport1.Available().Returns(true);
            transport2.Available().Returns(true);

            transport.ClientConnect(uri);
            transport1.Received().ClientConnect(uri);
            transport2.DidNotReceive().ClientConnect(uri);
        }


        // A Test behaves as an ordinary method
        [Test]
        public void TestConnectSecondUri()
        {
            Uri uri = new Uri("ws://some.server.com");

            transport1.Available().Returns(true);

            // first transport does not support websocket
            transport1
                .When(x => x.ClientConnect(uri))
                .Do(x => { throw new ArgumentException("Scheme not supported"); });

            transport2.Available().Returns(true);

            transport.ClientConnect(uri);
            transport2.Received().ClientConnect(uri);
        }

        [Test]
        public void TestConnected()
        {
            transport1.Available().Returns(true);
            transport.ClientConnect("some.server.com");

            transport1.ClientConnected().Returns(true);

            Assert.That(transport.ClientConnected());
        }

        [Test]
        public void TestDisconnect()
        {
            transport1.Available().Returns(true);
            transport.ClientConnect("some.server.com");

            transport.ClientDisconnect();

            transport1.Received().ClientDisconnect();
        }

        [Test]
        public void TestClientSend()
        {
            transport1.Available().Returns(true);
            transport.ClientConnect("some.server.com");

            byte[] data = { 1, 2, 3 };
            ArraySegment<byte> segment = new ArraySegment<byte>(data);

            transport.ClientSend(3, segment);

            transport1.Received().ClientSend(3, segment);
        }

        [Test]
        public void TestClient1Connected()
        {
            transport1.Available().Returns(true);
            transport2.Available().Returns(true);

            Action callback = Substitute.For<Action>();
            // find available
            transport.Awake();
            // set event and connect to give event to inner
            transport.onClientConnected = callback;
            transport.ClientConnect("localhost");
            transport1.onClientConnected.Invoke();
            callback.Received().Invoke();
        }

        [Test]
        public void TestClient2Connected()
        {
            transport1.Available().Returns(false);
            transport2.Available().Returns(true);

            Action callback = Substitute.For<Action>();
            // find available
            transport.Awake();
            // set event and connect to give event to inner
            transport.onClientConnected = callback;
            transport.ClientConnect("localhost");
            transport2.onClientConnected.Invoke();
            callback.Received().Invoke();
        }

        #endregion

        #region Server tests

        [Test]
        public void TestServerConnected()
        {
            byte[] data = { 1, 2, 3 };
            ArraySegment<byte> segment = new ArraySegment<byte>(data);


            // on connect, send a message back
            void SendMessage(int connectionId)
            {
                transport.ServerSend(connectionId, 5, segment);
            }

            // set event and Start to give event to inner
            transport.onServerConnected = SendMessage;
            transport.ServerStart();

            transport1.onServerConnected.Invoke(1);

            transport1.Received().ServerSend(1, 5, segment);
        }


        #endregion

    }
}
