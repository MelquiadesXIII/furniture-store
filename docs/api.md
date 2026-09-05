# API — Furnistore

Referencia de los servicios expuestos por `apps/api` (`API.Furnistore.API`), lista para consumir desde el frontend u otro cliente. Generado a partir del código fuente de los controladores (no de un contrato aparte), así que refleja el comportamiento real, incluidas sus inconsistencias.

## Base URL

| Entorno | URL | Notas |
|---|---|---|
| Local (perfil `http`) | `http://localhost:5135` | El que usa `apps/web` (`API_BASE_URL` en su `.env`), evita el certificado autofirmado |
| Local (perfil `https`) | `https://localhost:7064` | Certificado de desarrollo autofirmado |
| Swagger UI | `{baseUrl}/swagger` | Solo se sirve cuando `ASPNETCORE_ENVIRONMENT=Development` |

Todas las rutas cuelgan de `/api/[controller]` (convención de ASP.NET Core), p. ej. el controlador `ProductsController` → `/api/Products`.

## Formato general

- **Content-Type**: `application/json` en request y response.
- **Casing**: los cuerpos JSON van en `camelCase` (política por defecto de System.Text.Json en ASP.NET Core), aunque las clases en C# estén en `PascalCase`. Ej.: `Product.ProductCategoryId` → `productCategoryId`.
- **Autenticación**: JWT Bearer. Header `Authorization: Bearer <token>`.
- **Errores de validación de modelo** (falta un campo `[Required]`, tipos mal formados, etc.): como los controladores llevan `[ApiController]`, ASP.NET Core intercepta el `ModelState` inválido **antes** de que corra la acción y devuelve automáticamente un `400` con el formato estándar `ValidationProblemDetails`:
  ```json
  {
    "errors": { "Email": ["The Email field is required."] },
    "title": "One or more validation errors occurred.",
    "status": 400
  }
  ```
- **Errores de negocio en Authentication** (credenciales inválidas, email no confirmado, etc.): formato propio, no el de arriba:
  ```json
  { "result": false, "errors": ["Invalid Credentials"] }
  ```
- Los demás controladores (`Clients`, `Products`, `ProductCategories`, `Orders`) devuelven `404` o `400` sin cuerpo o con un string plano en los casos de error (no hay un envoltorio de error consistente — ver detalle por endpoint).

## Autenticación — `/api/Authentication`

No requiere token (son los endpoints para obtenerlo), salvo donde se indique.

### `POST /api/Authentication/Register`

Crea el usuario (Identity) y dispara el correo de confirmación (`IEmailSender`, SMTP configurado en `SmtpSettings`). El usuario **no puede iniciar sesión hasta confirmar el email**.

Body:
```json
{ "name": "string", "emailAddress": "string", "password": "string" }
```
Reglas de password (Identity, `Program.cs`): mínimo 8 caracteres, requiere al menos un dígito. No exige mayúscula, minúscula ni símbolo.

Respuestas:
- `200 OK` → `{ "result": true }`
- `400 BadRequest` → email ya existe: `{ "result": false, "errors": ["Email already exists"] }`
- `400 BadRequest` → error de Identity (password débil, etc.): `{ "result": false, "errors": ["..."] }`

> Nota: `name` se recibe pero no se persiste en el `IdentityUser` (no hay ninguna tabla de perfil aparte); solo se usan `EmailAddress` y `Password`.

### `POST /api/Authentication/Login`

Body:
```json
{ "email": "string", "password": "string" }
```

Respuestas:
- `200 OK` → `AuthResult`:
  ```json
  { "token": "jwt...", "refreshToken": "string", "result": true, "errors": null }
  ```
- `400 BadRequest` → usuario no existe: `{ "result": false, "errors": ["Invalid Payload"] }`
- `400 BadRequest` → email sin confirmar: `{ "result": false, "errors": ["Email needs to be confirmed."] }`
- `400 BadRequest` → password incorrecto: `{ "result": false, "errors": ["Invalid Credentials"] }`

El JWT expira según `JwtConfig:ExpiryTime` (`appsettings.json`, por defecto `01:00:00` = 1 hora). El `refreshToken` se genera y se guarda en BD con expiración de 6 meses.

### `POST /api/Authentication/RefreshToken`

Renueva un JWT expirado usando el refresh token emitido en el login.

Body:
```json
{ "token": "jwt-expirado", "refreshToken": "string" }
```

