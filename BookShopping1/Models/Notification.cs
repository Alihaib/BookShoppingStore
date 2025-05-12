public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; } // To associate with the user
    public string Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; } // To track if the notification has been read
}