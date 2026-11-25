using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersAPI.Data;

namespace OrdersAPI.Controllers
{
    /// <summary>CRUD des commandes du microservice OrdersAPI.</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class OrdersController : ControllerBase
    {
        private readonly OrdersAPIContext _context;

        public OrdersController(OrdersAPIContext context)
        {
            _context = context;
        }

        /// <summary>Retourne la liste de toutes les commandes.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Orders>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Orders>>> GetOrders()
        {
            return await _context.Orders.AsNoTracking().ToListAsync();
        }

        /// <summary>Retourne une commande par son identifiant.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(Orders), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Orders>> GetOrders(int id)
        {
            var orders = await _context.Orders.FindAsync(id);

            if (orders == null)
            {
                return NotFound(new { message = $"Commande {id} introuvable." });
            }

            return orders;
        }

        /// <summary>Met à jour une commande existante.</summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PutOrders(int id, Orders orders)
        {
            if (id != orders.OrdersId)
            {
                return BadRequest(new { message = "L'id de la route ne correspond pas à l'id de la commande." });
            }

            _context.Entry(orders).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrdersExists(id))
                {
                    return NotFound(new { message = $"Commande {id} introuvable." });
                }

                throw;
            }

            return NoContent();
        }

        /// <summary>Crée une nouvelle commande.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(Orders), StatusCodes.Status201Created)]
        public async Task<ActionResult<Orders>> PostOrders(Orders orders)
        {
            _context.Orders.Add(orders);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrders), new { id = orders.OrdersId }, orders);
        }

        /// <summary>Supprime une commande.</summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOrders(int id)
        {
            var orders = await _context.Orders.FindAsync(id);
            if (orders == null)
            {
                return NotFound(new { message = $"Commande {id} introuvable." });
            }

            _context.Orders.Remove(orders);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OrdersExists(int id)
        {
            return _context.Orders.Any(e => e.OrdersId == id);
        }
    }
}
