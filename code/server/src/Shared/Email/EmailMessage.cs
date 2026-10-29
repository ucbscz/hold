namespace UCB_Hold.Server.Shared.Email;

public sealed record EmailMessage(string Recipient, string Subject, string Body);
