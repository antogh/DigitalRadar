The digital radar sample project
-------------

I started this side project to try to put in practice part of what I learned in the Service Fabric tutorial. The idea that I chose to implement is a sort of digital radar where any logged user can see in real time on Google Map the moving position of all the other logged users.  

Before you read the rest of this document you can quickly view the application in action as it is today (19th of may 2016) in a short video (< 2 minutes) 

[![IMAGE ALT TEXT](http://i.imgur.com/4geD2py.png?1)](https://player.vimeo.com/video/166978620?autoplay=1)

The purpose of this project is to "learn by doing" service fabric and possibly share my findings with other developers approaching the platform. The application at current stage is quite simple and that makes it good to be used as a sample. You can use this repo to have an example of : 

 - **ASP.NET core** stateless service as a front end for a SF solution
 - **WebSocket** server and client logic for a SF solution front end. You can use this for many other projects [(1)](#note1) where you need bi-directional communication between browser and cluster, one of the many  cases could be an IoT command & control app from a browser.  
 - **Google map API** example
 - Easy instructions and tips to **deploy** the solution to a cluster in the cloud

I have many ideas for new features so check out new releases from time to time. Contact me for any suggestion or anything related to this project. You can use the "Issues" tab here on GitHub
<BR/><BR/>

#### The Visual studio solution
The whole applications is created as a Visual studio 2015 solution composed of multiple projects:<BR/>
 - **Web1** <BR/>Front end logic (ASP.NET core / Websocket / Google Map API)
 - **Web1Project**<BR/>Used to deploy Web1 app to the SF cluster
 - **UserActor** <BR/>Back end logic (SF actor model - stateful actor with reminders). This is the part I plan to enrich more for new features.
 - **UserActorProject**<BR/>Used to deploy UserActor to the SF cluster.

Additional projects in the solution contains interfaces and helper functions used by the main projects.
<BR/><BR/>

#### The logic flow  
![Entity Hierarchy](http://i.imgur.com/IBKleRQ.png)
The user login with the index.html page, he gives very simple credentials, just a username and his start location, at the moment there is no security nor  authentication.
After login he is redirected to UsersMap.html, in this page the client script first request a websocket connection to the web1 service and then display the map with the starting location at the center.
The starting user position is transmitted to Web1 service using a WebAPI function. 
Web1 in turn transmits the user position to UserActor using the SF ActorProxy. 
UserActor stores the new position in the reliable state and set a reminder that fires 200 msec later that will broadcast the new position to all the other users. 
The reminder will get from the FabricClient class the list of all active nodes addresses, this information together with the front end port stored in the actor state allow the reminder to transmit, with a WebAPI function, to all Web1 services instances (one for each node) that a particular user is located now at some lat/lng coordinates. 
Web1 broadcast the new user position received from the reminder to all the logged users using the list of all the currently open Websocket connections (_ws_collection). At this point the control return to the browser page UsersMap.html where a javascript callback receives the data transmitted from the server via WebSocket, this data used to display the new user position with the Google Map API. 
As the user change position the flow starts over. Multiple users moving generate a continuous stream of data the animates all the maps of logged users on their browsers.  
<BR/>

#### Build and deployment
Build and deployment to the local cluster on your dev machine is quite easy and it's the default action when you hit the F5 debug on Visual studio. I will provide some instruction for when you want to deploy to a remote cluster in the cloud.
Before you build and deploy the packages  make sure that the following configuration files contain appropriate values:

Front end port in:<BR/>**Web1\PackageRoot\ServiceManifest.xml**<BR/>
```<Endpoint Name="Web1TypeEndpoint" Protocol="http" Type="Input" Port="8505" />```
<BR/>The **port** value (in this example 8505) will be the front end port used by Web1 stateless service, so if for example the cluster address is:
http://party55564.westus.cloudapp.azure.com
you would get the index.html page with the following URL
http://party55564.westus.cloudapp.azure.com:8505/index.html

Front end service instance count in:<BR/>**Web1Project\ApplicationParameters\Cloud.xml**
<BR/>```<Parameter Name="Web1_InstanceCount" Value="-1" />```
<BR/>This value is important and must be -1, it allows to create an instance of the Web1 service on each node in the cluster. The cluster in the cloud is located behind a load balancer that will route requests to all the nodes, that's why you need an instance of Web1 service running on each node.

Cluster address for both front end and back end services in:<BR/>**Web1Project\PublishProfiles\Cloud.xml**<BR/>**UserActorProject\PublishProfiles\Cloud.xml**
<BR/>In these two files you must configure the cluster publishing address. This address is provided by the cloud vendor when you create or join to a SF cluster, for example it might be:
<BR/>```<ClusterConnectionParameters ConnectionEndpoint="party55564.westus.cloudapp.azure.com:19000" />```

To build the solution you just have to open it in Visual studio 2015 with the SF dev environment properly configured [(2)](#note2) then select "Build all" or "Rebuild all". The build will generate the packages that will be copied to the cluster with some scripts contained in the visual studio projects described above.

The easiest way to deploy the project packages to the cluster is with Visual studio, right click each of the 2 deployment projects (Web1Project and UserActorProject) and select "Publish", before that make sure you set the correct parameters described above and select cloud target profile in the dropdown list, then click publish and wait the time to transfer the package to the cloud, the output window will inform you about all the deployment steps. 
Pay attention to the deployment of Web1Project, the package has 50MB size and I discovered an issue [(3)](#note3) with the script  that has an hardcoded timeout of 10 minutes, if your internet connection is not able to upload this amount of data in < 10 minutes you might get a timeout error, in this case I can suggest you to create a Visual studio VM on azure and deploy the project from there. This issue might be solved by the time you read it or not affect you if you have a fast upload speed, anyway I wanted to warn you ;)

*I hope this sample can be useful to you! Happy learning Service Fabric and happy coding!* ;-)  <BR/>
Antonio
<BR/><BR/>
#### Notes / drill down
<a name="note1"></a>
(1) If you are interested in how the WebSocket is handled at the server side in ASP.NET core I recommend you a couple of links<BR/>
https://docs.asp.net/en/latest/fundamentals/middleware.html<BR/>
https://medium.com/@turowicz/websockets-in-asp-net-5-6094319a15a2#.o2rktyr9h

<a name="note2"></a>
(2) Prepare your development environment<BR/>
https://azure.microsoft.com/en-us/documentation/articles/service-fabric-get-started/

<a name="note3"></a>
(3) The SF team is aware of it and they are going to fix it soon. Read comment posted by me (as AntonM) at the bottom of this article, where I report the issue:<BR/>
https://azure.microsoft.com/en-us/documentation/articles/service-fabric-publish-app-remote-cluster/<BR/>
read also this:<BR/>
https://social.msdn.microsoft.com/Forums/azure/en-US/6e17923d-e7d4-4b57-a78f-1fdb9ba5a3b6/timeout-error-while-deploying-my-app-to-azure-or-party-cluster-how-can-i-change-the-tout-param?forum=AzureServiceFabric


