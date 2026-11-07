namespace TodoApi.Models
{
    public class ChangeStatusRequest
    {
        public TodoStatus NewStatus { get; set; }
        public bool Force { get; set; } = false;
    }
}
