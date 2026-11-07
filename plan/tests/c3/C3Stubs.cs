using System;
using Newtonsoft.Json.Linq;

namespace BiliLite.Helpers
{
    /// <summary>
    /// Mirrors the product HttpResults fields the envelope reader uses, so the real parsing layer
    /// can run outside UWP: transport status, message, HTTP code and the raw body.
    /// </summary>
    public class HttpResults
    {
        public bool status { get; set; } = true;
        public string message { get; set; }
        public int code { get; set; }
        public string results { get; set; }

        public bool TryGetJObject(out JObject value)
        {
            value = null;
            if (string.IsNullOrEmpty(results)) return false;
            try
            {
                value = JObject.Parse(results);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
