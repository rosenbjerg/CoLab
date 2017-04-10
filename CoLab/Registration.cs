namespace CoLab
{
    internal class Registration : IValidateable
    {
        public string fn { get; set; }
        public string ln { get; set; }
        public string dn { get; set; }
        public string ci { get; set; }
        public string co { get; set; }
        public string dt { get; set; }
        public string em { get; set; }
        public string pw { get; set; }

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(fn)) return false;
            if (string.IsNullOrWhiteSpace(ln)) return false;
            if (string.IsNullOrWhiteSpace(dn)) return false;
            if (string.IsNullOrWhiteSpace(ci)) return false;
            if (string.IsNullOrWhiteSpace(co)) return false;
            if (string.IsNullOrWhiteSpace(em)) return false;
            if (string.IsNullOrWhiteSpace(pw)) return false;
            if (pw.Length < 6) return false;
            if (dn.Length < 6) return false;
            if (co.Length != 2) return false;

            return true;
        }
    }
}