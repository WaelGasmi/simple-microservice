# Atelier 8 — Microservices avec ASP.NET Core

Solution complète de l'atelier : **2 microservices** (`ProductsAPI`, `OrdersAPI`), une **API Gateway Ocelot**
(`ApiGateway`) et une bibliothèque de contrats partagés (`Shared`), avec communication asynchrone
**RabbitMQ / MassTransit**.

```
                         ┌──────────────────────┐
   Postman / Client ───▶ │  ApiGateway (Ocelot) │  https://localhost:7162
                         └──────────┬───────────┘
                                    │ /apigateway/Products, /apigateway/Orders
                    ┌───────────────┴────────────────┐
                    ▼                                ▼
        ┌───────────────────────┐        ┌───────────────────────┐
        │     ProductsAPI       │        │       OrdersAPI       │
        │  https://localhost:7066│        │  https://localhost:7065│
        │  Swagger : /swagger   │        │  Swagger : /swagger   │
        │  DB : ProductsDB      │        │  DB : OrdersDB        │
        └───────────┬───────────┘        └───────────┬───────────┘
                    │ Publish(ProductCreated)        │ Consume(ProductCreated)
                    └──────────▶ RabbitMQ ◀──────────┘
                         exchange fanout / file « event-listener »
```

## 1. Contenu de la solution

| Projet | Type | URL HTTPS | Base de données | Swagger |
|---|---|---|---|---|
| `ProductsAPI` | Web API (microservice émetteur) | `https://localhost:7066` | `ProductsDB` (LocalDB) | `https://localhost:7066/swagger` |
| `OrdersAPI` | Web API (microservice consommateur) | `https://localhost:7065` | `OrdersDB` (LocalDB) | `https://localhost:7065/swagger` |
| `ApiGateway` | Web API + Ocelot | `https://localhost:7162` | — | — (uniquement les backends, comme demandé) |
| `Shared` | Bibliothèque de classes | — | — | — |

> Swagger n'est activé que sur les **deux applications backend** (`ProductsAPI` et `OrdersAPI`),
> pas sur la passerelle.

Versions utilisées : **.NET 10**, EF Core 10.0.15, **MassTransit 8.5.12** (+ `MassTransit.RabbitMQ`),
**Ocelot 25.0.3**, Swashbuckle 10.3.1.

## 2. Prérequis

- .NET 10 SDK ;
- SQL Server LocalDB (installé avec Visual Studio) — utilisé par défaut :
  `(localdb)\MSSQLLocalDB` ;
- Docker Desktop (ou RabbitMQ installé localement) pour le broker ;
- Postman pour tester les endpoints (collection fournie).

## 3. Démarrage

### 3.1 RabbitMQ (Docker)

```bash
docker compose up -d
```

ou, comme dans l'atelier :

```bash
docker run -d --hostname my-rabbit --name ecomm -p 15672:15672 -p 4001:5672 rabbitmq:3-management
```

- AMQP : `localhost:4001` (mappé sur le 5672 du conteneur) ;
- Administration : <http://localhost:15672> — `guest` / `guest`.

> Docker Desktop est disponible sur ce poste : `docker compose up -d` démarre le conteneur
> `ecomm`. Si le broker n'est pas démarré, les APIs fonctionnent quand même (le POST produit
> journalise un avertissement et la synchro RabbitMQ est simplement ignorée) ; dans ce cas,
> redémarrez les deux microservices après avoir lancé RabbitMQ pour qu'ils se reconnectent.

### 3.2 Lancer les trois services

Dans trois terminaux, depuis le dossier de la solution :

```bash
dotnet run --project ProductsAPI --launch-profile https
dotnet run --project OrdersAPI  --launch-profile https
dotnet run --project ApiGateway --launch-profile https
```

Ou ouvrir `MicroservicesApp.slnx` et lancer les 3 projets en startup multiple.

### 3.3 Points d'entrée

- Swagger ProductsAPI : <https://localhost:7066/swagger>
- Swagger OrdersAPI : <https://localhost:7065/swagger>
- Gateway : <https://localhost:7162/apigateway/Products>

