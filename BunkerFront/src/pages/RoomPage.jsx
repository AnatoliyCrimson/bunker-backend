import { Link, useNavigate, useParams } from "react-router";
import { useDeleteRoomMutation, useGetRoomQuery, useLeaveRoomMutation, useStartGameMutation } from "../store/api";
import { useSelector } from 'react-redux';
import "../styles/pages/Room.scss"
import ErrorPage from "./ErrorPage";
import Copy from "../components/ui/Copy";
import { useEffect, useState } from "react";
import OverlayingPopup from "../components/uikit/OverlayingPopup";

function RoomPage() {
    const { id } = useParams();
    const navigate = useNavigate();
    const user = useSelector((state) => state.auth.user);
    const { data: room, isLoading, error, refetch } = useGetRoomQuery(id, {
        refetchOnMountOrArgChange: true,
        pollingInterval: 2000, // shortPulling
    });
    const [leaveRoom, { isLoading: isLeaving }] = useLeaveRoomMutation();
    const [deleteRoom, { isLoading: isDeleting }] = useDeleteRoomMutation();
    const [startGame, { isLoading: isStarting }] = useStartGameMutation();

    const [isOpenedDeleteModal, setOpenedDeleteModal] = useState(false)
    const [isOpenedLeaveModal, setOpenedLeaveModal] = useState(false)

    const isPlayerInRoom = room?.players?.some(player => player.id === user?.id);

    useEffect(() => {
        // Проверяем: комната загрузилась? Игра началась? ID игры есть?
        if (room && room.isGameStart && room.gameId) {
            navigate(`/game/${room.gameId}`);
        }
    }, [room, navigate]); 



    const handleLeaveRoom = async () => {
        try {
            await leaveRoom(room.id).unwrap();
            navigate('/lobby');
        } catch (err) {
            console.error("Ошибка при выходе из комнаты:", err);
        }
    }

    const handleDeleteRoom = async () => {
        try {
            await deleteRoom().unwrap();
            navigate('/lobby');
        } catch (err) {
            console.error("Ошибка при удалении комнаты:", err);
        }
    }

    const handleStartGame = async () => {
        try {
            const response = await startGame(room.id).unwrap();

            if (response.gameId) {
                navigate(`/game/${response.gameId}`)
            }
        } catch (err) {
            console.error("Ошибка при создании игры:", err);
        }
    }

    if (error) {
        return (
            <ErrorPage 
                status={error.status || "Ошибка"}
                title={error.status === 404 ? "Комната не найдена" : "Ошибка загрузки"}
                message={error.status === 404 ? 
                    "Скорее всего все уже разошлись, либо ушли решать кто достоин места в бункере" :
                    "Ошибка связи с сервером"
                }
            />
        );
    }

    if (!room) return null;


    return (
        <>

            <div className="background room">
                {isLoading ? (
                    <>
                        <div className="room__loading">
                            Загрузка комнаты...
                        </div>
                    </>
                ) : (
                    <>
                        {isPlayerInRoom ? (
                            <>
                                <div className="room__container">
                                    <h2 className="room__title">
                                        Ожидание игроков...
                                    </h2>
                                    

                                    <ul className="player__list">
                                        {room.players.map((player) => (
                                            <li className="player__item" key={player.id}>
                                                {player.id === room.hostId && (
                                                    <>
                                                        <span className="player__host-info">
                                                            HOST
                                                        </span>
                                                    </>
                                                )}

                                                <div className="player__avatar-container">
                                                    {player.avatarUrl ? (
                                                        <>
                                                            <img className="player__avatar" src={"/" + player.avatarUrl} alt="" />
                                                        </>
                                                    ) : (
                                                        <>
                                                            <div className="player__empty-avatar">
                                                                {player.name[0].toUpperCase()}
                                                            </div>
                                                        </>
                                                    )}
                                                </div>
                                                <p className="player__name">
                                                    {player.id === user.id ? (
                                                        <>
                                                            {"Вы"}
                                                        </>
                                                    ) : (
                                                        <>
                                                            {player.name}
                                                        </>
                                                    )}
                                                </p>
                                            </li>
                                        ))}
                                    </ul>
                                    <div className="room__bottom-btns">
                                        {user.id === room.hostId ? (
                                            <>
                                                <button
                                                    className="btn room__btn-start"
                                                    onClick={handleStartGame}
                                                    disabled={isStarting || room.players.length < 4}
                                                >
                                                    {isLeaving ? "Создание..." : "Начать игру"}
                                                </button>
                                                <button
                                                    className="btn"
                                                    onClick={() => setOpenedDeleteModal(true)}
                                                >
                                                    Удалить комнату
                                                </button>
                                                
                                                <OverlayingPopup contentClassName={"room__modal-delete"} onClose={() => setOpenedDeleteModal(false)} isOpened={isOpenedDeleteModal}>
                                                    <p>
                                                        Вы уверены что хотите удалить комнату?
                                                    </p>
                                                    <button
                                                        className="btn"
                                                        onClick={handleDeleteRoom}
                                                        disabled={isDeleting}
                                                    >
                                                        {isLeaving ? "Удаление..." : "Удалить комнату"}
                                                    </button>
                                                </OverlayingPopup>
                                            </>
                                        ) : (
                                            <>
                                                <button 
                                                    className="btn"
                                                    onClick={() => setOpenedLeaveModal(true)}
                                                >   
                                                    Покинуть комнату
                                                </button>
                                                <OverlayingPopup contentClassName={"room__modal-leave"} onClose={() => setOpenedLeaveModal(false)} isOpened={isOpenedLeaveModal}>
                                                    
                                                    <p>
                                                        Вы уверены что хотите покинуть комнату?
                                                    </p>
                                                    <button
                                                        className="btn"
                                                        onClick={handleLeaveRoom}
                                                        disabled={isLeaving}
                                                    >
                                                        {isLeaving ? "Выход..." : "Покинуть"}
                                                    </button>
                                                </OverlayingPopup>
                                            </>
                                        )}
                                        
                                    </div>
                                </div>

                                <div className="invite-code__container">
                                    <span className="invite-code__info">
                                        Пригласительный код
                                    </span>
                                    <Copy text={room.inviteCode} textClass="invite-code__code"/>
                                </div>
                            </>
                        ) : (
                            <>
                                <div className="not-in-room__container">
                                    <h2 className="not-in-room__title">
                                        Хмм.. похоже вы не приглашены в эту комнату
                                    </h2>

                                    <Link className="btn" to="/lobby">Вернутся в лобби</Link>
                                </div>
                            </>
                        )}
                        
                    </>
                )}

            </div>
        </>
    );
}

export default RoomPage;