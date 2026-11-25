namespace Shared.Models
{
    /// <summary>
    /// Message publié par ProductsAPI quand un nouveau produit est créé.
    /// Il est consommé par OrdersAPI via RabbitMQ / MassTransit.
    /// </summary>
    public class ProductCreated
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
    }
}