Respuestas:
- `200 OK` → mismo shape que `AuthResult` (nuevo `token` + nuevo `refreshToken`; el anterior queda marcado como usado, uso único).
- `400 BadRequest` → `{ "result": false, "errors": ["Invalid Token"] }` (token no es HMAC-SHA256, refresh token no existe/ya usado/revocado, o el `jti` no coincide).
- `400 BadRequest` → `{ "result": false, "errors": ["Expired Token"] }` (el refresh token en sí ya expiró).

### `GET /api/Authentication/ConfirmEmail?userId={id}&code={code}`

Enlace al que apunta el correo de confirmación (`callbackUrl` generado en `Register`). No pensado para llamarse directo desde una SPA salvo que reconstruyas la misma URL.

Respuestas:
- `200 OK` con un string plano: `"Thanks you for confirming your email."` o `"There has been an error confirming your email"`.
- `400 BadRequest` si faltan `userId` o `code`.
- `404 NotFound` si el `userId` no existe.

## Products — `/api/Products`

**Lecturas públicas, escrituras con JWT.** El catálogo se muestra antes de iniciar sesión, así que `GET` no requiere token (`[AllowAnonymous]`); `POST`/`PUT`/`DELETE` sí (`[Authorize]`). Sin paginación, sin filtros por query aparte de `GetByCategory`.

> Datos de prueba: la migración `SeedCatalogTestData` inserta 4 `ProductCategories` (Sillas, Mesas, Estanterías, Lámparas) y 16 `Products` de ejemplo con `HasData`. Hay que aplicarla contra la base con `dotnet ef database update` (desde `apps/api`, con el `.env` configurado) para que el catálogo deje de estar vacío.

| Método | Ruta | Auth | Body | Descripción |
|---|---|---|---|---|
| `GET` | `/api/Products` | No | — | Lista completa de productos |
| `GET` | `/api/Products/{id}` | No | — | Un producto; `404` si no existe |
| `GET` | `/api/Products/GetByCategory/{productCategoryId}` | No | — | Productos de una categoría (lista vacía si no hay, nunca `404`) |
| `POST` | `/api/Products` | **Sí (JWT)** | `Product` (sin `id`) | Crea. Devuelve `201`, pero el `Location`/valor de `CreatedAtAction` está mal armado (pasa `product.Id` como nombre de ruta) — no confíes en el header `Location`, usa el body |
| `PUT` | `/api/Products` | **Sí (JWT)** | `Product` completo, **incluye `id`** | Actualiza. `204 NoContent`. No valida que el `id` exista antes de hacer `Update` |
| `DELETE` | `/api/Products` | **Sí (JWT)** | `Product` completo, **incluye `id`** | Borra. `204 NoContent`. El body debe traer el objeto, no solo el id |

`Product` shape:
```ts
{ id: number; name: string; price: number; productCategoryId: number }
```

> `PUT`/`DELETE` no llevan `{id}` en la ruta — el id va dentro del body JSON. Es distinto al patrón REST habitual, ten cuidado al construir el cliente.
>
> `POST`/`PUT`/`DELETE` solo exigen estar autenticado, no un rol de admin (no hay roles definidos en Identity todavía) — cualquier cuenta logueada puede escribir en el catálogo.

## Product Categories — `/api/ProductCategories`

**Lecturas públicas, escrituras con JWT.** Mismo patrón que `Products` (sin `GetByCategory`, claro).

| Método | Ruta | Auth | Body | Descripción |
|---|---|---|---|---|
| `GET` | `/api/ProductCategories` | No | — | Lista completa |
| `GET` | `/api/ProductCategories/{id}` | No | — | Una categoría; `404` si no existe |
| `POST` | `/api/ProductCategories` | **Sí (JWT)** | `ProductCategory` (sin `id`) | Crea. `201` |
| `PUT` | `/api/ProductCategories` | **Sí (JWT)** | `ProductCategory` completo, con `id` | Actualiza. `204` |
| `DELETE` | `/api/ProductCategories` | **Sí (JWT)** | `ProductCategory` completo, con `id` | Borra. `204` |

`ProductCategory` shape:
```ts
{ id: number; name: string }
```

## Clients — `/api/Clients`

**Requiere JWT**. Mismo patrón CRUD que los anteriores.

