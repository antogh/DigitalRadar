using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using System.Fabric;
using System.Fabric.Description;
using System.Net.WebSockets;

using System.Web.Script.Serialization;


using Common;

using Microsoft.ServiceFabric.Actors;
using UserActor.Interfaces;
using System.Threading;
using System.Text;

namespace Web1.Controllers {

   class WebSocketDataKpress {

      public WebSocketDataKpress(string pname, int pseconds, int pkey, string paddress) {
         user_name = pname;
         life_in_seconds = pseconds;
         key_pressed = pkey;
         node_address = paddress;
      }


      public string  user_name;
      public int life_in_seconds;
      public int key_pressed;
      public string node_address;   
   }


   class WebSocketDataUsrMove {

      public WebSocketDataUsrMove(string pname, double plat, double plng, string paddress) {
         user_name = pname;
         lat = plat;
         lng = plng;
         node_address = paddress;
      }


      public string user_name;
      public double lat, lng;
      public string node_address;
   }



   [Route("api/[controller]")]
   public class ValuesController : Controller {

      private ConcurrentDictionary<string, WebSocket> _ws_collection;

      public ValuesController(ConcurrentDictionary<string, WebSocket> ws_collection) {
         _ws_collection = ws_collection;
      }

      // GET: api/values
      [HttpGet]
      public IEnumerable<string> Get() {
         return new string[] { "value1", "value2" };
      }

      

      // GET api/values/name/seconds/key_pressed
      // broadcast the keypress message
      [HttpGet("{name}/{seconds}/{key_pressed}")]
      public bool Get(string name, int seconds, int key_pressed) {
         foreach (WebSocket cur_ws in _ws_collection.Values) {
            if (cur_ws.State == WebSocketState.Open) {
               var token = CancellationToken.None;
               JavaScriptSerializer jser = new JavaScriptSerializer();
               var wsdata = new WebSocketDataKpress(name, seconds, key_pressed, FabricRuntime.GetNodeContext().IPAddressOrFQDN);
               var type = WebSocketMessageType.Text;
               string message = jser.Serialize(wsdata);
               var data = Encoding.UTF8.GetBytes(message);
               var buffer = new ArraySegment<Byte>(data);
               cur_ws.SendAsync(buffer, type, true, token);
            }
         }
         return true;
      }

      // GET api/values/name/seconds/key_pressed
      // broadcast the position message
      [HttpGet(common_const.BCAST_POS_KWD + "/{name}/{lat}/{lng}")]
      public bool Get(string name, double lat, double lng) {
         foreach (WebSocket cur_ws in _ws_collection.Values) {
            if (cur_ws.State == WebSocketState.Open) {
               var token = CancellationToken.None;
               JavaScriptSerializer jser = new JavaScriptSerializer();
               var wsdata = new WebSocketDataUsrMove(name, lat, lng, FabricRuntime.GetNodeContext().IPAddressOrFQDN);
               var type = WebSocketMessageType.Text;
               string message = jser.Serialize(wsdata);
               var data = Encoding.UTF8.GetBytes(message);
               var buffer = new ArraySegment<Byte>(data);
               cur_ws.SendAsync(buffer, type, true, token);
            }
         }
         return true;
      }


      // GET api/values/name/key_pressed
      // exec a key press action on the actor
      [HttpGet("userMoved/{username}/{lat}/{lng}/{anti_cache}")]
      public string Get(string username, double lat, double lng, string anti_cache) { 
         ActorId act_id = new ActorId(username);
         IUserActor current_actor = ActorProxy.Create<IUserActor>(act_id, "UserActorProject", "UserActorService");
         current_actor.ExecUserMove(lat, lng);
         return "executed on " + FabricRuntime.GetNodeContext().IPAddressOrFQDN;
      }


      // GET api/values/login_name
      // login a new user
      [HttpGet("{login_name}")]
      public string Get(string login_name) {
         int frontend_port = Util.GetFrontendPort();
         bool init_actor_result = false;
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
