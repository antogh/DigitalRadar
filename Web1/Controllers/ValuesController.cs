using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.SignalR;

using Microsoft.ServiceFabric.Actors;
using UserActor.Interfaces;

namespace Web1.Controllers {
   [Route("api/[controller]")]
   public class ValuesController : Controller {
      private IHubContext myhub;

      public ValuesController(Microsoft.AspNet.SignalR.Infrastructure.IConnectionManager connectionManager) {
         myhub = connectionManager.GetHubContext<SigrHub>();
      }

      // GET: api/values
      [HttpGet]
      public IEnumerable<string> Get() {
         return new string[] { "value1", "value2" };
      }

      
      // GET api/values/name/message
      // broadcast the message
      [HttpGet("{name}/{seconds}")]
      public bool Get(string name, int seconds) {
         myhub.Clients.All.broadcastMessage(name, seconds);
         return true;
      }


      // GET api/values/login_name
      // login a new user
      [HttpGet("{login_name}")]
      public string Get(string login_name) {
         ActorId act_id = new ActorId(login_name);
         //IUserActor current_actor = ActorProxy.Create<IUserActor>(act_id, @"fabric:/UserActorProject/UserActorService");
         IUserActor current_actor = ActorProxy.Create<IUserActor>(act_id, "UserActorProject", "UserActorService");
         int result = current_actor.TestMyActor(1).Result;
         return login_name;
      }


      // POST api/values
      [HttpPost]
      public void Post([FromBody]string value) {
      }

      // PUT api/values/5
      [HttpPut("{id}")]
      public void Put(int id, [FromBody]string value) {
      }

      // DELETE api/values/5
      [HttpDelete("{id}")]
      public void Delete(int id) {
      }
   }
}
