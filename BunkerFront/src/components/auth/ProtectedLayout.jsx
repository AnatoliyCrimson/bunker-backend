import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useSelector } from 'react-redux';

const ProtectedLayout = () => {
  const { user, isAuthenticated } = useSelector((state) => state.auth);
  const location = useLocation();

  if (!(isAuthenticated && user)) {
    // Если не авторизован — перекидываем на логин, запоминая, откуда пришел
    return <Navigate to="/auth" state={{ from: location }} replace />;
  }

  // Если авторизован — рендерим дочерние роуты (Outlet)
  return <Outlet />;
};

export default ProtectedLayout;