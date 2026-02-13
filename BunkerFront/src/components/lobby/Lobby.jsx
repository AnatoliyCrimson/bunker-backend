import { Link, useNavigate } from "react-router";
import { useCreateRoomMutation } from "../../store/api";
import OverlayingPopup from "../uikit/OverlayingPopup";

function Lobby() {
    const navigate = useNavigate();
    const [createRoom, { isLoading }] = useCreateRoomMutation();
    const [isOpened, setOpened] = useState(false);  

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
    
                    onClick={setOpened(true)}
    
    
                >
                    Создать игру
                </button>

                <Link className="btn" to="/lobby/join">Войти в игру</Link>

                <OverlayingPopup contentClassName={"aabaw"} onClose={() => setOpened(false)} isOpened={isOpened}>
                    <div>
                        Укажите количество игроков
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
            </div>
        </>
    );
}

export default Lobby;