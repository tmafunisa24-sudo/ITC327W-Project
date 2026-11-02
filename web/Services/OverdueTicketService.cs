using ServiceHub_IT.Interfaces;
using ServiceHub_IT.Models;
using Microsoft.Extensions.Options;

namespace ServiceHub_IT.Services;

public sealed class OverdueTicketService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEmailSender _emailSender;
    private readonly string _serviceRoleKey;
    private readonly ILogger<OverdueTicketService> _logger;

    public OverdueTicketService(
        IServiceScopeFactory scopeFactory,
        IEmailSender emailSender,
        IOptions<SupabaseSettings> supabaseOptions,
        ILogger<OverdueTicketService> logger)
    {
        _scopeFactory = scopeFactory;
        _emailSender = emailSender;
        _serviceRoleKey = supabaseOptions.Value.ServiceRoleKey;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);

        do
        {
            try
            {
                await CheckOverdueTicketsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Overdue ticket SLA scan failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckOverdueTicketsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var ticketService = scope.ServiceProvider.GetRequiredService<TicketService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
        var supabaseService = scope.ServiceProvider.GetRequiredService<ISupabaseService>();

        var tickets = await ticketService.GetOverdueTicketsAsync(cancellationToken);
        var nowUtc = DateTime.UtcNow;
        foreach (var ticket in tickets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ticket.due_date is not { } dueDate ||
                NormalizeUtc(dueDate) >= nowUtc ||
                string.Equals(ticket.status, "Resolved", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ticket.status, "Closed", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var technician = ticket.technician_id is { } technicianId && technicianId != Guid.Empty
                    ? await supabaseService.GetProfileByIdAsync(technicianId, _serviceRoleKey, cancellationToken)
                    : null;

                if (technician is null && !string.IsNullOrWhiteSpace(ticket.assigned_technician))
                {
                    technician = await supabaseService.GetProfileByEmailAsync(
                        ticket.assigned_technician,
                        cancellationToken);
                }

                if (technician is null || technician.Id == Guid.Empty)
                {
                    _logger.LogWarning(
                        "Overdue ticket {TicketId} has no resolvable assigned technician; alert skipped.",
                        ticket.id);
                    continue;
                }

                var ticketLabel = !string.IsNullOrWhiteSpace(ticket.ticket_number)
                    ? ticket.ticket_number
                    : !string.IsNullOrWhiteSpace(ticket.title) ? ticket.title : ticket.id ?? "Unknown ticket";
                var title = "SLA Alert";
                var message = $"SLA Alert: Ticket {ticketLabel} is overdue!";
                var link = $"/Tickets/Details/{Uri.EscapeDataString(ticket.id ?? ticketLabel)}";

                if (await supabaseService.NotificationExistsAsync(technician.Id, title, link, cancellationToken))
                {
                    continue;
                }

                await notificationService.CreateNotificationAsync(
                    technician.Id,
                    title,
                    message,
                    "sla_alert",
                    link,
                    cancellationToken);

                if (_emailSender.IsConfigured && !string.IsNullOrWhiteSpace(technician.Email))
                {
                    await _emailSender.SendAsync(technician.Email, title, message, cancellationToken);
                }

                _logger.LogInformation(
                    "Created overdue SLA alert for ticket {TicketId} and technician {TechnicianId}.",
                    ticket.id,
                    technician.Id);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to send overdue SLA alert for ticket {TicketId}.", ticket.id);
            }
        }

        _logger.LogInformation("Overdue ticket SLA scan completed. Checked {TicketCount} active tickets.", tickets.Count);
    }

    private static DateTime NormalizeUtc(DateTime dateTime) => dateTime.Kind switch
    {
        DateTimeKind.Utc => dateTime,
        DateTimeKind.Local => dateTime.ToUniversalTime(),
        _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
    };
}
