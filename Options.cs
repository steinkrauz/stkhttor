using System;
using System.Text.RegularExpressions;
using StkCli;

namespace stkhttor
{
    class Options
    {
        [AutoHelp]
            public bool Help {get; set;}

        [FlagParam("-v","--verbose", "Print debug information")]
            public bool Trace {get; set;}

        [StrParam("","--tor-host",false,"Tor service host")]
            public string TorHost {get; set;}

        [IntParam("","--tor-port",false,"Tor service port")]
            public int TorPort {get; set; }

        private bool _UseEnv;
        [FlagParam("-e","--env", "Use http_proxy env var for listening settings")]
            public bool UseEnv {get => _UseEnv;
                set {
                    if (value==true) {
                        string envVal = Environment.GetEnvironmentVariable("http_proxy");
                        if (envVal == null)
                            throw new ArgumentException("Variable http_proxy is not defined!");

                        Match m = Regex.Match(envVal, @"http://(.+):(\d+)");
                        if (!m.Success)
                            throw new ArgumentException("Variable http_proxy has a wrong value!");
                        HttpHost = m.Groups[1].Value;
                        HttpPort = Int32.Parse(m.Groups[2].Value);
                    }
                    _UseEnv = value;
                }
            }

        [StrParam("-H","--host",false,"Listen address")]
            public string HttpHost {get; set;}

        [IntParam("-P","--port",false,"Listen port")]
            public int HttpPort {get; set;}

        public Options()
        {
            Trace = false;
            TorHost = "localhost";
            TorPort = 9150;
            UseEnv = false;
            HttpHost = "127.0.0.1";
            HttpPort = 13000;

        }

        private static Options _inst = null;

        public static Options GetOpts()
        {
            if (_inst == null)
                _inst = new Options();
            return _inst;
        }


    }
}        
