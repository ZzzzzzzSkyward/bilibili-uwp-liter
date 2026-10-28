        private static bool IsValidDanmuRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return false;
            try
            {
                new Regex(pattern);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
