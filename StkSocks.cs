using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using StkByter;

namespace StkSocks
{
	enum SocksAuthMethods : byte {
		NO_AUTH = 0,
		GSSAPI = 1,
		USRPWD = 2,
		NOTACCEPT = 255
	}

	enum SocksCmd : byte {
		CONNECT = 1,
		BIND = 2,
		UDPASS = 3
	}

	enum SocksAddrType : byte {
		IPv4 = 1,
		DOMAINNAME = 3,
		IPv6 = 4
	}

	enum SocksReplyType : byte {
		Success = 0,
		GenFalure = 1,
		NotAllowed = 2,
		NetUnreachable = 3,
		HostUnreachable = 4,
		Refused = 5,
		TTLexpired = 6,
		CmdNotSupported = 7,
		AddrNotSupported = 8
	}

#pragma warning disable 0649
	class Handshake {
		[DataType("Byte")]
			public byte ver = 5;
		[DataType("ListOfByte")]
			public List<byte> methods = new();
	}

	class HndShkRespone {
		[DataType("Byte")]
			public byte ver;
		[DataType("Byte")]
			public byte method;
	}

	class Request {
		[DataType("Byte")]
			public byte ver = 5;
		[DataType("Byte")]
			public byte cmd;
		[DataType("Byte")]
			public byte rsv = 0;
		[DataType("Byte")]
			public byte atyp;
		[DataType("String")]
			public string addr;
		[DataType("Int16")]
			public short port;
	}

	class Response {
		[DataType("Byte")]
			public byte ver;
		[DataType("Byte")]
			public byte rep;
		[DataType("Byte")]
			public byte rsv = 0;
		[DataType("Byte","GetAddrLen")]
			public byte atyp;
		[DataType("Bytes")]
			public byte[] addr;
		[DataType("Int16")]
			public ushort port;

		public int GetAddrLen() {
			switch (atyp) {
				case (byte)SocksAddrType.IPv4: return 4;
				case (byte)SocksAddrType.DOMAINNAME:return 0;
				case (byte)SocksAddrType.IPv6: return 16;
			}
			return 0;
		}

		public string GetAddress() {
			byte[] octets = addr;
			switch (atyp) {
				case (byte)SocksAddrType.IPv4:
					string s = $"{octets[0]}.{octets[1]}.{octets[2]}.{octets[3]}";
					return s;
				case (byte)SocksAddrType.DOMAINNAME:
					return  Encoding.ASCII.GetString(octets, 0, octets.Length);
				case (byte)SocksAddrType.IPv6: return ConvertToIPv6Address(octets);
			}
			return "";
		}

		string ConvertToIPv6Address(byte[] bytes)
		{
			var str = new StringBuilder();
			for (var i = 0; i < bytes.Length; i+=2)
			{
				var segment = (ushort)bytes[i] << 8 | bytes[i + 1];
				str.AppendFormat("{0:X}", segment);
				if (i + 2 != bytes.Length)
				{
					str.Append(':');
				}
			}

			return str.ToString();
		}
	}

#pragma warning restore 0649

	public class SocksConnector : IDisposable {
		private string hostName = "localhost";
		private int portNum = 9150;
		private TcpClient client;
		private NetworkStream ns;
		public bool Trace {get; set;}

		public SocksConnector(){
		}

		public SocksConnector(string h, int p) {
			hostName = h;
			portNum = p;
		}


		private S SocksReq<T,S>(T obj) where S:new()
		{
			byte[] bytes = Byter.Serialize<T>(obj);
			if (Trace) {
				Console.Write("<<<");
				Console.Write(HexDump.Utils.HexDump(bytes));
			}
			ns.Write(bytes, 0, bytes.Length);
			byte[] inBytes = new byte[1024];
			int numRead = ns.Read(inBytes);
			Array.Resize<byte>(ref inBytes, numRead);
			if (Trace) {
				Console.Write(">>>");
				Console.Write(HexDump.Utils.HexDump(inBytes));
			}

			S resp = Byter.Deserialize<S>(inBytes);

			return resp;
		}

		public bool Connect()
		{
			client = new TcpClient(hostName, portNum);
			ns = client.GetStream();
			Handshake hs = new();
			hs.methods.Add((byte)SocksAuthMethods.NO_AUTH);
			var resp = SocksReq<Handshake,HndShkRespone>(hs);
			if (resp.method==0) {
				return true;
			}
			return false;
		}

		public bool GetSockStream(string inAddr, short inPort, out NetworkStream sStream)
		{
			Request req = new();
			req.cmd = (byte)SocksCmd.CONNECT;
			req.atyp = (byte)SocksAddrType.DOMAINNAME;
			req.addr = inAddr;
			req.port = inPort;

			Response rsp = SocksReq<Request,Response>(req);

			if (rsp.rep!=0) {
				sStream = null;
				return false;
			}
			sStream = ns;
			return true;
		}

		public string SendString(string data)
		{
			byte[] send = Encoding.ASCII.GetBytes(data);
			ns.Write(send, 0, send.Length);
			byte[] inBytes = new byte[8192];
			int numRead = ns.Read(inBytes);
			string str = Encoding.UTF8.GetString(inBytes, 0, numRead);
			return str;
		}

		public void Dispose()
		{
			if (client!=null)
				client.Close();
		}

	}

}
