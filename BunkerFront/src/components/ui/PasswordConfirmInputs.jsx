import Input from "./Input"

function PasswordConfirmInputs({ 
    value, 
    confirmValue, 
    passwordPlaceholder, 
    passwordConfirmPlaceholder, 
    newPassword,
    setNewPassword, 
    confirmPassword,
    setConfirmPassword, 
    currentPassword,
    setPasswordError
}) {
    
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
                if (currentPassword && (currentPassword === value || currentPassword === confirmPassword)) {
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
                if (currentPassword && (currentPassword === newPassword || currentPassword === value)) {
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
    
    
    return (
        <>




            <Input
                value={value} 
                onChange={(e) => validateNewPassword(e.target.value, "new")} 
                type="password" 
                placeholder={passwordPlaceholder}
            />
            <Input 
                value={confirmValue} 
                onChange={(e) => validateNewPassword(e.target.value, "confirm")} 
                type="password" 
                placeholder={passwordConfirmPlaceholder}
            />
        </>
    );
}

export default PasswordConfirmInputs;