using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace NetworkProject
{
    public partial class GameStart : Form
    {
        public GameStart()
        {
            InitializeComponent();
            btnJoinAsClient.Enabled = false; 
        }

        private void btnStartAsServer_Click(object sender, EventArgs e)
        {
            GameSettingScreen gsc = new GameSettingScreen(true,null);
            gsc.Show();
            this.Visible = false;
            t.Abort();

        }
        IPAddress ip = null; 
        private void btnJoinAsClient_Click(object sender, EventArgs e)
        {
         
            //recieve the servers IP using UDP and place it in the variable "ip"



         
   
            GameSettingScreen gsc = new GameSettingScreen(false,ip);
            gsc.Show();
            this.Visible = false;
        }



        public static Object ByteArrayToObject(byte[] arrBytes)
        {
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(arrBytes, 0, arrBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = binForm.Deserialize(memStream);
                return obj;
            }
        }
        Thread t;
        private void GameStart_Load(object sender, EventArgs e)
        {
            t = new Thread(TryRecieveAddress);
            t.Start();
        }

        void TryRecieveAddress()
        {
            Socket Server_Sokcet = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Server_Sokcet.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            Server_Sokcet.EnableBroadcast = true;  
            Server_Sokcet.ExclusiveAddressUse = false; 
            Server_Sokcet.Bind(new IPEndPoint(IPAddress.Any, 8000));
         
            EndPoint rec = new IPEndPoint(IPAddress.Any, 8000);
            byte[] ServerIp = new byte[10000] ;
        Server_Sokcet.ReceiveFrom(ServerIp, ref rec)  ;

            ip =  (IPAddress) ByteArrayToObject(ServerIp); 

            if(ip != null  ){
                btnJoinAsClient.Enabled = true;
                btnStartAsServer.Enabled = false; 
            }
        }

    }
}
