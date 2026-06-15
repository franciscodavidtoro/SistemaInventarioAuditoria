# Reglas de Persistencia: Infraestructura de Base de Datos (Infrastructure/Database)

Este directorio gestiona el ApplicationDbContext, las migraciones y toda la interacción directa con el motor de base de datos relacional.

## Reglas Técnicas Obligatorias

### 1. Configuración de ORM y Estrategia Code-First
* **Uso de EF Core:** Toda la persistencia se manipula a través de Entity Framework Core mediante el enfoque **Code-First**.
* **Manejo Estricto de Migraciones:** No se permiten modificaciones manuales en las tablas del servidor SQL Server; cualquier cambio estructural debe estar respaldado por una migración de EF Core.

### 2. Almacenamiento y Manejo de Archivos Multimedia
* **Prohibición de BLOBs:** Queda estrictamente prohibido almacenar arreglos de bytes binarios (BLOBs/VARBINARY(MAX)) dentro de las tablas de la base de datos para evitar la degradación del rendimiento.
* **Persistencia Local:** Las imágenes de los bienes se guardarán directamente en el sistema de archivos del servidor bajo el directorio físico wwwroot/images/.
* **Control de Tamaño:** Validación estricta del peso máximo de los archivos (Tanto imágenes como lotes de Excel) en la API antes de ser procesados.

### 3. Procesamiento Masivo y Rendimiento de Consultas
* **Optimización de Operaciones Masivas:** Para la inserción de ráfagas masivas (importaciones), es mandatorio el uso de la extensión **EFCore.BulkExtensions** para traducir operaciones complejas en un SqlBulkCopy nativo de SQL Server.
* **Procesamiento por Streams:** La lectura/escritura de archivos masivos (Excel/CSV) debe hacerse mediante streaming de bajo consumo de memoria con librerías como **MiniExcel o CsvHelper** para evitar desbordamientos de RAM.
* **Consultas Diferidas (IQueryable):** Las búsquedas y filtros se construirán dinámicamente y se evaluarán en caliente diferida directamente en el motor de base de datos, previniendo la carga de colecciones masivas sin filtrar en la memoria de la API.
