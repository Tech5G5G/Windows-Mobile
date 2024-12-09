namespace Windows_Mobile.Helpers
{
    public static class ValueAssigner
    {
        public static T TryAssign<T>(Func<T> operation)
        {
            try
            {
                return operation.Invoke();
            }
            catch
            {
                return default;
            }
        }
        public static T TryAssign<T>(Func<T> operation, Func<T> altOperation)
        {
            try
            {
                return operation.Invoke();
            }
            catch
            {
                return altOperation.Invoke();
            }
        }
        public static T TryAssign<T, E>(Func<T> operation, Func<T> altOperation) where E : Exception
        {
            try
            {
                return operation.Invoke();
            }
            catch (E)
            {
                return altOperation.Invoke();
            }
        }

        public async static Task<T> TryAssignAsync<T>(Func<Task<T>> operation)
        {
            try
            {
                return await operation.Invoke();
            }
            catch
            {
                return default;
            }
        }
        public async static Task<T> TryAssignAsync<T>(Func<Task<T>> operation, Func<Task<T>> altOperation)
        {
            try
            {
                return await operation.Invoke();
            }
            catch
            {
                return await altOperation.Invoke();
            }
        }
        public async static Task<T> TryAssignAsync<T, E>(Func<Task<T>> operation, Func<Task<T>> altOperation) where E : Exception
        {
            try
            {
                return await operation.Invoke();
            }
            catch (E)
            {
                return await altOperation.Invoke();
            }
        }
    }
}
