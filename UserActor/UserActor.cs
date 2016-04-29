using UserActor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Fabric;

using Common;

namespace UserActor {
   /// <remarks>
   /// Each ActorID maps to an instance of this class.
   /// The IProjName  interface (in a separate DLL that client code can
   /// reference) defines the operations exposed by ProjName objects.
   /// </remarks>
   internal class UserActor : StatefulActor<UserActor.ActorState>, IUserActor, IRemindable       {
      /// <summary>
      /// This class contains each actor's replicated state.
      /// Each instance of this class is serialized and replicated every time an actor's state is saved.
      /// For more information, see http://aka.ms/servicefabricactorsstateserialization
      /// </summary>
      [DataContract]
      internal sealed class ActorState {
         [DataMember]
         public int Count { get; set; }
         [DataMember]
         public DateTime activation_time { get; set; }
         [DataMember]
         public int key_pressed { get; set; }
         [DataMember]
         public int frontend_port { get; set; }
         [DataMember]
         public double lat { get; set; }
         [DataMember]
         public double lng { get; set; }

         public override string ToString() {
            return string.Format(CultureInfo.InvariantCulture, "UserActor.ActorState[Count = {0} activation time = {1}]", Count, activation_time);
         }
      }


      public class UserActorConst {
         public const string heartbit_reminder_name = "heartbeat";
         public const string key_press_reminder_name = "key_pressed";
         public const string kill_user_reminder_name = "kill_user";
         public const string user_moved_reminder_name = "user_moved";
      }

      /// <summary>
      /// This method is called whenever an actor is activated.
      /// </summary>
      protected override Task OnActivateAsync() {
         if (this.State == null) {
            // This is the first time this actor has ever been activated.
            // Set the actor's initial state values.
            this.State = new ActorState { Count = 0, activation_time = DateTime.Now , key_pressed = 0,
                                          frontend_port =common_const.FRONTEND_PORT, lat=0, lng=0};
         }

         ActorEventSource.Current.ActorMessage(this, "State initialized to {0}", this.State);
         return Task.FromResult(true);
      }

      public Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period) {
         ActorEventSource.Current.ActorMessage(this, "Received reminder {0}", reminderName);
         //if (reminderName.Equals(UserActorConst.heartbit_reminder_name)) {
         //   BroadcastMessage(this.State.key_pressed);
         //}
         if (reminderName.Equals(UserActorConst.key_press_reminder_name)) {
            int pressed_key = BitConverter.ToInt32(context, 0);
            BroadcastMessage(pressed_key);
         }
         else if (reminderName.Equals(UserActorConst.user_moved_reminder_name)) {
            double lat, lng;
            Util.ByteArrayToLatLng(context, out lat, out lng);
            BroadcastMessage(lat, lng);
         }
         else if (reminderName.Equals(UserActorConst.kill_user_reminder_name)) {
            BroadcastMessage(-1);
         }
         else
            return Task.FromResult(false);
         return Task.FromResult(true);
      }



