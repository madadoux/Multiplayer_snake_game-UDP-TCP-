﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetworkProject
{
    public partial class GamePlayingScreen : Form
    {
        char[,] gameBoard;
        Dictionary<Point, int> Snakes;
        Dictionary<Point, int> Ladders;
        List<Client> Clients;
        List<Point> PlayersLocation;
        int myIndex;
        Socket currentPlayer;
        bool IsServer;
        Bitmap Board;
        SoundPlayer GamePlaysp = new SoundPlayer("GameTrack.wav"); 
        public GamePlayingScreen(char[,] board, Dictionary<Point, int> snakes, Dictionary<Point, int> ladders, List<Client> clients, int numberOfPlayers, int Rank, Socket me, bool Server)
        {
            InitializeComponent();
            GamePlaysp.PlayLooping();
            Clients = clients;
            gameBoard = board;
            Snakes = snakes;
            Ladders = ladders;
            currentPlayer = me;
            myIndex = Rank;
            // numberOfPlayers = 5;
            PlayersLocation = new List<Point>();
            for (int i = 0; i < numberOfPlayers; i++)
            {
                PlayersLocation.Add(new Point(0, 0));
            }

            GeneratePlayerList(numberOfPlayers);
            IsServer = Server;

            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            DrawBoard();

            if (IsServer)
            {
                btnRollTheDice.Enabled = true;
                myIndex = 0;
                for (int i = 1; i < clients.Count; i++)
                {
                    Thread t = new Thread(new ParameterizedThreadStart(RecieveFromClients));
                    t.Start(clients[i]);
                }
            }
            else
            {
                Thread t = new Thread(RecieveFromServer);
                t.Start();
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////DRAWING FUNCTIONS/////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////YOU DON'T NEED TO WRITE ANY CODE HERE///////////////////////////////////////////////////////////////////////////////////
        void GeneratePlayerList(int numberOfPlayers)
        {
            //maximum number of players is 8
            numberOfPlayers = numberOfPlayers > 8 ? 8 : numberOfPlayers;

            for (int i = 0; i < numberOfPlayers; i++)
            {
                Label label = new Label();
                label.AutoSize = true;
                label.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                label.Location = new System.Drawing.Point(85, 65 + i * 50);
                label.Name = "label2";
                label.Size = new System.Drawing.Size(76, 19);
                label.TabIndex = 0;
                label.Text = "Player " + (i + 1);

                this.groupBox1.Controls.Add(label);

                PictureBox pictureBox = new PictureBox();
                pictureBox.Location = new System.Drawing.Point(30, 55 + i * 50);
                pictureBox.Name = "pictureBox2";
                pictureBox.Size = new System.Drawing.Size(48, 40);
                pictureBox.TabIndex = 0;
                pictureBox.TabStop = false;
                GeneratePlayerColor(i + 1);
                Image bmp = new Bitmap(pictureBox.Width, pictureBox.Height);
                Graphics g = Graphics.FromImage(bmp);
                g.FillEllipse(new SolidBrush(PlayerColors[i]), 0, 0, 48, 40);
                g.Flush();
                pictureBox.BackgroundImage = bmp;
                this.groupBox1.Controls.Add(pictureBox);

            }
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
        }
        List<Color> PlayerColors = new List<Color>();
        void GeneratePlayerColor(int index)
        {
            PlayerColors.Add(Color.FromArgb(index * 200 % 255, index * 300 % 255, index * 400 % 255));
        }
        void DrawBoard()
        {
            Bitmap bmp = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height);
            Graphics g = Graphics.FromImage(bmp);

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (i % 2 == 0)
                    {
                        if (j % 2 == 0)
                            g.FillRectangle(Brushes.White, new Rectangle(j * 50, i * 50, 50, 50));
                        else
                            g.FillRectangle(Brushes.Gray, new Rectangle(j * 50, i * 50, 50, 50));
                    }
                    else
                    {
                        if (j % 2 == 0)
                            g.FillRectangle(Brushes.Gray, new Rectangle(j * 50, i * 50, 50, 50));
                        else
                            g.FillRectangle(Brushes.White, new Rectangle(j * 50, i * 50, 50, 50));
                    }
                }
            }

            for (int i = 0; i < 11; i++)
            {
                g.DrawLine(Pens.Black, new Point(0, i * 50), new Point(500, i * 50));
                g.DrawLine(Pens.Black, new Point(i * 50, 0), new Point(i * 50, 500));
            }

            g.FillRectangle(Brushes.LightPink, new Rectangle(450, 0, 50, 50));
            g.FillRectangle(Brushes.LightPink, new Rectangle(0, 450, 50, 50));

            Bitmap snakeImg = new Bitmap("snake.png");
            foreach (var snake in Snakes)
            {
                g.DrawImage(snakeImg, snake.Key.X * 50, (9 - snake.Key.Y) * 50, 50, (snake.Value + 1) * 50);
            }
            Bitmap ladderImg = new Bitmap("ladder.png");
            foreach (var ladder in Ladders)
            {
                g.DrawImage(ladderImg, ladder.Key.X * 50, (9 - ladder.Key.Y - ladder.Value) * 50 + 25, 50, ladder.Value * 50 + 10);
            }

            g.DrawString("START", SystemFonts.DefaultFont, Brushes.Red, new PointF(5, 470));
            g.DrawString("END", SystemFonts.DefaultFont, Brushes.Red, new PointF(470, 20));
            Board = bmp;
            pictureBox1.BackgroundImage = bmp;
        }
        private void GamePlayingScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        void DrawAllPlayers()
        {
            Bitmap bmp = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height);
            Graphics g = Graphics.FromImage(bmp);
            for (int i = 0; i < PlayersLocation.Count; i++)
            {
                g.FillEllipse(new SolidBrush(PlayerColors[i]), new Rectangle(PlayersLocation[i].X * 50, (9 - PlayersLocation[i].Y) * 50, 50 - i, 50 - i));
            }
            pictureBox1.Image = bmp;
        }

        private void GamePlayingScreen_Paint(object sender, PaintEventArgs e)
        {
            DrawAllPlayers();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////YOUR CODE HERE///////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        public Point getPositionAsCell(int position, int numberOfrows)
        {
            var X = 0;

            var Y = 0;
            X = (position) % numberOfrows;
            Y = (position / numberOfrows);

            if (Y < 0)
            {
                Y = 0;
            }

            if (X == 0 && Y > 0)
            {
                X = 9;
                Y--;
            }
            else
            {
                X--;
            }

            return new Point(X, Y);

        }

        public int FlatCell(Point p, int numberOfRows)
        {
            return ((p.Y) * numberOfRows) + p.X + 1;
        }


        SoundPlayer snakeSoundEffect = new SoundPlayer( "powerDown.wav");
        SoundPlayer LadderSoundEffect = new SoundPlayer("powerUp.wav"); 
        
        bool IWon = false;
        void UpdatePlayerLoc(int index, int val)
        {
            var flattedVal = FlatCell(PlayersLocation[index], 10);
            if (flattedVal + val >= 100)
            {
                flattedVal = 100;
                IWon = true;
            }
            else
            {
                flattedVal += val;
                // Console.WriteLine("Flat" + flattedVal);
            }
            var elmfrodPoint = getPositionAsCell(flattedVal, 10);

            if (Snakes.ContainsKey(elmfrodPoint))
            {
                elmfrodPoint.Y -= Snakes[elmfrodPoint];
                if (elmfrodPoint.Y < 0)
                    elmfrodPoint.Y = 0;

                snakeSoundEffect.Play(); 
            }

            if (Ladders.ContainsKey(elmfrodPoint))
            {
                elmfrodPoint.Y += Ladders[elmfrodPoint];
                if (elmfrodPoint.Y > 9)
                    elmfrodPoint.Y = 9;

                LadderSoundEffect.Play();
            }

            PlayersLocation[index] = elmfrodPoint;
        }

        private void btnRollTheDice_Click(object sender, EventArgs e)
        {
            //write the button code here:
            //1- disable "RollTheDice" button
            //2- generate random number and write it in textbox
            //3- after 3 sec move the player coin
            //4- check if new location is ladder or snake using gameBoard array and modify the new location based on the value of gameBoard[y,x] = 'S' or = 'L'
            //6- update the location of currentPlayer (to be modified in drawing)
            Random r = new Random();

            int val = r.Next(1, 6);
            textBox1.Text = val.ToString();
    Thread.Sleep(1000); 

            //if (!IsServer)
            //    val /= 2;

            UpdatePlayerLoc(myIndex, val);

            var obj = (Bitmap)Properties.Resources.ResourceManager.GetObject(string.Format("_{0}", val-1));
            ((Button)sender).BackgroundImage = obj;

            if (IsServer)
            {
                BroadCastLocation();
                BroadCastWhoseTurn(0);
                if (IWon)
                {
                    BroadCastTheWinnerIs(0);
                    WinningForm wf = new WinningForm(0);
                    this.Visible = false;
                    wf.ShowDialog();

                }
                //call BroadCastLocation(0) as the server index is always 0 in the client list
                //call BroadCastWhoseTurn(0) to see which player will play after server
            }
            else
            {
                if (IWon)
                {
                    SendTheWinnerIsMeToServer();
                }
                else
                    SendLocationToServer();


                //if final location is the winning location then call the function SendTheWinnerIsMeToServer()
                //else send the final location to server by calling SendLocationToServer()
            }

            btnRollTheDice.Enabled = false;
        }



        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////CLIENT///////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            bool recieving = true;
        void RecieveFromServer()
        {
            //use the currentPlayer socket to recieve from the server

            //parse the recieved message


            while (recieving)
            {
                byte[] arr = new byte[10000];
                int size = currentPlayer.Receive(arr);

                if (arr[0] == (byte)'l' && arr[1] == (byte)'o')
                {
                    byte[] newByte = new byte[size];
                    for (int i = 4; i < size; i++)
                    {
                        newByte[i - 4] = arr[i];
                    }
                    PlayersLocation = (List<Point>)GameStart.ByteArrayToObject(newByte);
                    Console.WriteLine("playerLocationRecieved ");

                }
                else
                {
                    var message = Encoding.ASCII.GetString(arr);
                    Console.WriteLine(message);

                ParseMessage(message);
                }
            }

            //if turn message check if the IP matched with my IP
            //then check if currentPlayer boolean = true
            //enable "RollTheDice" button and play
            //else keep it disabled

            //if location message then update the location of player n
            //update client n location

            //if winning message
            //go to WinningForm with the playerNumber
        }

        void ParseMessage(string message)
        {
            var tockens = message.Split(new char[] { '#' });
            //server 
            if (tockens[0] == "myLocation")
            {

                int PlayerIndexWhoSendTheMessage = int.Parse(tockens[1]);
                int px = int.Parse(tockens[2]);
                int py = int.Parse(tockens[3]);
                PlayersLocation[PlayerIndexWhoSendTheMessage] = new Point(px, py);
                BroadCastLocation();
                BroadCastWhoseTurn(PlayerIndexWhoSendTheMessage);
                Console.WriteLine("mylocation me " + PlayerIndexWhoSendTheMessage + " point " + PlayersLocation[PlayerIndexWhoSendTheMessage]);
     



            }

            //server 
            if (tockens[0] == "gameEnded")
            {
                BroadCastTheWinnerIs(int.Parse(tockens[1]));
       
            }
            //client 
            if (tockens[0] == "gameEndedS")
            {
                WinningForm wf = new WinningForm(int.Parse(tockens[1]));
                this.Visible = false;
                wf.ShowDialog();
        
            }
            //client 
            if (tockens[0] == "currentPlayerTurn")
            {
                currPlayerTurn = int.Parse(tockens[1]);
                if (currPlayerTurn == myIndex)
                {
                    btnRollTheDice.Enabled = true;

                }

                Console.WriteLine("currentPlayerTurn" + tockens[1] + "myindex " + myIndex);
            }




        }
        void SendLocationToServer()
        {
            //use the currentPlayer socket to send to server "PlayersLocation[myIndex]"
            //message should look like this:
            //IP#PlayersLocation[myIndex]#


            currentPlayer.Send(Encoding.ASCII.GetBytes(string.Format("myLocation#{0}#{1}#{2}#", myIndex, PlayersLocation[myIndex].X, PlayersLocation[myIndex].Y)));


        }



        string GetMyIp()
        {
            IPAddress[] ipv4Addresses = Array.FindAll(
             Dns.GetHostEntry(Dns.GetHostName()).AddressList,
             a => a.AddressFamily == AddressFamily.InterNetwork);

            string ip = ipv4Addresses[ipv4Addresses.Length - 1].ToString();
            return ip;
        }
        void SendTheWinnerIsMeToServer()
        {
            //use the currentPlayer socket to send to server the winner message
            //message should look like this:
            //IP#

            currentPlayer.Send(Encoding.ASCII.GetBytes(string.Format("gameEnded#{0}#", myIndex)));

        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////SERVER///////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        bool winningMessage = false;
        int winnerRank;
        void RecieveFromClients(Object client)
        {
            Client c = (Client)client;
            //recieve message and parse it

            //if Winning Message
            //call BroadCastTheWinnerIs(playerNumber)
            //go to WinningForm

            //if LocationMessage
            //call BraodCastLocation(player number)
            //call BroadCastWhoseTurn(player number)


            while (!winningMessage)
            {
                byte[] arr = new byte[2048];
                c.player.Receive(arr);
                string message = Encoding.ASCII.GetString(arr);

                ParseMessage(message);


            }


        }

        void BroadCastLocation()
        {
            //here send the mssage to all clients, containing the location of PlayersLocation[playerNumber] and attach its IP and playerNumber
            List<byte> bytesToSend = new List<byte>();
            bytesToSend.Add((byte)'l'); bytesToSend.Add((byte)'o'); bytesToSend.Add((byte)'c'); bytesToSend.Add((byte)'#');

            var locArrAsBytes = GameSettingScreen.ObjectToByteArray(PlayersLocation);


            for (int j = 0; j < locArrAsBytes.Length; j++)
            {
                bytesToSend.Add(locArrAsBytes[j]);
            }


            for (int i = 1; i < Clients.Count; i++)
            {

                Clients[i].player.Send(bytesToSend.ToArray());


            }


        }
        int currPlayerTurn = -1;
        void BroadCastWhoseTurn(int playerNumber)
        {
            currPlayerTurn = playerNumber + 1;
            if (currPlayerTurn > Clients.Count - 1)
            {
                currPlayerTurn = 0;
                btnRollTheDice.Enabled = true;

            }

            for (int i = 1; i < Clients.Count; i++)
            {
                Clients[i].player.Send(Encoding.ASCII.GetBytes(string.Format("currentPlayerTurn#{0}#", currPlayerTurn)));

            }

        }
        void BroadCastTheWinnerIs(int playerNumber)
        {
            //send to all clients message, containing IP,playerNumber
            for (int i = 1; i < Clients.Count; i++)
            {
                Clients[i].player.Send(Encoding.ASCII.GetBytes(string.Format("gameEndedS#{0}#", playerNumber)));

            }
            WinningForm wf = new WinningForm(playerNumber);
            this.Visible = false;
            wf.ShowDialog();
        }




    }
}
