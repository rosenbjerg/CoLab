using System;

namespace CoLab
{
    class Session
    {
        public string Token { get; set; }
        public string User { get; set; }
        public DateTime ExpireUTC { get; set; }
    }
}