import { Link, useNavigate } from "react-router";
import { useCreateRoomMutation } from "../../store/api";
import OverlayingPopup from "../uikit/OverlayingPopup";
import { useState } from "react";
import JoinRoom from "./JoinRoom";

function Lobby() {
    const navigate = useNavigate();
    const [createRoom, { isLoading }] = useCreateRoomMutation();
    const [isOpenedCreateRoom, setOpenedCreateRoom] = useState(false);  
    const [isOpenedJoinRoom, setOpenedJoinRoom] = useState(false);  

    const handleCreateRoom = async () => {
        try {
            const response = await createRoom().unwrap();

            if (response.roomId) {
                navigate(`/room/${response.roomId}`)
            }
        } catch (error) {
            console.error("Не удалось создать комнату", error);
        }
    }

    return ( 
        <>  
            <div className="lobby__btns-container">
                
                <button
                    onClick={() => setOpenedCreateRoom(true)}
                    className="btn"
                >
                    Создать игру
                </button>

                <button
                    onClick={() => setOpenedJoinRoom(true)}
                    className="btn"
                >
                    Войти в игру
                </button>
                {/* <Link className="btn" to="/lobby/join">Войти в игру</Link> */}

                <OverlayingPopup contentClassName={"aabaw"} onClose={() => setOpenedCreateRoom(false)} isOpened={isOpenedCreateRoom}>
                    <div>
                        Укажите количество доп раундов
                        <button
                            className="btn"
                            onClick={handleCreateRoom}
                            disabled={isLoading}
                            to="/room"
                        >
                            {isLoading ? "Создание..." : "Создать игру"}
                        </button>
                    </div>
                </OverlayingPopup>

                <OverlayingPopup contentClassName={"aabaw"} onClose={() => setOpenedJoinRoom(false)} isOpened={isOpenedJoinRoom}>
                    <JoinRoom />
                </OverlayingPopup>
            </div>
        </>
    );
}

export default Lobby;