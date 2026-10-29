namespace UCB_Hold.Server.Shared.Email;

public sealed class EmailSettings
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; }
    public EmailProvider Provider { get; set; } = EmailProvider.Smtp;
    public string From { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
}
