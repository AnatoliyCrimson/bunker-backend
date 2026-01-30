import { useState } from 'react';
import Input from '../ui/Input'
import { useCheckPasswordMutation, useChangePasswordMutation } from '../../store/api';

function ChangePassword() {

    const [currentPassword, setCurrentPassword] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [isPasswordConfirmed, setPasswordConfirmed] = useState(false);
    const [passwordError, setPasswordError] = useState('');

    const validateNewPassword = (value, type) => {
        setPasswordError("")
        if (value.length > 20) {
            setPasswordError("Длина пароля не может быть больше 20 символов")
        }

        if (value.length < 6) {
            setPasswordError("Длина пароля должна быть как минимум 6 символов")
        }

        

        switch (type) {
            case 'new':
                setNewPassword(value)
                if (confirmPassword && value && value !== confirmPassword) {
                    setPasswordError("Пароли не совпадают")
                    return
                }
                if (currentPassword === value || currentPassword === confirmPassword) {
                    setPasswordError("Новый пароль должен быть отличен от старого")
                    return
                }
                break

            case 'confirm':
                setConfirmPassword(value)
                if (value && newPassword && newPassword !== value) {
                    setPasswordError("Пароли не совпадают")
                    return
                }
                if (currentPassword === newPassword || currentPassword === value) {
                    setPasswordError("Новый пароль должен быть отличен от старого")
                    return
                }
                break
            default:
                break
        }
        
        if (!/[^a-zA-Z0-9]/.test(value)) {
            setPasswordError("Пароль должен содержать хотя бы один спецсимвол (!@#$...)")
        }

        if (!/\d/.test(value)) {
            setPasswordError("Пароль должен содержать хотя бы одну цифру (0-9)")
        }

        if (!/[A-Z]/.test(value)) {
            setPasswordError("Пароль должен содержать хотя бы одну заглавную букву (A-Z)")
        }

        if (!/[a-z]/.test(value)) {
            setPasswordError("Пароль должен содержать хотя бы одну строчную букву (a-z)")
        }

        
    }

    const [checkPassword, { isLoading: isPasswordChecking }] = useCheckPasswordMutation();
    const [changePassword, { isLoading: isPasswordChanging, isSuccess: isPasswordChanged }] = useChangePasswordMutation();

    const handleVerifyCurrentPassword = async (e) => {
        e.preventDefault();
        setPasswordError('');

        if (!currentPassword) return;

        try {
            await checkPassword(currentPassword).unwrap();
            
            setPasswordConfirmed(true);
        } catch (err) {
            console.error(err);
            setPasswordError("Неверный текущий пароль");
            setCurrentPassword(''); 
        }
    };

    const handleChangePassword = async (e) => {
        e.preventDefault();
        setPasswordError('');

        if (newPassword !== confirmPassword) {
            setPasswordError("Новые пароли не совпадают");
            return;
        }
        if (newPassword.length < 6) {
            setPasswordError("Пароль должен быть не менее 6 символов");
            return;
        }

        try {
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
                    </form>    
                </>
            ) : (
                <>
                    <form onSubmit={handleChangePassword} className="profile__item-form">                        
                        <Input value={newPassword} onChange={(e) => validateNewPassword(e.target.value, "new")} type="password" placeholder='Новый пароль'/>
                        <Input value={confirmPassword} onChange={(e) => validateNewPassword(e.target.value, "confirm")} type="password" placeholder='Подтверждение пароля'/>
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