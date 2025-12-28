using System;

namespace env_analysis_project.Models
{
    public static class ParameterTypeHelper
    {
        public static string Normalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "water";
            }

            var normalized = input.Trim().ToLowerInvariant();
            return normalized is "air" or "water" ? normalized : "water";
        }

        public static bool IsValid(string? input) =>
            !string.IsNullOrWhiteSpace(input) &&
            (string.Equals(input.Trim(), "air", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(input.Trim(), "water", StringComparison.OrdinalIgnoreCase));
    }
}
