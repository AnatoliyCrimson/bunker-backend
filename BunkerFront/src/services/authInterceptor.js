import axios from 'axios';
import { saveAccessTokenToStorage, removeAccessTokenFromStorage } from '../utils/tokenUtils';

const API_BASE_URL = '/api';

// Асинхронная функция для обновления токена
export const refreshAccessToken = async () => {
  try {
    const response = await axios.post(
      `${API_BASE_URL}/auth/refresh`,
      {},
      {
        withCredentials: true, // Обязательно для HttpOnly cookie
      }
    );

    const { accessToken } = response.data;

    if (accessToken) {
      saveAccessTokenToStorage(accessToken);
      return accessToken;
    } else {
      throw new Error('No new access token received');
    }
  } catch (error) {
    console.error('Failed to refresh token:', error);

    // Если refresh не удался (например, 401 или 403), очищаем токены
    removeAccessTokenFromStorage();
    // В следующем этапе вызовем logout через Redux
    throw error;
  }
};