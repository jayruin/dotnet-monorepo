using System;

namespace Utils;

public static class ComparableExtensions
{
    extension<T>(T value) where T : IComparable<T>
    {
        public T Clamp(T min, T max)
        {
            if (value.CompareTo(min) < 0) return min;
            else if (value.CompareTo(max) > 0) return max;
            return value;
        }
    }
}
