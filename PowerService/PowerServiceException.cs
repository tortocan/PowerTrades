
namespace PowerService
{
    [Serializable]
    public class PowerServiceException : Exception
    {
        public PowerServiceException()
        {
        }

        public PowerServiceException(string? message) : base(message)
        {
        }

        public PowerServiceException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}