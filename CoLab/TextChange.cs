using ServiceStack.Text;

namespace CoLab
{
    public class TextChange
    {
        public Position start { get; set; }
        public Position end { get; set; }
        public string action { get; set; }
        public string[] lines { get; set; }
        public string sender { get; set; }

        public static TextChange FromJson(string json)
        {
            return JsonSerializer.DeserializeFromString<TextChange>(json);
        }
    }
}