//**************************************************************************
//**
//**  StkCli -- trivial command-line parsing library
//**  Copyright (C) 2022 Stein Krauz
//**
//**  This program is free software: you can redistribute it and/or modify
//**  it under the terms of the GNU General Public License as published by
//**  the Free Software Foundation, version 3 of the License ONLY.
//**
//**  This program is distributed in the hope that it will be useful,
//**  but WITHOUT ANY WARRANTY; without even the implied warranty of
//**  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//**  GNU General Public License for more details.
//**
//**  You should have received a copy of the GNU General Public License
//**  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//**
//    USAGE:
//    Each parameter definition has four attibutes: short name, long name, 
//    mandatory flag and description. Flag and description are optional.
//    You may use short or long form, or both at the same time. The unused
//    form is defined with an empty string.
//
//    The following parameters are supported:
//	   *  StrParam -- just a string parameter
//	   *  IntParam -- a signed 32-bit integer
//	   *  FloatParam --  a floating point value. Beware, system locale is used to parse
//	   *  FlagParam -- sets boolean property to true. No value required.
//	   *  AutoHelp -- shows basic help from params' names and descriptions and exits the program
//
//	  //Create a class with some properties
//  class Options
//  {
//      //put AutoHelp before any mandatory param or missing value error will be triggered
//      [AutoHelp]
//      public bool Help {get; set;} // just a dummy prop
//
//      //A mandatory string param with just a short name
//      [StrParam("-u","", true)]
//      public string User {get; set;}
//
//      //need a storage field beause of non-trivial setter
//      private int _count;
//      //Attribute must be on the property, not field
//      [IntParam("-c","--count")]
//      public int Count {get => _count;
//      // here we can add some value checks. Throw an ArugmentException to handle it within library
//      set {
//              if (value<=0) throw new ArgumentException("Count cannot be negative or zero");
//              _count = value;
//          }
//      }
//
//      [FloatParam("-V","--volume")]
//      public double Volume {get; set;}
//
//      [FlagParam("-s","--sober")]
//      public bool BeSober {get; set;}
//      
//      //the constructor is a good place to put the defaults.
//      public Options(){
//            Count = 1;
//            Volume = 0.5;
//      }
//   }
//
//   //call the parser
//    ArgHandler<Options> ah = new();
//    //ArgHandler has two properties to show with AutoHelp
//    ah.Title = "StkCLi usage primer";
//    ah.Copyright = "(c) Stein Krauz, 2022";
//    Options o;
//    try {
//        o = ah.Parse(args);
//    }catch(ArgumentException ex) {
//        Console.WriteLine(ex.Message);
//        return;
//    }
//**************************************************************************
using System;
using System.Text;
using System.Net;
using System.Reflection;
using System.Collections.Generic;

namespace StkCli {
	public class BaseParamAttribute : System.Attribute {
		public string Name {get;}
		public string LongName {get;}
		public string Desc {get;set;}
		public bool Mandatory {get; set;}

		public BaseParamAttribute(string n, string l, bool m = false, string d="") {
			Name = n;
			LongName = l;
			Desc = d;
			Mandatory = m;
		}
	}


	public sealed class IntParamAttribute : BaseParamAttribute 
	{
		public IntParamAttribute(string n, string l, bool m = false, string d=""):base(n,l,m,d) { }
	}

	public sealed class FloatParamAttribute : BaseParamAttribute 
	{
		public FloatParamAttribute(string n, string l, bool m = false, string d=""):base(n,l,m,d) { }
	}

	public sealed class StrParamAttribute : BaseParamAttribute
	{
		public StrParamAttribute(string n, string l, bool m = false, string d=""):base(n,l,m,d) { }
	}

	public sealed class FlagParamAttribute : BaseParamAttribute
	{
		public FlagParamAttribute(string n, string l, string d=""):base(n,l,false,d) { }
	}

	public sealed class AutoHelpAttribute : BaseParamAttribute
	{
		public AutoHelpAttribute(string n="-h", string l="--help", string d="Prints this help"):base(n,l,false,d) { }
	}


	public class ArgHandler<T>where T:new()
	{
		public string Title {get; set;}
		public string Copyright {get; set;}

		private T obj;

		public ArgHandler() 
		{
			obj = new();
		}

		public ArgHandler(T inObj)
		{
			obj = inObj;
		}



