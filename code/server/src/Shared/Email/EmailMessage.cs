namespace IMT_Reservas.Server.Application.Abstraction;

public sealed record EmailMessage(string Recipient, string Subject, string Body);
