import { createSlice } from '@reduxjs/toolkit';
import { getAccessTokenFromStorage, removeAccessTokenFromStorage } from '../utils/tokenUtils';

// Вспомогательная функция для получения начального состояния из localStorage
const getInitialState = () => {
  const token = getAccessTokenFromStorage();
  return {
    user: null,
    accessToken: token,
    isAuthenticated: !!token, // true, если токен есть и не истек (в реальности можно проверить `isTokenExpired` при запуске)
    loading: false,
    error: null,
  };
};

const authSlice = createSlice({
  name: 'auth',
  initialState: getInitialState(),
  reducers: {
    setUser: (state, action) => {
      state.user = action.payload;
      state.isAuthenticated = true;
      state.loading = false; // На всякий случай сбрасываем
    },
    loginStart: (state) => {
      state.loading = true;
      state.error = null;
    },
    loginSuccess: (state, action) => {
      const { userInfo, accessToken } = action.payload;
      state.user = userInfo;
      state.accessToken = accessToken;
      state.isAuthenticated = true;
      state.loading = false;
      state.error = null;
      // Сохраняем токен в localStorage
      // (В будущем можно добавить проверку на истечение)
    },
    loginFailure: (state, action) => {
      state.loading = false;
      state.error = action.payload;
    },
    registerStart: (state) => {
      state.loading = true;
      state.error = null;
    },
    registerSuccess: (state, action) => {
      const { userInfo, accessToken } = action.payload;
      state.user = userInfo;
      state.accessToken = accessToken;
      state.isAuthenticated = true;
      state.loading = false;
      state.error = null;
    },
    registerFailure: (state, action) => {
      state.loading = false;
      state.error = action.payload;
    },
    logout: (state) => {
      state.user = null;
      state.accessToken = null;
      state.isAuthenticated = false;
      state.error = null;
      // Удаляем токен из localStorage
      removeAccessTokenFromStorage();
    },
    tokenRefreshed: (state, action) => {
      const { accessToken } = action.payload;
      state.accessToken = accessToken;
      state.isAuthenticated = true;
      // Не меняем user, т.к. он не изменился
    },
  },
});

export const {
  setUser,
  loginStart,
  loginSuccess,
  loginFailure,
  registerStart,
  registerSuccess,
  registerFailure,
  logout,
  tokenRefreshed,
} = authSlice.actions;

export default authSlice.reducer;