import { useEffect, useState } from 'react';
import Input from '../ui/Input'
import { useChangeEmailMutation } from '../../store/api';


function ChangeEmail({ userEmail }) {
    const [email, setEmail] = useState("")
    const [emailError, setEmailError] = useState("")
    const [viewEmailError, setViewEmailError] = useState(false)


    const validateEmail = (e) => {
        setEmailError("")
        const newValue = e.target.value

        if (!newValue) {
            setEmailError("")
            setEmail("")
            return
        }

        const lengthRegex = /^[a-zA-Z0-9.-]{2,40}@[a-zA-Z0-9]{2,15}\.[a-z]{2,10}$/


        if (!lengthRegex.test(newValue)) {
            setEmailError("Слишком длинная почта, обратитесь в поддержку")
        }
        const regex = /^[a-zA-Z0-9.-]{2,}@[a-zA-Z0-9]{2,}\.[a-z]{2,}$/

        if (!regex.test(newValue)) {
            setEmailError("Почта не валидна, проверьте правильность написания в соответствии с примером")
        }
        setEmail(newValue)


    }

    const [changeEmail, { isLoading: isEmailChanging, isSuccess: isEmailChanged }] = useChangeEmailMutation();

    const handleChangeEmail = async (e) => {
        e.preventDefault()
        setEmailError("")

        try {
            await changeEmail(email).unwrap()

            setEmail("")
        } catch (error) {
            console.error(error)
            setEmailError(error.data?.message || "Ошибка при смене имени")
        }
    }

    useEffect(() => {
        if (!emailError || !email) {
            setViewEmailError(false);
            return
        }

        const timer = setTimeout(() => {
            setViewEmailError(true)
        }, 1900)

        return () => clearTimeout(timer);
    }, [emailError, email])

    return (
        <>
            <p className="email__title">
                {"Почта: " + userEmail}
            </p>
            <form onSubmit={handleChangeEmail} className="profile__item-form">
                <Input value={email} onChange={validateEmail} type="text" placeholder="example@mail.com"/>
                <button disabled={!email || emailError} className="btn btn--small">Изменить</button>
                {viewEmailError && <span className="email__error error">{emailError}</span>}
                {isEmailChanging && <span className="email__loading loading">Загрузка...</span>}
                {isEmailChanged && !viewEmailError && <span className="email__success success">Почта успешно изменена</span>}
            </form>
        </>
    );
}

export default ChangeEmail;