  
########################################################################  
 This project is subject to the terms of the Mozilla Public  
 License, v. 2.0. If a copy of the MPL was not distributed with this  
 file, You can obtain one at http://mozilla.org/MPL/2.0/  
 Any copyright is dedicated to the NominalNimbus.  
 https://github.com/NominalNimbus  
########################################################################
  
**_NominalNimbus_ project is a client <=> server Trading Platform with stron focus on Algorithmic Trading with build in features like:**  
**+ .net Scripting Library on client side**
**+ Server Side headless deployment of trading algos**
**+ Backtesting Engine**  
**+ Marging Broker and Non-Marging Broker Simulation**  
**+ Combine real market datafeed with Broker-Account Simulation to test your trading ideas**  
**+ LMAX API for Demo & Live Trading**  
**+ Poloniex API for Live Trading**  
  
  
  # ServiceNimbus
  
Service Nimbus is a .net service application to receive marketdata from any maketdata source and and distribute them to:  
ClientNimbus (GUI client),  
TacticNimbus (service to execute  scripts)  
Database (store market data and porfolio info).  
  
It also hosts an order management system (OMS) to route orders comming from ClientNimbus and TacticNimbus.  
Inside the application it is named "TradingServer"
  
To build and run ServiceNimbus follow this steps:  
+ download and install Visual Studio Community Edition (free)  
+ checkout ServiceNimbus repository  
+ download and install SQL Server (free)  
+ create new database named "TradingServer"  
+ execute "script.sql" and check if tabels where created  
+ build ServiceNimbus solution in Visual Studio  
+ follow the steps from "How To.doc" for further setup instructions  
+ **Video tutorials available here: https://www.youtube.com/playlist?list=PLbiaIO7sG7FYlrgOV1XLHrnaufJ7fK6Hx**  
  
  
![servicenimbus](https://user-images.githubusercontent.com/44921994/53283180-b70d9280-3742-11e9-8172-5fbe6f56d14b.png)  
.  
.  
.  
## TacticNimbus (ScriptingService)

The 2nd part of ServiceNimbus is TacticNimbus wich is a .net service application to host and run user generated scripts.  
These scripcts could contain trading rules or any kind of data analysis.  
  
How to rund TacticNimbus:  
+ follow the steps in How To.doc
+ in addition you need to intstall RabbitMQ and Erlang
+ after ServiceNimbus build, go to ScriptingService target folder  
+ optional: copy the target folder to any place of you choice for better overview and maintainace
+ run ScriptingService.exe  
+ **Video tutorials available here: https://www.youtube.com/playlist?list=PLbiaIO7sG7FYlrgOV1XLHrnaufJ7fK6Hx**  
.  
.  
.  
![tacticnimbus](https://user-images.githubusercontent.com/44921994/53283182-bd037380-3742-11e9-9eb1-f098f6f00cd3.png)  
.  
.  
### Donation to keep this up and running:
IOTA: FKD9BYAHVBMDDW9DQBBTOFJEHFWZNTYB9UBHPBMABACHFMGGQIBVBLLDLEWYXOGAGGVZVCPVVXHUFTJU9YGNADFNGW
ETH:  0x88920B317625fDfe27A8a2353A1173D3097083D2
