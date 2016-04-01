using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace Web1
{
    public class SigrHub : Hub
    {
      public void Send(string message) {
        // Call the broadcastMessage method to update clients.
        Clients.All.broadcastMessage(message, 0, 0, string.Empty);
      }
    }
}
