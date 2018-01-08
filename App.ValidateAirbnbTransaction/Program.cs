using System;
using System.Globalization;
using Senstay.Dojo.FtpClient;
using Senstay.Dojo.Airbnb.Import;

namespace DojoAirbnbImportValidation
{
    class Program
    {
        static void Main(string[] args)
        {
            var transactionType = FtpTransactionType.Root; // assume to process both completed and future transactions
            DateTime startDate = DateTime.Today.Date;
            DateTime endDate = DateTime.Today.Date;
            if (args.Length > 0)
            {
                int type;
                if (Int32.TryParse(args[0], out type) == true && Enum.IsDefined(typeof(FtpTransactionType), type))
                {
                    transactionType = (FtpTransactionType)type;
                }
                DateTime.TryParseExact(args[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate);
                DateTime.TryParseExact(args[2], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate);
            }

            try
            {
                AirbnbImportService airbnbService = new AirbnbImportService();

                if (transactionType == FtpTransactionType.Completed)
                    airbnbService.ValidateTransactions(FtpTransactionType.Completed, startDate, endDate);

                //if (transactionType == FtpTransactionType.Future)
                //    airbnbService.ImportTransactions(FtpTransactionType.Future);

                //if (transactionType == FtpTransactionType.Gross)
                //    airbnbService.ImportTransactions(FtpTransactionType.Gross);
            }
            catch (Exception ex)
            {
                string innerMsg = (ex.InnerException == null ? "" : (ex.InnerException.Message == null ? "" : ex.InnerException.Message));
                Console.WriteLine(ex.Message + "\r\n" + innerMsg);
            }

            return;
        }
    }
}
