namespace Caruti.Http.Extensions;

public static class NullableExtensions
{
    public static T GetOrThrowException<T>(this T? source)
        where T : struct
    {
        if (!source.HasValue)
            throw new NullReferenceException(nameof(source));

        return source.Value;
    }
}