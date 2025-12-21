namespace TextEvaluator.Cache
{
    public class DBCacheModel
    {
        public string Key { get; set; } = null!;
        public string IDModel { get; set; } = null!;
        public string IDCriteria { get; set; } = null!;
        public string JsonData { get; set; } = null!;
        public string FullNameTypename { get; set; } = null!;
    }
}