using System;
using System.Text;
using System.Net;
using System.Reflection;
using System.Collections.Generic;

namespace StkByter {
	public sealed class DataTypeAttribute : System.Attribute {
		private string typeName;
		private string _call;

		public DataTypeAttribute(string txt, string call = "") {
			typeName = txt;
			_call = call;
		}

		public string Name { get {return typeName;} }
		public string Call { get {return _call;} }
	}


	public class Byter {
		private static List<byte> outBytes;
		private static byte[] inBytes;
		private static int idx;
		private static int len = 0;

		private static void AddByte(Object o, FieldInfo fi) {
			byte b = (byte) fi.GetValue(o);
			outBytes.Add(b);
		}

		private static void AddInt16(Object o, FieldInfo fi) {
			short b = (short) fi.GetValue(o);
			short n = IPAddress.HostToNetworkOrder(b);
			byte[] octets = BitConverter.GetBytes(n);
			outBytes.AddRange(octets);
		}

		private static void AddString(Object o, FieldInfo fi) {
			string s = (string) fi.GetValue(o);
			byte l = (byte)s.Length;
			outBytes.Add(l);
			byte[] octets = Encoding.ASCII.GetBytes(s);
			outBytes.AddRange(octets);
		}

		private static void GetByte(Object o, FieldInfo fi) {
			byte b = inBytes[idx++];
			fi.SetValue(o, b);
			var attr = (DataTypeAttribute)Attribute.GetCustomAttribute(fi,typeof(DataTypeAttribute),false);
			if (attr.Call.Length>0) {
				var mi = o.GetType().GetMethod(attr.Call);
				object result = mi.Invoke(o, null);
				len = (Int32) result;
			}
		}

		private static void GetInt16(Object o, FieldInfo fi) {
			byte[] b = new byte[2];
			b[0] = inBytes[idx++];
			b[1] = inBytes[idx++];
			ushort s = (ushort)(b[1]+ 256*b[0]);
			fi.SetValue(o, s);
		}

		private static void GetString(Object o, FieldInfo fi) {
			int size = len>0?len:inBytes[idx++];
			byte[] b = new byte[size];
			for (int i=0; i<size; i++)
				b[i] = inBytes[idx++];
			string s = Encoding.ASCII.GetString(b, 0, size);
			fi.SetValue(o, s);
			len = 0;
		}

		private static void GetBytes(Object o, FieldInfo fi) {
			int size = len>0?len:inBytes[idx++];
			byte[] b = new byte[size];
			for (int i=0; i<size; i++)
				b[i] = inBytes[idx++];
			fi.SetValue(o, b);
			len = 0;
		}

		private static void AddListOfByte(Object o, FieldInfo fi) {
			List<byte> val = (List<byte>)fi.GetValue(o);
			byte len = (byte)val.Count;
			outBytes.Add(len);
			outBytes.AddRange(val);
		}

		public static byte[] Serialize<T>(T obj)
		{
			outBytes = new();
			Type theType = obj.GetType();
			foreach (var fi in theType.GetFields()) {
				var attr = (DataTypeAttribute)Attribute.GetCustomAttribute(fi,typeof(DataTypeAttribute),false);
				switch (attr.Name) {
					case "Byte": 
						AddByte(obj, fi);
						break;
					case "Int16": 
						AddInt16(obj, fi);
						break;
					case "String": 
						AddString(obj, fi);
						break;
					case "ListOfByte": 
						AddListOfByte(obj, fi);
						break;
					default: 
						throw new NotSupportedException($"Type {attr.Name} is not supported yet.");
				}
			}
			return outBytes.ToArray();
		}

		public static T Deserialize<T>(byte[] bytes) where T:new()
		{
			T obj = new();
			Type theType = obj.GetType();
			inBytes = bytes;
			idx = 0;
			foreach (var fi in theType.GetFields()) {
				var attr = (DataTypeAttribute)Attribute.GetCustomAttribute(fi,typeof(DataTypeAttribute),false);
				switch (attr.Name) {
					case "Byte": 
						GetByte(obj, fi);
						break;
					case "Bytes": 
						GetBytes(obj, fi);
						break;
					case "Int16": 
						GetInt16(obj, fi);
						break;
					case "String": 
						GetString(obj, fi);
						break;
					default: 
						throw new NotSupportedException($"Type {attr.Name} is not supported yet.");
				}
			}
			return obj;	
		}

	}
}
