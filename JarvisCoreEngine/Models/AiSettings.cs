namespace JarvisCoreEngine.Models
{
    public class AiSettings
    {
        public string Provider { get; set; } = "OpenAI";
        public string ApiKey { get; set; } = "";
        public string Model { get; set; } = "gpt-4o-mini";
        public string Endpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
        public string SystemPrompt { get; set; } = "You are JARVIS.";
        public int MaxHistory { get; set; } = 10;
    }
}