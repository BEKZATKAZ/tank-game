namespace Tanks.Core
{
    public readonly struct ResultType
    {
        public ResultType(string errorCode, bool isSuccess)
        {
            ErrorCode = errorCode;
            IsSuccess = isSuccess;
        }

        public readonly string ErrorCode;
        public readonly bool IsSuccess;
    }

    public readonly struct ResultType<T>
    {
        public ResultType(T value, string errorCode, bool isSuccess)
        {
            Value = value;
            ErrorCode = errorCode;
            IsSuccess = isSuccess;
        }

        public readonly T Value;
        public readonly string ErrorCode;
        public readonly bool IsSuccess;

        public static implicit operator ResultType(ResultType<T> result)
        {
            return new ResultType(result.ErrorCode, result.IsSuccess);
        }

        public static implicit operator ResultType<T>(T value)
        {
            return new ResultType<T>(value, null, true);
        }
    }

    public static class Result
    {
        public static ResultType Success() => new("", true);
        public static ResultType Failure(string errorCode) => new(errorCode, false);

        public static ResultType<T> Success<T>(T value) => new(value, null, true);
        public static ResultType<T> Failure<T>(string errorCode) => new (default, errorCode, false);
    }
}