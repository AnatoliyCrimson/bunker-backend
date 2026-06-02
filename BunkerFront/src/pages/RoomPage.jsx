import { Link, useNavigate } from "react-router";
import { useDeleteRoomMutation, useGetRoomQuery, useLeaveRoomMutation, useStartGameMutation } from "../store/api";
import { useSelector } from 'react-redux';
import "../styles/pages/Room.scss"
import ErrorPage from "./ErrorPage";
import Copy from "../components/ui/Copy";
import { useEffect, useState } from "react";
import OverlayingPopup from "../components/uikit/OverlayingPopup";
import { useSignalR } from "../context/SignalRContext";

function RoomPage() {
    const navigate = useNavigate();
    const user = useSelector((state) => state.auth.user);
    const { data: room, isLoading, error, refetch } = useGetRoomQuery(undefined, {
        refetchOnMountOrArgChange: true,
    });
    const { connection, isConnected, startConnection, joinRoom, leaveRoom } = useSignalR();

    const [leaveRoomMutation, { isLoading: isLeaving }] = useLeaveRoomMutation();
    const [deleteRoomMutation, { isLoading: isDeleting }] = useDeleteRoomMutation();
    const [startGameMutation, { isLoading: isStarting }] = useStartGameMutation();

    const [isOpenedDeleteModal, setOpenedDeleteModal] = useState(false)
    const [isOpenedLeaveModal, setOpenedLeaveModal] = useState(false)

    const isPlayerInRoom = room?.users?.some(player => player.id === user?.id);

    // 1. Инициализация подключения
    useEffect(() => {
        startConnection();
    }, [startConnection]);

    // 2. Вход в группу комнаты и прослушивание событий
    useEffect(() => {
        if (!room?.id || !isConnected || !connection) return;

        joinRoom(room.id);

        const onRoomUpdated = () => {
            console.log("SignalR: RoomUpdated received");
            refetch();
        };

        const onRoomDeleted = () => {
            console.log("SignalR: RoomDeleted received");
            navigate('/lobby');
        };

        const onGameStarted = (gameId) => {
            console.log("SignalR: GameStarted received", gameId);
            navigate(`/game`);
        };

        connection.on("RoomUpdated", onRoomUpdated);
        connection.on("RoomDeleted", onRoomDeleted);
        connection.on("GameStarted", onGameStarted);

        return () => {
            connection.off("RoomUpdated", onRoomUpdated);
            connection.off("RoomDeleted", onRoomDeleted);
            connection.off("GameStarted", onGameStarted);
            leaveRoom(room.id);
        };
    }, [room?.id, isConnected, connection, joinRoom, leaveRoom, refetch, navigate]);

    useEffect(() => {
        // Запасной вариант редиректа, если пропустили событие
        if (room && room.isGameStart) {
            navigate(`/game`);
        }
    }, [room, navigate]); 

    const handleLeaveRoom = async () => {
        try {
            await leaveRoomMutation(room.id).unwrap();
            navigate('/lobby');
        } catch (err) {
            console.error("Ошибка при выходе из комнаты:", err);
        }
    }

    const handleDeleteRoom = async () => {
        try {
            await deleteRoomMutation().unwrap();
            navigate('/lobby');
        } catch (err) {
            console.error("Ошибка при удалении комнаты:", err);
        }
    }

    const handleStartGame = async () => {
        try {
            await startGameMutation(room.id).unwrap();
            // Навигация произойдет по событию GameStarted или через useEffect выше
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
                                        {room.users.map((player) => (
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
                                                    disabled={isStarting || room.users.length < 4}
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