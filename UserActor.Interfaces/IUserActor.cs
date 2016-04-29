using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace UserActor.Interfaces {
   /// <summary>
   /// This interface represents the actions a client app can perform on an actor.
   /// It MUST derive from IActor and all methods MUST return a Task.
   /// </summary>
   public interface IUserActor : IActor {
      Task<int> GetCountAsync();

      Task SetCountAsync(int count);
      //Task<int> TestMyActor(int num);
      Task KillUser();
      Task ExecKeyPress(int key_pressed);
      Task ExecUserMove(double lat, double lng);
      Task<bool> InitUserActor(int frontend_port);
   }
}
