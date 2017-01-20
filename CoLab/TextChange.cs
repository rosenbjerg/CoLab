namespace CoLab
{
    public class TextChange
    {
        public static TextChange FromJson(string json)
        {
            return ServiceStack.Text.JsonSerializer.DeserializeFromString<TextChange>(json);
        }

        public Position start { get; set; }
        public Position end { get; set; }
        public string action { get; set; }
        public string[] lines { get; set; }
        public string sender { get; set; }
    }
}