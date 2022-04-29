# stkhttor
Ad-hoc http proxy for Tor

A simple command-line utility that allows to use Tor as a http proxy for any of your programs that cannot into SOCKS.

## Sample usage

```
set http_proxy=http://127.0.0.1:13001
start /b stkhttor -e
..\fossil.exe  pull
set /p PID=<skthttor.pid
taskkill /f /pid %PID%
```

In this example `http_proxy` variable is shared between stkhttor and fossil as a source of proxy settings. Stkhttor is started as a background process to get out of the way, but its messages are still in the console. After the work is done, we need to shutdown the proxy, so we are using process idenifier to kill it.

## Arguments

  | Argument | Description |
  | -------- | ----------- |
  | -v, --verbose | Print debug information |
  | --tor-host <string> | Tor service host (default: localhost) |
  | --tor-port <int> | Tor service port (default: 9150) |
  | -e, --env | Use http_proxy env var for listening settings |
  | -H, --host <string> | Listen address (default: localhost) |
  | -P, --port <int> | Listen port (default: 13000) |

  Obviously, -e conflicts with -H and -P, but there are no checks on this. 
  
  ## Requirements
  
  * TOR
  * .NET 5.0
  
  ## License
  
This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, version 3 of the License ONLY.
