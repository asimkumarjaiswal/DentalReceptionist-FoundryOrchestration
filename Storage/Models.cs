namespace VoiceDentalReceptionist.Storage;

public record Appointment
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public string PatientName { get; init; } = string.Empty;
    public string MobileNumber { get; init; } = string.Empty;
    public string Date { get; set; } = string.Empty;    // kept as free-form string on purpose —
    public string Time { get; set; } = string.Empty;    // Phase 1 doesn't need real date parsing/validation
    public string Status { get; set; } = "Booked";       // Booked | Rescheduled | Cancelled
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public record CallbackRequest
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; init; } = string.Empty;
    public string MobileNumber { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
