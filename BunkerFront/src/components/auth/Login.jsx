import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useLoginMutation } from '../../store/api';
import { Link } from "react-router-dom"; // Используем react-router-dom
import "../../styles/components/auth/Login.scss"
import Input from '../ui/Input';

function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const [login, { isLoading, error }] = useLoginMutation();
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();

    try {
      let rememberMe = false
      await login({ email, password, rememberMe }).unwrap();
      // При успешном входе перенаправляем на главную
      navigate('/');
    } catch (err) {
      console.error('Login failed:', err);
      // Ошибка будет доступна в `error` из мутации
    }
  };

  return (
    <>
      <div className="login">
        <h2 className="login__title">Вход</h2>

        <form onSubmit={handleSubmit} className="login__form">
          <div className="login__input-group">
            <label htmlFor="email">Email</label>
            <Input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>

          <div className="login__input-group">
            <label htmlFor="password">Пароль</label>
            <Input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </div>
          
          {isLoading ? 'Вход...' : ''}
          
          <button type="submit" className='btn login__btn' disabled={isLoading}>
            Войти
          </button>

          {error && (
            <div className="login__error">
              Ошибка: {error.data?.message || error.error}
            </div>
          )}
        </form>

        <p className="login__bottom">
          <span className="login__bottom-text">
            Еще нет аккаунта?&nbsp;
          </span>
          <Link to="/auth/registration" className="login__bottom-link">
            Зарегистрироваться
          </Link>
        </p>
      </div>
    </>
  );
}

export default Login;