namespace DuAnCuaToi.Services
{
    public sealed class GeminiServiceException : Exception
    {
        public int HttpStatusCode { get; }

        public GeminiServiceException(
            int httpStatusCode,
            string message)
            : base(message)
        {
            HttpStatusCode = httpStatusCode;
        }
    }
}
