using System.ComponentModel.DataAnnotations;

namespace VoiceDentalReceptionist.Models.Requests;

public class SendMessageRequest
{
    [Required]
    public string Message { get; set; } = string.Empty;
}
