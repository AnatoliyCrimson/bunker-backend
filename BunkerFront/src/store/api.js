import { createApi } from '@reduxjs/toolkit/query/react';
import { getAccessTokenFromStorage, saveAccessTokenToStorage } from '../utils/tokenUtils';
import apiClient from '../services/apiClient';
import { loginSuccess, registerSuccess, logout, setUser  } from './authSlice';

const baseQuery = async ({ url, method, data, params, headers }) => {
  try {
    const token = getAccessTokenFromStorage();
    const result = await apiClient({
      url,
      method,
      data,
      params,
      headers: {
        ...headers,
        Authorization: token ? `Bearer ${token}` : undefined,
      },
    });
    return { data: result.data };
  } catch (axiosError) {
    const err = axiosError;
    // Нормализация ошибок для удобства отображения в компонентах
    let errorData = err.response?.data;
    
    // Обработка специфичного формата ASP.NET Identity (массивы ошибок)
    if (errorData?.errors) {
      errorData = Object.values(errorData.errors).flat().join(', ');
    } else {
      errorData = errorData?.message || errorData?.error || err.message;
    }

    return {
      error: {
        status: err.response?.status,
        data: errorData,
      },
    };
  }
};

// Обертка с логикой REFRESH
const baseQueryWithReauth = async (args, api, extraOptions) => {
  let result = await baseQuery(args, api, extraOptions);
  const authUrls = ['/Auth/login', '/Auth/register', '/Auth/refresh']

  // ПРОВЕРКА: Если 401 И это НЕ запрос логина
  if (result.error && result.error.status === 401 && !authUrls.includes(args.url) ) {
    
    // Пытаемся обновить токен
    const refreshResult = await baseQuery({ url: '/Auth/refresh', method: 'POST' }, api, extraOptions);

    if (refreshResult.data) {
      const { accessToken } = refreshResult.data;
      
      // 1. Сохраняем новый токен
      saveAccessTokenToStorage(accessToken);
      // 2. Обновляем Store
      api.dispatch(tokenRefreshed({ accessToken }));
      
      // 3. Повторяем исходный запрос с НОВЫМ токеном
      result = await baseQuery(args, api, extraOptions);
    } else {
      // Если рефреш не удался (например, кука протухла)
      api.dispatch(logout());
    }
  }
  return result;
};

const clearSessionOnQueryStarted = async (arg, { dispatch, queryFulfilled, getState }) => {
  try {
    await queryFulfilled;
    const state = getState();
    const user = state.auth.user;

    if (user) {
      dispatch(setUser({
        ...user,
        currentRoomId: null,
        currentGameId: null
      }));
    }
  } catch {}
};

