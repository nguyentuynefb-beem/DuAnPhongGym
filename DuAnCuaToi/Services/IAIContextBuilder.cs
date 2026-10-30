using DuAnCuaToi.Models;

namespace DuAnCuaToi.Services
{
    public interface IAIContextBuilder
    {
        AIMemberContext BuildMemberAIContext(int userId);
        string BuildWorkoutAIContext(int userId);
        AIGymContext BuildGymAIContext();
        AIMemberContext BuildMemberAIChatContext(int userId, string question);
        AIGymContext BuildGymAIChatContext(string question);
        List<AIPTAvailabilityData> BuildPTAvailabilityForAI(string question);
    }
}
