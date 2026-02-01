namespace AiAgent;

public class AiAgent : AiAgentClientBase
{
    public AiAgent() : base(new Uri(Environment.GetEnvironmentVariable("AI_API_URL") ??
                                    throw new Exception("AI_API_URL environment variable not set.")))
    {
    }
}