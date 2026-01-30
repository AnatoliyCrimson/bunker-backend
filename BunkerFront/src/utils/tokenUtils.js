import { jwtDecode } from 'jwt-decode'; // Обрати внимание на импорт

const ACCESS_TOKEN_KEY = 'access_token';

export const saveAccessTokenToStorage = (token) => {
  localStorage.setItem(ACCESS_TOKEN_KEY, token);
};

export const getAccessTokenFromStorage = () => {
  return localStorage.getItem(ACCESS_TOKEN_KEY);
};

export const removeAccessTokenFromStorage = () => {
  localStorage.removeItem(ACCESS_TOKEN_KEY);
};

export const isTokenExpired = (token) => {
  if (!token) return true;
  try {
    const decoded = jwtDecode(token);
    const currentTime = Date.now() / 1000;
    return decoded.exp < currentTime;
  } catch (error) {
    console.error('Error decoding token:', error);
    return true;
  }
};

export const decodeToken = (token) => {
  if (!token) return null;
  try {
    return jwtDecode(token);
  } catch (error) {
    console.error('Error decoding token:', error);
    return null;
  }
};