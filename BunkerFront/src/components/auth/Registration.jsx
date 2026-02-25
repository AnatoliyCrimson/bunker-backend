import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useRegisterMutation } from '../../store/api';
import "../../styles/components/auth/Registration.scss"
import { Link } from "react-router-dom"; // Используем react-router-dom
import Input from '../ui/Input';
import PasswordConfirmInputs from '../ui/PasswordConfirmInputs';

function Registration() {
    const [name, setName] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [isPasswordConfirmed, setPasswordConfirmed] = useState(false);
    const [passwordError, setPasswordError] = useState('');

    const [register, { isLoading, error }] = useRegisterMutation();
    const navigate = useNavigate();

    const validatePassword = (pass) => {
    const regex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$/;
    return regex.test(pass);
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!passwordError) {
            try {
                await register({ name, email, password }).unwrap();
                
                navigate('/');
                
                passwordError('');
                // При успешной регистрации перенаправляем на главную
            } catch (err) {
                console.error('Registration failed:', err);
                // Ошибка будет доступна в `error` из мутации
            }
        }
    };
console.log(!(password && confirmPassword) || passwordError);

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
                        <PasswordConfirmInputs
                            value={password} 
                            confirmValue={confirmPassword} 
                            passwordPlaceholder={'Пароль'}
                            passwordConfirmPlaceholder={'Подтверждение пароля'} 
                            newPassword={password}
                            setNewPassword={(value) => setPassword(value)}
                            confirmPassword={confirmPassword}
                            setConfirmPassword={(value) => setConfirmPassword(value)}
                            setPasswordError={(error) => setPasswordError(error)}
                        /> 
                        {/* <Input
                            value={newPassword}
                            onChange={(e) => validateNewPassword(e.target.value, "new")}
                            type="password" 
                            placeholder='Новый пароль'
                        />
                        <Input 
                            value={confirmPassword} 
                            onChange={(e) => validateNewPassword(e.target.value, "confirm")} 
                            type="password" 
                            placeholder='Подтверждение пароля'
                        /> */}

                        {/* <Input
                            id="password"
                            type="password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            required
                        /> */}
                    </div>

                    <button disabled={!(password && confirmPassword) || passwordError || isLoading} className='btn registration__btn' type="submit">
                        {isLoading ? 'Регистрация...' : 'Зарегистрироваться'}
                    </button>

                    {passwordError && <span className='registration__error error'>{ passwordError }</span>}  
                    {(passwordError || error) && (
                        <div className="registration__error">
                            Ошибка: {passwordError || error.data}
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