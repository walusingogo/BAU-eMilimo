using System;

namespace UCP.Core.Utils
{
    public static class NumberToWords
    {
        public static string Convert(decimal number)
        {
            if (number == 0)
                return "Zero";

            var wholePart = (int)number;
            var fractionalPart = (int)((number - wholePart) * 100);

            var words = ConvertWholeNumber(wholePart);

            if (fractionalPart > 0)
            {
                words += $" and {fractionalPart:00}/100";
            }

            return words + " Kwacha";
        }

        private static string ConvertWholeNumber(int number)
        {
            if (number == 0)
                return "";

            var words = "";

            if (number >= 1000000)
            {
                words += ConvertWholeNumber(number / 1000000) + " Million ";
                number %= 1000000;
            }

            if (number >= 1000)
            {
                words += ConvertWholeNumber(number / 1000) + " Thousand ";
                number %= 1000;
            }

            if (number >= 100)
            {
                words += ConvertWholeNumber(number / 100) + " Hundred ";
                number %= 100;
            }

            if (number > 0)
            {
                if (words != "")
                    words += "and ";

                var unitsMap = new[]
                {
                    "Zero", "One", "Two", "Three", "Four", "Five",
                    "Six", "Seven", "Eight", "Nine", "Ten",
                    "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen",
                    "Sixteen", "Seventeen", "Eighteen", "Nineteen"
                };
                var tensMap = new[]
                {
                    "Zero", "Ten", "Twenty", "Thirty", "Forty",
                    "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
                };

                if (number < 20)
                    words += unitsMap[number];
                else
                {
                    words += tensMap[number / 10];
                    if ((number % 10) > 0)
                        words += "-" + unitsMap[number % 10];
                }
            }

            return words.Trim();
        }
    }
}