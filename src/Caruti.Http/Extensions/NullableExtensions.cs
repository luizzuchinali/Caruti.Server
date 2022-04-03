namespace Caruti.Http.Extensions;

public static class NullableExtensions
{
    public static T GetOrThrowException<T>(this T? source) where T : struct
    {
        if (!source.HasValue)
            throw new NullReferenceException(nameof(source));

        return source.Value;
    }

    public static T GetOrThrowException<T>(this T? source) where T : class
    {
        if (source == null)
            throw new NullReferenceException(nameof(source));

        return source;
    }

    public static string GetOrThrowException(this string? source) =>
        GetOrThrowException<string>(source);
}