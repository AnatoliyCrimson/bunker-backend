import axios from 'axios';
import { refreshAccessToken } from './authInterceptor'; // Импортируем функцию обновления токена



const apiClient = axios.create({
  baseURL: '/api',
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Response Interceptor
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true; // Защита от бесконечного цикла
      try {
        const newAccessToken = await refreshAccessToken();

        if (newAccessToken) {
          // Обновляем заголовок Authorization для оригинального запроса
          originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
          return apiClient(originalRequest); // Повторяем запрос
        }
      } catch (refreshError) {
        console.error('Token refresh failed:', refreshError);
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);

export default apiClient;