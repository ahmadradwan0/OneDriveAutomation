
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Resend;


/// <summary>
/// class used for nethods run Globally across all app classes 
/// </summary>
public static class Utils
{
    /// <summary>
    /// method to log all the changes on the console and in a log file saved besids the EXE file 
    /// </summary>
    /// 

    private static readonly IResend _resendClient;

    // Static constructor to initialize ResendClient once
    static Utils()
    {
        // Use the static Create method to initialize the client.
        // This is the correct way for a non-DI application.
        string apiToken = "";
        Console.WriteLine($"API Token being used: {apiToken}");
        _resendClient = ResendClient.Create(apiToken);
    }


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

   public static async Task<bool> SendEmailUsingResend(string errorMessage)
    {
        try
        {
            // The sender email must be a verified domain in your Resend account
            string from = Config.SenderEmailAddress;
            string subject = "OneDrive Script Failed";

            // Build the email message
            var emailMessage = new EmailMessage
            {
                From = from,
                To = { Config.RecipientsEmailAddresses.First() },
                Subject = subject,
                HtmlBody = $@"
                    <h1>Hello,</h1>
                    <p>This email is from your OneDrive script.</p>
                    <p>An error has occurred during the last run.</p>
                    <p>Please check the logs for more details.</p>
                    <p>Error Message:</p>
                    <p>{errorMessage}</p>
                    <p>Thank you,</p>
                    <p>OneDrive App</p>"
            };

            // Use the initialized Resend client
            var emailResponse = await _resendClient.EmailSendAsync(emailMessage);

            Log($"Email sent successfully with Resend ID: {emailResponse.Success}");
            return true;
        }
        catch (Exception ex)
        {
            Log($"Error in EmailSenderService: {ex.Message}", "ERROR");
            return false;
        }
    }

}