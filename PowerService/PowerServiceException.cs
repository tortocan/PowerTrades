namespace PowerService
{
    [Serializable]
    public class PowerServiceException : Exception
    {
        public PowerServiceException(string? message) : base(message)
        {
        }
    }
}