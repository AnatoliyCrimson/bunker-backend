import { useEffect } from 'react';
import { Outlet, Navigate } from 'react-router-dom';
import { useSelector } from 'react-redux';
import { useGetMeQuery } from '../../store/api';

const PersistLogin = () => {
  const { accessToken, user } = useSelector((state) => state.auth);

  // Мы делаем запрос только если:
  // 1. У нас есть токен (мы вроде как залогинены)
  // 2. У нас НЕТ данных юзера (ситуация после F5)
  // skip: true означает "не делать запрос"
  const skipRequest = !accessToken || !!user; 

  const { isLoading, isError, error } = useGetMeQuery(undefined, {
    skip: skipRequest,
  });

  // Если токена нет вообще — пусть ProtectedLayout сам решает, что делать (обычно редирект)
  // Но для чистоты можно пропустить дальше, ProtectedLayout перехватит.
  if (!accessToken) {
    return <Outlet />;
  }

  // Если мы сейчас загружаем данные пользователя — показываем лоадер
  if (isLoading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', marginTop: '20%' }}>
        {/* Здесь можно поставить твой красивый спиннер */}
        <h3>Загрузка пользователя...</h3>
      </div>
    );
  }

  // Если произошла ошибка (например, 401 даже после попытки рефреша внутри apiClient)
  if (isError) {
    // Можно добавить логирование или тост с ошибкой
    console.error("Failed to restore session:", error);
    // apiClient/authSlice скорее всего уже вызвали logout, но для надежности:
    return <Navigate to="/auth" replace />;
  }

  // Если все ок (user уже был или успешно загрузился) — рендерим дочерние роуты
  return <Outlet />;
};

export default PersistLogin;