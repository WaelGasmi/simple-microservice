using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Charge la configuration Ocelot (routage vers les microservices)
builder.Configuration.AddJsonFile("Ocelot.json", optional: false, reloadOnChange: true);

// Enregistre Ocelot dans le conteneur DI
builder.Services.AddOcelot();

var app = builder.Build();

app.UseHttpsRedirection();

// Middleware Ocelot : redirige les requêtes /apigateway/... vers les microservices
await app.UseOcelot();

app.Run();