		private int GetIndex(string[] args, string par)
		{
			int idx = -1;
			if (par.Length==0) return idx;
			for(int i=0; i<args.Length; i++) {
				if (args[i]==par) {
					idx = i;
					break;
				}	
			}
			return idx;
		}

		private int FindParam(string[] args, BaseParamAttribute attr)
		{
			int sIdx = GetIndex(args, attr.Name);
			int lIdx = GetIndex(args, attr.LongName);
			int Idx = sIdx>=0?sIdx:lIdx;
			if (Idx<0 && attr.Mandatory)
				throw new ArgumentException($"Missed argument: {attr.Name}");
			return Idx;
		}

		private void SetAttr(PropertyInfo pi, FlagParamAttribute attr, string[] args)
		{
			int Idx = FindParam(args, attr);
			if (Idx < 0) return;
			try{
				pi.SetValue(obj, true);
			} catch (Exception sfe) {
				RaiseException(attr, sfe);
			}
		}

		private void SetAttr(PropertyInfo pi, StrParamAttribute attr, string[] args)
		{
			int Idx = FindParam(args, attr);
			if (Idx < 0) return;

			if (Idx==args.Length-1)
				throw new ArgumentException($"Argument {attr.Name} requries a value");

			try{
				pi.SetValue(obj, args[Idx+1]);
			} catch (Exception sfe) {
				RaiseException(attr, sfe);
			}
		}

		private void SetAttr(PropertyInfo pi, IntParamAttribute attr, string[] args)
		{
			int Idx = FindParam(args, attr);
			if (Idx < 0) return;

			if (Idx==args.Length-1)
				throw new ArgumentException($"Argument {attr.Name} requries a value");

			try{
				pi.SetValue(obj, Int32.Parse(args[Idx+1]));
			} catch (Exception sfe) {
				RaiseException(attr, sfe);
			}
		}

		private void SetAttr(PropertyInfo pi, FloatParamAttribute attr, string[] args)
		{
			int Idx = FindParam(args, attr);
			if (Idx < 0) return;

			if (Idx==args.Length-1)
				throw new ArgumentException($"Argument {attr.Name} requries a value");

			try {
				pi.SetValue(obj, Double.Parse(args[Idx+1]));
			} catch (Exception sfe) {
				RaiseException(attr, sfe);
			}
		}

		private void RaiseException(BaseParamAttribute attr, Exception ex)
		{
			if (ex is TargetInvocationException)
				ex = ex.InnerException;
			throw new ArgumentException($"Argument {attr.Name} got a wrong value: {ex.Message}", ex);
		}

		private void SetAttr(PropertyInfo pInfo, AutoHelpAttribute attribute, string[] args)
		{
			int Idx = FindParam(args, attribute);
			if (Idx < 0) return;

			if (Title!=null) Console.WriteLine(Title);
			if (Copyright!=null) Console.WriteLine(Copyright);
			Console.WriteLine();

			Type theType = obj.GetType();

			foreach (var pi in theType.GetProperties()) {
				Attribute[] attrs = Attribute.GetCustomAttributes(pi);
				foreach (var attr in attrs) {
					if (attr is BaseParamAttribute) {
						PrintAttr((BaseParamAttribute)attr); 
						break;
					}
				}
			}
			Environment.Exit(0);
		}

		private void PrintAttr(BaseParamAttribute attr)
		{
			string mFlag = attr.Mandatory?"*":"";
			string keys = attr.Name.Length>0&&attr.LongName.Length>0?$"{attr.Name}, {attr.LongName}":$"{attr.Name}{attr.LongName}";
			string val = GetAttrType((dynamic)attr);
			Console.WriteLine($"{mFlag}\t{keys} {val}\t\t{attr.Desc}");
		}

		private string GetAttrType(IntParamAttribute attr)
		{
			return "<int>";
		}

		private string GetAttrType(StrParamAttribute attr)
		{
			return "<string>";
		}

		private string GetAttrType(FloatParamAttribute attr)
		{
			return "<float>";
		}

		private string GetAttrType(BaseParamAttribute attr)
		{
			return "";
		}


		public T Parse(string[] args)
		{
			Type theType = obj.GetType();

			foreach (var pi in theType.GetProperties()) {
				Attribute[] attrs = Attribute.GetCustomAttributes(pi);
				foreach (var attr in attrs) {
					if (attr is BaseParamAttribute) {
						SetAttr(pi, (dynamic)attr, args); 
						break;
					}
				}
			}
			return obj;
		}
	}

}
