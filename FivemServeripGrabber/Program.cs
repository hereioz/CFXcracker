using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace FivemServeripGrabber
{
    public enum Protocol
    {
        TCP = 6,
        UDP = 17,
        Unknown = -1
    };

    class Program
    {
        public const string fs = " - ";
        private static IDictionary<string, int> counter = new Dictionary<string, int>()
        {
            {"test", 0 }
        };
        private static Socket mainSocket;                          //The socket which captures all incoming packets
        private static byte[] byteData = new byte[50000];
        private static bool bContinueCapturing = true;            //A flag to check if packets are to be captured or not
        public static string listIP = "";

        static void Main(string[] args)
        {
            Console.Title = "Fivem IP Grabber By: MatrixX#4970";
            string url = "";
            string listenip = "";
            string ip = "";
            try
            {
                if (args[0].Contains("-u"))
                {
                    if (args[1].StartsWith("http"))
                    {
                        url = args[1];
                    }
                    else
                    {
                        url = "https://" + args[1];
                    }
                    WebClient client = new WebClient();
                    client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                    client.OpenRead(url);
                    string[] headers = Convert.ToString(client.ResponseHeaders).Split('\n');
                    foreach (string header in headers)
                    {
                        if (header.Contains("X-Citizenfx-Url"))
                        {
                            string[] head = header.Split(':');
                            string ip1 = head[2].Replace("/", "").Trim();
                            Console.Clear();
                            Console.WriteLine("Server IP: " + ip1);
                            Console.Title = "Fivem IP Grabber By: MatrixX#4970  -  " + ip1;
                        }
                    }
                    Console.ReadKey();
                    Environment.Exit(0);
                }
                else if (args[0].Contains("-l"))
                {
                    listenip = args[1];
                }
                else if (args[0].Contains("-i"))
                {
                    Console.Clear();
                    ip = args[1];
                    string Url = "http://ip-api.com/json/" + ip.ToString();
                    string info = new WebClient().DownloadString(Url);
                    var json = JObject.Parse(info);
                    if ((string)json["status"] == "success")
                    {
                        var ipv4 = (string)json["query"];
                        var country = (string)json["country"];
                        var countryCode = (string)json["countryCode"];
                        var region = (string)json["region"];
                        var regionName = (string)json["regionName"];
                        var city = (string)json["city"];
                        var zip = (string)json["zip"];
                        var lat = (string)json["lat"];
                        var lon = (string)json["lon"];
                        var timezone = (string)json["timezone"];
                        var isp = (string)json["isp"];
                        var org = (string)json["org"];
                        var asp = (string)json["as"];

                        Console.WriteLine("Get Information's From ip " + ipv4.ToString() + ": ");
                        Console.WriteLine(fs + "country: " + country.ToString());
                        Console.WriteLine(fs + "Country Code: " + countryCode.ToString());
                        Console.WriteLine(fs + "Region: " + region.ToString());
                        Console.WriteLine(fs + "Region Name: " + regionName.ToString());
                        Console.WriteLine(fs + "City: " + city.ToString());
                        Console.WriteLine(fs + "zip Code: " + zip.ToString());
                        Console.WriteLine(fs + "LAT: " + lat.ToString());
                        Console.WriteLine(fs + "LON: " + lon.ToString());
                        Console.WriteLine(fs + "Time Zone: " + timezone.ToString());
                        Console.WriteLine(fs + "ISP: " + isp.ToString());
                        Console.WriteLine(fs + "Org: " + org.ToString());
                        Console.WriteLine(fs + "ASP: " + asp.ToString());
                        Console.ReadKey();
                        Environment.Exit(0);

                    }
                    else
                    {
                        Console.Clear();
                        Console.WriteLine("ip not found");
                        Console.ReadKey();
                        Environment.Exit(0);
                    }
                }
                else
                {
                    Console.WriteLine("Usage:\n  -u   [CFX.re url]   CFX.re to ip\n  -l   [Local IP]   listen for fivem servers you connect and get you ip");
                    Console.ReadKey(); 
                    Environment.Exit(0);
                }
            } 
            catch 
            { 
                Console.WriteLine("Usage:\n  -u   [CFX.re url]   CFX.re to ip\n  -l   [Local IP]   listen for fivem servers you connect and get you ip\n  -i   [ip]   get ip information"); 
                Console.ReadKey(); 
                Environment.Exit(0); 
            }

            Console.Clear();
            Console.Title = "Fivem IP Grabber By: MatrixX#4970  -  " + listenip;
            mainSocket = new Socket(AddressFamily.InterNetwork,
    SocketType.Raw, ProtocolType.IP);

            //Bind the socket to the selected IP address
            mainSocket.Bind(new IPEndPoint(IPAddress.Parse(listenip), 0));

            //Set the socket  options
            mainSocket.SetSocketOption(SocketOptionLevel.IP,            //Applies only to IP packets
                                       SocketOptionName.HeaderIncluded, //Set the include the header
                                       true);                           //option to true

            byte[] byTrue = new byte[4] { 1, 0, 0, 0 };
            byte[] byOut = new byte[4] { 1, 0, 0, 0 }; //Capture outgoing packets

            //Socket.IOControl is analogous to the WSAIoctl method of Winsock 2
            mainSocket.IOControl(IOControlCode.ReceiveAll,              //Equivalent to SIO_RCVALL constant
                                 byTrue,
                                 byOut);

            Thread startTimer = new Thread(timer_);
            startTimer.Start();


            try
            {
                mainSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
    new AsyncCallback(OnReceive), null);
            }
            catch
            {
                Console.WriteLine("ERROR");
            }

        }

        private static void OnReceive(IAsyncResult ar)
        {
            int nReceived = mainSocket.EndReceive(ar);

            //Analyze the bytes received...

            ParseData(byteData, nReceived);
            //Console.WriteLine(nReceived);
            if (bContinueCapturing)
            {
                byteData = new byte[50000];

                //Another call to BeginReceive so that we continue to receive the incoming
                //packets
                mainSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                    new AsyncCallback(OnReceive), null);
            }

        }

        private static void ParseData(byte[] byteData, int nReceived)
        {
            IPHeader ipHeader = new IPHeader(byteData, nReceived);
            UDPHeader udpHeader = new UDPHeader(ipHeader.Data, (int)ipHeader.MessageLength);
            
            if (Convert.ToInt32(udpHeader.DestinationPort) == 30120)
            {
                if (listIP != Convert.ToString(ipHeader.DestinationAddress))
                {
                    listIP = Convert.ToString(ipHeader.DestinationAddress);
                    Console.WriteLine($"Catch ip: {ipHeader.DestinationAddress}:{udpHeader.DestinationPort}");
                }
            }
        }

        private static void timer_()
        {
            while (true)
            {
                Thread.Sleep(1000);
                counter.Clear();
            }
        }
    }
}
