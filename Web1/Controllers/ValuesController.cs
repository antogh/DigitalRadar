using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.SignalR;
using System.Fabric;
using System.Fabric.Description;
using Common;

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

      
      // GET api/values/name/seconds
      // broadcast the message
      /*
      [HttpGet("{name}/{seconds}")]
      public bool Get(string name, int seconds) {
         myhub.Clients.All.broadcastMessage(name, seconds, 0);
         return true;
      }
      */

      // GET api/values/name/seconds/key_pressed
      // broadcast the message
      [HttpGet("{name}/{seconds}/{key_pressed}")]
      public bool Get(string name, int seconds, int key_pressed) {
         myhub.Clients.All.broadcastMessage(name, seconds, key_pressed, FabricRuntime.GetNodeContext().IPAddressOrFQDN);
         return true;
      }


      // GET api/values/name/key_pressed
      // exec a key press action on the actor
      [HttpGet("keyPressed/{username}/{key_pressed}/{anti_cache}")]
      public string Get(string username, int key_pressed, string anti_cache) { 
         ActorId act_id = new ActorId(username);
         IUserActor current_actor = ActorProxy.Create<IUserActor>(act_id, "UserActorProject", "UserActorService");
         current_actor.ExecKeyPress(key_pressed);
         return "executed on " + FabricRuntime.GetNodeContext().IPAddressOrFQDN;
      }


      // GET api/values/login_name
      // login a new user
      [HttpGet("{login_name}")]
      public string Get(string login_name) {
         int frontend_port = common_const.FRONTEND_PORT;
         bool init_actor_result = false;

         CodePackageActivationContext ca = FabricRuntime.GetActivationContext();
         try {
            EndpointResourceDescription ep = ca.GetEndpoint(common_const.FRONTEND_ENDPOINT_NAME);
            frontend_port = ep.Port;
         }
         catch (Exception e) {
            string err_descr = e.Message;
            frontend_port = common_const.FRONTEND_PORT;
         }
         string username = "";
         bool kill_user = false;
         string[] login_params = login_name.Split(new char[] { ' ' });
         if ((login_params.Count() > 1) && (login_params[0].Trim().ToLower() == "kill")) {
            username = login_params[1];
            kill_user = true;
         }
         else
            username = login_params[0];
         ActorId act_id = new ActorId(username);
         IUserActor current_actor = ActorProxy.Create<IUserActor>(act_id, "UserActorProject", "UserActorService");
         if (!kill_user)
            init_actor_result = current_actor.InitUserActor(frontend_port).Result;
         else
            current_actor.KillUser();
         string ret_descr = string.Format("login name = {0}, init_actor_result = {1}, frontend_port={2}", 
                                          login_name, init_actor_result, frontend_port);
         return (ret_descr);
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
