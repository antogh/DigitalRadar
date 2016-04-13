using System;
using System.Fabric;
using System.Fabric.Description;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Common;

namespace Web1 {
   public class Util {
      public static int GetFrontendPort() {
         int frontend_port = common_const.FRONTEND_PORT;

         CodePackageActivationContext ca = FabricRuntime.GetActivationContext();
         try {
            EndpointResourceDescription ep = ca.GetEndpoint(common_const.FRONTEND_ENDPOINT_NAME);
            frontend_port = ep.Port;
         }
         catch (Exception e) {
            string err_descr = e.Message;
            frontend_port = common_const.FRONTEND_PORT;
         }
         return frontend_port;
      }
   }
}
