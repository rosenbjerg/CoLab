using System.Collections.Generic;

namespace CoLab
{
    internal class NewProject : IValidateable
    {
        public string pn { get; set; }
        public string pd { get; set; }
        public List<string> co { get; set; }

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(pn)) return false;

            return true;
        }
    }
}