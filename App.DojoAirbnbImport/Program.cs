using System;
using Senstay.Dojo.FtpClient;
using Senstay.Dojo.Airbnb.Import;

namespace DojoAirbnbImport
{
    /// <summary>
    /// Import Airbnb transaction data downloaded by scraper to Dojo database
    /// </summary>
    /// <param name="transaction type">numerical value of FtpTransactionType; if not given, both completed and future transactions will be processed</param>
    /// <returns>none</returns>
    class Program
    {
        static void Main(string[] args)
        {
            var transactionType = FtpTransactionType.Root; // assume to process both completed and future transactions
            if (args.Length > 0)
            {
                int type;
                if (Int32.TryParse(args[0], out type) == true && Enum.IsDefined(typeof(FtpTransactionType), type))
                {
                    transactionType = (FtpTransactionType)type;
                }
            }

            try
            {
                AirbnbImportService airbnbService = new AirbnbImportService();

                if (transactionType == FtpTransactionType.Root || transactionType == FtpTransactionType.Completed)
                    airbnbService.ImportTransactions(FtpTransactionType.Completed);

                if (transactionType == FtpTransactionType.Root || transactionType == FtpTransactionType.Future)
                    airbnbService.ImportTransactions(FtpTransactionType.Future);

                if (transactionType == FtpTransactionType.Root || transactionType == FtpTransactionType.Gross)
                    airbnbService.ImportTransactions(FtpTransactionType.Gross);
                // TODO: import other transaction types
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
