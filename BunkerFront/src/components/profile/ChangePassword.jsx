import { useState } from 'react';
import Input from '../ui/Input'
import { useCheckPasswordMutation, useChangePasswordMutation } from '../../store/api';
import PasswordConfirmInputs from '../ui/PasswordConfirmInputs';

function ChangePassword() {

    const [currentPassword, setCurrentPassword] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [isPasswordConfirmed, setPasswordConfirmed] = useState(false);
    const [passwordError, setPasswordError] = useState('');

    const [checkPassword, { isLoading: isPasswordChecking }] = useCheckPasswordMutation();
    const [changePassword, { isLoading: isPasswordChanging, isSuccess: isPasswordChanged }] = useChangePasswordMutation();

    const handleVerifyCurrentPassword = async (e) => {
        e.preventDefault();
        
        if (!currentPassword) return;
        
        try {
            await checkPassword(currentPassword).unwrap();
            
            setPasswordError('');
            setPasswordConfirmed(true);
        } catch (err) {
            console.error(err);
            setPasswordError("Неверный текущий пароль");
            setCurrentPassword(''); 
        }
    };

    const handleChangePassword = async (e) => {
        e.preventDefault();
        
        if (newPassword !== confirmPassword) {
            setPasswordError("Новые пароли не совпадают");
            return;
        }
        if (newPassword.length < 6) {
            setPasswordError("Пароль должен быть не менее 6 символов");
            return;
        }
        if (!passwordError) {
            try {
                setPasswordError('');
                await changePassword({
                    currentPassword: currentPassword, 
                    newPassword: newPassword,
                    confirmPassword: confirmPassword
                }).unwrap();
    
                setPasswordConfirmed(false);
                setCurrentPassword('');
                setNewPassword('');
                setConfirmPassword('');
                
                
            } catch (err) {
                console.error(err);
                setPasswordError(err.data?.message || "Ошибка при смене пароля"); 
            }
        }
    };

    return (
        <>
            <p className="password__title">
                Пароль:
            </p>
            {!isPasswordConfirmed ? (
                <>
                    <form onSubmit={handleVerifyCurrentPassword} className="profile__item-form">
                        <Input value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} type="password" placeholder='Текущий пароль'  />
                        {/* <input value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} type="password" placeholder='Текущий пароль'/> */}
                        <button disabled={currentPassword.length < 6} className="btn btn--small">Далее</button>
                        {passwordError && <span className='password__error error'>{ passwordError }</span>} 
                        {isPasswordChecking && <span className="password__loading loading">Загрузка...</span>}
                        {isPasswordChanged && !passwordError && <span className="password__success success">Пароль успешно изменен</span>}
                    </form>    
                </>
            ) : (
                <>
                    <form onSubmit={handleChangePassword} className="profile__item-form">
                        <PasswordConfirmInputs
                            value={newPassword} 
                            confirmValue={confirmPassword} 
                            passwordPlaceholder={'Новый пароль'}
                            passwordConfirmPlaceholder={'Подтверждение пароля'} 
                            newPassword={newPassword}
                            setNewPassword={(value) => setNewPassword(value)}
                            confirmPassword={confirmPassword}
                            setConfirmPassword={(value) => setConfirmPassword(value)}
                            currentPassword={currentPassword}
                            setPasswordError={(error) => setPasswordError(error)}
                        />                           
                        {/* <Input value={newPassword} onChange={(e) => validateNewPassword(e.target.value, "new")} type="password" placeholder='Новый пароль'/>
                        <Input value={confirmPassword} onChange={(e) => validateNewPassword(e.target.value, "confirm")} type="password" placeholder='Подтверждение пароля'/> */}
                        <button disabled={!(newPassword && confirmPassword) || passwordError} className="btn btn--small">Изменить</button>
                        {isPasswordChanged && !passwordError && <span className="password__success success">Пароль успешно изменен</span>}
                        {isPasswordChanging && <span className="password__loading loading">Загрузка...</span>}
                        {passwordError && <span className='password__error error'>{ passwordError }</span>}       
                    </form>
                </>
            )}
                

            {(!isPasswordConfirmed) && <span className="password__info info">Что бы изменить пароль нужно подтвердить текущий</span>}
        </>
    );
}

export default ChangePassword;