namespace Windows_Mobile
{
    public static class Extensions
    {
        public static BitmapImage ToBitmapImage(this Stream stream)
        {
            MemoryStream ms = new();
            stream.CopyTo(ms);
            ms.Position = 0;
            var bitmapImage = new BitmapImage();
            bitmapImage.SetSource(ms.AsRandomAccessStream());
            return bitmapImage;
        }

        public static int Clamp(this int input, int max, int min = 0)
        {
            if (input >= min && input <= max)
                return input;
            else if (input > max)
                return max;
            else if (input < min)
                return min;
            else
                throw new Exception("Unable to compare value of input");
        }

        /// <summary>Converts a number from 1 to 12 into a month name</summary>
        /// <returns>The name of the month</returns>
        /// <exception cref="InvalidDataException"/>
        public static string ToMonthName(this int monthNum)
        {
            return monthNum switch
            {
                1 => "January",
                2 => "February",
                3 => "March",
                4 => "April",
                5 => "May",
                6 => "June",
                7 => "July",
                8 => "August",
                9 => "September",
                10 => "October",
                11 => "November",
                12 => "December",
                _ => throw new InvalidDataException("Value is not a a month number (1, 7, 12, etc.)"),
            };
        }
    }
}
