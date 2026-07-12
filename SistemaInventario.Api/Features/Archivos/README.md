# Módulo: Archivos

Gestión de subida, consulta, reemplazo y eliminación de archivos/imágenes del sistema.

## Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/archivos` | Sube un archivo (`multipart/form-data`, campo `archivo`) y registra su información. |
| GET | `/api/archivos/{id}` | Obtiene la información de un archivo específico. No existe listado general. |
| PUT | `/api/archivos/{id}` | Reemplaza el archivo físico y actualiza el registro (propietario o Admin). |
| DELETE | `/api/archivos/{id}` | Elimina el archivo físico y su registro (propietario o Admin). |

## Almacenamiento físico

Los archivos se guardan en `FileStorage:ArchivosPath` (por defecto `wwwroot/archivos/`) con un nombre físico generado con `Guid.NewGuid()` + la extensión original (ej. `4cb5d0d7-a12d-46b5-98ec-29f5af1ce923.png`). El nombre original nunca se usa como nombre de archivo en disco, evitando colisiones y problemas de seguridad (path traversal, sobrescritura).

## Validaciones

- Archivo requerido, no vacío.
- Tamaño máximo: 10 MB (`IFileStorageService.TamanioMaximoBytes`).
- Extensión permitida: `.jpg, .jpeg, .png, .gif, .webp, .pdf, .docx, .xlsx, .csv`.
- Tipo MIME permitido, validado contra la extensión.

## Consistencia en escritura

- **Reemplazo (PUT):** se guarda primero el archivo nuevo, se actualiza la base de datos y solo si esa operación tiene éxito se elimina el archivo físico anterior. Si la base de datos falla, se elimina el archivo nuevo recién guardado y se conserva el anterior intacto.
- **Eliminación (DELETE):** se elimina primero el archivo físico y luego el registro en base de datos. Si el archivo físico ya no existe, no se trata como error fatal para evitar registros huérfanos.