      private void BroadcastMessage(double lat, double lng) {
         int cont, num_target_nodes = 1;
         string cur_address;
         bool node_is_up;

         FabricClient fcli = new FabricClient();
         FabricClient.QueryClient qcli = fcli.QueryManager;
         System.Fabric.Query.NodeList nl = qcli.GetNodeListAsync().Result;
         System.Fabric.Query.Node cnd;
         for (cont = 0; cont < nl.Count; cont++) {
            cnd = nl[cont];
            cur_address = cnd.IpAddressOrFQDN;
            if (cur_address.ToLower() != common_const.LOCAL_NODE_ADDRESS)
               num_target_nodes = nl.Count;
         }

         string responseString, req_uri;
         TimeSpan sec_life = DateTime.Now - this.State.activation_time;
         int spent_seconds = (int)sec_life.TotalSeconds;
         for (cont = 0; cont < num_target_nodes; cont++) {
            if (num_target_nodes == 1) {
               cur_address = common_const.LOCAL_NODE_ADDRESS;
               node_is_up = true;
            }
            else {
               cnd = nl[cont];
               cur_address = cnd.IpAddressOrFQDN;
               node_is_up = cnd.NodeStatus == System.Fabric.Query.NodeStatus.Up;
            }
            if (node_is_up) {
               req_uri = string.Format(@"http://{0}:{1}/{2}/{3}/{4}/{5}/{6}", cur_address, this.State.frontend_port, common_const.API_OFFSET, common_const.BCAST_POS_KWD, this.Id, lat, lng);
               HttpWebRequest request = (HttpWebRequest)WebRequest.Create(req_uri);
               request.Method = "GET";
               // Make the request and get the response.
               ActorEventSource.Current.ActorMessage(this, "Trying to execute broadcast message webapi request. Uri = {0}", req_uri);
               try {
                  using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                     using (StreamReader streamReader = new StreamReader(response.GetResponseStream(), true)) {
                        // Capture the response string.
                        responseString = streamReader.ReadToEnd();
                     }
                  }
               }
               catch (WebException e) {
                  // If there is a web exception, display the error message.
                  ActorEventSource.Current.ActorMessage(this, "Error while executing broadcast message webapi request: {0}", e.Message);
                  if (e.InnerException != null)
                     ActorEventSource.Current.ActorMessage(this, e.InnerException.Message);
               }
               catch (Exception e) {
                  // If there is another kind of exception, throw it.
                  throw (e);
               }
            }
         }
         fcli.Dispose();
      }




      private void BroadcastMessage(int key_pressed) {
         int      cont, num_target_nodes = 1;
         string   cur_address;
         bool     node_is_up;

         FabricClient fcli = new FabricClient();
         FabricClient.QueryClient qcli =  fcli.QueryManager;
         System.Fabric.Query.NodeList nl = qcli.GetNodeListAsync().Result;
         System.Fabric.Query.Node cnd;
         for (cont = 0; cont < nl.Count; cont++) {
            cnd = nl[cont];
            cur_address = cnd.IpAddressOrFQDN;
            if (cur_address.ToLower() != common_const.LOCAL_NODE_ADDRESS) 
               num_target_nodes = nl.Count;
         }

         string responseString, req_uri;
         TimeSpan sec_life = DateTime.Now - this.State.activation_time;
         int spent_seconds = (key_pressed == -1) ? -1 : (int) sec_life.TotalSeconds;
         for (cont = 0; cont < num_target_nodes; cont++) {
            if (num_target_nodes == 1) {
               cur_address = common_const.LOCAL_NODE_ADDRESS;
               node_is_up = true;
            }
            else {
               cnd = nl[cont];
               cur_address = cnd.IpAddressOrFQDN;
               node_is_up = cnd.NodeStatus == System.Fabric.Query.NodeStatus.Up;
            }
            if (node_is_up) {
               req_uri = string.Format(@"http://{0}:{1}/{2}/{3}/{4}/{5}", cur_address, this.State.frontend_port, common_const.API_OFFSET, this.Id, spent_seconds, key_pressed);
               HttpWebRequest request = (HttpWebRequest)WebRequest.Create(req_uri);
               request.Method = "GET";
               // Make the request and get the response.
               ActorEventSource.Current.ActorMessage(this, "Trying to execute broadcast message webapi request. Uri = {0}", req_uri);
               try {
                  using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                     using (StreamReader streamReader = new StreamReader(response.GetResponseStream(), true)) {
                        // Capture the response string.
                        responseString = streamReader.ReadToEnd();
                     }
                  }
               }
               catch (WebException e) {
                  // If there is a web exception, display the error message.
                  ActorEventSource.Current.ActorMessage(this, "Error while executing broadcast message webapi request: {0}", e.Message);
                  if (e.InnerException != null)
                     ActorEventSource.Current.ActorMessage(this, e.InnerException.Message);
               }
               catch (Exception e) {
                  // If there is another kind of exception, throw it.
                  throw (e);
               }
            }
         }
         fcli.Dispose();
      }

      /*
      private void SendHeartBeat() {
         BroadcastMessage(this.State.key_pressed);
      }
      */

      /*
      [Readonly]
      public Task<int> TestMyActor(int num) {
         IActorReminder myrem = RegisterReminderAsync(UserActorConst.heartbit_reminder_name, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), ActorReminderAttributes.Readonly).Result;
         ActorEventSource.Current.ActorMessage(this, "Registered reminder {0}", UserActorConst.heartbit_reminder_name);
         return Task.FromResult(num + 1);
      }
      */

      [Readonly]
      public Task KillUser() {
         IActorReminder myrem = RegisterReminderAsync(UserActorConst.kill_user_reminder_name, null, TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(-1), ActorReminderAttributes.Readonly).Result;
         ActorEventSource.Current.ActorMessage(this, "Registered reminder {0}", UserActorConst.key_press_reminder_name);
         /*
         IActorReminder reminder;
         try {
            reminder = this.GetReminder(UserActorConst.heartbit_reminder_name);
         }
         catch (System.Fabric.FabricException) {
            reminder = null;
         }

         return (reminder == null) ? Task.FromResult(true) : this.UnregisterReminderAsync(reminder);
         */
         return Task.FromResult(true);
      }

      public Task ExecUserMove(double lat, double lng) {
         this.State.lat = lat;
         this.State.lng = lng;
         IActorReminder myrem = RegisterReminderAsync(UserActorConst.user_moved_reminder_name, Util.LatLngToByteArray(lat, lng), TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(-1), ActorReminderAttributes.Readonly).Result;
         ActorEventSource.Current.ActorMessage(this, "Registered reminder {0}", UserActorConst.user_moved_reminder_name);
         return Task.FromResult(true);
      }

      public Task ExecKeyPress(int key_pressed) {
         this.State.key_pressed = key_pressed;
         IActorReminder myrem = RegisterReminderAsync(UserActorConst.key_press_reminder_name, BitConverter.GetBytes(key_pressed), TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(-1), ActorReminderAttributes.Readonly).Result;
         ActorEventSource.Current.ActorMessage(this, "Registered reminder {0}", UserActorConst.key_press_reminder_name);
         return Task.FromResult(true);
      }

      public Task<bool> InitUserActor(int frontend_port) {
         this.State.frontend_port = frontend_port;
         //IActorReminder myrem = RegisterReminderAsync(UserActorConst.heartbit_reminder_name, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), ActorReminderAttributes.Readonly).Result;
         //ActorEventSource.Current.ActorMessage(this, "Registered reminder {0}", UserActorConst.heartbit_reminder_name);
         return Task.FromResult(true);
      }

      [Readonly]
      Task<int> IUserActor.GetCountAsync() {
         // For methods that do not change the actor's state,
         // [Readonly] improves performance by not performing serialization and replication of the actor's state.
         ActorEventSource.Current.ActorMessage(this, "Getting current count value as {0}", this.State.Count);
         return Task.FromResult(this.State.Count);
      }

      Task IUserActor.SetCountAsync(int count) {
         ActorEventSource.Current.ActorMessage(this, "Setting current count of value to {0}", count);
         this.State.Count = count;  // Update the state

         return Task.FromResult(true);
         // When this method returns, the Actor framework automatically
         // serializes & replicates the actor's state.
      }


   }
}





















