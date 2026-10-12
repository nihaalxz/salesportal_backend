using System;
using System.Text;

namespace TexasPortal.Application.Features.Invoices.Services;

public static class NumberToWordsConverter
{
    private static readonly string[] UnitsMap = { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
    private static readonly string[] TensMap = { "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

    public static string ToBhdWords(decimal amount)
    {
        if (amount < 0)
            return "Minus " + ToBhdWords(Math.Abs(amount));

        long dinars = (long)Math.Truncate(amount);
        int fils = (int)Math.Round((amount - dinars) * 1000m);

        if (fils >= 1000)
        {
            dinars += fils / 1000;
            fils %= 1000;
        }

        var sb = new StringBuilder();

        if (dinars == 0 && fils == 0)
        {
            return "Zero Dinars Only";
        }

        if (dinars > 0)
        {
            sb.Append(ConvertIntegerToWords(dinars));
            sb.Append(dinars == 1 ? " Dinar" : " Dinars");
        }

        if (fils > 0)
        {
            if (dinars > 0)
            {
                sb.Append(" And ");
            }
            sb.Append(ConvertIntegerToWords(fils));
            sb.Append(fils == 1 ? " Fils" : " Fils");
        }

        sb.Append(" Only");

        return sb.ToString();
    }

    private static string ConvertIntegerToWords(long number)
    {
        if (number == 0)
            return "Zero";

        if (number < 0)
            return "Minus " + ConvertIntegerToWords(Math.Abs(number));

        var sb = new StringBuilder();

        if ((number / 1000000) > 0)
        {
            sb.Append(ConvertIntegerToWords(number / 1000000)).Append(" Million ");
            number %= 1000000;
        }

        if ((number / 1000) > 0)
        {
            sb.Append(ConvertIntegerToWords(number / 1000)).Append(" Thousand ");
            number %= 1000;
        }

        if ((number / 100) > 0)
        {
            sb.Append(ConvertIntegerToWords(number / 100)).Append(" Hundred ");
            number %= 100;
        }

        if (number > 0)
        {
            if (number < 20)
            {
                sb.Append(UnitsMap[number]);
            }
            else
            {
                sb.Append(TensMap[number / 10]);
                if ((number % 10) > 0)
                {
                    sb.Append("-").Append(UnitsMap[number % 10]);
                }
            }
        }

        return sb.ToString().Trim();
    }
}
