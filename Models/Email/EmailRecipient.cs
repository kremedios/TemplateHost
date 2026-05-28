namespace Host.Models.Email;

public class EmailRecipient
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
