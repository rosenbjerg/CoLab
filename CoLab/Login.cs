namespace CoLab
{
    internal class Login : IValidateable
    {
        public string u { get; set; }
        public string p { get; set; }

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(u)) return false;
            if (string.IsNullOrWhiteSpace(p)) return false;
            if (p.Length < 6) return false;

            return true;
        }
    }
}