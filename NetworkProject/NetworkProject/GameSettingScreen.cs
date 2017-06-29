using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetworkProject
{
    public enum lang
    {
        def, gameStarted, numberOfPlayers, Mylocation
    }
    public partial class GameSettingScreen : Form
    {
        Socket currentSocket;
        List<Client> clients = new List<Client>();
        Dictionary<Point, int> snakes = new Dictionary<Point, int>();
        Dictionary<Point, int> ladders = new Dictionary<Point, int>();



        //Form Constructor
        public GameSettingScreen(bool isServer, IPAddress IP)
        {
            InitializeComponent();
            if (!isServer)
            {
                //joined as client (initialize the TCP client socket, connect to Servers IP and wait for the server to start the game
                btnStartGame.Enabled = false;
                button1.Enabled = false;
                JoinServer(IP);
                Thread t = new Thread(RecieveDataFromServer);
                t.Start();
            }
            else
            {
                //joined as server (start the TCP server socket, start UDP server socket to broadcasting the servers IP and finally accept client sockets using the TCP socket
                InitializeServer();
                tmrBroadCastIP.Start();
                Thread tt = new Thread(   AcceptPlayers);
                tt.Start();
            }
        }




        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////CLIENT///////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //Client Function implementation

        void JoinServer(IPAddress IP)
        {
            //write code to initialize currentSocket to be client socket and connect to server IP  
            //Console.WriteLine(IP.ToString()); 
            currentSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


            try
            {
                currentSocket.Connect(new IPEndPoint(IP, 8000));

            }
            catch (Exception exx)
            {
                Console.WriteLine(exx.Message);
            }
            Console.WriteLine("Joined to server " + IP.ToString());


        }
        int numberOfPlayers = -1;
        bool recieving = true;
        void RecieveDataFromServer()
        {
            //this function in different thread as it will halt the application during recieving from server


            //write code to recieve (numberOfPlayers)

            while (recieving)
            {
                byte[] arr = new byte[2014];
                currentSocket.Receive(arr);
                var message = Encoding.ASCII.GetString(arr);
                Console.WriteLine(message);

                ParseMessage(message);

            }
            GenerateSnakesAndLadders();
            char[,] board = GenerateBoard(snakes, ladders);
            GamePlayingScreen gpc = new GamePlayingScreen(board, snakes, ladders, null, numberOfPlayers, Rank, currentSocket, false);

            this.Visible = false;
            gpc.ShowDialog();
            Console.WriteLine("opening gpc");
        }



        int Rank = -1;


        void ParseMessage(string message)
        {
            var tockens = message.Split(new char[] { '#' });
            if (tockens[0] == "numberOfPlayers")
            {
                Console.WriteLine("numberofPlayers" + tockens[1]);
                numberOfPlayers = int.Parse(tockens[1]);
                Rank = int.Parse(tockens[2]);
                Console.WriteLine("my Rank " + tockens[2]);

            }

            if (tockens[0] == "GameStarted")
            {
                Console.WriteLine("GameStarted");
                recieving = false;

            }



        }



        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////SERVER///////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //Server Functions implementation
        void InitializeServer()
        {

            IPEndPoint ipEnd = new IPEndPoint(IPAddress.Any, 8000);
            //8000 is any unused port
            //127.0.0.1 refers to the local computer (localhost)

            currentSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            currentSocket.Bind(ipEnd);

            currentSocket.Listen(100);


               IPAddress[] ipv4Addresses = Array.FindAll(
                Dns.GetHostEntry(Dns.GetHostName()).AddressList,
              a => a.AddressFamily == AddressFamily.InterNetwork);

            Client serverClient = new Client();
            serverClient.player = currentSocket;
            serverClient.Rank = 0;
            serverClient.IP = ipv4Addresses[ipv4Addresses.Length - 1].ToString();

            clients.Add(serverClient);
            listBox1.Items.Add(serverClient.ToString());

            //write code to initialize currentSocket to be server socket
            //add the server to clientList
            //set the servers rank = 0;
        }

        void BroadCastIP()
        {
            //write code to broadcast your IP

            IPAddress[] ipv4Addresses = Array.FindAll(
                Dns.GetHostEntry(Dns.GetHostName()).AddressList,
              a => a.AddressFamily == AddressFamily.InterNetwork);

            string ip = ipv4Addresses[ipv4Addresses.Length - 1].ToString();


            byte[] myip = ObjectToByteArray(ipv4Addresses[ipv4Addresses.Length - 1]);
            //   Encoding.UTF8.GetBytes(ip);



            Socket Server_Sokcet = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Server_Sokcet.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            Server_Sokcet.EnableBroadcast = true;
            Server_Sokcet.ExclusiveAddressUse = false;
            var rec = new IPEndPoint(IPAddress.Broadcast, 8000);

            Server_Sokcet.SendTo(myip, rec);

            Console.WriteLine("sending my ip TO ALLLLL " + ip);

            //Hint: this function is called repeatedly using timer every 5seconds
        }



        void AcceptPlayers()
        {
            //write the code of server socket to accept incoming players
            //create an object from class Client and fill in its information
            //assign a rank for this created object (which is the client index in list) and add to list of clients
            //where Client is a class contains all information about client (mentioned in the project document)
            while (recieving)
            {
                Socket incomingSocket = currentSocket.Accept();

                incomingSocket.Send(Encoding.ASCII.GetBytes("hello to our game "));
                Console.WriteLine(incomingSocket);
                Client incomingClient = new Client();
                incomingClient.player = incomingSocket;
                incomingClient.IP = incomingSocket.RemoteEndPoint.ToString();
                incomingClient.Rank = clients.Count;

                clients.Add(incomingClient);

                listBox1.Items.Add(incomingClient.ToString());

            }


        }

        private void btnStartGame_Click(object sender, EventArgs e)
        {

            if (clients.Count > 1)
            {
                //stop broadcasting the IP
                tmrBroadCastIP.Stop();

                //generate board

                for (int i = 0; i < clients.Count; i++)
                {
                    if (clients[i].Rank != 0)
                    {
                        clients[i].player.Send(Encoding.ASCII.GetBytes(string.Format("numberOfPlayers#{0}#{1}#", clients.Count, clients[i].Rank)));
                        clients[i].player.Send(Encoding.ASCII.GetBytes(string.Format("GameStarted#")));


                    }
                }

                GenerateSnakesAndLadders();
                char[,] board = GenerateBoard(snakes, ladders);
                GamePlayingScreen gpc = new GamePlayingScreen(board, snakes, ladders, clients, clients.Count, 0, currentSocket, true);
                gpc.Show();
                this.Visible = false;
            }
            else MessageBox.Show("there is no enough clients "); 
        }
        Random r;

        private void tmrBroadCastIP_Tick(object sender, EventArgs e)
        {
            BroadCastIP();
        }





        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////COMMON FUNCTION USED BY BOTH///////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //Common function
        void GenerateSnakesAndLadders()
        {
            snakes.Add(new Point(6, 1), 1);
            snakes.Add(new Point(2, 2), 2);
            snakes.Add(new Point(9, 4), 1);
            snakes.Add(new Point(1, 6), 3);
            snakes.Add(new Point(2, 6), 2);
            snakes.Add(new Point(4, 6), 1);
            snakes.Add(new Point(1, 8), 1);
            snakes.Add(new Point(2, 9), 2);
            snakes.Add(new Point(8, 9), 1);
            snakes.Add(new Point(0, 7), 1);
            ladders.Add(new Point(1, 0), 1);
            ladders.Add(new Point(3, 0), 2);
            ladders.Add(new Point(8, 1), 1);
            ladders.Add(new Point(6, 2), 1);
            ladders.Add(new Point(0, 2), 2);
            ladders.Add(new Point(6, 5), 1);
            ladders.Add(new Point(3, 6), 2);
            ladders.Add(new Point(8, 4), 3);
            ladders.Add(new Point(4, 8), 1);
            ladders.Add(new Point(0, 8), 1);
        }
        char[,] GenerateBoard(Dictionary<Point, int> snakes, Dictionary<Point, int> ladders)
        {
            char[,] board = new char[10, 10];
            foreach (var snake in snakes)
            {
                board[snake.Key.Y, snake.Key.X] = 'S';
            }
            foreach (var ladder in ladders)
            {
                board[ladder.Key.Y, ladder.Key.X] = 'L';
            }
            return board;
        }
        private void GameSettingScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }




        public static byte[] ObjectToByteArray(Object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AcceptPlayers();
        }


    }
}
