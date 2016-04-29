using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common {
   public class common_const {
      public const int FRONTEND_PORT = 8505; // if you change this change also the endpoint value in the web service servicemanifest.xml
      public const string API_OFFSET = "api/values";
      public const string BCAST_POS_KWD = "broadcastPos";
      public const string LOCAL_NODE_ADDRESS = "localhost";
      public const string FRONTEND_ENDPOINT_NAME = "Web1TypeEndpoint";
   }

   public class Util {
      public static byte[] LatLngToByteArray(double lat, double lng) {
         byte[] latarr = BitConverter.GetBytes(lat);
         byte[] lngarr = BitConverter.GetBytes(lng);
         byte[] totarr = new byte[latarr.Length + lngarr.Length];
         latarr.CopyTo(totarr, 0);
         lngarr.CopyTo(totarr, latarr.Length);
         return totarr;
      }

      public static void ByteArrayToLatLng(byte[] totarr, out double lat, out double lng) {
         lat = lng = 0;
         byte[] testarr = BitConverter.GetBytes(lat);
         lat = BitConverter.ToDouble(totarr, 0);
         lng = BitConverter.ToDouble(totarr, testarr.Length);
      }
   }

}
