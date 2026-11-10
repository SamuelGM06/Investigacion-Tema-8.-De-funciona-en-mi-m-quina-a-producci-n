// Producción (imagen Docker): ruta relativa. Nginx recibe /api y lo reenvía al
// contenedor de la API, así el front no necesita conocer la URL ni hay problemas de CORS.
export const environment = {
    production: true,
    apiUrl: '/api',
};