## 4. Endpoints

### ProductsAPI (direct)

| Méthode | URL |
|---|---|
| GET | `/api/Products` |
| GET | `/api/Products/{id}` |
| POST | `/api/Products` |
| PUT | `/api/Products/{id}` |
| DELETE | `/api/Products/{id}` |

### OrdersAPI (direct)

| Méthode | URL | Remarque |
|---|---|---|
| GET | `/api/Orders` | |
| GET | `/api/Orders/{id}` | |
| POST | `/api/Orders` | |
| PUT | `/api/Orders/{id}` | |
| DELETE | `/api/Orders/{id}` | |
| GET | `/api/Products` | produits synchronisés via RabbitMQ (bonus de vérification) |

### Via la Gateway Ocelot

| Méthode | URL | Redirige vers |
|---|---|---|
| GET / POST | `/apigateway/Products` | `https://localhost:7066/api/Products` |
| GET / PUT / DELETE | `/apigateway/Products/{id}` | `https://localhost:7066/api/Products/{id}` |
| GET / POST | `/apigateway/Orders` | `https://localhost:7065/api/Orders` |
| GET / PUT / DELETE | `/apigateway/Orders/{id}` | `https://localhost:7065/api/Orders/{id}` |

Les routes sont définies dans `ApiGateway/Ocelot.json`. Contrairement au fichier de l'atelier
(qui ne mappait que `Get`/`Post` sur `/{id}`), les verbes **PUT** et **DELETE** sont aussi autorisés
pour que les mises à jour et suppressions passent par la passerelle.

## 5. Postman

Deux fichiers sont fournis dans `postman/` :

- `MicroservicesApp.postman_collection.json` : 4 dossiers
  1. **Gateway Ocelot** (tous les endpoints via le port 7162) ;
  2. **ProductsAPI (direct :7066)** ;
  3. **OrdersAPI (direct :7065)** — dont `GET /api/Products` (produits synchronisés) ;
  4. **Swagger & RabbitMQ** (swagger.json + API de management RabbitMQ).
- `MicroservicesApp.postman_environment.json` : variables `gatewayUrl`, `productsApiUrl`,
  `ordersApiUrl`, `rabbitUrl`, `productId`, `orderId`…

**Import** : Postman → *Import* → sélectionner les deux fichiers → choisir l'environnement
« MicroservicesApp - Local » en haut à droite.

Des tests Postman sont inclus : les requêtes `POST` mémorisent automatiquement
`productId` / `orderId` dans les variables pour les requêtes suivantes.

### Scénario de test (communication inter-microservices)

1. `POST https://localhost:7162/apigateway/Products`

```json
{ "name": "prod12", "stockQte": 660, "price": 1500 }
```

2. Réponse `201` :

```json
{ "productId": 12, "name": "prod12", "stockQte": 660, "price": 1500 }
```

3. `GET https://localhost:7065/api/Products` → le produit `{ "id": 1, "name": "prod12" }`
   apparaît dans la table `Products` d'`OrdersAPI` (via RabbitMQ).
4. Dans RabbitMQ Management, la file `event-listener` est visible sur l'onglet *Queues*.

## 6. Base de données et migrations

La chaîne de connexion se trouve dans `appsettings.json` de chaque service :

```json
"DefaultConnection": "server=(localdb)\\MSSQLLocalDB;database=ProductsDB;Trusted_Connection=true;MultipleActiveResultSets=true"
```

Les migrations sont déjà créées et appliquées. Pour les recréer :

```bash
dotnet ef migrations add dbcreate --project ProductsAPI --startup-project ProductsAPI
dotnet ef database update           --project ProductsAPI --startup-project ProductsAPI

dotnet ef migrations add dbcreate --project OrdersAPI  --startup-project OrdersAPI
dotnet ef database update           --project OrdersAPI  --startup-project OrdersAPI
```

> La migration d'`OrdersAPI` contient directement les tables `Orders` **et** `Products`.
> Dans l'atelier, `Products` est ajoutée plus tard avec `Add-Migration addproduct` ;
> ici le modèle final a été construit d'un coup.

