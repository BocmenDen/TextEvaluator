namespace TextEvaluator.AI.Base
{
    public static class Utils
    {
        public static string ConvertRole(this RoleType roleType) => roleType switch { RoleType.User => "user", RoleType.System => "system", _ => throw new Exception() }; 
    }
}
