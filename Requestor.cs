using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using StkSocks;

namespace stkhttor 
{
	class Requestor 
	{

		private const int bufSize = 1024;

		public Requestor() 
		{
		}

		int SearchBytes(byte[] haystack, byte[] needle) 
		{
			var len = needle.Length;
			var limit = haystack.Length - len;
			for (var i=0; i<=limit; i++) {
				var k=0;
				for ( ; k<len; k++) {
					if (needle[k] != haystack[i+k]) break;
				}
				if (k == len) return i;
			}
			return -1;
		}

		string GetHost(byte[] data)
		{
			byte[] hostHeader = Encoding.ASCII.GetBytes("Host: ");
			int idx = SearchBytes(data, hostHeader);
			if (idx==-1) return "";
			idx += hostHeader.Length;
			int i=idx;
			while(i<data.Length && data[i]!=13) i++;
			byte[] hostBytes = new byte[i-idx];
			Array.Copy(data, idx, hostBytes, 0, i-idx);
			return Encoding.ASCII.GetString(hostBytes);
		}

		void ProxyToRequest(byte[] data, out int index)
		{
			int i = 0;
			int startIdx = -1, endIdx = -1;
			while(data[i]!=0x20) i++; //first space after HTTP verb
			i++; //shift to address
			startIdx = i;
			int slashCount = 0;
			while(data[i]!=0x20) {
				if (data[i]==0x2f) slashCount++;
				if (slashCount==3) {
					endIdx = i;
					break;
				}
				i++;
			}
			Array.Copy(data, 0, data, endIdx-startIdx, startIdx);
			index = endIdx-startIdx;
		}

		public void HandleRequest(TcpClient client)
		{
			NetworkStream stream = client.GetStream();

			int numInBytes=bufSize;
			Byte[] bytes = new Byte[bufSize];
			Options opts = Options.GetOpts();

			using(var s = new SocksConnector(opts.TorHost,opts.TorPort)) {
				NetworkStream ss = null; 
				while(numInBytes==bytes.Length)
				{
					numInBytes = stream.Read(bytes, 0, bytes.Length);
					HexDump.Utils.LogBytes(">>>", bytes, numInBytes);
					if (ss == null) {
						string host = GetHost(bytes);
						string[] hostparts = host.Split(':');
						host = hostparts[0];
						short port = hostparts.Length==2?Int16.Parse(hostparts[1]):(short)80;

						HexDump.Utils.LogString($"Host={host}, Port={port}");
						s.Connect();
						if (!s.GetSockStream(host, port, out ss))
							throw new System.IO.IOException("Cannot get SOCKS stream, disconnecting");

						int shift = 0;
						ProxyToRequest(bytes, out shift);
						ss.Write(bytes, shift, numInBytes-shift);
						HexDump.Utils.LogBytes(">>>(send fixed)\n", bytes, numInBytes);
					} else {
						ss.Write(bytes, 0, numInBytes);
						HexDump.Utils.LogBytes(">>>(send)\n", bytes, numInBytes);
					}

				} //end send

				byte[] resp = new byte[bufSize];
				int numRespBytes;
				while ((numRespBytes = ss.Read(resp,0, resp.Length))!=0) {
					HexDump.Utils.LogBytes($"<<<{numRespBytes}\n", resp, numRespBytes);
					stream.Write(resp, 0, numRespBytes);
				}
			}
		}
	}
}