export const api = createApi({
  reducerPath: 'api',
  baseQuery: baseQueryWithReauth,
  tagTypes: ['Auth', 'Room', 'User', 'Game'],
  endpoints: (builder) => ({ 
    
    // ------------- Регистрация, аутентификация -------------
    
    login: builder.mutation({
      query: (credentials) => ({
        url: '/Auth/login',
        method: 'POST',
        data: credentials,
      }),
      async onQueryStarted(credentials, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          const { userInfo, accessToken } = data;

          // Сохраняем токен в localStorage
          saveAccessTokenToStorage(accessToken);

          // Обновляем состояние Redux
          dispatch(loginSuccess({ userInfo, accessToken }));
        } catch (error) {
          // Ошибку логина обработает компонент через result.error
          
        }
      },
    }),

    // Мутация для регистрации
    register: builder.mutation({
      query: (userData) => ({
        url: '/Auth/register',
        method: 'POST',
        data: userData,
      }),
      async onQueryStarted(userData, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          const { userInfo, accessToken } = data;

          // Сохраняем токен в localStorage
          saveAccessTokenToStorage(accessToken);

          // Обновляем состояние Redux
          dispatch(registerSuccess({ userInfo, accessToken }));
        } catch (error) {
          console.log(error);
        }
      },
    }),

    logout: builder.mutation({
      query: () => ({
        url: '/Auth/logout',
        method: 'POST',
      }),
      async onQueryStarted(args, { dispatch, queryFulfilled }) {
        try {
          await queryFulfilled;
        } catch (error) {
          console.log(error);
        } finally {
          // Сбрасываем состояние в любом случае
          dispatch(logout());
        }
      },
    }),

    getMe: builder.query({
      query: () => (
        {
          url: '/Auth/me',
          method: 'GET',
        }
      ),
      providesTags: ['User'], 
      async onQueryStarted(arg, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          // Твой бэкенд возвращает объект { UserInfo: { ... } }
          // Поэтому берем data.userInfo (с маленькой буквы, если у тебя camelCase formatter на бэке,
          // или data.UserInfo, если JSON возвращается PascalCase. Обычно в JS это camelCase).
          // Судя по твоему C# коду: return Ok(new { UserInfo = userInfo });
          // Скорее всего придет data.userInfo
          
          if (data.userInfo) {
             dispatch(setUser(data.userInfo));
          } else if (data.UserInfo) {
             dispatch(setUser(data.UserInfo));
          }
          
        } catch (error) {
          console.error(error)
          // Если токен протух окончательно и refresh не помог — разлогиниваем
          dispatch(logout());
        }
      },
    }),


    // ------------- Профиль пользователя -------------

    changeName: builder.mutation({
      query: (newName) => ({
        url: `/Profile/name`,
        method: 'PUT',
        data: { newName },
      }),
      invalidatesTags: ['User'],
      async onQueryStarted(newName, { dispatch, queryFulfilled, getState }) {
        try {
          await queryFulfilled; // Ждем успеха от сервера (что имя реально сменилось)
          
          const state = getState();
          const currentUser = state.auth.user;

          if (currentUser) {
            dispatch(setUser({
              ...currentUser,
              name: newName // Обновляем имя в Redux
            }));
          }
        } catch (error) {
          console.error(error);
        }
      },
    }),

    changeEmail: builder.mutation({
      query: (newEmail) => ({
        url: '/Profile/email',
        method: 'PUT',
        data: { newEmail },
      }),
      invalidatesTags: ['User'],
            async onQueryStarted(newEmail, { dispatch, queryFulfilled, getState }) {
        try {
          await queryFulfilled;
          
          const state = getState();
          const currentUser = state.auth.user;

          if (currentUser) {
            dispatch(setUser({
              ...currentUser,
              email: newEmail // Обновляем почту в Redux
            }));
          }
        } catch (error) {
           console.error(error);
           
        }
      },

    }),

    uploadAvatar: builder.mutation({
      query: (file) => {
        const formData = new FormData();
        formData.append('File', file); 
      
        return {
          url: '/Profile/avatar',
          method: 'POST',
          data: formData,
          headers: {
            'Content-Type': undefined 
          }
        };
      },
      invalidatesTags: ['User'],

      async onQueryStarted(arg, { dispatch, queryFulfilled, getState }) {
        try {
          const { data } = await queryFulfilled; 

          const state = getState();
          const currentUser = state.auth.user;

          if (currentUser) {
            dispatch(setUser({
              ...currentUser,
              avatarUrl: data.url
            }));
          }
        } catch (error) {
          console.error(error);
        }
      },
    }),

    changePassword: builder.mutation({
      query: (passwords) => ({
        url: '/Profile/password',
        method: 'PUT',
        data: passwords, // { currentPassword, newPassword, confirmPassword }
      }),
    }),

    checkPassword: builder.mutation({
      query: (password) => ({
        url: '/Profile/check-password',
        method: 'POST',
        data: { password }, // DTO: { password: "..." }
      }),
    }),


    
   // ------------- Комната ------------- 

    // Создание комнаты
    createRoom: builder.mutation({
      query: () => ({
        url: '/Room/create',
        method: 'POST',
        data: {},
      }),
      invalidatesTags: ['Room', 'User'],
      // Добавляем мгновенное обновление
      async onQueryStarted(arg, { dispatch, queryFulfilled, getState }) {
        try {
          const { data } = await queryFulfilled; 

          const state = getState();
          const user = state.auth.user;

          if (user) {
            dispatch(setUser({
              ...user,
              currentRoomId: data.roomId // Сразу ставим ID комнаты
            }));
          }
        } catch {}
      },
    }),

    // Присоединение к комнате
    joinRoom: builder.mutation({
      query: (inviteCode) => ({
        url: '/Room/join',
        method: 'POST',
        data: { inviteCode },
      }),
      invalidatesTags: ['Room', 'User'],
      async onQueryStarted(arg, { dispatch, queryFulfilled, getState }) {
        try {
          const { data } = await queryFulfilled;

          const state = getState();
          const user = state.auth.user;

          if (user) {
            dispatch(setUser({
              ...user,
              currentRoomId: data.roomId
            }));
          }
        } catch {}
      },
    }),

    // Получение данных комнаты
    getRoom: builder.query({
      query: (roomId) => ({
        url: `/Room/${roomId}`,
        method: 'GET',
      }),
      providesTags: ['Room'],
    }),
    
    leaveRoom: builder.mutation({
      query: (roomId) => ({
        url: '/Room/leave',
        method: 'POST',
        data: { roomId },
      }),
      invalidatesTags: ['Room', 'User'],
      onQueryStarted: clearSessionOnQueryStarted,
    }),

    deleteRoom: builder.mutation({
      query: () => ({
        url: `/Room/host`,
        method: 'DELETE',
        data: {},
      }),
      invalidatesTags: ['User'],
      onQueryStarted: clearSessionOnQueryStarted,
    }),


    // ------------- Процесс игры -------------

    startGame: builder.mutation({
      query: (roomId) => ({
        url: `/Game/start`,
        method: 'POST',
        data: {roomId}
      }),
      providesTags: ['Game', 'User'], 
      async onQueryStarted(arg, { dispatch, queryFulfilled, getState }) {
        try {
          const { data } = await queryFulfilled;
          // data = { gameId: "..." }

          const state = getState();
          const user = state.auth.user;

          if (user) {
            dispatch(setUser({
              ...user,
              currentRoomId: null, // Вышли из комнаты
              currentGameId: data.gameId // Вошли в игру
            }));
          }
        } catch {}
      },
    }),

    getGameState: builder.query({
      query: (gameId) => ({
        url: `/Game/${gameId}/state`,
        method: 'GET',
      }),
      providesTags: ['Game'], 
    }),

    revealCharacteristic: builder.mutation({
      query: ({ gameId, traitName }) => ({
        url: '/Play/reveal',
        method: 'POST',
        data: { gameId, traitName },
      }),
      invalidatesTags: ['Game'], 
    }),

    votePlayer: builder.mutation({
      query: ({ gameId, targetPlayerId }) => ({
        url: '/Play/vote',
        method: 'POST',
        data: { gameId, targetPlayerId },
      }),
      invalidatesTags: ['Game'],
    }),

    deleteGame: builder.mutation({
      query: (gameId) => ({
        url: `/Game/${gameId}`,
        method: 'DELETE',
      }),
      // Не обязательно, но полезно, чтобы сбросить кэш
      invalidatesTags: ['Game', 'User'], 
      onQueryStarted: clearSessionOnQueryStarted,
    }),

  }),
});

export const { 
  useLoginMutation, 
  useRegisterMutation, 
  useLogoutMutation, 
  useGetMeQuery,
  useCreateRoomMutation,
  useJoinRoomMutation,
  useGetRoomQuery,
  useLeaveRoomMutation,
  useDeleteRoomMutation,
  useChangeNameMutation,
  useChangeEmailMutation,
  useUploadAvatarMutation,
  useChangePasswordMutation,
  useCheckPasswordMutation,
  useStartGameMutation,
  useGetGameStateQuery,
  useRevealCharacteristicMutation,
  useVotePlayerMutation,
  useDeleteGameMutation,
  useApiQuery, 
  useApiMutation
} = api;