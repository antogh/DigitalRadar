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

         public override string ToString() {
            return string.Format(CultureInfo.InvariantCulture, "UserActor.ActorState[Count = {0} activation time = {1}]", Count, activation_time);
         }
      }


      public class UserActorConst {
         public const string heartbit_reminder_name = "heartbeat";
         public const string key_press_reminder_name = "key_pressed";
         public const string kill_user_reminder_name = "kill_user";
      }

      /// <summary>
      /// This method is called whenever an actor is activated.
      /// </summary>
      protected override Task OnActivateAsync() {
         if (this.State == null) {
            // This is the first time this actor has ever been activated.
            // Set the actor's initial state values.
            this.State = new ActorState { Count = 0, activation_time = DateTime.Now , key_pressed = 0};
         }

         ActorEventSource.Current.ActorMessage(this, "State initialized to {0}", this.State);
         return Task.FromResult(true);
      }

      public Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period) {
         ActorEventSource.Current.ActorMessage(this, "Received reminder {0}", reminderName);
         if (reminderName.Equals(UserActorConst.heartbit_reminder_name)) {
            BroadcastMessage(this.State.key_pressed);
         }
         else if (reminderName.Equals(UserActorConst.key_press_reminder_name)) {
            int pressed_key = BitConverter.ToInt32(context, 0);
            BroadcastMessage(pressed_key);
         }
         else if (reminderName.Equals(UserActorConst.kill_user_reminder_name)) {
            BroadcastMessage(-1);
         }
         else
            return Task.FromResult(false);
         return Task.FromResult(true);
      }


      private void BroadcastMessage(int key_pressed) {
         string responseString, req_uri;
         TimeSpan sec_life = DateTime.Now - this.State.activation_time;
         int spent_seconds = (key_pressed == -1) ? -1 : (int) sec_life.TotalSeconds;
         req_uri = string.Format(@"http://localhost:{0}/{1}/{2}/{3}/{4}", common_const.webapi_port, common_const.API_OFFSET, this.Id, spent_seconds, key_pressed);
         HttpWebRequest request = (HttpWebRequest)WebRequest.Create(req_uri);
         request.Method = "GET";

         // Make the request and get the response.
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
            Console.WriteLine("Error getting the list of system services:");
            Console.WriteLine(e.Message);
            if (e.InnerException != null)
               Console.WriteLine(e.InnerException.Message);
            return;
         }
         catch (Exception e) {
            // If there is another kind of exception, throw it.
            throw (e);
         }
      }

      /*
      private void SendHeartBeat() {
         BroadcastMessage(this.State.key_pressed);
      }
      */ 


      [Readonly]
      public Task<int> TestMyActor(int num) {
         IActorReminder myrem = RegisterReminderAsync(UserActorConst.heartbit_reminder_name, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), ActorReminderAttributes.Readonly).Result;
         ActorEventSource.Current.ActorMessage(this, "Registered reminder {0}", UserActorConst.heartbit_reminder_name);
         return Task.FromResult(num + 1);
      }

      [Readonly]
      public Task KillUser() {
         IActorReminder myrem = RegisterReminderAsync(UserActorConst.kill_user_reminder_name, null, TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(-1), ActorReminderAttributes.Readonly).Result;
         ActorEventSource.Current.ActorMessage(this, "Registered reminder {0}", UserActorConst.key_press_reminder_name);
         IActorReminder reminder;
         try {
            reminder = this.GetReminder(UserActorConst.heartbit_reminder_name);
         }
         catch (System.Fabric.FabricException) {
            reminder = null;
         }

         return (reminder == null) ? Task.FromResult(true) : this.UnregisterReminderAsync(reminder);
      }


      public Task ExecKeyPress(int key_pressed) {
         this.State.key_pressed = key_pressed;
         IActorReminder myrem = RegisterReminderAsync(UserActorConst.key_press_reminder_name, BitConverter.GetBytes(key_pressed), TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(-1), ActorReminderAttributes.Readonly).Result;
         ActorEventSource.Current.ActorMessage(this, "Registered reminder {0}", UserActorConst.key_press_reminder_name);
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





















