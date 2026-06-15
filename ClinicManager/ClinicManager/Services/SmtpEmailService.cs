using ClinicManager.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ClinicManager.Services;

public class SmtpEmailService
{
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly SmtpOptions _options;

    public SmtpEmailService(
        IOptions<SmtpOptions> options,
        ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendReportAsync(
        byte[] pdfBytes,
        string fileName,
        int visitCount,
        CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_options.FromAddress));
        message.To.Add(MailboxAddress.Parse(_options.RecipientAddress));
        message.Subject = "ClinicManager - raport nadchodzących wizyt";

        var body = new BodyBuilder
        {
            TextBody =
                $"W załączniku znajduje się raport nadchodzących wizyt. " +
                $"Liczba wizyt w raporcie: {visitCount}."
        };
        body.Attachments.Add(fileName, pdfBytes, new ContentType("application", "pdf"));
        message.Body = body.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = _options.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.None;

        await client.ConnectAsync(
            _options.Host,
            _options.Port,
            socketOptions,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            await client.AuthenticateAsync(
                _options.Username,
                _options.Password,
                cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation(
            "Wysłano raport {FileName} na adres {RecipientAddress}.",
            fileName,
            _options.RecipientAddress);
    }
}
