namespace KeClientTracing.LogReading.LogDescribing
{
    public class ParameterRule
    {
        public string ParameterName { get; private set; }
        public string Description { get; private set; }
        public bool IncludeValue { get; private set; }

        public ParameterRule( string parameterName, string description, bool includeValue)
        {
            Description = description;
            IncludeValue = includeValue;
            ParameterName = parameterName;
        }
    }
}