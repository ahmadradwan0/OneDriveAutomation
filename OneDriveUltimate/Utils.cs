using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading;

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



    public static void EmailSender()
    {
        if (EmailSenderService())
        {
            Log("Email sent successfully.");
        }
        else
        {
            Log("Failed to send email.", "ERROR");
        }

    }
    
     private static bool EmailSenderService()
    {
        try
        {
            string[] Scopes = { GmailService.Scope.GmailSend };
            string ApplicationName = "Gmail API C# Sender";

            UserCredential credential;

            using (var stream = new FileStream("email.json", FileMode.Open, FileAccess.Read))
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

            // Build MIME message manually
            string from = Config.SenderEmailAddress;
            List<string> toRecipients = Config.RecipientsEmailAddresses;
            string subject = "One Drive Script";
            string body = "This is a Test Email from OneDrive App";
            // Start the for loop here
            foreach (string recipient in toRecipients)
                {
                string mimeMessage = $"From: {from}\r\n" +
                                    $"To: {recipient}\r\n" +
                                    $"Subject: {subject}\r\n" +
                                    "Content-Type: text/plain; charset=UTF-8\r\n\r\n" +
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