| Método | Ruta | Body | Descripción |
|---|---|---|---|
| `GET` | `/api/Clients` | — | Lista completa |
| `GET` | `/api/Clients/{id}` | — | Un cliente; `404` si no existe |
| `POST` | `/api/Clients` | `Client` (sin `id`) | Crea. `201` |
| `PUT` | `/api/Clients` | `Client` completo, con `id` | Actualiza. `204` |
| `DELETE` | `/api/Clients` | `Client` completo, con `id` | Borra. `204` |

`Client` shape:
```ts
{
  id: number;        // en C# la propiedad se llama "ID", pero serializa igual como "id"
  firstName: string;
  lastName: string;
  birthDate: string; // ISO 8601
  phone: string;
  address: string;
}
```

## Orders — `/api/Orders`

**Requiere JWT**. Es el único recurso con relación anidada (`OrderDetails`).

| Método | Ruta | Body | Descripción |
|---|---|---|---|
| `GET` | `/api/Orders` | — | Lista completa, cada orden incluye `orderDetails` |
| `GET` | `/api/Orders/{id}` | — | Una orden con `orderDetails`; `404` si no existe |
| `POST` | `/api/Orders` | `Order` con `orderDetails` (ver abajo) | Crea la orden y sus detalles. `400` si `orderDetails` es `null` (una lista vacía `[]` sí pasa la validación) |
| `PUT` | `/api/Orders` | `Order` completo, con `id` | Reemplaza cabecera **y** borra+recrea todos los `orderDetails` con los que vengan en el body |
| `DELETE` | `/api/Orders` | `Order` completo, con `id` | Borra la orden y sus `orderDetails`. `404` si no existe |

`Order` shape:
```ts
{
  id: number;
  orderNumber: number;
  clientId: number;
  orderDate: string;     // ISO 8601
  deliveryDate: string;  // ISO 8601
  orderDetails: OrderDetail[];
}
```

`OrderDetail` shape (no tiene `id` propio, es tabla de detalle simple):
```ts
{ orderId: number; productId: number; quantity: number }
```

> No hay validación de que `clientId` / `productId` existan, ni de que `quantity` sea positivo — eso queda del lado del cliente o falla como error de FK en la base de datos.

## Test — `/api/Test`

Sin autenticación. Solo para verificar que la API está viva; no pensado para producción.

| Método | Ruta | Query | Respuesta |
|---|---|---|---|
| `GET` | `/api/Test` | — | `"Hola, este es el test controller"` |
| `GET` | `/api/Test/welcome` | `name`, `age` | `"Hola {name}, tu edad es {age}"` (HTML-encoded) |

## Variables de entorno requeridas (`apps/api/.env`)

Ver `apps/api/.env.example`:

```
JWT_SECRET=
JWT_ISSUER=
JWT_AUDIENCE=
DATABASE_URL=
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80
```

En desarrollo, `Program.cs` carga este `.env` con `dotenv.net` solo si `ASPNETCORE_ENVIRONMENT=Development`. `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE` y `DATABASE_URL` son obligatorios: la app lanza `InvalidOperationException` al arrancar si faltan (no hay fallback silencioso).

Para que `Register`/confirmación de email funcionen hace falta además `SmtpSettings:UserName`/`Password` (o el user-secret equivalente) — si no hay SMTP configurado, `Register` devuelve 500 al intentar enviar el correo.

## Resumen rápido de endpoints

| Endpoint | Auth | Métodos |
|---|---|---|
| `/api/Authentication/Register` | No | POST |
| `/api/Authentication/Login` | No | POST |
| `/api/Authentication/RefreshToken` | No | POST |
| `/api/Authentication/ConfirmEmail` | No | GET |
| `/api/Products`, `/api/Products/{id}`, `/api/Products/GetByCategory/{id}` | No en GET · **Sí (JWT) en POST/PUT/DELETE** | GET, POST, PUT, DELETE |
| `/api/ProductCategories`, `/api/ProductCategories/{id}` | No en GET · **Sí (JWT) en POST/PUT/DELETE** | GET, POST, PUT, DELETE |
| `/api/Clients`, `/api/Clients/{id}` | **Sí (JWT)** | GET, POST, PUT, DELETE |
| `/api/Orders`, `/api/Orders/{id}` | **Sí (JWT)** | GET, POST, PUT, DELETE |
| `/api/Test`, `/api/Test/welcome` | No | GET |
