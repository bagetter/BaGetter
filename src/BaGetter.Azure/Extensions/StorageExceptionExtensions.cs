using System.Net;
using Azure;

namespace BaGetter.Azure.Extensions
{

    internal static class StorageExceptionExtensions
    {
        public static bool IsAlreadyExistsException(this RequestFailedException e)
        {
            return e?.Status == (int?)HttpStatusCode.Conflict;
        }

        //public static bool IsNotFoundException(this TableStorageException e)
        //{
        //    return e?.RequestInformation?.HttpStatusCode == (int?)HttpStatusCode.NotFound;
        //}

        //public static bool IsAlreadyExistsException(this TableStorageException e)
        //{
        //    return e?.RequestInformation?.HttpStatusCode == (int?)HttpStatusCode.Conflict;
        //}

        public static bool IsPreconditionFailedException(this RequestFailedException e)
        {
            return e?.Status == (int?)HttpStatusCode.PreconditionFailed;
        }
    }
}
