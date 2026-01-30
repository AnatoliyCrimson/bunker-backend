import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useRegisterMutation } from '../../store/api';
import "../../styles/components/auth/Registration.scss"
import { Link } from "react-router-dom"; // Используем react-router-dom
import Input from '../ui/Input';

function Registration() {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const [register, { isLoading, error }] = useRegisterMutation();
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();

    try {
      await register({ name, email, password }).unwrap();
      // При успешной регистрации перенаправляем на главную
      navigate('/');
    } catch (err) {
      console.error('Registration failed:', err);
      // Ошибка будет доступна в `error` из мутации
    }
  };

  return (
    <>
      <div className="registration__container">
        <h2 className="registration__title">Регистрация</h2>

        <form onSubmit={handleSubmit} className="registration__form">
          <div className="registration__input-group">
            <label htmlFor="name">Имя:</label>
            <Input
              id="name"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
            />
          </div>

          <div className="registration__input-group">
            <label htmlFor="email">Email:</label>
            <Input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>

          <div className="registration__input-group">
            <label htmlFor="password">Пароль:</label>
            <Input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </div>

          <button className='btn registration__btn' type="submit" disabled={isLoading}>
            {isLoading ? 'Регистрация...' : 'Зарегистрироваться'}
          </button>

          {error && (
            <div className="registration__error">
              Ошибка: {error.data?.message || error.error}
            </div>
          )}
        </form>

        <p className="registration__bottom">
          <span className="registration__bottom-text">
            Уже есть аккаунт?&nbsp;
          </span>
          <Link to="/auth" className="registration__bottom-link">
            Войти
          </Link>
        </p>
      </div>
    </>
  );
}

export default Registration;