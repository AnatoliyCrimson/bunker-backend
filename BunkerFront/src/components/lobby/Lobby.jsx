import { Link, useNavigate } from "react-router";
import { useCreateRoomMutation } from "../../store/api";

function Lobby() {
    const navigate = useNavigate();
    const [createRoom, { isLoading }] = useCreateRoomMutation();

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
                    className="btn"
                    onClick={handleCreateRoom}
                    disabled={isLoading}
                    to="/room"
                >
                    {isLoading ? "Создание..." : "Создать игру"}
                </button>
                <Link className="btn" to="/lobby/join">Войти в игру</Link>
            </div>
        </>
    );
}

export default Lobby;