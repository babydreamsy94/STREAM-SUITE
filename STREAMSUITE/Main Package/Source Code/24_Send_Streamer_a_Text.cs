using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading;

public class CPHInline
{
    // SECURITY: Leave this false until all placeholders below are replaced.
    private const bool SmsEnabled = false;
    private const string SenderGmailAddress = "YOUR_GMAIL_ADDRESS";
    private const string GoogleAppPassword = "YOUR_16_CHARACTER_GOOGLE_APP_PASSWORD";
    private const string SmsGatewayAddress = "YOUR_10_DIGIT_NUMBER@YOUR_CARRIER_GATEWAY";

    // Optional. Leave blank to disable the sound.
    private const string OptionalSoundPath = "";
    private const int MaximumMessageLength = 500;

    public bool Execute()
    {
        if (!SmsEnabled)
        {
            CPH.LogWarn("Stream Suite Text: SMS is disabled. Configure the placeholders, then set SmsEnabled to true.");
            return true;
        }

        if (HasPlaceholder(SenderGmailAddress) ||
            HasPlaceholder(GoogleAppPassword) ||
            HasPlaceholder(SmsGatewayAddress))
        {
            CPH.LogWarn("Stream Suite Text: A required email-to-SMS placeholder has not been configured.");
            return true;
        }

        string smsBody = GetArg("rawInput");
        if (string.IsNullOrWhiteSpace(smsBody))
            return true;

        smsBody = smsBody.Trim().Replace("\r", " ").Replace("\n", " ");
        if (smsBody.Length > MaximumMessageLength)
            smsBody = smsBody.Substring(0, MaximumMessageLength);

        string sender = GetArg("user");
        if (string.IsNullOrWhiteSpace(sender))
            sender = GetArg("userName");
        if (string.IsNullOrWhiteSpace(sender))
            sender = "a viewer";

        smsBody += "\n- Sent by " + sender.Trim();

        try
        {
            using (SmtpClient client = new SmtpClient("smtp.gmail.com", 587))
            using (MailMessage mail = new MailMessage())
            {
                client.Credentials = new NetworkCredential(SenderGmailAddress, GoogleAppPassword);
                client.EnableSsl = true;
                mail.From = new MailAddress(SenderGmailAddress);
                mail.To.Add(SmsGatewayAddress);
                mail.Body = smsBody;
                client.Send(mail);
            }

            if (!string.IsNullOrWhiteSpace(OptionalSoundPath) && File.Exists(OptionalSoundPath))
            {
                Thread.Sleep(4000);
                CPH.PlaySound(OptionalSoundPath, 0.5f);
            }
        }
        catch (Exception ex)
        {
            CPH.LogWarn("Stream Suite Text: The message could not be sent. " + ex.Message);
        }

        return true;
    }

    private string GetArg(string key)
    {
        if (args == null || !args.ContainsKey(key) || args[key] == null)
            return null;
        return args[key].ToString();
    }

    private bool HasPlaceholder(string value)
    {
        return string.IsNullOrWhiteSpace(value) ||
            value.IndexOf("YOUR_", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
