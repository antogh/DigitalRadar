
var API_OFFSET = "/api/values/";
var LAT_DEG_TO_KM = 0.00904371732957114692423173621285;
var LNG_DEG_TO_KM = 0.00898311174991016888250089831117;
var MIN_JUMP_METERS = 1;
var MAX_JUMP_METERS = 10000000;
var DEFAULT_JUMP_METERS = 5000;
var KEYCODE_LEFT = 37;
var KEYCODE_RIGHT = 39;
var KEYCODE_UP = 38;
var KEYCODE_DOWN = 40;
var KEYCODE_PLUS = 107;
var KEYCODE_MINUS = 109;
var KEYCODE_PGUP = 33;
var KEYCODE_PGDN = 34;
function cosDegrees(angle) { return Math.cos(angle / 180 * Math.PI); };
 

function myParse(str, min, max, def_val) {
   n = parseFloat(str);
   if (isNaN(min))
      return def_val;
   if (n >= min && n <= max)
      return n;
   return def_val;
}
