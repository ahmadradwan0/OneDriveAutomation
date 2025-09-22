
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


    private static readonly IResend _resendClient;

    // Static constructor to initialize ResendClient once
    static Utils()
    {
        // Use the static Create method to initialize the client.
        // This is the correct way for a non-DI application.
        string apiToken = "re_TqBNSRA5_HC2DyuBsdtCeRp1AW768Mew7";
        Console.WriteLine($"API Token being used: {apiToken}");
        _resendClient = ResendClient.Create(apiToken);
    }

    /// <summary>
    /// Logs a message to the console and a log file with a timestamp and log level.
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

    /// <summary>
    /// Sends an email using the Resend service.
    /// </summary>
    /// <param name="errorMessage"></param>
    /// <returns></returns>
    public static async Task<bool> SendEmailUsingResend(string errorMessage)
    {
        try
        {
            // The sender email must be a verified domain in your Resend account
            string from = Config.SenderEmailAddress;
            string subject = "[Critical] OneDrive Script Failed";

            foreach (var recipientt in Config.RecipientsEmailAddresses)
            {
                // Build the email message
                var emailMessage = new EmailMessage
                {
                    From = from,
                    To = { recipientt },
                    Subject = subject,

                    HtmlBody = $@"
                    <div style=""font-family:Segoe UI, sans-serif; max-width:600px; margin:20px auto; padding:20px; border:1px solid #e0e0e0; border-radius:8px; background-color:#fafafa;"">
                        <h2 style=""color:#2c3e50;"">🚨 OneDrive Script Alert</h2>

                        <p style=""font-size:16px;"">An error occurred during the last run of your OneDrive script.</p>

                        <div style=""margin:20px 0; padding:15px; background-color:#ffe6e6; border-left:5px solid #e74c3c;"">
                            <strong style=""color:#c0392b;"">Error Details:</strong>
                            <pre style=""white-space:pre-wrap; word-wrap:break-word; color:#333;"">
                An error occurred at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
                Machine Name: {Environment.MachineName}
                User: {Environment.UserName}

                Exception:
                {errorMessage}
                            </pre>
                        </div>

                        <p style=""color:#555;"">Please check the logs for more details.</p>

                        <p style=""color:#999;"">– OneDrive App</p>
                    </div>"
                };

                // Use the initialized Resend client
                var emailResponse = await _resendClient.EmailSendAsync(emailMessage);

                Log($"Email sent successfully with Resend ID: {emailResponse.Success}");
            }

            return true;
        }
        catch (Exception ex)
        {
            Log($"Error in EmailSenderService: {ex.Message}", "ERROR");
            return false;
        }
    }

}