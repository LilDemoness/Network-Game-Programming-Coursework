namespace UnityServices
{
    public struct UnityServiceErrorMessage
    {
        public enum Service
        {
            Authentication,
            Session,
        }

        public string Title;
        public string Message;
        public Service AffectedService;
        public System.Exception OriginalException;

        public UnityServiceErrorMessage(string title, string message, Service service, System.Exception originalException = null)
        {
            this.Title = title;
            this.Message = message;
            this.AffectedService = service;
            this.OriginalException = originalException;
        }
    }
}