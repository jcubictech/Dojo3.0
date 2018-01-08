using System;
using System.Collections.Generic;
using System.Web;
using System.Net.Mail;

namespace Senstay.Dojo.Helpers
{
    public class EmailHelper
    {
        public static void SendEmail(string recipient, string subject, string message)
        {
            string[] recipients = new string[] { recipient };
            SendEmail(string.Empty, 0, string.Empty, recipients, new List<string>(), subject, message, true, null);
        }

        public static void SendEmail(string recipient, string subject, string message, List<string> Ccs)
        {
            string[] recipients = new string[] { recipient };
            SendEmail(string.Empty, 0, string.Empty, recipients, Ccs, subject, message, true, null);
        }

        public static void SendEmail(string sender, string[] recipients, string subject, string message)
        {
            SendEmail(string.Empty, 0, sender, recipients, new List<string>(), subject, message, true, null);
        }

        public static void SendEmail(string sender, string[] recipients, string subject, string message, bool isHtml)
        {
            SendEmail(string.Empty, 0, sender, recipients, new List<string>(), subject, message, isHtml, null);
        }

        public static void SendEmail(EmailInfo emailInfo, List<string> attachments)
        {
            if (emailInfo.AllowAttachment)
                SendEmail(emailInfo.SmtpHost, emailInfo.SmtpPort, emailInfo.Sender, emailInfo.Recipients, emailInfo.CCs, emailInfo.Subject, emailInfo.Message, emailInfo.IsHtml, attachments);
            else
                SendEmail(emailInfo.SmtpHost, emailInfo.SmtpPort, emailInfo.Sender, emailInfo.Recipients, emailInfo.CCs, emailInfo.Subject, emailInfo.Message, emailInfo.IsHtml, null);
        }

        public static void SendEmail(string host, int port, string sender, string[] recipients, List<string> CCs, string subject, string message, bool isHtml, List<string> attachedFiles)
        {
            if (recipients == null || recipients.Length == 0) throw new Exception("no recipient email given");

            string user, passCode;
            if (checkSmtpUserAccount(out user, out passCode) == false) throw new Exception("no SMTP account given");

            if (string.IsNullOrEmpty(sender)) sender = SettingsHelper.GetSafeSetting(AppConstants.EMAIL_SENDER_KEY, string.Empty);
            if (string.IsNullOrEmpty(sender)) throw new Exception("no from email account given");

            sender = SettingsHelper.GetSafeSetting("GmailSender", string.Empty);

            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(sender);
                foreach (string recipient in recipients) mail.To.Add(new MailAddress(recipient));
                if (CCs != null) foreach (string email in CCs) mail.CC.Add(email);
                mail.Subject = subject;
                mail.Body = message;
                mail.IsBodyHtml = isHtml;
                if (attachedFiles != null)
                {
                    foreach (string file in attachedFiles)
                    {
                        Attachment attachment = new Attachment(file);
                        if (file.Contains("/")) attachment = new Attachment(HttpContext.Current.Server.MapPath(file));
                        mail.Attachments.Add(attachment);
                    }
                }

                // use smtp client to send email
                System.Net.Mail.SmtpClient smtpClient = new SmtpClient();

                if (string.IsNullOrEmpty(host))
                {
                    //host = SettingsHelper.GetSafeSetting(AppConstants.EMAIL_HOST_KEY, AppConstants.EMAIL_HOST_DEFAULT);
                    //port = SettingsHelper.GetSafeInteger(AppConstants.EMAIL_PORT_KEY, AppConstants.EMAIL_PORT_DEFAULT);

                    // TODO: replace this with above code when SES production email server is setup
                    // use jcubic email server to avoid email pre-verification by SES
                    host = SettingsHelper.GetSafeSetting("GmailHost", AppConstants.EMAIL_HOST_DEFAULT);
                    port = SettingsHelper.GetSafeInteger("GmailPort", AppConstants.EMAIL_PORT_DEFAULT);
                }

                smtpClient.Host = host;
                smtpClient.Port = port <= 0 ? AppConstants.EMAIL_PORT_DEFAULT : port;
                smtpClient.EnableSsl = true;
                smtpClient.Credentials = new System.Net.NetworkCredential(user, passCode);

                smtpClient.Send(mail);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        private static bool checkSmtpUserAccount(out string user, out string passCode)
        {
            //user = SettingsHelper.GetSafeSetting(AppConstants.SMTP_USER_KEY, string.Empty);
            //passCode = SettingsHelper.GetSafeSetting(AppConstants.SMTP_USER_CODE_KEY, string.Empty);

            user = SettingsHelper.GetSafeSetting("GmailSender", string.Empty);
            passCode = SettingsHelper.GetSafeSetting("GmailCode", string.Empty);
            return !string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(passCode);
        }
    }

    public class EmailInfo
    {
        public EmailInfo()
        {
            SmtpHost = string.Empty;
            SmtpPort = 25;
            Sender = string.Empty;
            Recipients = null;
            Subject = string.Empty;
            IsHtml = true;
            AllowAttachment = false;
            Message = string.Empty;
            CCs = new List<string>();
        }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string Sender { get; set; }
        public string[] Recipients { get; set; }
        public List<string> CCs { get; set; }
        public string Subject { get; set; }
        public bool IsHtml { get; set; }
        public bool AllowAttachment { get; set; }
        public string Message { get; set; }
    }

}