namespace Domain
{
    public class Order
    {
        public int Id { get; set; }
        public Guid Guid { get; set; }
        public string Title { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public OrderStatus Status { get; set; }
    }
}
