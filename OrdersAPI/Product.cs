namespace OrdersAPI
{
    /// <summary>
    /// Copie locale (partielle) du produit, synchronisée depuis ProductsAPI
    /// grâce au message RabbitMQ <c>ProductCreated</c>.
    /// </summary>
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
    }
}