Un `IDesignTimeDbContextFactory` est présent dans chacun des contextes : il permet à
`dotnet ef` de fonctionner même si RabbitMQ n'est pas démarré.

## 7. Communication RabbitMQ / MassTransit

- `Shared.Models.ProductCreated` est le contrat du message (namespace identique côté émetteur
  et côté consommateur, donc même type de message pour MassTransit).
- `ProductsAPI` : injection de `IPublishEndpoint` dans `ProductsController` ; `PostProduct`
  publie `ProductCreated` après l'enregistrement en base.
- `OrdersAPI` : `ProductCreatedConsumer` implémente `IConsumer<ProductCreated>` et enregistre
  le produit dans sa table `Products` ; la file `event-listener` est configurée dans `Program.cs`.
- Configuration (identique dans les deux `Program.cs`) :

```csharp
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri("rabbitmq://localhost:4001"), h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});
```

Le package historique `MassTransit.AspNetCore` mentionné dans l'atelier n'est plus nécessaire :
depuis MassTransit 8, l'intégration ASP.NET Core (hosted service, DI) est incluse dans `MassTransit`.

## 8. Remarque importante : Smart App Control et projet `Shared`

Sur ce poste, **Smart App Control** (Windows 11) bloque le chargement des DLL non signées générées
localement : `Shared.dll` renvoie l'erreur `0x800711C7` (« An Application Control policy has blocked
this file »), y compris pour `dotnet ef` et pour l'exécution des microservices.

Pour que la solution fonctionne sans désactiver la protection du système, le contrat partagé est
compilé **directement dans chaque microservice** à partir des sources du projet `Shared` :

```xml
<Compile Include="..\Shared\Models\**\*.cs" Link="Shared\Models\%(RecursiveDir)%(Filename)%(Extension)" />
```

Le projet `Shared` reste la seule source de vérité (un seul fichier `ProductCreated.cs`, même
namespace `Shared.Models`, donc le même type de message des deux côtés).

Si Smart App Control est désactivé sur la machine (ou sur un autre poste), on peut revenir à la
référence de projet classique de l'atelier, dans `ProductsAPI.csproj` et `OrdersAPI.csproj` :

```xml
<ItemGroup>
  <ProjectReference Include="..\Shared\Shared.csproj" />
</ItemGroup>
```

## 9. Dépannage

| Problème | Solution |
|---|---|
| `dotnet ef` : DbContext introuvable | vérifier `IDesignTimeDbContextFactory` et relancer `dotnet build` avant `dotnet ef` |
| `502` / `404` via la gateway | vérifier que `ProductsAPI` (7066) et `OrdersAPI` (7065) tournent |
| Erreur de certificat HTTPS | `dotnet dev-certs https --trust` |
| Avertissement « Publication de ProductCreated impossible » | RabbitMQ n'est pas démarré (port 4001) |
| `GET http://localhost:15672` inaccessible | conteneur `ecomm` non démarré |
| Fichier `Shared.dll` bloqué (0x800711C7) | Smart App Control — voir section 8 |

## 10. Arborescence

```
MicroservicesApp/
├─ ProductsAPI/            # microservice produits + Swagger + producer MassTransit
│  ├─ Controllers/ProductsController.cs
│  ├─ Data/ProductsAPIContext.cs
│  ├─ Migrations/
│  └─ Program.cs
├─ OrdersAPI/              # microservice commandes + Swagger + consumer MassTransit
│  ├─ Consumer/ProductCreatedConsumer.cs
│  ├─ Controllers/OrdersController.cs
│  ├─ Controllers/ProductsController.cs   # lecture des produits synchronisés
│  ├─ Data/OrdersAPIContext.cs
│  ├─ Migrations/
│  └─ Program.cs
├─ ApiGateway/
│  ├─ Ocelot.json
│  └─ Program.cs
├─ Shared/
│  └─ Models/ProductCreated.cs
├─ postman/
│  ├─ MicroservicesApp.postman_collection.json
│  └─ MicroservicesApp.postman_environment.json
├─ docker-compose.yml
├─ MicroservicesApp.http
└─ MicroservicesApp.slnx
```
