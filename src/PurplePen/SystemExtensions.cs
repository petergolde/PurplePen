using System;

namespace PurplePen
{
    public static class SystemExtensions
    {
        public static TResult[] ConvertAll<TSource, TResult>(this TSource[] _, Converter<TSource, TResult> converter)
        {
            return Array.ConvertAll(_, converter);
        }
    }
}