using System;

namespace ImgProj.Utility;

internal static class StringFormatter
{
    public static string PadPageNumber(int pageNumber, int pageCount)
    {
        int digits = Convert.ToInt32(Math.Floor(Math.Log(pageCount, 10))) + 1;
        return pageNumber.ToString().PadLeft(digits, '0');
    }
}
