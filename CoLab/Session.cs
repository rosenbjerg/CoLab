using System;

namespace CoLab
{
    internal class Session
    {
        public string Token { get; set; }
        public DateTime ExpireUTC { get; set; }
    }
}