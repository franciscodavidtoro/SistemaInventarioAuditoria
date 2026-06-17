# Módulo de Autenticación y Cuentas (Features/Auth)

Este directorio contiene la lógica completa para el registro de nuevos usuarios, validación de credenciales y emisión de tokens de acceso para el Sistema de Gestión de Inventario y Auditoría. 

El módulo está construido utilizando **Vertical Slice Architecture** (Arquitectura de Cortes Verticales), aislando de manera limpia las peticiones (Requests), respuestas (Responses), lógica de negocio (Handlers) y la exposición de rutas (Endpoints) utilizando Minimal APIs.

## Reglas de Negocio y Seguridad Implementadas

### 1. Validación Estricta de Identidad
* **Cédula Ecuatoriana (Módulo 10):** El sistema rechaza cualquier intento de registro que no cumpla con el algoritmo matemático oficial de validación de cédulas ecuatorianas.
* **Control de Unicidad:** Se valida a nivel de aplicación (antes de llegar al ORM) que no existan correos electrónicos ni números de cédula duplicados para evitar conflictos en la base de datos.

### 2. Protección de Credenciales en Reposo
* **Hashing Unidireccional:** El sistema no tiene visibilidad de las contraseñas en texto plano. Se utiliza la librería `BCrypt.Net-Next` para aplicar un algoritmo de hashing criptográfico unidireccional con sal (*salt*) de alta iteración antes de la persistencia.

### 3. Prevención de Escalabilidad de Privilegios (RBAC)
* **Asignación Inmutable de Roles:** El endpoint de registro autónomo es de uso público. Por motivos estrictos de seguridad, el sistema descarta cualquier rol enviado por el cliente en la petición e impone de manera forzada e inmutable el rol **`User`** en la base de datos.

### 4. Autenticación Sin Estado (Stateless)
* **Emisión de JWT:** El inicio de sesión exitoso emite un JSON Web Token (JWT) firmado con el algoritmo `HMAC-SHA256`. 
* **Claims Integrados:** El token encapsula el UUID del usuario (Subject), su Rol y su Correo Electrónico para facilitar la toma de decisiones de autorización inmediata en el resto de los módulos del sistema sin necesidad de consultar repetitivamente a la base de datos.
* **Tiempo de Vida (TTL):** Los tokens emitidos tienen una validez estricta de 8 horas desde su generación.

## Endpoints Expuestos

| Método | Ruta | Descripción | Acceso |
| :--- | :--- | :--- | :--- |
| **POST** | `/api/auth/registro` | Crea una nueva cuenta de usuario en el sistema. | Público (`AllowAnonymous`) |
| **POST** | `/api/auth/login` | Valida credenciales y retorna un Token JWT. | Público (`AllowAnonymous`) |

## Dependencias Clave del Módulo
* `System.IdentityModel.Tokens.Jwt`: Para la estructuración y firma del Token Bearer.
* `BCrypt.Net-Next`: Para la encriptación de las contraseñas.