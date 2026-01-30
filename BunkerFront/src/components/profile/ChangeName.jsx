import { useState } from 'react';
import Input from '../ui/Input'
import { useChangeNameMutation } from '../../store/api';

function ChangeName({ userName }) {
    const [name, setName] = useState("");
    const [errorName, setErrorName] = useState("");

    const validateName = (e) => {
        setErrorName("")
        const newValue = e.target.value

        if (!newValue) {
            setErrorName("")
            setName("")
            return
        }

        if (newValue.length > 20) {
            setErrorName("Длина имени не должна превышать 20 символов")
        }

        if (newValue.length < 2) {
            setErrorName("Длина имени должна быть хотя бы 2 символа")
        }

        const regex = /^[a-zA-Zа-яА-ЯёЁ0-9_]*$/

        if (!regex.test(newValue)) {
            setErrorName("Имя может содержать только кирилицу, латиницу, цифры и нижнее подчеркивание")
            return
        }

        setName(newValue)
    }

    const [changeName, { isLoading: isNameChanging, isSuccess: isNameChanged }] = useChangeNameMutation();
    
    const handleChangeName = async (e) => {
        e.preventDefault()
        setErrorName("")


        try {
            await changeName(name).unwrap()

            setName("")
        } catch (err) {
            console.error(err);
            setErrorName(err.data?.message || "Ошибка при смене имени")
        }
    }

    return (
        <>
            <p className="name__title">
                {"Имя: " + userName}
            </p>
            <form onSubmit={handleChangeName} className="profile__item-form">                
                <Input value={name} onChange={validateName} type="text" placeholder="Имя" />
                <button disabled={!name || errorName} className="btn btn--small">Изменить</button>
                {errorName && <span className="name__error error">{errorName}</span>}
                {isNameChanging && <span className="name__loading loading">Загрузка...</span>}
                {isNameChanged && !errorName && <span className="name__success success">Имя успешно изменено</span>}
            </form>
    
        </>
    );
}

export default ChangeName;