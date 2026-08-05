namespace TicketReservationSystem.Application.Abstractions
{
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    }
}
