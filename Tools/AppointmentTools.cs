using System.ComponentModel;
using VoiceDentalReceptionist.Storage;

namespace VoiceDentalReceptionist.Tools;

/// <summary>
/// Phase 1: the three tools the Appointment Agent calls. Phase 2: invoked
/// directly by Agents/Foundry/FoundryAgentService.ExecuteToolAsync when the
/// Foundry-hosted agent requests a function call (the [Description]
/// attributes are inert now - kept for documentation, not consumed by any
/// SDK reflection anymore since Microsoft.Agents.AI was removed from this
/// project). This class has zero knowledge of "intent" or conversation
/// state either way.
/// </summary>
public class AppointmentTools
{
    private readonly JsonStorage<Appointment> _storage;

    public AppointmentTools(string appointmentsFilePath)
    {
        _storage = new JsonStorage<Appointment>(appointmentsFilePath);
    }

    [Description("Books a new dental appointment for a patient. Requires the patient's name, mobile number, date, and time.")]
    public async Task<string> BookAppointment(
        [Description("Full name of the patient")] string patientName,
        [Description("Patient's mobile number")] string mobileNumber,
        [Description("Requested date, e.g. 'tomorrow' or '2026-08-14'")] string date,
        [Description("Requested time, e.g. '5 PM'")] string time)
    {
        Console.WriteLine($"[TOOL] BookAppointment called: {patientName}, {mobileNumber}, {date} {time}");

        if (string.IsNullOrWhiteSpace(patientName) || string.IsNullOrWhiteSpace(mobileNumber)
            || string.IsNullOrWhiteSpace(date) || string.IsNullOrWhiteSpace(time))
        {
            return "ERROR: Missing required information. Ask the caller for whichever of " +
                   "patient name, mobile number, date, or time is still missing.";
        }

        if (!IsValidMobileNumber(mobileNumber))
        {
            return "ERROR: That doesn't look like a valid mobile number. Ask the caller to repeat it.";
        }

        var appointment = new Appointment
        {
            PatientName = patientName,
            MobileNumber = mobileNumber,
            Date = date,
            Time = time,
            Status = "Booked"
        };

        await _storage.AppendAsync(appointment);
        Console.WriteLine($"[TOOL] Appointment created: {appointment.Id}");

        return $"Appointment booked. Id={appointment.Id}, Patient={patientName}, Date={date}, Time={time}.";
    }

    [Description("Reschedules an existing appointment to a new date/time. Identify the appointment by the patient's mobile number.")]
    public async Task<string> RescheduleAppointment(
        [Description("Mobile number used when the appointment was booked")] string mobileNumber,
        [Description("New date for the appointment")] string newDate,
        [Description("New time for the appointment")] string newTime)
    {
        Console.WriteLine($"[TOOL] RescheduleAppointment called: {mobileNumber} -> {newDate} {newTime}");

        var appointments = await _storage.LoadAllAsync();
        var existing = appointments
            .Where(a => a.MobileNumber == mobileNumber && a.Status == "Booked")
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefault();

        if (existing is null)
        {
            return $"ERROR: No active appointment found for mobile number {mobileNumber}. " +
                   "Ask the caller to confirm the number, or offer to book a new appointment instead.";
        }

        existing.Date = newDate;
        existing.Time = newTime;
        existing.Status = "Rescheduled";
        await _storage.SaveAllAsync(appointments);

        Console.WriteLine($"[TOOL] Appointment {existing.Id} rescheduled");
        return $"Appointment {existing.Id} rescheduled to {newDate} at {newTime}.";
    }

    [Description("Cancels an existing appointment. Identify the appointment by the patient's mobile number.")]
    public async Task<string> CancelAppointment(
        [Description("Mobile number used when the appointment was booked")] string mobileNumber)
    {
        Console.WriteLine($"[TOOL] CancelAppointment called: {mobileNumber}");

        var appointments = await _storage.LoadAllAsync();
        var existing = appointments
            .Where(a => a.MobileNumber == mobileNumber && a.Status == "Booked")
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefault();

        if (existing is null)
        {
            return $"ERROR: No active appointment found for mobile number {mobileNumber}.";
        }

        existing.Status = "Cancelled";
        await _storage.SaveAllAsync(appointments);

        Console.WriteLine($"[TOOL] Appointment {existing.Id} cancelled");
        return $"Appointment {existing.Id} has been cancelled.";
    }

    private static bool IsValidMobileNumber(string mobileNumber)
    {
        var digits = new string(mobileNumber.Where(char.IsDigit).ToArray());
        return digits.Length is >= 10 and <= 15;
    }
}
