namespace OrdersAPI
{
    /// <summary>Commande gérée par le microservice OrdersAPI.</summary>
    public class Orders
    {
        public int OrdersId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public string? CustomerName { get; set; }
        public int ProductId { get; set; }
    }
}
