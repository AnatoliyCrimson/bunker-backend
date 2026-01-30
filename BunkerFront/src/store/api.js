import { createApi } from '@reduxjs/toolkit/query/react';
import { getAccessTokenFromStorage, saveAccessTokenToStorage } from '../utils/tokenUtils';
import apiClient from '../services/apiClient';
import { loginSuccess, registerSuccess, logout, setUser  } from './authSlice';

// Вспомогательная функция для добавления токена в заголовки
const prepareHeaders = (headers) => {
  const token = getAccessTokenFromStorage();
  if (token) {
    headers['Authorization'] = `Bearer ${token}`; // axios использует объект заголовков
  }
  return headers;
};

// Создаём базовый query с axios
const axiosBaseQuery = () => async ({ url, method, data, params, headers }) => {
  try {
    const response = await apiClient({
      url,
      method,
      data,
      params,
      headers: prepareHeaders(headers || {}), 
    });
    return { data: response.data };
  } catch (axiosError) {
    const err = axiosError;
    return {
      error: {
        status: err.response?.status,
        data: err.response?.data || err.message,
      },
    };
  }
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
  baseQuery: axiosBaseQuery(),
  tagTypes: ['Auth', 'Room', 'User', 'Game'],
  endpoints: (builder) => ({
    
    // ------------- Регистрация -------------
    
    login: builder.mutation({
      query: (credentials) => ({
        url: '/auth/login',
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
          console.log(error);
          
        }
      },
    }),

    // Мутация для регистрации
    register: builder.mutation({
      query: (userData) => ({
        url: '/auth/register',
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
        url: '/auth/logout',
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
          url: '/auth/me',
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
      query: (roomId) => ({
        url: `/Room/${roomId}`,
        method: 'DELETE',
        data: { roomId },
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