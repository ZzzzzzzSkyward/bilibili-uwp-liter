        private static bool IsValidDanmuRegex(string pattern)
        {
            try
            {
                Regex.IsMatch(string.Empty, pattern);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
