using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using StkSocks;
using StkCli;

namespace stkhttor
{

    class Program
    {

        static void SavePid()
        {
            Int32 nProcessID = System.Diagnostics.Process.GetCurrentProcess().Id;
            File.WriteAllText("stkhttor.pid", nProcessID.ToString());
        }

        static void Main(string[] args)
        {

            Options opt = Options.GetOpts();
            ArgHandler<Options> ah = new(opt);
            ah.Title = "StkHTTor: http-Tor proxy";
            ah.Copyright = "(c) Stein Krauz, 2022";
            try {
                ah.Parse(args);
            }catch(ArgumentException ex) {
                Console.WriteLine(ex.Message);
                return;
            }

            TcpListener server = null;
            SavePid();
            HexDump.Utils.Trace = opt.Trace;
            try
            {
                Int32 port = opt.HttpPort;
                IPAddress localAddr = IPAddress.Parse(opt.HttpHost);

                server = new TcpListener(localAddr, port);
                server.Start();
                Console.WriteLine("stkHTTor engaged.");

                while(true)
                {
                    HexDump.Utils.LogString("Waiting for a connection... ");
                    TcpClient client = server.AcceptTcpClient();
                    HexDump.Utils.LogString("Connected!");

					Thread t = new Thread(new ParameterizedThreadStart(HandleConnect));
					t.Start(client);

                }
            }
            catch(SocketException e)
            {
                Console.WriteLine("ACHTUNG! SocketException: {0}", e);
            }
            finally
            {
                server.Stop();
            }

        }

		static void HandleConnect(Object o)
		{
		    TcpClient client = (TcpClient) o;	
			try {
				Requestor r = new();
				r.HandleRequest(client);
			} catch(System.IO.IOException ioex) {
				HexDump.Utils.LogString($"Connection aborted: {ioex.Message}");
				return;
			}

			client.Close();
			HexDump.Utils.LogString("Connection closed");
		}

	} //end class
}
