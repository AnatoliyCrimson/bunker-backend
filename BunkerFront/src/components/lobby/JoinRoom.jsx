import { useState } from "react";
import { useNavigate } from "react-router";
import { useJoinRoomMutation } from "../../store/api";

function JoinRoom() {
    const navigate = useNavigate();
    const [inviteCode, setInviteCode] = useState("");

    const [joinRoom, {isLoading, error}] = useJoinRoomMutation();

    const handleJoin = async () => {
        if (!inviteCode.trim()) return
        try {
            const response = await joinRoom(inviteCode).unwrap();

            navigate("/room/");
        } catch (err) {
            console.error("Ошибка подключения", err);
        }
    }

    const getErrorMessage = () => {
        if (!error) return null

        const serverMessage = error.data?.message || error.data;

        if (error.status === 404) return "Комната не найдена"

        if (error.status === 400) return "Код неверный"

        return typeof serverMessage === 'string' ? serverMessage : "Произошла ошибка сервера";
    }


    return ( 
        <>
            <div className="join">
                <p className="join__info">Введите пригласительный код</p>

                <input
                    type="text" 
                    value={inviteCode}
                    onChange={(e) => setInviteCode(e.target.value.toUpperCase())}
                    placeholder="CX3VG4"
                    className="join__input"
                    maxlength="6"
                    disabled={isLoading}
                />
                <p className="join__code-info">Пригласительный код состоит только из заглавных латинских букв и цифр от 1 до 9</p>
                {error && (
                    <p style={{ color: 'red', marginTop: '10px' }}>
                        {getErrorMessage()}
                    </p>
                )}
                <button
                    className="btn"
                    onClick={handleJoin}
                    disabled={isLoading || !inviteCode}
                >
                    {isLoading ? "Подключение..." : "Присоединиться"}
                </button>
            </div>
        </>
    );
}

export default JoinRoom;