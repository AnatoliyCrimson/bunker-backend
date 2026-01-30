import { configureStore } from '@reduxjs/toolkit';
import authSlice from './authSlice';
import { api } from './api';

export const store = configureStore({
  reducer: {
    auth: authSlice,
    [api.reducerPath]: api.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(api.middleware),
});

export const { useApiQuery, useApiMutation } = api;
export const { useDispatch, useSelector } = store;
export default store;