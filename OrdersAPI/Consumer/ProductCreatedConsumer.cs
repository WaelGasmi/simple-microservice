using MassTransit;
using OrdersAPI.Data;
using Shared.Models;

namespace OrdersAPI.Consumer
{
    /// <summary>
    /// Consommateur RabbitMQ : à chaque message <see cref="ProductCreated"/> publié par
    /// ProductsAPI, le produit est enregistré dans la table Products d'OrdersAPI.
    /// </summary>
    public class ProductCreatedConsumer : IConsumer<ProductCreated>
    {
        private readonly OrdersAPIContext _ordersAPIContext;
        private readonly ILogger<ProductCreatedConsumer> _logger;

        public ProductCreatedConsumer(OrdersAPIContext ordersAPIContext, ILogger<ProductCreatedConsumer> logger)
        {
            _ordersAPIContext = ordersAPIContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ProductCreated> context)
        {
            var newProduct = new Product
            {
                // Id est auto-généré côté OrdersAPI (identity), on ne reprend que le nom.
                Name = context.Message.Name
            };

            _ordersAPIContext.Products.Add(newProduct);
            await _ordersAPIContext.SaveChangesAsync();

            _logger.LogInformation(
                "ProductCreated consommé : '{Name}' (id source {SourceId}) ajouté à la table Products d'OrdersAPI.",
                context.Message.Name,
                context.Message.Id);
        }
    }
}
