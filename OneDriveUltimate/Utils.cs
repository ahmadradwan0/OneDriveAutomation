using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// class used for nethods run Globally across all app classes 
/// </summary>
public static class Utils
{
    /// <summary>
    /// method to log all the changes on the console and in a log file saved besids the EXE file 
    /// </summary>

    public static void Log(string message, string level = "INFO")
    {
        string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
        Console.WriteLine(logEntry);
        //File.AppendAllText("onedrive_updater.log", logEntry + Environment.NewLine);
        // Log to a file
        try
        {
            File.AppendAllText(Config.LogFile, logEntry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            // Fallback: log an error to the console if file logging fails
            Console.WriteLine($"[ERROR] Failed to write to log file: {ex.Message}");
        }
    }



    public static async Task EmailSender(string errorMessage)
    {
        if (await SendEmailUsingSmtp(errorMessage))
        {
            Log("Email sent successfully.");
        }
        else
        {
            Log("Failed to send email.", "ERROR");
        }

    }

/// <summary>
/// this method uses a smtp client to send emails
/// the service used here is sendgrid by twilio
/// if you want to use the same method with postmark just un comment the postmark lines and comment the sendgrid ones
/// or if you need to keep using twilio sendgrid just replace the password key with your own user name stays as apikey
/// port number by default is 587 for both services can be changed if needed
/// </summary>
/// <param name="errorMessage"></param>
/// <returns></returns>
    private static async Task<bool> SendEmailUsingSmtp(string errorMessage)
    {
        try
        {
            // The sender's email address
            string from = Config.SenderEmailAddress;
            List<string> toRecipients = Config.RecipientsEmailAddresses;
            string subject = "[CRITICAL] OneDrive Script Failed";

            // SMTP settings for a dedicated transactional email service
            // un comment to use postmark
            //string smtpServer = "smtp.postmarkapp.com"; 
            string smtpServer = "smtp.sendgrid.net";
            int smtpPort = 587;

            // The Postmark Server API Token acts as both the SMTP username and password.
            // Replace this placeholder with your actual Postmark Server API Token.
            //string postmarkApiToken = "16c8bcad-ebfd-4a2c-a111-65eb4a594586";
            //string sendGridApikey = ""; // if you want to use sendgrid replace the postmark api token with your sendgrid api key

            string smtpUsername = "apikey";
            string smtpPassword = ""; // sendgrid

            // Build the email message
            using (var mailMessage = new MailMessage())
            {
                mailMessage.From = new MailAddress(from);
                foreach (string recipient in toRecipients)
                {
                    mailMessage.To.Add(recipient);
                }
                mailMessage.Subject = subject;
                // Set IsBodyHtml to false for plain text email (Note I tried to use html but might go to junk mail)
                mailMessage.IsBodyHtml = false;

                // This is the plain text body of the email
                string plainTextBody = $@"
                Hello,

                This email is from your OneDrive script.
                An error has occurred during the last run.
                Please check the logs for more details.

                Error Message:
                {errorMessage}

                Thank you,
                OneDrive App";
                mailMessage.Body = plainTextBody;

                // Send the email using SmtpClient
                using (var smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    await smtpClient.SendMailAsync(mailMessage);
                }
            }

            Log("Email sent successfully using SMTP client.");
            return true;
        }
        catch (SmtpException smtpEx)
        {
            Log($"SMTP Exception in EmailSenderService: {smtpEx.Message}", "ERROR");
            return false;
        }
        catch (Exception ex)
        {
            Log($"General Exception in EmailSenderService: {ex.Message}", "ERROR");
            return false;
        }
    }


/// <summary>/// THIS METHOD IS NOT USED RIGHT NOW CAUSE IT GOES TO JUNK MAIL THE FIRST TIME
/// this code is not used anymore but kept for reference if needed in the future
/// it uses the google gmail api to send emails
/// the only thing you need to add to the project is to add a file called email.json
///  that you can get from the google cloud console after creating a project and enabling the gmail api 
/// </summary>
/// <param name="errorMessage"></param>
/// <returns></returns>
    private static bool EmailSenderServiceGoogleAPI(string errorMessage)
    {
        try
        {
            string[] Scopes = { GmailService.Scope.GmailSend };
            string ApplicationName = "Gmail API C# Sender";

            UserCredential credential;

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });


            // Log the email address being used as the sender to confirm it's what you expect
            Log($"Using sender email: {credential.UserId}");

            // Build MIME message manually
            string from = "apps@tlprojectautomation.com";
            List<string> toRecipients = Config.RecipientsEmailAddresses;
            string subject = "[CRITICAL] OneDrive Script Failed";
            string body = @"
                            <html>
                            <body style='font-family:Arial, sans-serif; background:#f4f4f4; padding:20px;'>
                                <table style='max-width:600px; margin:auto; background:#ffffff; padding:25px; border-radius:12px; box-shadow:0 2px 8px rgba(0,0,0,0.1);'>
                                <tr>
                                    <td>
                                    <h2 style='color:#2E86C1; margin-bottom:15px;'>OneDrive Script Notification</h2>
                                    <p style='font-size:15px; color:#333; line-height:1.6;'>
                                        Hello,<br><br>
                                        This is an <b>Email</b> sent from your OneDrive application.
                                    </p>
                                    <p style='font-size:15px; color:#333; line-height:1.6;'>
                                        An error occurred while running your OneDrive script
                                    </p>
                                    <p style='font-size:14px; color:#e74c3c; background:#fbeaea; padding:10px; border-radius:5px;'>
                                    <b>Error:</b><br>
                                    {ERROR_MESSAGE}
                                    </p>
                                    <hr style='margin:25px 0; border:none; border-top:1px solid #ddd;'>
                                    <p style='font-size:12px; color:#888; text-align:center;'>
                                        © 2025 OneDrive App • This is an automated message
                                    </p>
                                    </td>
                                </tr>
                                </table>
                            </body>
                            </html>";


            // Replace placeholder inside the html body string with the real error message getting passed from outside
            body = body.Replace("{ERROR_MESSAGE}", errorMessage);

            // loop through all recipients to send individual emails
            foreach (string recipient in toRecipients)
            {
                string mimeMessage = $"From: {from}\r\n" +
                                    $"To: {recipient}\r\n" +
                                    $"Subject: {subject}\r\n" +
                                    "Content-Type: text/html; charset=UTF-8\r\n\r\n" +
                                    $"{body}";

                // Encode message
                string raw = Convert.ToBase64String(Encoding.UTF8.GetBytes(mimeMessage))
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Replace("=", "");

                var message = new Message { Raw = raw };

                // Send email
                var result = service.Users.Messages.Send(message, "me").Execute();
                Log($"Email sent! Message ID: {result.Id}");
            }

            return true;
        }
        catch (Exception ex)
        {
            Log($"Exception in EmailSenderService: {ex.Message}", "ERROR");
            return false;
        }
    }
}